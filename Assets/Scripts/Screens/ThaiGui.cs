using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Design system for our IMGUI screens: "western noir at golden hour".
    // Restrained and typographic: warm near-black surfaces, thin bone rules, one brass accent.
    // Thai text uses the OS font (Leelawadee UI / Tahoma ship with Windows), so no font file is redistributed.
    // Screens are laid out on a 1920x1080 canvas and scaled to the window. All textures are made in code.
    public static class ThaiGui
    {
        public const float RefWidth = 1920f;
        public const float RefHeight = 1080f;

        // ---- Palette -------------------------------------------------------------------------------------
        public static readonly Color Night = new Color32(0x0A, 0x09, 0x08, 0xFF);   // ink
        public static readonly Color Surface = new Color32(0x14, 0x11, 0x0E, 0xFF); // panel base
        public static readonly Color Bone = new Color32(0xEC, 0xE2, 0xCC, 0xFF);    // primary text
        public static readonly Color Muted = new Color32(0xA7, 0x9D, 0x88, 0xFF);   // secondary text
        public static readonly Color Gold = new Color32(0xD9, 0xA2, 0x48, 0xFF);    // the one accent (brass)
        public static readonly Color Orange = new Color32(0xCF, 0x6B, 0x3D, 0xFF);  // ember: warnings, trouble
        public static readonly Color Rust = new Color32(0x9C, 0x3F, 0x2D, 0xFF);    // pressed / danger fill
        public static readonly Color Teal = new Color32(0x9B, 0xAE, 0x9C, 0xFF);    // sage: secondary headings only
        public static Color Accent => Gold;
        public static Color Danger => Orange;

        // ---- Type scale ----------------------------------------------------------------------------------
        public const int Display = 120, H1 = 68, H2 = 44, Lead = 36, Body = 30, Small = 25, Cap = 21;

        // Opened screens set these for the fade/slide transition; they are unscaled-time driven.
        public static float CanvasOffsetY;

        static Font font, serif;
        static readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<Color, Texture2D> solids = new Dictionary<Color, Texture2D>();
        static readonly Dictionary<int, float> hover = new Dictionary<int, float>();
        static Texture2D white, gradH, gradV, vignette, noise, panelFill, panelRing, circle, sliderLine;

        public static Font Font
        {
            get
            {
                if (font == null)
                    font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Leelawadee", "Tahoma", "Arial" }, 32);
                return font;
            }
        }

        // Headline face: a serif for Latin titles, falling back to the Thai UI font for Thai glyphs.
        public static Font Serif
        {
            get
            {
                if (serif == null)
                    serif = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Palatino Linotype", "Times New Roman", "Leelawadee UI", "Tahoma" }, 32);
                return serif;
            }
        }

        // In a linear-colour project IMGUI text is blended without gamma correction and comes out too dark;
        // pre-encode text colours so they land on the palette values.
        public static Color Ink(Color c) => QualitySettings.activeColorSpace == ColorSpace.Linear
            ? new Color(Mathf.LinearToGammaSpace(c.r), Mathf.LinearToGammaSpace(c.g), Mathf.LinearToGammaSpace(c.b), c.a) : c;

        // ---- Canvas --------------------------------------------------------------------------------------
        // Call at the start of OnGUI: scales the 1920x1080 layout to the window, letterboxed.
        public static Rect BeginCanvas()
        {
            float scale = Mathf.Min(Screen.width / RefWidth, Screen.height / RefHeight);
            float offsetX = (Screen.width - RefWidth * scale) * 0.5f;
            float offsetY = (Screen.height - RefHeight * scale) * 0.5f + CanvasOffsetY * scale;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
            return new Rect(0f, 0f, RefWidth, RefHeight);
        }

        public static void EndCanvas() => GUI.matrix = Matrix4x4.identity;

        // Full-window rect in canvas coordinates (use for backdrops that must cover the letterbox bars).
        public static Rect Window()
        {
            float scale = Mathf.Min(Screen.width / RefWidth, Screen.height / RefHeight);
            float offsetX = (Screen.width - RefWidth * scale) * 0.5f;
            float offsetY = (Screen.height - RefHeight * scale) * 0.5f + CanvasOffsetY * scale;
            return new Rect(-offsetX / scale, -offsetY / scale, Screen.width / scale, Screen.height / scale);
        }

        // ---- Animation helpers (unscaled time, safe while the game is paused) ----------------------------
        public static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }

        // 0..1 progress of an element that starts `delay` seconds after the screen opened.
        public static float Appear(float age, float delay, float duration = 0.45f) => Ease((age - delay) / duration);

        public readonly struct FadeScope : IDisposable
        {
            readonly Color previous;
            public FadeScope(float alpha)
            {
                previous = GUI.color;
                GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * Mathf.Clamp01(alpha));
            }
            public void Dispose() => GUI.color = previous;
        }

        public static FadeScope Fade(float alpha) => new FadeScope(alpha);

        // ---- Generated textures --------------------------------------------------------------------------
        static Texture2D Make(int w, int h, Func<int, int, Color> pixel, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = wrap,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = pixel(x, y);
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        static Texture2D White => white != null ? white : white = Make(1, 1, (x, y) => Color.white);

        // Alpha 1 at the left edge fading to 0 at the right edge.
        static Texture2D GradH => gradH != null ? gradH : gradH = Make(256, 1, (x, y) =>
            new Color(1f, 1f, 1f, 1f - Smooth((x / 255f - 0.38f) / 0.62f)));

        // Alpha 0 at the top fading to 1 at the bottom (texture row 0 is the bottom edge).
        static Texture2D GradV => gradV != null ? gradV : gradV = Make(1, 256, (x, y) =>
            new Color(1f, 1f, 1f, 1f - Smooth(y / 255f)));

        static Texture2D Vignette => vignette != null ? vignette : vignette = Make(128, 128, (x, y) =>
        {
            float dx = (x / 127f - 0.5f) * 2f, dy = (y / 127f - 0.5f) * 2f;
            float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
            return new Color(1f, 1f, 1f, Smooth((d - 0.45f) / 0.95f) * 0.85f);
        });

        static Texture2D Noise => noise != null ? noise : noise = Make(128, 128, (x, y) =>
        {
            float v = UnityEngine.Random.value;
            return new Color(v, v, v, 1f);
        }, TextureWrapMode.Repeat);

        // Soft rounded surface and its 1px outline, used with a 9-slice style border.
        const int PanelSize = 24, PanelRadius = 5;
        static float RoundedCoverage(int x, int y, int size, float radius)
        {
            float cx = Mathf.Clamp(x + 0.5f, radius, size - radius), cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
            float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
            return Mathf.Clamp01(radius - d + 0.5f);
        }

        static Texture2D PanelFill => panelFill != null ? panelFill : panelFill = Make(PanelSize, PanelSize, (x, y) =>
            new Color(1f, 1f, 1f, RoundedCoverage(x, y, PanelSize, PanelRadius)));

        static Texture2D PanelRing => panelRing != null ? panelRing : panelRing = Make(PanelSize, PanelSize, (x, y) =>
        {
            float outer = RoundedCoverage(x, y, PanelSize, PanelRadius);
            float inner = RoundedCoverage(x - 1, y - 1, PanelSize - 2, PanelRadius - 1f);
            if (x < 1 || y < 1 || x >= PanelSize - 1 || y >= PanelSize - 1) inner = 0f;
            return new Color(1f, 1f, 1f, Mathf.Clamp01(outer - inner));
        });

        static Texture2D Circle => circle != null ? circle : circle = Make(32, 32, (x, y) =>
            new Color(1f, 1f, 1f, RoundedCoverage(x, y, 32, 16f)));

        static Texture2D SliderLine => sliderLine != null ? sliderLine : sliderLine = Make(8, 18, (x, y) =>
            new Color(1f, 1f, 1f, y == 8 || y == 9 ? 1f : 0f));

        static GUIStyle nineFill, nineRing;

        // Texture art from Resources/UI. Only portraits remain; missing art returns null.
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

        // ---- Drawing primitives (all respect the current GUI.color alpha for transitions) ----------------
        static Color Tint(Color c) { var g = GUI.color; return new Color(c.r * g.r, c.g * g.g, c.b * g.b, c.a * g.a); }

        public static void Fill(Rect r, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            var previous = GUI.color;
            GUI.color = Tint(c);
            GUI.DrawTexture(r, White);
            GUI.color = previous;
        }

        // 1px rules (canvas units; thick lines are drawn at least one device pixel).
        public static void HRule(float x, float y, float width, Color c, float thickness = 1.5f) => Fill(new Rect(x, y, width, thickness), c);
        public static void VRule(float x, float y, float height, Color c, float thickness = 1.5f) => Fill(new Rect(x, y, thickness, height), c);

        static void Nine(Rect r, Texture2D tex, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (nineFill == null)
            {
                nineFill = new GUIStyle { border = new RectOffset(PanelRadius + 3, PanelRadius + 3, PanelRadius + 3, PanelRadius + 3) };
                nineRing = new GUIStyle { border = nineFill.border };
            }
            var style = tex == PanelRing ? nineRing : nineFill;
            style.normal.background = tex;
            var previous = GUI.color;
            GUI.color = Tint(c);
            style.Draw(r, GUIContent.none, false, false, false, false);
            GUI.color = previous;
        }

        public static void Surfaced(Rect r, Color fill, Color? outline = null)
        {
            Nine(r, PanelFill, fill);
            if (outline.HasValue) Nine(r, PanelRing, outline.Value);
        }

        // Soft vertical/horizontal scrims, drawn in window space so they ignore the letterbox.
        public static void ScrimLeft(float width, float alpha, Color? color = null)
        {
            if (Event.current.type != EventType.Repaint) return;
            var previous = GUI.color;
            var win = Window();
            var c = color ?? Night;
            GUI.color = Tint(new Color(c.r, c.g, c.b, alpha));
            GUI.DrawTexture(new Rect(win.x, win.y, width + (0f - win.x), win.height), GradH);
            GUI.color = previous;
        }

        public static void ScrimBottom(float height, float alpha, Color? color = null)
        {
            if (Event.current.type != EventType.Repaint) return;
            var previous = GUI.color;
            var win = Window();
            var c = color ?? Night;
            GUI.color = Tint(new Color(c.r, c.g, c.b, alpha));
            GUI.DrawTexture(new Rect(win.x, win.yMax - height, win.width, height), GradV);
            GUI.color = previous;
        }

        public static void Dim(float alpha)
        {
            Fill(Window(), new Color(Night.r, Night.g, Night.b, alpha));
        }

        public static void VignetteOverlay(float alpha)
        {
            if (Event.current.type != EventType.Repaint) return;
            var previous = GUI.color;
            GUI.color = Tint(new Color(0f, 0f, 0f, alpha));
            GUI.DrawTexture(Window(), Vignette);
            GUI.color = previous;
        }

        // Subtle film grain: a tiled noise texture whose offset jumps a few times a second.
        public static void Grain(float alpha = 0.05f)
        {
            if (Event.current.type != EventType.Repaint) return;
            var win = Window();
            int frame = Mathf.FloorToInt(Time.unscaledTime * 12f);
            var rng = new System.Random(frame);
            var uv = new Rect((float)rng.NextDouble(), (float)rng.NextDouble(), win.width / 128f, win.height / 128f);
            var previous = GUI.color;
            GUI.color = Tint(new Color(1f, 1f, 1f, alpha));
            GUI.DrawTextureWithTexCoords(win, Noise, uv);
            GUI.color = previous;
        }

        // Opaque warm backdrop for screens that do not sit on top of the live world.
        public static void Backdrop(float depth = 1f)
        {
            if (Event.current.type != EventType.Repaint) return;
            var win = Window();
            var previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(win.x, win.y, win.width, win.height), Solid(Night));
            GUI.color = previous;
            // Warm light pooled in the upper right, like late sun through a window.
            Fill(new Rect(win.x, win.y, win.width, win.height), new Color(0.16f, 0.1f, 0.05f, 0.35f * depth));
            ScrimLeft(win.width * 0.9f, 0.65f, Night);
            ScrimBottom(win.height * 0.5f, 0.7f, Night);
            VignetteOverlay(0.6f);
            Grain(0.05f);
        }

        // Kept for the older screens: the art argument is ignored now (no painted backgrounds).
        public static void Background(Rect canvas, string art, float shade = 0.55f) => Backdrop();

        public static void Panel(Rect rect, float alpha = 0.88f)
        {
            Surfaced(rect, new Color(Surface.r, Surface.g, Surface.b, alpha), new Color(Bone.r, Bone.g, Bone.b, 0.16f));
            // Short brass tick at the top-left corner: the one decorative detail.
            Fill(new Rect(rect.x + 1f, rect.y, 34f, 2f), Accent);
        }

        // ---- Text ----------------------------------------------------------------------------------------
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
                style.normal.textColor = Ink(color);
                styles[key] = style;
            }
            return style;
        }

        // Display face for titles.
        public static GUIStyle Headline(int size, Color color, FontStyle fontStyle = FontStyle.Normal,
            TextAnchor anchor = TextAnchor.UpperLeft)
        {
            string key = $"h{size}_{color}_{fontStyle}_{anchor}";
            if (!styles.TryGetValue(key, out var style))
            {
                style = new GUIStyle(Text(size, color, fontStyle, anchor)) { font = Serif };
                styles[key] = style;
            }
            return style;
        }

        public static float Label(Rect at, string text, GUIStyle style)
        {
            float h = style.CalcHeight(new GUIContent(text), at.width);
            GUI.Label(new Rect(at.x, at.y, at.width, h), text, style);
            return h;
        }

        // Lays out each line break as its own paragraph with a gap between; returns the total height.
        public static float Paragraphs(Rect at, string text, GUIStyle style, float gap = 14f)
        {
            float y = at.y;
            foreach (var part in text.Split((char)10))
            {
                if (part.Length == 0) continue;
                y += Label(new Rect(at.x, y, at.width, 0f), part, style) + gap;
            }
            return Mathf.Max(0f, y - at.y - gap);
        }

        static bool IsLatin(string text)
        {
            foreach (char ch in text) if (ch >= 0x0E00 && ch <= 0x0E7F) return false;
            return true;
        }

        // Small-caps style label with letter spacing (Latin text only; Thai is drawn untracked).
        public static float Tracked(float x, float y, string text, int size, Color color, float tracking = 3.2f,
            FontStyle fontStyle = FontStyle.Bold)
        {
            var style = Text(size, color, fontStyle);
            style.wordWrap = false;
            if (!IsLatin(text))
            {
                var s = style.CalcSize(new GUIContent(text));
                GUI.Label(new Rect(x, y, s.x + 4f, s.y), text, style);
                return s.x;
            }
            text = text.ToUpperInvariant();
            float cx = x;
            foreach (char ch in text)
            {
                var content = new GUIContent(ch.ToString());
                float w = ch == ' ' ? size * 0.32f : style.CalcSize(content).x;
                if (ch != ' ') GUI.Label(new Rect(cx, y, w + 4f, size * 1.6f), content, style);
                cx += w + tracking;
            }
            return cx - x;
        }

        // Section label: short brass rule + tracked small caps.
        public static void Kicker(Rect at, string text) => Kicker(at, text, Accent);

        public static void Kicker(Rect at, string text, Color color)
        {
            Fill(new Rect(at.x, at.y + 15f, 30f, 2f), color);
            Tracked(at.x + 46f, at.y, text, Cap + 1, color);
        }

        // ---- Controls ------------------------------------------------------------------------------------
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
                    border = new RectOffset(PanelRadius + 3, PanelRadius + 3, PanelRadius + 3, PanelRadius + 3),
                };
                style.normal.background = ButtonTexture(new Color(1f, 1f, 1f, 0.03f), new Color(Bone.r, Bone.g, Bone.b, 0.3f));
                style.hover.background = style.normal.background;
                style.active.background = ButtonTexture(new Color(Gold.r, Gold.g, Gold.b, 0.3f), Gold);
                style.focused.background = style.normal.background;
                style.normal.textColor = Ink(Bone);
                style.hover.textColor = Ink(Bone);
                style.active.textColor = Color.white;
                style.focused.textColor = Ink(Bone);
                style.onNormal = style.normal;
                style.onHover = style.hover;
                style.onActive = style.active;
                style.onFocused = style.focused;
                styles[key] = style;
            }
            return style;
        }

        static Texture2D ButtonTexture(Color fill, Color border)
        {
            string key = "btn" + fill + border;
            if (textures.TryGetValue(key, out var cached) && cached != null) return cached;
            var tex = Make(PanelSize, PanelSize, (x, y) =>
            {
                float outer = RoundedCoverage(x, y, PanelSize, PanelRadius);
                float inner = RoundedCoverage(x - 1, y - 1, PanelSize - 2, PanelRadius - 1f);
                if (x < 1 || y < 1 || x >= PanelSize - 1 || y >= PanelSize - 1) inner = 0f;
                var c = Color.Lerp(new Color(fill.r, fill.g, fill.b, fill.a * inner), border, outer > 0f ? Mathf.Clamp01(outer - inner) : 0f);
                c.a = Mathf.Max(fill.a * inner, border.a * Mathf.Clamp01(outer - inner));
                return c;
            });
            textures[key] = tex;
            return tex;
        }

        static float Hover(int id, bool over)
        {
            hover.TryGetValue(id, out float v);
            if (Event.current.type == EventType.Repaint)
            {
                v = Mathf.MoveTowards(v, over ? 1f : 0f, Time.unscaledDeltaTime * 7f);
                hover[id] = v;
            }
            return v;
        }

        // Label with a colour that changes every frame (hover) without creating a style per colour.
        static void InkLabel(Rect r, string text, int size, Color color, FontStyle fontStyle, TextAnchor anchor)
        {
            var previous = GUI.contentColor;
            GUI.contentColor = Ink(color);
            GUI.Label(r, text, Text(size, Color.white, fontStyle, anchor));
            GUI.contentColor = previous;
        }

        // Primary (filled brass) or secondary (outlined) button. Returns true when clicked.
        public static bool Btn(Rect r, string text, int size = 32, bool primary = false)
        {
            int id = r.GetHashCode() ^ text.GetHashCode();
            // Decided: hovering never changes a control's colour (the pointer is only a click target).
            float h = Hover(id, false);
            if (primary)
            {
                Surfaced(r, Color.Lerp(Accent, new Color(0.96f, 0.76f, 0.4f), h));
                InkLabel(r, text, size, Night, FontStyle.Bold, TextAnchor.MiddleCenter);
            }
            else
            {
                Surfaced(r, new Color(Gold.r, Gold.g, Gold.b, 0.14f * h + 0.03f),
                    Color.Lerp(new Color(Bone.r, Bone.g, Bone.b, 0.32f), Gold, h));
                InkLabel(r, text, size, Color.Lerp(Bone, Gold, h), FontStyle.Bold, TextAnchor.MiddleCenter);
            }
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        // Left-aligned menu entry with a brass bar and slide on hover/selection.
        public static bool MenuItem(Rect r, string text, bool selected, int size = 40)
        {
            int id = r.GetHashCode() ^ text.GetHashCode();
            float h = Hover(id, selected); // selection lights an entry; hovering does not
            Fill(r, new Color(Gold.r, Gold.g, Gold.b, 0.10f * h));
            Fill(new Rect(r.x, r.y + 8f, 3f, r.height - 16f), new Color(Gold.r, Gold.g, Gold.b, h));
            var label = new Rect(r.x + 28f + 12f * h, r.y, r.width - 40f, r.height);
            InkLabel(label, text, size, Color.Lerp(Bone, Gold, h), FontStyle.Normal, TextAnchor.MiddleLeft);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        public static float Slider(Rect r, float value, float min, float max)
        {
            string key = "slider";
            if (!styles.TryGetValue(key, out var track))
            {
                track = new GUIStyle { fixedHeight = 18f, border = new RectOffset(3, 3, 0, 0) };
                track.normal.background = SliderLine;
                styles[key] = track;
                var thumb = new GUIStyle { fixedWidth = 16f, fixedHeight = 16f, border = new RectOffset(0, 0, 0, 0) };
                thumb.normal.background = Circle;
                thumb.hover.background = Circle;
                thumb.active.background = Circle;
                styles["sliderThumb"] = thumb;
            }
            var previous = GUI.color;
            GUI.color = Tint(new Color(Bone.r, Bone.g, Bone.b, 0.5f));
            var thumbStyle = styles["sliderThumb"];
            float result = GUI.HorizontalSlider(new Rect(r.x, r.y, r.width, 18f), value, min, max, track, thumbStyle);
            GUI.color = previous;
            float t = Mathf.InverseLerp(min, max, result);
            // Filled part of the track, in brass.
            Fill(new Rect(r.x, r.y + 8f, Mathf.Max(0f, (r.width - 16f) * t + 8f), 2f), Gold);
            if (Event.current.type == EventType.Repaint)
            {
                float tx = r.x + (r.width - 16f) * t;
                var g = GUI.color;
                GUI.color = Tint(Gold);
                GUI.DrawTexture(new Rect(tx, r.y + 1f, 16f, 16f), Circle);
                GUI.color = g;
            }
            return result;
        }
    }
}
