#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Ataka
{
    // Opt-in verification of the built player. Never runs during ordinary play.
    public sealed class BuildSmokeCheck : MonoBehaviour
    {
        string output;
        int errors;
        float deadline;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            string[] args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "-m0-smoke");
            if (flag < 0 || flag + 1 >= args.Length) return;
            var check = new GameObject("Development build smoke check").AddComponent<BuildSmokeCheck>();
            check.output = Path.GetFullPath(args[flag + 1]);
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(output);
            Application.logMessageReceived += CountErrors;
            deadline = Time.realtimeSinceStartup + 60;
            Application.runInBackground = true;
            yield return null;
            var session = FindFirstObjectByType<GameSession>();
            if (session == null) { Debug.LogError("Smoke: no GameSession"); Finish(); yield break; }
            yield return Capture("01-menu");
            session.NewGame();
            foreach (var door in FindObjectsByType<Door>(FindObjectsSortMode.None)) door.Restore(1);
            session.player.Restore(new Vector3(5.8f, 11.72f, 3.7f), 215, 3, false);
            session.player.inputEnabled = false;
            yield return Capture("02-living");
            session.player.Restore(new Vector3(7.3f, 11.72f, 2.5f), 150, 4, false);
            yield return Capture("03-kitchen");
            session.player.Restore(new Vector3(4.1f, 11.72f, .6f), 180, 12, false);
            yield return Capture("04-courtyard");
            var pickups = FindObjectsByType<PickUp>(FindObjectsSortMode.None);
            foreach (var pickup in pickups) pickup.Interact(session);
            if (session.inventory.Count != 2) Debug.LogError("Smoke: expected two collected items");
            session.NewGame();
            session.player.inputEnabled = false;
            if (session.inventory.Count != 0 || pickups.Any(x => !x.gameObject.activeSelf))
                Debug.LogError("Smoke: new game did not reset pickups");
            var switches = FindObjectsByType<LampSwitch>(FindObjectsSortMode.None);
            foreach (var lamp in switches) lamp.Interact(session);
            yield return Capture("05-lights-off");
            foreach (var lamp in switches) lamp.Interact(session);
            float elapsed = 0;
            int frames = 0;
            float slowest = 0;
            while (frames < 120)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
                slowest = Mathf.Max(slowest, Time.unscaledDeltaTime);
                frames++;
            }
            File.WriteAllText(Path.Combine(output, "result.json"), JsonUtility.ToJson(new Result
            {
                errors = errors, frames = frames, meanFps = frames / Mathf.Max(.001f, elapsed),
                slowestFrameMs = slowest * 1000, gpu = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(), unity = Application.unityVersion
            }, true));
            Finish();
        }

        IEnumerator Capture(string name)
        {
            for (int i = 0; i < 8; i++) yield return null;
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            if (texture == null)
            {
                Debug.LogError("Smoke: no rendered framebuffer. Run the player in a visible window.");
                Finish();
                yield break;
            }
            File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
            Destroy(texture);
        }
        void CountErrors(string text, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
        }
        void Update()
        {
            if (deadline > 0 && Time.realtimeSinceStartup > deadline)
            {
                Debug.LogError("Smoke: timed out before verification completed.");
                Finish();
            }
        }
        void Finish()
        {
            Application.logMessageReceived -= CountErrors;
            Debug.Log("M0_PLAYER_SMOKE_COMPLETE errors=" + errors);
            Application.Quit(errors == 0 ? 0 : 1);
        }
        [Serializable] sealed class Result
        {
            public int errors, frames;
            public float meanFps, slowestFrameMs;
            public string gpu, graphicsApi, unity;
        }
    }
}
#endif
