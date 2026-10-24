using System.Collections.Generic;
using UnityEngine;

namespace HorseCity.EditorTools
{
    /// <summary>5x7 pixel font built from boxes, for glowing building signs without any font assets.</summary>
    public static class BlockFont
    {
        static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
            ['C'] = new[] { ".####", "#....", "#....", "#....", "#....", "#....", ".####" },
            ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
            ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['I'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
            ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" },
            ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
            ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
            ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['V'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." },
            ['+'] = new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." },
        };

        /// <summary>
        /// Writes text centered on 'center', facing -Z in the part's current frame.
        /// px is the size of one font pixel in meters.
        /// </summary>
        public static void Write(Part p, Material m, string text, Vector3 center, float px, float depth = 0.12f)
        {
            text = text.ToUpperInvariant();
            float charW = 6 * px;
            float total = text.Length * charW - px;
            float x0 = center.x - total * 0.5f;
            for (int i = 0; i < text.Length; i++)
            {
                if (!Glyphs.TryGetValue(text[i], out string[] rows)) continue;
                for (int row = 0; row < 7; row++)
                {
                    // merge horizontal runs into one box to keep the triangle count low
                    string line = rows[row];
                    int c = 0;
                    while (c < 5)
                    {
                        if (line[c] != '#') { c++; continue; }
                        int start = c;
                        while (c < 5 && line[c] == '#') c++;
                        float runW = (c - start) * px;
                        float x = x0 + i * charW + start * px + runW * 0.5f;
                        float y = center.y + (3 - row) * px;
                        p.Box(m, new Vector3(x, y, center.z), new Vector3(runW, px, depth));
                    }
                }
            }
        }

        public static float Width(string text, float px) => text.Length * 6 * px - px;
    }
}
