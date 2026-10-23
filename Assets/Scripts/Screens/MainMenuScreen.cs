using System;
using UnityEngine;

namespace ScammerNoScamming
{
    public class MainMenuScreen : GameScreen
    {
        Action onStart;
        int selected;
        Vector2 lastMouse;

        protected override bool FillsScreen => false;
        protected override bool WantsLiveWorld => true;
        protected override float FadeIn => 1.0f;

        public void Show(Action start)
        {
            onStart = start;
            selected = 0;
            Open();
        }

        void StartGame()
        {
            Close();
            onStart?.Invoke();
        }

        protected override void Draw(Rect canvas)
        {
            // The scene behind is the live city; darken the left for the text and settle the corners.
            ThaiGui.ScrimLeft(1250f, 0.94f);
            ThaiGui.ScrimBottom(360f, 0.55f);
            ThaiGui.VignetteOverlay(0.55f);
            ThaiGui.Grain(0.035f);

            const float x = 150f;
            float age = Age;
            using (ThaiGui.Fade(ThaiGui.Appear(age, 0.25f)))
            {
                ThaiGui.Kicker(new Rect(x, 130f, 0f, 0f), "2035 · SIAM PHIROM");
            }
            using (ThaiGui.Fade(ThaiGui.Appear(age, 0.45f, 0.7f)))
            {
                float y = 215f - 16f * (1f - ThaiGui.Appear(age, 0.45f, 0.7f));
                GUI.Label(new Rect(x - 6f, y, 1000f, 150f), "SCAMMER", ThaiGui.Headline(ThaiGui.Display, ThaiGui.Bone, FontStyle.Bold));
                GUI.Label(new Rect(x - 6f, y + 128f, 1000f, 150f), "NO SCAMMING", ThaiGui.Headline(ThaiGui.Display - 8, ThaiGui.Bone, FontStyle.Bold));
            }
            using (ThaiGui.Fade(ThaiGui.Appear(age, 0.8f)))
            {
                ThaiGui.Fill(new Rect(x, 508f, 70f, 2f), ThaiGui.Gold);
                GUI.Label(new Rect(x, 530f, 900f, 80f), "ม้าเหล็กหมดน้ำมัน", ThaiGui.Text(ThaiGui.H2, ThaiGui.Gold, FontStyle.Bold));
            }

            using (ThaiGui.Fade(ThaiGui.Appear(age, 1.05f)))
            {
                var items = new[] { "เริ่มเกม", "ออกจากเกม" };
                HandleKeys(items.Length);
                for (int i = 0; i < items.Length; i++)
                {
                    var r = new Rect(x - 6f, 650f + i * 74f, 460f, 64f);
                    if (Event.current.type == EventType.Repaint && Event.current.mousePosition != lastMouse && r.Contains(Event.current.mousePosition)) selected = i;
                    if (ThaiGui.MenuItem(r, items[i], selected == i, 34)) Activate(i);
                }
                lastMouse = Event.current.mousePosition;
                SensitivitySlider(new Rect(x + 22f, 828f, 400f, 70f));
            }

            using (ThaiGui.Fade(ThaiGui.Appear(age, 1.3f)))
            {
                float fy = canvas.height - 78f;
                ThaiGui.HRule(x, fy - 18f, 760f, new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.18f));
                Controls(x, fy);
            }
        }

        // Small key hints on one line: keys in bone, actions muted.
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
            else Application.Quit();
        }

        public const string SensitivityKey = "lookSensitivity";

        public static void LoadSensitivity()
        {
            var player = PlayerLocator.Player;
            if (player == null) return;
            try
            {
                if (PlayerPrefs.HasKey(SensitivityKey)) player.lookSensitivity = PlayerPrefs.GetFloat(SensitivityKey);
            }
            catch { }
        }

        public static void SensitivitySlider(Rect r)
        {
            var player = PlayerLocator.Player;
            if (player == null) return;
            ThaiGui.Tracked(r.x, r.y, "ความไวเมาส์", ThaiGui.Cap + 1, ThaiGui.Muted, 0f, FontStyle.Normal);
            var value = ThaiGui.Text(ThaiGui.Cap + 1, ThaiGui.Bone, FontStyle.Bold, TextAnchor.UpperRight);
            GUI.Label(new Rect(r.xMax - 100f, r.y, 100f, 30f), player.lookSensitivity.ToString("0.00"), value);
            float v = ThaiGui.Slider(new Rect(r.x, r.y + 36f, r.width, 18f), player.lookSensitivity, 0.02f, 0.4f);
            if (!Mathf.Approximately(v, player.lookSensitivity))
            {
                player.lookSensitivity = v;
                try { PlayerPrefs.SetFloat(SensitivityKey, v); } catch { }
            }
        }
    }
}
