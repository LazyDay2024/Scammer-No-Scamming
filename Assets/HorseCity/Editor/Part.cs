using System.Collections.Generic;
using UnityEngine;

namespace HorseCity.EditorTools
{
    /// <summary>
    /// A MeshKit that picks submeshes by material, so geometry code just says
    /// "box of plaster here" and the builder turns it into one mesh with a material per submesh.
    /// </summary>
    public class Part
    {
        public readonly MeshKit kit = new MeshKit();
        public readonly List<Material> mats = new List<Material>();

        public Matrix4x4 Frame { get => kit.frame; set => kit.frame = value; }
        public bool IsEmpty => kit.IsEmpty;

        public int S(Material m)
        {
            int i = mats.IndexOf(m);
            if (i >= 0) return i;
            mats.Add(m);
            return mats.Count - 1;
        }

        public void Box(Material m, Vector3 c, Vector3 size, float yaw = 0f) => kit.Box(S(m), c, size, yaw);
        public void Box(Material m, Vector3 c, Vector3 size, Vector2 tile, float yaw = 0f, bool top = true, bool bottom = false)
            => kit.Box(S(m), c, size, tile, yaw, top, bottom);

        /// <summary>Box from min/max corners (handy for interiors).</summary>
        public void MinMax(Material m, Vector3 min, Vector3 max, float tile = 2f)
            => kit.Box(S(m), (min + max) * 0.5f, max - min, new Vector2(tile, tile), 0f, true, true);

        public void Beam(Material m, Vector3 a, Vector3 b, float t) => kit.Beam(S(m), a, b, t);
        public void Cylinder(Material m, Vector3 c, float r, float h, int seg = 8, bool cap = true) => kit.Cylinder(S(m), c, r, h, seg, cap);
        public void Blob(Material m, Vector3 c, Vector3 radii, int seed) => kit.Blob(S(m), c, radii, seed);
        public void Gable(Material m, Vector3 c, float w, float d, float rise, float overhang, float yaw = 0f)
            => kit.GableRoof(S(m), c, w, d, rise, overhang, yaw);

        /// <summary>Runs geometry code inside a local frame (position + yaw).</summary>
        public void Local(Vector3 pos, float yaw, System.Action body)
        {
            Matrix4x4 saved = kit.frame;
            kit.frame = saved * Matrix4x4.TRS(pos, Quaternion.Euler(0f, yaw, 0f), Vector3.one);
            body();
            kit.frame = saved;
        }

        /// <summary>
        /// Straight wall from a to b (floor level, local space) with door/window openings.
        /// Two skins: outMat faces away from 'inside', inMat faces toward it.
        /// </summary>
        public void Wall(Material outMat, Material inMat, Vector3 a, Vector3 b, float height, float thick,
                         Vector3 inside, params Opening[] openings)
        {
            Vector3 d = b - a; d.y = 0;
            float len = d.magnitude;
            Vector3 dir = d / len;
            Vector3 normal = new Vector3(-dir.z, 0, dir.x); // left of direction
            if (Vector3.Dot(inside - a, normal) < 0) normal = -normal;
            float yaw = Mathf.Atan2(-dir.z, dir.x) * Mathf.Rad2Deg;

            var cuts = new List<Opening>(openings);
            cuts.Sort((x, y) => x.center.CompareTo(y.center));

            void Piece(float s0, float s1, float y0, float y1)
            {
                if (s1 - s0 < 0.01f || y1 - y0 < 0.01f) return;
                float mid = (s0 + s1) * 0.5f;
                Vector3 basePos = a + dir * mid;
                Vector3 size = new Vector3(s1 - s0, y1 - y0, thick * 0.5f);
                Vector2 tile = new Vector2(3f, 3f);
                kit.Box(S(inMat), basePos + normal * thick * 0.25f + Vector3.up * (a.y + (y0 + y1) * 0.5f) - Vector3.up * a.y, size, tile, yaw);
                kit.Box(S(outMat), basePos - normal * thick * 0.25f + Vector3.up * (a.y + (y0 + y1) * 0.5f) - Vector3.up * a.y, size, tile, yaw);
            }

            float cursor = 0f;
            foreach (Opening o in cuts)
            {
                float s0 = Mathf.Clamp(o.center - o.width * 0.5f, 0, len);
                float s1 = Mathf.Clamp(o.center + o.width * 0.5f, 0, len);
                Piece(cursor, s0, 0, height);
                Piece(s0, s1, 0, o.bottom);            // sill
                Piece(s0, s1, o.top, height);          // lintel
                if (o.glass != null && o.bottom > 0.01f) // window pane
                {
                    float mid = (s0 + s1) * 0.5f;
                    kit.Box(S(o.glass), a + dir * mid + Vector3.up * ((o.bottom + o.top) * 0.5f),
                            new Vector3(s1 - s0, o.top - o.bottom, 0.05f), new Vector2(1, 1), yaw);
                }
                cursor = s1;
            }
            Piece(cursor, len, 0, height);
        }

        public struct Opening
        {
            public float center, width, bottom, top;
            public Material glass;
            public static Opening Door(float at, float w, float h) => new Opening { center = at, width = w, bottom = 0, top = h };
            public static Opening Window(float at, float w, float sill, float top, Material glass = null)
                => new Opening { center = at, width = w, bottom = sill, top = top, glass = glass };
        }
    }
}
