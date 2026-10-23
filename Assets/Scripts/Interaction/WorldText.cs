using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ScammerNoScamming
{
    // World-space TextMesh helpers. Unity's font material ignores depth, so labels drew through walls and over
    // each other; WorldText swaps in a depth-tested material and fits Thai text inside a physical sign.
    public static class WorldText
    {
        // TextMesh draws one font pixel as 0.1 * characterSize metres.
        const float PixelsToWorld = .1f;
        static Material template;
        static readonly Dictionary<Font, Material> cache = new Dictionary<Font, Material>();
        static bool hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); template = null; }

        public static Material For(Font font)
        {
            if (font == null) return null;
            if (cache.TryGetValue(font, out var material) && material != null)
            {
                material.mainTexture = font.material.mainTexture;
                return material;
            }
            if (template == null) template = Resources.Load<Material>("WorldText");
            if (template == null) return font.material;
            material = new Material(template) { name = "WorldText_" + font.name, mainTexture = font.material.mainTexture };
            cache[font] = material;
            if (!hooked) { Font.textureRebuilt += Rebuilt; hooked = true; }
            return material;
        }

        // Dynamic fonts rebuild their atlas when new glyphs appear; keep our copy pointing at the new texture.
        static void Rebuilt(Font font)
        {
            if (cache.TryGetValue(font, out var material) && material != null) material.mainTexture = font.material.mainTexture;
        }

        public static void Apply(TextMesh text)
        {
            if (text == null || text.font == null) return;
            var material = For(text.font);
            if (material != null) text.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        // Picks the largest characterSize up to maxSize whose wrapped text fits width x height (metres).
        public static void Fit(TextMesh text, string content, float width, float height, float maxSize, float minSize = .012f)
        {
            content = content ?? "";
            var font = text.font;
            int size = text.fontSize > 0 ? text.fontSize : font != null ? font.fontSize : 16;
            if (font != null) font.RequestCharactersInTexture(content, size, text.fontStyle);
            float ratio = font != null && font.lineHeight > 0 && font.fontSize > 0 ? (float)font.lineHeight / font.fontSize : 1.3f;
            float characterSize = maxSize;
            List<string> lines = null;
            for (int guard = 0; guard < 40; guard++)
            {
                lines = Wrap(font, size, text.fontStyle, content, width / (characterSize * PixelsToWorld));
                float lineHeight = size * ratio * text.lineSpacing * characterSize * PixelsToWorld;
                if (lines.Count * lineHeight <= height || characterSize * .9f < minSize) break;
                characterSize *= .9f;
            }
            text.characterSize = characterSize;
            text.text = string.Join("\n", lines);
        }

        // Breaks lines by measured glyph width. Prefers spaces; inside Thai words it never separates a
        // consonant from its vowel or tone marks, and never splits Latin words or numbers.
        public static List<string> Wrap(Font font, int size, FontStyle style, string content, float maxPixels)
        {
            var lines = new List<string>();
            foreach (string paragraph in (content ?? "").Replace("\r", "").Split('\n'))
            {
                int start = 0;
                while (start < paragraph.Length)
                {
                    float width = 0f;
                    int spaceBreak = -1, softBreak = -1, end = paragraph.Length;
                    for (int i = start; i < paragraph.Length; i++)
                    {
                        if (i > start && CanBreakBefore(paragraph, i))
                        {
                            if (paragraph[i - 1] == ' ' || paragraph[i] == ' ') spaceBreak = i;
                            else softBreak = i;
                        }
                        width += Advance(font, size, style, paragraph[i]);
                        if (width > maxPixels && i > start)
                        {
                            end = spaceBreak > start ? spaceBreak : softBreak > start ? softBreak : i;
                            break;
                        }
                    }
                    lines.Add(paragraph.Substring(start, end - start).Trim());
                    start = end;
                    while (start < paragraph.Length && paragraph[start] == ' ') start++;
                }
                if (paragraph.Length == 0) lines.Add("");
            }
            return lines;
        }

        static float Advance(Font font, int size, FontStyle style, char c)
        {
            if (IsThaiMark(c)) return 0f;
            if (font != null && font.GetCharacterInfo(c, out var info, size, style)) return info.advance;
            return c == ' ' ? size * .3f : size * .55f;
        }

        public static bool CanBreakBefore(string text, int i)
        {
            char c = text[i], previous = text[i - 1];
            if (IsThaiMark(c) || IsThaiFollowingVowel(c) || c == 'ๆ') return false;
            if (previous >= 'เ' && previous <= 'ไ') return false; // leading vowels stay with their consonant
            if (previous == ' ' || c == ' ') return true;
            if (",.!?:;)]}%".IndexOf(c) >= 0 || "([{".IndexOf(previous) >= 0) return false;
            if (IsLatinOrDigit(c) && IsLatinOrDigit(previous)) return false;
            return true;
        }

        static bool IsThaiMark(char c) => c == 'ั' || (c >= 'ิ' && c <= 'ฺ') || (c >= '็' && c <= '๎');
        static bool IsThaiFollowingVowel(char c) => c == 'ะ' || c == 'า' || c == 'ำ' || c == 'ๅ';
        static bool IsLatinOrDigit(char c) => (c < 128 && char.IsLetterOrDigit(c)) || (c >= '๐' && c <= '๙') || c == '/' || c == '-';
    }
}
