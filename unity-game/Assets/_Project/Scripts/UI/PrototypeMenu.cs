using UnityEngine;

namespace Ataka
{
    public sealed class PrototypeMenu : MonoBehaviour
    {
        public GameSession session;
        public string milestoneDescription = "M0 · проверка управления и планировки";
        bool options;
        GUIStyle text, title, button;
        void OnGUI()
        {
            if (session == null) return;
            if (text == null)
            {
                text = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
                title = new GUIStyle(text) { fontSize = 28 };
                button = new GUIStyle(GUI.skin.button) { fontSize = 18, fixedHeight = 42 };
            }
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
            float width = Screen.width / scale, height = Screen.height / scale;
            if (session.Paused)
            {
                GUI.Box(new Rect(0, 0, width, height), GUIContent.none);
                GUILayout.BeginArea(new Rect(width / 2 - 255, height / 2 - 280, 510, 560), GUI.skin.box);
                GUILayout.Label("АТАКА НА ЗАПОМИНАНИЕ", title);
                GUILayout.Label(milestoneDescription, text);
                GUILayout.Space(15);
                if (options)
                {
                    GUILayout.Label("Чувствительность мыши", text);
                    session.settings.sensitivity = GUILayout.HorizontalSlider(session.settings.sensitivity, .025f, .25f);
                    GUILayout.Label("Поле зрения: " + Mathf.RoundToInt(session.settings.fieldOfView), text);
                    session.settings.fieldOfView = GUILayout.HorizontalSlider(session.settings.fieldOfView, 60, 90);
                    session.settings.invertY = GUILayout.Toggle(session.settings.invertY, "Инверсия Y");
                    session.settings.vhs = GUILayout.Toggle(session.settings.vhs, "Лёгкий VHS");
                    session.ApplySettings();
                    if (GUILayout.Button("Назад", button)) { session.SaveSettings(); options = false; }
                }
                else
                {
                    if (session.Started && GUILayout.Button("Продолжить", button)) session.SetPaused(false);
                    if (GUILayout.Button("Новая игра", button)) session.NewGame();
                    GUI.enabled = session.CanLoad;
                    if (GUILayout.Button("Загрузить сохранение", button)) session.Load();
                    GUI.enabled = true;
                    if (session.Started && GUILayout.Button("Сохранить", button)) session.Save();
                    if (GUILayout.Button("Настройки", button)) options = true;
                    if (GUILayout.Button("Выйти", button)) Application.Quit();
                }
                if (!string.IsNullOrEmpty(session.Notice)) GUILayout.Label(session.Notice, text);
                GUILayout.EndArea();
            }
            else
            {
                GUI.Label(new Rect(width / 2 - 3, height / 2 - 12, 20, 24), "·", text);
                if (session.Target != null) GUI.Label(new Rect(width / 2 - 220, height * .69f, 440, 60), "[E] " + session.Target.Prompt, text);
                if (!string.IsNullOrEmpty(session.Notice)) GUI.Label(new Rect(25, height - 85, width - 50, 65), session.Notice, text);
            }
        }
    }
}
