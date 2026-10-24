using UnityEngine;

namespace ScammerNoScamming
{
    public static class GameHud
    {
        static float shownHealth = -1f;
        static float shownTrouble = -1f;

        public static void Draw(string caseTitle, string objective, float health, float healthMax, float trouble,
            float troubleMax, bool policeComing, Vector3? waypoint = null)
        {
            if (shownHealth < 0f)
            {
                shownHealth = health;
                shownTrouble = trouble;
            }

            var player = PlayerLocator.Player;
            Transform playerTransform = player != null ? player.transform : null;

            float step = Time.unscaledDeltaTime * 4f;
            shownHealth = Mathf.Lerp(shownHealth, health, Mathf.Clamp01(step));
            shownTrouble = Mathf.Lerp(shownTrouble, trouble, Mathf.Clamp01(step));

            var canvas = ThaiGui.BeginCanvas();
            DrawObjective(caseTitle, objective, playerTransform, waypoint);
            DrawCompass(canvas, playerTransform, waypoint);
            DrawTrouble(canvas, shownTrouble, troubleMax, policeComing);
            DrawBar(new Rect(64f, 220f, 540f, 34f), "HEALTH", shownHealth, healthMax,
                shownHealth / Mathf.Max(1f, healthMax) < 0.3f ? ThaiGui.Danger : ThaiGui.Rust);
            Minimap.Draw(waypoint);
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

        static void DrawObjective(string caseTitle, string objective, Transform player, Vector3? waypoint)
        {
            var panel = new Rect(40f, 42f, 610f, 270f);
            ThaiGui.Panel(panel, 0.7f);
            float x = panel.x + 24f;
            ThaiGui.Tracked(x, panel.y + 12f, caseTitle, ThaiGui.Cap, ThaiGui.Gold, 0f, FontStyle.Bold);
            GUI.Label(new Rect(x, panel.y + 42f, panel.width - 48f, 70f), objective ?? string.Empty,
                ThaiGui.Text(ThaiGui.Small + 2, ThaiGui.Bone));

            if (waypoint.HasValue && player != null)
            {
                Vector3 offset = waypoint.Value - player.position;
                offset.y = 0f;
                int distance = Mathf.RoundToInt(offset.magnitude);
                GUI.Label(new Rect(x, panel.y + 112f, panel.width - 48f, 28f),
                    $"WAYPOINT   {distance} m", ThaiGui.Text(ThaiGui.Cap, ThaiGui.Gold, FontStyle.Bold));
            }
        }

        static void DrawCompass(Rect canvas, Transform player, Vector3? waypoint)
        {
            float center = canvas.center.x;
            float width = 500f;
            float half = width * 0.5f;
            float y = 52f;
            float yaw = player != null ? player.eulerAngles.y : 0f;
            var camera = player != null ? player.GetComponentInChildren<Camera>() : null;
            if (camera != null) yaw = camera.transform.eulerAngles.y;

            ThaiGui.Fill(new Rect(center - half, y, width, 2f), new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.55f));
            for (int i = -6; i <= 6; i++)
            {
                float x = center + i * 15f / 90f * half;
                float tickHeight = i == 0 ? 18f : i % 2 == 0 ? 12f : 7f;
                ThaiGui.Fill(new Rect(x - 1f, y - tickHeight * 0.5f, 2f, tickHeight),
                    i == 0 ? ThaiGui.Gold : new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.65f));
            }

            DrawCardinal(center, half, y, yaw, 0f, "N");
            DrawCardinal(center, half, y, yaw, 90f, "E");
            DrawCardinal(center, half, y, yaw, 180f, "S");
            DrawCardinal(center, half, y, yaw, 270f, "W");

            if (waypoint.HasValue && player != null)
            {
                Vector3 delta = waypoint.Value - player.position;
                float bearing = Mathf.Repeat(Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 360f);
                float relative = Mathf.DeltaAngle(yaw, bearing);
                float x = center + Mathf.Clamp(relative, -90f, 90f) / 90f * half;
                Diamond(new Vector2(x, y), 12f, ThaiGui.Gold);
            }
        }

        static void DrawCardinal(float center, float half, float lineY, float yaw, float bearing, string label)
        {
            float relative = Mathf.DeltaAngle(yaw, bearing);
            if (Mathf.Abs(relative) > 90f) return;
            float x = center + relative / 90f * half;
            GUI.Label(new Rect(x - 18f, lineY - 34f, 36f, 26f), label,
                ThaiGui.Text(ThaiGui.Cap, label == "N" ? ThaiGui.Gold : ThaiGui.Bone, FontStyle.Bold, TextAnchor.MiddleCenter));
        }

        static void DrawTrouble(Rect canvas, float trouble, float maximum, bool policeComing)
        {
            float center = canvas.center.x;
            float y = 84f;
            float fraction = Mathf.Clamp01(trouble / Mathf.Max(1f, maximum));
            Color fill = policeComing || fraction >= 0.75f ? ThaiGui.Danger : ThaiGui.Gold;
            float start = center - 270f;
            GUI.Label(new Rect(start, y - 7f, 92f, 28f), "TROUBLE",
                ThaiGui.Text(ThaiGui.Cap, fill, FontStyle.Bold, TextAnchor.MiddleRight));

            var track = new Rect(start + 106f, y + 4f, 320f, 8f);
            ThaiGui.Fill(track, new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.22f));
            ThaiGui.Fill(new Rect(track.x, track.y, track.width * fraction, track.height), fill);
            GUI.Label(new Rect(track.xMax + 14f, y - 7f, 110f, 28f),
                policeComing ? "INBOUND" : $"{trouble:0} / {maximum:0}",
                ThaiGui.Text(ThaiGui.Cap, fill, FontStyle.Bold, TextAnchor.MiddleLeft));
        }

        static void DrawBar(Rect r, string label, float current, float maximum, Color color)
        {
            float fraction = Mathf.Clamp01(current / Mathf.Max(1f, maximum));
            GUI.Label(new Rect(r.x, r.y, 105f, r.height), label,
                ThaiGui.Text(ThaiGui.Cap, ThaiGui.Bone, FontStyle.Bold, TextAnchor.MiddleLeft));
            var track = new Rect(r.x + 112f, r.y + r.height * 0.5f - 4f, r.width - 206f, 8f);
            ThaiGui.Fill(track, new Color(ThaiGui.Bone.r, ThaiGui.Bone.g, ThaiGui.Bone.b, 0.2f));
            ThaiGui.Fill(new Rect(track.x, track.y, track.width * fraction, track.height), color);
            GUI.Label(new Rect(track.xMax + 12f, r.y, 82f, r.height), $"{current:0}",
                ThaiGui.Text(ThaiGui.Cap, ThaiGui.Bone, FontStyle.Bold, TextAnchor.MiddleRight));
        }

        static void Diamond(Vector2 center, float size, Color color)
        {
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, center);
            ThaiGui.Fill(new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size), color);
            GUI.matrix = matrix;
        }
    }
}