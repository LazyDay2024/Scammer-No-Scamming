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
