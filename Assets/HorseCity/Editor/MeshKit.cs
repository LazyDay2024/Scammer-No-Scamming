using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HorseCity.EditorTools
{
    /// <summary>
    /// Tiny procedural mesh builder. Every shape is flat shaded and gets UVs measured in
    /// meters divided by a per-call tile size, so window textures line up with floors.
    /// </summary>
    public class MeshKit
    {
        readonly List<Vector3> _v = new List<Vector3>();
        readonly List<Vector3> _n = new List<Vector3>();
        readonly List<Vector2> _uv = new List<Vector2>();
        readonly List<List<int>> _tris = new List<List<int>>();

        /// <summary>Transform applied to everything added (lets callers build in a local frame).</summary>
        public Matrix4x4 frame = Matrix4x4.identity;

        public int VertexCount => _v.Count;
        public bool IsEmpty => _v.Count == 0;

        List<int> Sub(int s)
        {
            while (_tris.Count <= s) _tris.Add(new List<int>());
            return _tris[s];
        }

        /// <summary>Adds a quad from four corners (counter-clockwise seen from the front).</summary>
        public void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
        {
            a = frame.MultiplyPoint3x4(a); b = frame.MultiplyPoint3x4(b);
            c = frame.MultiplyPoint3x4(c); d = frame.MultiplyPoint3x4(d);
            // Normal from the diagonals, so quads that collapse to a triangle (one edge of zero
            // length, e.g. at the center of a disk) still get a valid normal. A zero normal turns
            // into NaN in the shader and bloom then spreads it across the whole screen.
            Vector3 cr = Vector3.Cross(d - b, c - a);
            if (cr.sqrMagnitude < 1e-12f) return;
            Vector3 n = cr.normalized;
            int i = _v.Count;
            _v.Add(a); _v.Add(b); _v.Add(c); _v.Add(d);
            _n.Add(n); _n.Add(n); _n.Add(n); _n.Add(n);
            _uv.Add(uvA); _uv.Add(uvB); _uv.Add(uvC); _uv.Add(uvD);
            List<int> t = Sub(sub);
            t.Add(i); t.Add(i + 3); t.Add(i + 2);
            t.Add(i); t.Add(i + 2); t.Add(i + 1);
        }

        public void Tri(int sub, Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC)
        {
            a = frame.MultiplyPoint3x4(a); b = frame.MultiplyPoint3x4(b); c = frame.MultiplyPoint3x4(c);
            Vector3 cr = Vector3.Cross(c - a, b - a);
            if (cr.sqrMagnitude < 1e-12f) return; // degenerate (e.g. the pole of a blob)
            Vector3 n = cr.normalized;
            int i = _v.Count;
            _v.Add(a); _v.Add(b); _v.Add(c);
            _n.Add(n); _n.Add(n); _n.Add(n);
            _uv.Add(uvA); _uv.Add(uvB); _uv.Add(uvC);
            List<int> t = Sub(sub);
            t.Add(i); t.Add(i + 2); t.Add(i + 1);
        }

        /// <summary>
        /// Axis-aligned box in the current frame, optionally yawed around its own center.
        /// tile is the size in meters of one texture repeat (x = horizontal, y = vertical).
        /// The v coordinate uses the box's real height so stacked boxes continue the same floors.
        /// </summary>
        public void Box(int sub, Vector3 center, Vector3 size, Vector2 tile, float yaw = 0f, bool top = true, bool bottom = false)
        {
            Matrix4x4 saved = frame;
            frame = saved * Matrix4x4.TRS(center, Quaternion.Euler(0f, yaw, 0f), Vector3.one);

            Vector3 h = size * 0.5f;
            float y0 = center.y - h.y, y1 = center.y + h.y;
            float v0 = y0 / tile.y, v1 = y1 / tile.y;
            float ux = size.x / tile.x, uz = size.z / tile.x;

            Vector3 p000 = new Vector3(-h.x, -h.y, -h.z), p100 = new Vector3(h.x, -h.y, -h.z);
            Vector3 p010 = new Vector3(-h.x, h.y, -h.z), p110 = new Vector3(h.x, h.y, -h.z);
            Vector3 p001 = new Vector3(-h.x, -h.y, h.z), p101 = new Vector3(h.x, -h.y, h.z);
            Vector3 p011 = new Vector3(-h.x, h.y, h.z), p111 = new Vector3(h.x, h.y, h.z);

            // front (-z), right (+x), back (+z), left (-x)
            Quad(sub, p000, p100, p110, p010, new Vector2(0, v0), new Vector2(ux, v0), new Vector2(ux, v1), new Vector2(0, v1));
            Quad(sub, p100, p101, p111, p110, new Vector2(0, v0), new Vector2(uz, v0), new Vector2(uz, v1), new Vector2(0, v1));
            Quad(sub, p101, p001, p011, p111, new Vector2(0, v0), new Vector2(ux, v0), new Vector2(ux, v1), new Vector2(0, v1));
            Quad(sub, p001, p000, p010, p011, new Vector2(0, v0), new Vector2(uz, v0), new Vector2(uz, v1), new Vector2(0, v1));
            if (top)
                Quad(sub, p010, p110, p111, p011, new Vector2(0, 0), new Vector2(ux, 0), new Vector2(ux, uz), new Vector2(0, uz));
            if (bottom)
                Quad(sub, p001, p101, p100, p000, new Vector2(0, 0), new Vector2(ux, 0), new Vector2(ux, uz), new Vector2(0, uz));

            frame = saved;
        }

        /// <summary>Box with uniform tiling and no special UV handling. Good for props.</summary>
        public void Box(int sub, Vector3 center, Vector3 size, float yaw = 0f)
        {
            Box(sub, center, size, new Vector2(2f, 2f), yaw, true, false);
        }

        /// <summary>Thin box stretched between two points (wires, rails, beams).</summary>
        public void Beam(int sub, Vector3 a, Vector3 b, float thickness)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.0001f) return;
            Matrix4x4 saved = frame;
            Quaternion rot = Quaternion.LookRotation(d / len, Mathf.Abs(Vector3.Dot(d / len, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up);
            frame = saved * Matrix4x4.TRS((a + b) * 0.5f, rot, Vector3.one);
            Box(sub, Vector3.zero, new Vector3(thickness, thickness, len), new Vector2(1f, 1f), 0f, true, true);
            frame = saved;
        }

        /// <summary>Gable roof: ridge runs along local x. Sits on top of a w x d footprint at height y.</summary>
        public void GableRoof(int sub, Vector3 center, float w, float d, float rise, float overhang, float yaw = 0f)
        {
            Matrix4x4 saved = frame;
            frame = saved * Matrix4x4.TRS(center, Quaternion.Euler(0f, yaw, 0f), Vector3.one);
            float hw = w * 0.5f + overhang, hd = d * 0.5f + overhang;
            Vector3 fl = new Vector3(-hw, 0, -hd), fr = new Vector3(hw, 0, -hd);
            Vector3 bl = new Vector3(-hw, 0, hd), br = new Vector3(hw, 0, hd);
            Vector3 rl = new Vector3(-hw, rise, 0), rr = new Vector3(hw, rise, 0);
            float slope = Mathf.Sqrt(hd * hd + rise * rise) / 2f;
            float u = w / 2f;
            Quad(sub, fl, fr, rr, rl, new Vector2(0, 0), new Vector2(u, 0), new Vector2(u, slope), new Vector2(0, slope));
            Quad(sub, br, bl, rl, rr, new Vector2(0, 0), new Vector2(u, 0), new Vector2(u, slope), new Vector2(0, slope));
            Tri(sub, bl, fl, rl, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f));
            Tri(sub, fr, br, rr, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f));
            // underside so the overhang is not see-through from below
            Quad(sub, bl, br, fr, fl, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            frame = saved;
        }

        /// <summary>Upright cylinder (no caps on the bottom).</summary>
        public void Cylinder(int sub, Vector3 baseCenter, float radius, float height, int segments, bool cap = true)
        {
            float circ = 2f * Mathf.PI * radius;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius;
                Vector3 d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius;
                Vector3 b0 = baseCenter + d0, b1 = baseCenter + d1, t0 = b0 + Vector3.up * height, t1 = b1 + Vector3.up * height;
                float u0 = circ * i / segments / 2f, u1 = circ * (i + 1) / segments / 2f;
                Quad(sub, b0, b1, t1, t0, new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, height / 2f), new Vector2(u0, height / 2f));
                if (cap)
                {
                    Vector3 c = baseCenter + Vector3.up * height;
                    Tri(sub, c, t0, t1, new Vector2(0.5f, 0.5f), new Vector2(0.5f + d0.x, 0.5f + d0.z), new Vector2(0.5f + d1.x, 0.5f + d1.z));
                }
            }
        }

        /// <summary>Low-poly blob for tree canopies and hay piles.</summary>
        public void Blob(int sub, Vector3 center, Vector3 radii, int seed)
        {
            const int rings = 4, segs = 7;
            System.Random rnd = new System.Random(seed);
            Vector3[,] p = new Vector3[rings + 1, segs];
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.PI * r / rings;
                for (int s = 0; s < segs; s++)
                {
                    float th = Mathf.PI * 2f * s / segs + r * 0.4f;
                    float j = (r == 0 || r == rings) ? 1f : 0.85f + (float)rnd.NextDouble() * 0.3f;
                    p[r, s] = center + Vector3.Scale(new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th)) * j, radii);
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segs; s++)
                {
                    int s1 = (s + 1) % segs;
                    Vector3 a = p[r, s], b = p[r, s1], c = p[r + 1, s1], d = p[r + 1, s];
                    Tri(sub, a, c, b, Vector2.zero, Vector2.right, Vector2.up);
                    Tri(sub, a, d, c, Vector2.zero, Vector2.up, Vector2.one);
                }
        }

        /// <summary>
        /// Flat annular sector (ring piece) at height y with optional curb walls.
        /// Angles in degrees, counter-clockwise from +X.
        /// </summary>
        public void RingSector(int sub, float r0, float r1, float a0Deg, float a1Deg, float y, float curb, float tile, int sideSub = -1)
        {
            float arc = (a1Deg - a0Deg) * Mathf.Deg2Rad;
            int steps = Mathf.Max(1, Mathf.CeilToInt(arc * r1 / 6f));
            // split radially too, so no triangle edge gets longer than ~60m (physics dislikes huge triangles)
            int rings = Mathf.Max(1, Mathf.CeilToInt((r1 - r0) / 60f));
            for (int i = 0; i < steps; i++)
            {
                float t0 = a0Deg * Mathf.Deg2Rad + arc * i / steps;
                float t1 = a0Deg * Mathf.Deg2Rad + arc * (i + 1) / steps;
                Vector3 d0 = new Vector3(Mathf.Cos(t0), 0, Mathf.Sin(t0)), d1 = new Vector3(Mathf.Cos(t1), 0, Mathf.Sin(t1));
                Vector3 i0 = d0 * r0 + Vector3.up * y, i1 = d1 * r0 + Vector3.up * y;
                Vector3 o0 = d0 * r1 + Vector3.up * y, o1 = d1 * r1 + Vector3.up * y;
                for (int j = 0; j < rings; j++)
                {
                    float ra = Mathf.Lerp(r0, r1, (float)j / rings), rb = Mathf.Lerp(r0, r1, (float)(j + 1) / rings);
                    Vector3 a0 = d0 * ra + Vector3.up * y, a1 = d1 * ra + Vector3.up * y;
                    Vector3 b0 = d0 * rb + Vector3.up * y, b1 = d1 * rb + Vector3.up * y;
                    Quad(sub, a0, b0, b1, a1, UvXZ(a0, tile), UvXZ(b0, tile), UvXZ(b1, tile), UvXZ(a1, tile));
                }
                if (curb > 0f)
                {
                    int ss = sideSub < 0 ? sub : sideSub;
                    Vector3 dn = Vector3.down * curb;
                    Quad(ss, o0 + dn, o1 + dn, o1, o0, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
                    if (r0 > 0.01f)
                        Quad(ss, i1 + dn, i0 + dn, i0, i1, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
                    if (i == 0)
                        Quad(ss, i0 + dn, o0 + dn, o0, i0, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
                    if (i == steps - 1)
                        Quad(ss, o1 + dn, i1 + dn, i1, o1, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
                }
            }
        }

        static Vector2 UvXZ(Vector3 p, float tile) => new Vector2(p.x / tile, p.z / tile);

        public Mesh ToMesh(string name)
        {
            Mesh m = new Mesh { name = name };
            if (_v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(_v);
            m.SetNormals(_n);
            m.SetUVs(0, _uv);
            m.subMeshCount = _tris.Count;
            for (int s = 0; s < _tris.Count; s++) m.SetTriangles(_tris[s], s, false);
            m.RecalculateBounds();
            m.RecalculateTangents();
            return m;
        }

        /// <summary>Which submeshes actually received triangles.</summary>
        public int SubmeshCount => _tris.Count;
        public bool SubmeshUsed(int s) => s < _tris.Count && _tris[s].Count > 0;
    }
}
