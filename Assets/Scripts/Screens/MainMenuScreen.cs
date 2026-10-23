using System;
using UnityEngine;

namespace ScammerNoScamming
{
    // Title screen: the title block and three entries, nothing else. Options live in SettingsPanel.
    public class MainMenuScreen : GameScreen
    {
        Action onStart;
        int selected;
        bool showSettings;
        Vector2 lastMouse;

        protected override bool FillsScreen => false;
        protected override bool WantsLiveWorld => true;
        protected override float FadeIn => 1.0f;

        public void Show(Action start)
        {
            onStart = start;
            selected = 0;
            showSettings = false;
            Open();
        }

        public void DebugSettings(bool on) => showSettings = on;

        void StartGame()
        {
            Close();
            onStart?.Invoke();
        }

        protected override void Draw(Rect canvas)
        {
            // The scene behind is the live city; darken the left for the text and settle the corners.
            ThaiGui.ScrimLeft(1250f, 0.94f);
            ThaiGui.ScrimBottom(360f, 0.5f);
            ThaiGui.VignetteOverlay(0.55f);
            ThaiGui.Grain(0.035f);

            if (showSettings)
            {
                if (SettingsPanel.Draw(canvas)) showSettings = false;
                return;
            }

            const float x = 150f;
            float age = Age;
            using (ThaiGui.Fade(ThaiGui.Appear(age, 0.25f)))
                ThaiGui.Kicker(new Rect(x, 150f, 0f, 0f), "2035 · สยามภิรมย์");
            float rise = 1f - ThaiGui.Appear(age, 0.4f, 0.7f);
            using (ThaiGui.Fade(ThaiGui.Appear(age, 0.4f, 0.7f)))
            {
                float y = 232f - 16f * rise;
                var title = ThaiGui.Headline(104, ThaiGui.Bone, FontStyle.Bold);
                GUI.Label(new Rect(x - 5f, y, 1000f, 140f), "SCAMMER", title);
                GUI.Label(new Rect(x - 5f, y + 112f, 1000f, 140f), "NO SCAMMING", title);
            }
            using (ThaiGui.Fade(ThaiGui.Appear(age, 0.75f)))
            {
                ThaiGui.Fill(new Rect(x, 482f, 64f, 2f), ThaiGui.Gold);
                GUI.Label(new Rect(x, 508f, 900f, 70f), "ม้าเหล็กหมดน้ำมัน", ThaiGui.Text(ThaiGui.H2 - 6, ThaiGui.Gold, FontStyle.Bold));
            }

            using (ThaiGui.Fade(ThaiGui.Appear(age, 1.0f)))
            {
                var items = new[] { "เริ่มเกม", "ตั้งค่า", "ออกจากเกม" };
                HandleKeys(items.Length);
                for (int i = 0; i < items.Length; i++)
                {
                    // Text lines up with the title; the brass bar hangs in the left margin.
                    var r = new Rect(x - 28f, 640f + i * 68f, 420f, 60f);
                    if (Event.current.type == EventType.Repaint && Event.current.mousePosition != lastMouse && r.Contains(Event.current.mousePosition)) selected = i;
                    if (ThaiGui.MenuItem(r, items[i], selected == i, 32)) Activate(i);
                }
                lastMouse = Event.current.mousePosition;
            }
        }

        void HandleKeys(int count)
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;
            if (e.keyCode == KeyCode.DownArrow || e.keyCode == KeyCode.S) { selected = (selected + 1) % count; e.Use(); }
            else if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.W) { selected = (selected + count - 1) % count; e.Use(); }
            else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Space)
            {
                e.Use();
                Activate(selected);
            }
        }

        void Activate(int index)
        {
            if (index == 0) StartGame();
            else if (index == 1) showSettings = true;
            else Application.Quit();
        }

        // Short key reminder used by the in-game HUD during the first seconds of play.
        public static void Controls(float x, float y)
        {
            var keys = ThaiGui.Text(ThaiGui.Cap, ThaiGui.Bone, FontStyle.Bold);
            var label = ThaiGui.Text(ThaiGui.Cap + 1, ThaiGui.Muted);
            keys.wordWrap = false;
            label.wordWrap = false;
            var hints = new[] { ("WASD", "เดิน"), ("เมาส์", "มอง"), ("E", "ใช้ / ขึ้นม้า"), ("คลิกซ้าย", "ยิง"), ("Esc", "พักเกม") };
            foreach (var (key, action) in hints)
            {
                var ks = keys.CalcSize(new GUIContent(key));
                GUI.Label(new Rect(x, y, ks.x + 4f, 34f), key, keys);
                x += ks.x + 12f;
                var ls = label.CalcSize(new GUIContent(action));
                GUI.Label(new Rect(x, y, ls.x + 4f, 34f), action, label);
                x += ls.x + 34f;
            }
        }

        public const string SensitivityKey = SettingsPanel.SensitivityKey;
        public static void LoadSensitivity() => SettingsPanel.Load();
    }
}
