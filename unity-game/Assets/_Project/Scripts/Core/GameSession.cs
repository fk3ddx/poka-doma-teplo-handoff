using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ataka
{
    [Serializable]
    public sealed class GameSettings
    {
        public float sensitivity = .085f;
        public float fieldOfView = 70;
        public bool invertY;
        public bool vhs = true;
        public int fpsLimit = 60;
    }

    public sealed class GameSession : MonoBehaviour
    {
        public FirstPersonController player;
        public Material vhsMaterial;
        public string checkpointFile = "checkpoint-m0.json";
        public Vector3 checkpointMin = new Vector3(-.1f,11.5f,-1.5f);
        public Vector3 checkpointMax = new Vector3(9,14.3f,8.5f);
        public HashSet<string> inventory = new HashSet<string>();
        public GameSettings settings = new GameSettings();
        public bool Paused { get; private set; } = true;
        public bool Started { get; private set; }
        public string Notice { get; private set; }
        public Interactable Target { get; private set; }
        public bool CanLoad => store != null && store.Exists;
        readonly InteractionQuery query = new InteractionQuery();
        Interactable[] objects;
        PlayerControls controls;
        CheckpointStore store;
        float noticeUntil;
        Vector3 initialPosition;
        float initialYaw;
        string SettingsPath => Path.Combine(Application.persistentDataPath, "settings-m0.json");

        void Start()
        {
            objects = FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            controls = player.GetComponent<PlayerControls>();
            initialPosition = player.transform.position;
            initialYaw = player.transform.eulerAngles.y;
            store = new CheckpointStore(Path.Combine(Application.persistentDataPath, checkpointFile));
            try { if (File.Exists(SettingsPath)) settings = JsonUtility.FromJson<GameSettings>(File.ReadAllText(SettingsPath)) ?? new GameSettings(); }
            catch (Exception e) when (e is IOException || e is ArgumentException) { Notify("Настройки сброшены: файл повреждён."); }
            ApplySettings();
            SetPaused(true);
        }

        void Update()
        {
            if (controls == null) return;
            if (Started && controls.Pause.WasPressedThisFrame()) SetPaused(!Paused);
            if (Paused) { Target = null; return; }
            if (Time.unscaledTime > noticeUntil) Notice = null;
            Target = query.Resolve(new Ray(player.view.transform.position, player.view.transform.forward));
            if (Target != null && controls.Interact.WasPressedThisFrame()) Target.Interact(this);
        }

        public void NewGame()
        {
            // Every interactive object owns its authored baseline, including later additions.
            inventory.Clear();
            foreach (var item in objects) item.Restore(item.InitialState);
            player.Restore(initialPosition, initialYaw, 0, false);
            Started = true;
            SetPaused(false);
            Notify("WASD — ходить · E — взаимодействовать · Esc — меню");
        }

        public void SetPaused(bool paused)
        {
            Paused = paused;
            player.inputEnabled = !paused;
            Time.timeScale = paused ? 0 : 1;
            AudioListener.pause = paused;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
            player.SuppressLook();
        }

        public Checkpoint Capture()
        {
            var state = new Checkpoint
            {
                scene = SceneManager.GetActiveScene().name,
                position = player.transform.position,
                yaw = player.transform.eulerAngles.y,
                pitch = player.pitch,
                crouched = player.crouched,
                inventory = new List<string>(inventory)
            };
            foreach (var item in objects) state.objects.Add(new ObjectState(item.persistentId, item.Capture(), item.CaptureAuxiliary()));
            return state;
        }

        public void Save()
        {
            try { store.Save(Capture()); Notify("Сохранено"); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { Notify("Не удалось сохранить игру"); Debug.LogException(e); }
        }

        public void Load()
        {
            if (!store.TryLoad(out var state)) { Notify("Нет корректного сохранения"); return; }
            if (state.scene != SceneManager.GetActiveScene().name) { Notify("Сохранение относится к другой версии квартиры"); return; }
            if (state.position.x < checkpointMin.x || state.position.x > checkpointMax.x || state.position.z < checkpointMin.z || state.position.z > checkpointMax.z ||
                state.position.y < checkpointMin.y || state.position.y > checkpointMax.y)
            { Notify("Сохранение содержит недопустимую позицию"); return; }
            inventory = new HashSet<string>(state.inventory);
            foreach (var item in objects)
            {
                // Old checkpoints do not contain objects added in a later art pass.
                item.Restore(item.InitialState);
                foreach (var record in state.objects)
                    if (record.id == item.persistentId) { item.RestoreSnapshot(record); break; }
            }
            Physics.SyncTransforms();
            player.Restore(state.position, state.yaw, state.pitch, state.crouched);
            Started = true;
            SetPaused(false);
            Notify("Сохранение загружено");
        }

        public void ApplySettings()
        {
            settings.sensitivity = Mathf.Clamp(settings.sensitivity, .025f, .25f);
            settings.fieldOfView = Mathf.Clamp(settings.fieldOfView, 60, 90);
            player.sensitivity = settings.sensitivity;
            player.invertY = settings.invertY;
            player.view.fieldOfView = settings.fieldOfView;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = settings.fpsLimit;
            if (vhsMaterial != null) vhsMaterial.SetFloat("_Strength", settings.vhs ? 1 : 0);
        }

        public void SaveSettings()
        {
            ApplySettings();
            try { Directory.CreateDirectory(Application.persistentDataPath); File.WriteAllText(SettingsPath, JsonUtility.ToJson(settings, true)); }
            catch (IOException e) { Notify("Не удалось сохранить настройки"); Debug.LogException(e); }
        }
        public void Notify(string text) { Notice = text; noticeUntil = Time.unscaledTime + 4; }
        void OnApplicationFocus(bool focused) { if (!focused && player != null) SetPaused(true); }
        void OnDestroy()
        {
            Time.timeScale = 1; AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }
}
