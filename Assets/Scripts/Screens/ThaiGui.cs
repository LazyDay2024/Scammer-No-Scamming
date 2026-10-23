using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Shared look for our IMGUI screens. Thai text uses the OS font (Leelawadee UI / Tahoma ship with
    // Windows), so no font file is redistributed; the Windows build is the target.
    // Screens are laid out on a 1920x1080 canvas and scaled to the window.
    public static class ThaiGui
    {
        public const float RefWidth = 1920f;
        public const float RefHeight = 1080f;

        public static readonly Color Gold = new Color32(0xE5, 0xB9, 0x6E, 0xFF);
        public static readonly Color Orange = new Color32(0xD9, 0x78, 0x43, 0xFF);
        public static readonly Color Rust = new Color32(0xA8, 0x49, 0x32, 0xFF);
        public static readonly Color Teal = new Color32(0x3E, 0x99, 0x91, 0xFF);
        public static readonly Color Bone = new Color32(0xF3, 0xE9, 0xD2, 0xFF);
        public static readonly Color Night = new Color32(0x0B, 0x11, 0x14, 0xFF);

        static Font font;
        static readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<Color, Texture2D> solids = new Dictionary<Color, Texture2D>();

        public static Font Font
        {
            get
            {
                if (font == null)
                    font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Leelawadee", "Tahoma", "Arial" }, 32);
                return font;
            }
        }

        // Call at the start of OnGUI: scales the 1920x1080 layout to the window, letterboxed.
        public static Rect BeginCanvas()
        {
            float scale = Mathf.Min(Screen.width / RefWidth, Screen.height / RefHeight);
            float offsetX = (Screen.width - RefWidth * scale) * 0.5f;
            float offsetY = (Screen.height - RefHeight * scale) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
            return new Rect(0f, 0f, RefWidth, RefHeight);
        }

        public static void EndCanvas() => GUI.matrix = Matrix4x4.identity;

        // UI art from Resources/UI (made by Codex, see docs/AI_ART_LOG*.md). Missing art returns null.
        public static Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (!textures.TryGetValue(name, out var tex))
            {
                tex = Resources.Load<Texture2D>("UI/" + name);
                textures[name] = tex;
            }
            return tex;
        }

        public static Texture2D Solid(Color color)
        {
            if (!solids.TryGetValue(color, out var tex) || tex == null)
            {
                tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                tex.SetPixel(0, 0, color);
                tex.Apply();
                solids[color] = tex;
            }
            return tex;
        }

        public static GUIStyle Text(int size, Color color, FontStyle fontStyle = FontStyle.Normal,
            TextAnchor anchor = TextAnchor.UpperLeft)
        {
            string key = $"t{size}_{color}_{fontStyle}_{anchor}";
            if (!styles.TryGetValue(key, out var style))
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    font = Font,
                    fontSize = size,
                    fontStyle = fontStyle,
                    alignment = anchor,
                    wordWrap = true,
                    richText = true,
                };
                style.normal.textColor = color;
                styles[key] = style;
            }
            return style;
        }

        public static GUIStyle Button(int size = 34)
        {
            string key = "b" + size;
            if (!styles.TryGetValue(key, out var style))
            {
                style = new GUIStyle(GUI.skin.button)
                {
                    font = Font,
                    fontSize = size,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    padding = new RectOffset(24, 24, 12, 12),
                };
                var plate = Art("ui_button");
                style.normal.background = plate != null ? plate : Solid(new Color(0.21f, 0.23f, 0.24f, 0.95f));
                style.hover.background = Solid(new Color(0.85f, 0.47f, 0.26f, 0.95f));
                style.active.background = Solid(Rust);
                style.border = plate != null ? new RectOffset(40, 40, 30, 30) : new RectOffset(0, 0, 0, 0);
                style.normal.textColor = Bone;
                style.hover.textColor = Color.white;
                style.active.textColor = Color.white;
                styles[key] = style;
            }
            return style;
        }

        public static void Background(Rect canvas, string art, float shade = 0.55f)
        {
            var tex = Art(art);
            if (tex != null) GUI.DrawTexture(canvas, tex, ScaleMode.ScaleAndCrop);
            else GUI.DrawTexture(canvas, Solid(Night));
            if (shade > 0f) GUI.DrawTexture(canvas, Solid(new Color(0.04f, 0.07f, 0.08f, shade)));
        }

        public static void Panel(Rect rect, float alpha = 0.88f)
        {
            GUI.DrawTexture(rect, Solid(new Color(0.07f, 0.1f, 0.11f, alpha)));
            GUI.DrawTexture(new Rect(rect.x, rect.y, 8f, rect.height), Solid(Orange));
        }

        public static void Kicker(Rect at, string text)
        {
            var style = Text(26, Night, FontStyle.Bold, TextAnchor.MiddleCenter);
            var size = style.CalcSize(new GUIContent(text));
            var r = new Rect(at.x, at.y, size.x + 36f, 46f);
            GUI.DrawTexture(r, Solid(Gold));
            GUI.Label(r, text, style);
        }

        public static float Label(Rect at, string text, GUIStyle style)
        {
            float h = style.CalcHeight(new GUIContent(text), at.width);
            GUI.Label(new Rect(at.x, at.y, at.width, h), text, style);
            return h;
        }
    }
}
