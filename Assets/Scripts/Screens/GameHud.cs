using UnityEngine;

namespace ScammerNoScamming
{
    // In-action HUD: case, objective, health and trouble. Quiet panel in the top-left corner.
    public static class GameHud
    {
        static float shownHealth = -1f, shownTrouble = -1f;

        public static void Draw(string caseTitle, string objective, float health, float healthMax, float trouble,
            float troubleMax, bool policeComing)
        {
            if (shownHealth < 0f) { shownHealth = health; shownTrouble = trouble; }
            float step = Time.unscaledDeltaTime * 4f;
            shownHealth = Mathf.Lerp(shownHealth, health, Mathf.Clamp01(step));
            shownTrouble = Mathf.Lerp(shownTrouble, trouble, Mathf.Clamp01(step));

            ThaiGui.BeginCanvas();
            var r = new Rect(48f, 48f, 600f, 214f);
            ThaiGui.Panel(r, 0.72f);
            float x = r.x + 30f;
            ThaiGui.Tracked(x, r.y + 20f, caseTitle, ThaiGui.Cap, ThaiGui.Gold, 0f, FontStyle.Bold);
            GUI.Label(new Rect(x, r.y + 54f, r.width - 60f, 70f), objective, ThaiGui.Text(ThaiGui.Small, ThaiGui.Bone));

            float w = r.width - 60f;
            Bar(new Rect(x, r.y + 138f, w, 28f), "เลือด", health.ToString("0"), shownHealth / Mathf.Max(1f, healthMax),
                shownHealth / Mathf.Max(1f, healthMax) < 0.3f ? ThaiGui.Danger : ThaiGui.Bone);
            string troubleText = $"{trouble:0} / {troubleMax:0}";
            Bar(new Rect(x, r.y + 174f, w, 28f), "Trouble", policeComing ? "ตำรวจเทากำลังมา" : troubleText,
                shownTrouble / Mathf.Max(1f, troubleMax), shownTrouble / Mathf.Max(1f, troubleMax) > 0.7f ? ThaiGui.Danger : ThaiGui.Gold,
                policeComing);
            ThaiGui.EndCanvas();
        }

        public static void DrawAmmo(int loaded, int reserve, bool reloading)
        {
            if (GameScreen.AnyOpen) return;
            var canvas = ThaiGui.BeginCanvas();
            var panel = new Rect(canvas.width - 378f, canvas.height - 156f, 330f, 108f);
            ThaiGui.Panel(panel, 0.72f);

            loaded = Mathf.Clamp(loaded, 0, RevolverAmmo.Capacity);
            for (int i = 0; i < RevolverAmmo.Capacity; i++)
            {
                float x = panel.x + 26f + i * 25f;
                float y = panel.y + 21f;
                var color = i < loaded ? ThaiGui.Gold : new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.2f);
                // A narrow tip, cartridge body and rim keep each pip readable as one round.
                ThaiGui.Fill(new Rect(x + 4f, y, 4f, 3f), color);
                ThaiGui.Fill(new Rect(x + 2f, y + 3f, 8f, 5f), color);
                ThaiGui.Fill(new Rect(x + 1f, y + 8f, 10f, 17f), color);
                ThaiGui.Fill(new Rect(x, y + 26f, 12f, 3f), color);
            }

            GUI.Label(new Rect(panel.x + 184f, panel.y + 16f, 122f, 40f), $"สำรอง {Mathf.Max(0, reserve)}",
                ThaiGui.Text(ThaiGui.Cap, ThaiGui.Bone, FontStyle.Bold, TextAnchor.MiddleRight));
            string status = reloading ? "กำลังบรรจุกระสุน…" : loaded == 0 ? "R · บรรจุกระสุน" : null;
            if (status != null)
                GUI.Label(new Rect(panel.x + 24f, panel.y + 62f, panel.width - 48f, 34f), status,
                    ThaiGui.Text(ThaiGui.Cap, reloading ? ThaiGui.Muted : ThaiGui.Gold, FontStyle.Normal, TextAnchor.MiddleRight));
            ThaiGui.EndCanvas();
        }

        static void Bar(Rect r, string label, string value, float t, Color color, bool alert = false)
        {
            GUI.Label(new Rect(r.x, r.y - 3f, 110f, r.height), label, ThaiGui.Text(ThaiGui.Cap, ThaiGui.Muted, FontStyle.Normal, TextAnchor.MiddleLeft));
            var track = new Rect(r.x + 112f, r.y + r.height * 0.5f - 2f, r.width - 112f - 170f, 4f);
            ThaiGui.Fill(track, new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.16f));
            ThaiGui.Fill(new Rect(track.x, track.y, track.width * Mathf.Clamp01(t), track.height), color);
            GUI.Label(new Rect(track.xMax + 14f, r.y - 3f, 156f, r.height), value,
                ThaiGui.Text(ThaiGui.Cap, alert ? ThaiGui.Danger : ThaiGui.Bone, FontStyle.Bold, TextAnchor.MiddleRight));
        }
    }
}
