using UnityEngine;

namespace ScammerNoScamming
{
    // Settings shared by the title menu and the pause screen: mouse sensitivity, master volume, controls.
    public static class SettingsPanel
    {
        public const string SensitivityKey = "lookSensitivity";
        public const string VolumeKey = "masterVolume";

        public static void Load()
        {
            try
            {
                if (PlayerPrefs.HasKey(VolumeKey)) AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey));
                var player = PlayerLocator.Player;
                if (player != null && PlayerPrefs.HasKey(SensitivityKey)) player.lookSensitivity = PlayerPrefs.GetFloat(SensitivityKey);
            }
            catch { }
        }

        // Draws the panel centred on the canvas. Returns true when the player pressed back (button or Esc).
        public static bool Draw(Rect canvas)
        {
            ThaiGui.Dim(0.55f);
            var r = new Rect(canvas.center.x - 380f, 150f, 760f, 780f);
            ThaiGui.Panel(r, 0.92f);
            const float pad = 64f;
            float w = r.width - pad * 2f;
            float x = r.x + pad;
            GUI.Label(new Rect(x - 4f, r.y + 48f, w, 90f), "ตั้งค่า", ThaiGui.Headline(ThaiGui.H1 - 6, ThaiGui.Bone, FontStyle.Bold));
            ThaiGui.HRule(x, r.y + 150f, w, new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.18f));

            float y = r.y + 182f;
            var player = PlayerLocator.Player;
            if (player != null)
            {
                float v = Row(x, y, w, "ความไวเมาส์", player.lookSensitivity, 0.02f, 0.4f, player.lookSensitivity.ToString("0.00"));
                if (!Mathf.Approximately(v, player.lookSensitivity))
                {
                    player.lookSensitivity = v;
                    try { PlayerPrefs.SetFloat(SensitivityKey, v); } catch { }
                }
                y += 92f;
            }
            float vol = Row(x, y, w, "ระดับเสียง", AudioListener.volume, 0f, 1f, Mathf.RoundToInt(AudioListener.volume * 100f) + "%");
            if (!Mathf.Approximately(vol, AudioListener.volume))
            {
                AudioListener.volume = vol;
                try { PlayerPrefs.SetFloat(VolumeKey, vol); } catch { }
            }
            y += 100f;

            ThaiGui.HRule(x, y, w, new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.18f));
            y += 26f;
            ThaiGui.Kicker(new Rect(x, y, 0f, 0f), "ปุ่มควบคุม", ThaiGui.Gold);
            y += 58f;
            var keys = new[] { ("WASD", "เดิน"), ("Shift", "วิ่ง"), ("Space", "กระโดด"), ("เมาส์", "มอง"),
                ("คลิกซ้าย", "ยิง"), ("E", "ใช้ / ขึ้นม้า"), ("Esc", "พักเกม") };
            var keyStyle = ThaiGui.Text(ThaiGui.Cap + 3, ThaiGui.Bone, FontStyle.Bold);
            var actStyle = ThaiGui.Text(ThaiGui.Cap + 3, ThaiGui.Muted);
            for (int i = 0; i < keys.Length; i++)
            {
                float cx = x + (i % 2) * (w * 0.5f);
                float cy = y + (i / 2) * 46f;
                GUI.Label(new Rect(cx, cy, 130f, 34f), keys[i].Item1, keyStyle);
                GUI.Label(new Rect(cx + 140f, cy, w * 0.5f - 150f, 34f), keys[i].Item2, actStyle);
            }

            bool back = ThaiGui.Btn(new Rect(x, r.yMax - 118f, w, 66f), "กลับ", 28);
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { e.Use(); back = true; }
            return back;
        }

        static float Row(float x, float y, float w, string label, float value, float min, float max, string shown)
        {
            GUI.Label(new Rect(x, y, w * 0.6f, 34f), label, ThaiGui.Text(ThaiGui.Small - 2, ThaiGui.Bone));
            GUI.Label(new Rect(x + w - 140f, y, 140f, 34f), shown, ThaiGui.Text(ThaiGui.Small - 2, ThaiGui.Gold, FontStyle.Bold, TextAnchor.UpperRight));
            return ThaiGui.Slider(new Rect(x, y + 48f, w, 18f), value, min, max);
        }
    }
}
