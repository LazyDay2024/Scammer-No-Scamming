using System;
using UnityEngine;

namespace ScammerNoScamming
{
    public class MainMenuScreen : GameScreen
    {
        Action onStart;

        public void Show(Action start)
        {
            onStart = start;
            Open();
        }

        protected override void Draw(Rect canvas)
        {
            ThaiGui.Background(canvas, "bg_main_menu", 0.25f);
            GUI.DrawTexture(new Rect(0f, 0f, 900f, 1080f), ThaiGui.Solid(new Color(0.04f, 0.07f, 0.08f, 0.6f)));
            ThaiGui.Kicker(new Rect(110f, 150f, 0f, 0f), "2035 · SIAM PHIROM");
            float titleH = ThaiGui.Label(new Rect(110f, 220f, 800f, 300f), "Scammer\nNo Scamming", ThaiGui.Text(110, ThaiGui.Bone, FontStyle.Bold));
            ThaiGui.Label(new Rect(110f, 220f + titleH + 10f, 800f, 80f), "ม้าเหล็กหมดน้ำมัน", ThaiGui.Text(56, ThaiGui.Gold, FontStyle.Bold));

            if (GUI.Button(new Rect(110f, 620f, 520f, 100f), "เริ่มเกม", ThaiGui.Button(44)))
            {
                Close();
                onStart?.Invoke();
            }
            SensitivitySlider(new Rect(110f, 760f, 520f, 90f));
            if (GUI.Button(new Rect(110f, 880f, 520f, 80f), "ออกจากเกม", ThaiGui.Button(32)))
                Application.Quit();
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
            ThaiGui.Label(new Rect(r.x, r.y, r.width, 40f), $"ความไวเมาส์ {player.lookSensitivity:0.00}", ThaiGui.Text(28, ThaiGui.Bone));
            float value = GUI.HorizontalSlider(new Rect(r.x, r.y + 52f, r.width, 30f), player.lookSensitivity, 0.02f, 0.4f);
            if (!Mathf.Approximately(value, player.lookSensitivity))
            {
                player.lookSensitivity = value;
                try { PlayerPrefs.SetFloat(SensitivityKey, value); } catch { }
            }
        }
    }
}
