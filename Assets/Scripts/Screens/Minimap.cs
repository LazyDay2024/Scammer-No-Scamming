using UnityEngine;

namespace ScammerNoScamming
{
    public static class Minimap
    {
        const float Size = 330f;
        const float Margin = 48f;
        const float ViewRadius = 150f;
        const float DiscFraction = .43f;
        const int Segments = 56;
        static Material material;

        public static void Draw(Vector3? waypoint = null)
        {
            var view = CityMapView.Instance;
            var player = PlayerLocator.Player;
            if (view == null || player == null || GameScreen.AnyOpen) return;
            view.Prepare();
            var picture = view.Picture;
            var frame = ThaiGui.Art("minimap_frame");
            var arrow = ThaiGui.Art("player_marker");
            if (picture == null || frame == null) return;

            var rect = new Rect(ThaiGui.RefWidth - Margin - Size, Margin, Size, Size);
            if (Event.current.type != EventType.Repaint) return;
            float radius = Size * DiscFraction;
            DrawDisc(rect.center, radius, picture, view.ToUv(player.transform.position), ViewRadius / (view.halfSize * 2f));
            if (waypoint.HasValue) DrawWaypoint(rect.center, radius, player.transform.position, waypoint.Value);
            GUI.DrawTexture(rect, frame, ScaleMode.StretchToFill, true);

            if (arrow == null) return;
            var camera = player.GetComponentInChildren<Camera>();
            float yaw = camera != null ? camera.transform.eulerAngles.y : player.transform.eulerAngles.y;
            var saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(yaw, rect.center);
            GUI.DrawTexture(new Rect(rect.center.x - 17f, rect.center.y - 18f, 34f, 36f), arrow, ScaleMode.StretchToFill, true);
            GUI.matrix = saved;
        }

        static void DrawWaypoint(Vector2 center, float radius, Vector3 player, Vector3 target)
        {
            Vector3 offset = target - player;
            Vector2 direction = new Vector2(offset.x, -offset.z);
            if (direction.sqrMagnitude < 0.01f) return;

            float distance = direction.magnitude;
            Vector2 point = center + direction.normalized * Mathf.Min(distance / ViewRadius, 0.88f) * radius;
            float visibleDistance = Mathf.Min(Vector2.Distance(center, point), radius * 0.88f);
            Vector2 end = center + (point - center).normalized * visibleDistance;

            int dashes = Mathf.Clamp(Mathf.CeilToInt(visibleDistance / 11f), 1, 24);
            for (int i = 2; i < dashes; i++)
            {
                Vector2 dash = Vector2.Lerp(center, end, i / (float)dashes);
                ThaiGui.Fill(new Rect(dash.x - 1.5f, dash.y - 1.5f, 3f, 3f),
                    new Color(ThaiGui.Gold.r, ThaiGui.Gold.g, ThaiGui.Gold.b, 0.75f));
            }

            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, point);
            ThaiGui.Fill(new Rect(point.x - 6f, point.y - 6f, 12f, 12f), ThaiGui.Gold);
            GUI.matrix = matrix;
        }

        static void DrawDisc(Vector2 centre, float radius, Texture picture, Vector2 uvCentre, float uvRadius)
        {
            if (material == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) return;
                material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            material.mainTexture = picture;
            material.SetPass(0);
            var tint = GUI.color;
            Vector2 Screen(Vector2 canvas) => GUI.matrix.MultiplyPoint3x4(canvas);
            Vector2 c = Screen(centre);
            float scale = GUI.matrix.m00;
            bool srgb = GL.sRGBWrite;
            GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0f, UnityEngine.Screen.width, UnityEngine.Screen.height, 0f);
            GL.Begin(GL.TRIANGLES);
            GL.Color(tint);
            for (int i = 0; i < Segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / Segments;
                float a1 = (i + 1) * Mathf.PI * 2f / Segments;
                Vertex(c, uvCentre);
                Vertex(c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius * scale,
                    uvCentre + new Vector2(Mathf.Cos(a0), -Mathf.Sin(a0)) * uvRadius);
                Vertex(c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius * scale,
                    uvCentre + new Vector2(Mathf.Cos(a1), -Mathf.Sin(a1)) * uvRadius);
            }
            GL.End();
            GL.PopMatrix();
            GL.sRGBWrite = srgb;
        }

        static void Vertex(Vector2 screen, Vector2 uv)
        {
            GL.TexCoord2(uv.x, uv.y);
            GL.Vertex3(screen.x, screen.y, 0f);
        }
    }
}