using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    public static class SettingsPanel
    {
        public const string SensitivityKey = "lookSensitivity";
        public const string VolumeKey = "masterVolume";
        const string FullscreenKey = "displayFullscreen";
        const string ResolutionWidthKey = "displayWidth";
        const string ResolutionHeightKey = "displayHeight";

        static Resolution[] resolutions;
        static int resolutionIndex = -1;

        static Resolution[] AvailableResolutions
        {
            get
            {
                if (resolutions == null || resolutions.Length == 0)
                {
                    var available = Screen.resolutions;
                    var distinct = new List<Resolution>(available.Length);
                    foreach (var candidate in available)
                    {
                        bool found = false;
                        foreach (var existing in distinct)
                            if (existing.width == candidate.width && existing.height == candidate.height)
                            {
                                found = true;
                                break;
                            }
                        if (!found) distinct.Add(candidate);
                    }

                    if (distinct.Count == 0) distinct.Add(Screen.currentResolution);
                    resolutions = distinct.ToArray();
                }
                return resolutions;
            }
        }

        public static void Load()
        {
            try
            {
                if (PlayerPrefs.HasKey(VolumeKey))
                    AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey));

                if (PlayerPrefs.HasKey(FullscreenKey))
                    Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;

                if (PlayerPrefs.HasKey(ResolutionWidthKey) && PlayerPrefs.HasKey(ResolutionHeightKey))
                {
                    int width = PlayerPrefs.GetInt(ResolutionWidthKey);
                    int height = PlayerPrefs.GetInt(ResolutionHeightKey);
                    if (width > 0 && height > 0)
                        Screen.SetResolution(width, height, Screen.fullScreen);
                }

                resolutionIndex = FindResolutionIndex(Screen.width, Screen.height);
                var player = PlayerLocator.Player;
                if (player != null && PlayerPrefs.HasKey(SensitivityKey))
                    player.lookSensitivity = PlayerPrefs.GetFloat(SensitivityKey);
            }
            catch { }
        }

        public static bool Draw(Rect canvas)
        {
            ThaiGui.Dim(0.58f);
            ThaiGui.VignetteOverlay(0.45f);
            var r = new Rect(canvas.center.x - 460f, 65f, 920f, 950f);
            ThaiGui.Panel(r, 0.95f);
            const float pad = 60f;
            float w = r.width - pad * 2f;
            float x = r.x + pad;

            GUI.Label(new Rect(x - 4f, r.y + 28f, w, 84f), "ตั้งค่า",
                ThaiGui.Headline(ThaiGui.H1 - 6, ThaiGui.Bone, FontStyle.Bold));
            ThaiGui.HRule(x, r.y + 119f, w, new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.18f));

            float y = r.y + 146f;
            var player = PlayerLocator.Player;
            if (player != null)
            {
                float sensitivity = Row(x, y, w, "ความไวเมาส์", player.lookSensitivity, 0.02f, 0.4f,
                    player.lookSensitivity.ToString("0.00"));
                if (!Mathf.Approximately(sensitivity, player.lookSensitivity))
                {
                    player.lookSensitivity = sensitivity;
                    try { PlayerPrefs.SetFloat(SensitivityKey, sensitivity); } catch { }
                }
                y += 82f;
            }

            float volume = Row(x, y, w, "เสียงหลัก", AudioListener.volume, 0f, 1f,
                Mathf.RoundToInt(AudioListener.volume * 100f) + "%");
            if (!Mathf.Approximately(volume, AudioListener.volume))
            {
                AudioListener.volume = volume;
                try { PlayerPrefs.SetFloat(VolumeKey, volume); } catch { }
            }
            y += 82f;

            DrawResolutionRow(x, y, w);
            y += 68f;
            DrawFullscreenRow(x, y, w);
            y += 72f;

            ThaiGui.HRule(x, y, w, new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.18f));
            y += 24f;
            ThaiGui.Kicker(new Rect(x, y, 0f, 0f), "ปุ่มควบคุม", ThaiGui.Gold);
            y += 42f;

            var keys = new[] { ("WASD", "เดิน / บังคับม้า"), ("SHIFT", "วิ่ง / ควบเร็ว"),
                ("SPACE", "กระโดด"), ("เมาส์", "มองและเล็ง"), ("คลิกซ้าย", "ยิง"),
                ("E", "ใช้ / ขึ้นม้า"), ("ESC", "พักเกม") };
            var keyStyle = ThaiGui.Text(ThaiGui.Cap + 1, ThaiGui.Bone, FontStyle.Bold);
            var actionStyle = ThaiGui.Text(ThaiGui.Cap, ThaiGui.Muted);
            for (int i = 0; i < keys.Length; i++)
            {
                float columnX = x + (i % 2) * (w * 0.5f);
                float rowY = y + (i / 2) * 40f;
                GUI.Label(new Rect(columnX, rowY, 140f, 34f), keys[i].Item1, keyStyle);
                GUI.Label(new Rect(columnX + 145f, rowY, w * 0.5f - 160f, 34f), keys[i].Item2, actionStyle);
            }

            bool back = ThaiGui.Btn(new Rect(x, r.yMax - 84f, w, 58f), "กลับ", 26);
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                e.Use();
                back = true;
            }
            return back;
        }

        static void DrawResolutionRow(float x, float y, float w)
        {
            var options = AvailableResolutions;
            if (resolutionIndex < 0 || resolutionIndex >= options.Length)
                resolutionIndex = FindResolutionIndex(Screen.width, Screen.height);

            GUI.Label(new Rect(x, y, w * 0.5f, 48f), "ความละเอียด",
                ThaiGui.Text(ThaiGui.Small, ThaiGui.Bone, FontStyle.Normal, TextAnchor.MiddleLeft));
            string value = $"{options[resolutionIndex].width} × {options[resolutionIndex].height}";
            float buttonX = x + w - 232f;
            if (ThaiGui.Btn(new Rect(buttonX, y + 2f, 46f, 44f), "<", 22))
                ChangeResolution(-1);
            GUI.Label(new Rect(buttonX + 52f, y + 2f, 128f, 44f), value,
                ThaiGui.Text(ThaiGui.Cap + 1, ThaiGui.Gold, FontStyle.Bold, TextAnchor.MiddleCenter));
            if (ThaiGui.Btn(new Rect(buttonX + 186f, y + 2f, 46f, 44f), ">", 22))
                ChangeResolution(1);
        }

        static void DrawFullscreenRow(float x, float y, float w)
        {
            GUI.Label(new Rect(x, y, w * 0.5f, 48f), "โหมดหน้าจอ",
                ThaiGui.Text(ThaiGui.Small, ThaiGui.Bone, FontStyle.Normal, TextAnchor.MiddleLeft));
            string value = Screen.fullScreen ? "เต็มจอ" : "หน้าต่าง";
            if (ThaiGui.Btn(new Rect(x + w - 232f, y + 2f, 232f, 44f), value, 22))
            {
                Screen.fullScreen = !Screen.fullScreen;
                try
                {
                    PlayerPrefs.SetInt(FullscreenKey, Screen.fullScreen ? 1 : 0);
                    PlayerPrefs.SetInt(ResolutionWidthKey, Screen.width);
                    PlayerPrefs.SetInt(ResolutionHeightKey, Screen.height);
                    PlayerPrefs.Save();
                }
                catch { }
            }
        }

        static float Row(float x, float y, float w, string label, float value, float min, float max, string shown)
        {
            GUI.Label(new Rect(x, y, w * 0.6f, 30f), label,
                ThaiGui.Text(ThaiGui.Small - 2, ThaiGui.Bone));
            GUI.Label(new Rect(x + w - 140f, y, 140f, 30f), shown,
                ThaiGui.Text(ThaiGui.Small - 2, ThaiGui.Gold, FontStyle.Bold, TextAnchor.UpperRight));
            return ThaiGui.Slider(new Rect(x, y + 36f, w, 18f), value, min, max);
        }

        static void ChangeResolution(int direction)
        {
            var options = AvailableResolutions;
            resolutionIndex = (resolutionIndex + direction + options.Length) % options.Length;
            var selected = options[resolutionIndex];
            Screen.SetResolution(selected.width, selected.height, Screen.fullScreen);
            try
            {
                PlayerPrefs.SetInt(ResolutionWidthKey, selected.width);
                PlayerPrefs.SetInt(ResolutionHeightKey, selected.height);
                PlayerPrefs.Save();
            }
            catch { }
        }

        static int FindResolutionIndex(int width, int height)
        {
            var options = AvailableResolutions;
            for (int i = 0; i < options.Length; i++)
                if (options[i].width == width && options[i].height == height)
                    return i;
            return Mathf.Clamp(options.Length - 1, 0, options.Length - 1);
        }
    }
}