using UnityEngine;

namespace HorseCity.EditorTools
{
    /// <summary>Small reusable props, all built from a handful of boxes.</summary>
    public class Furniture
    {
        readonly CityAssets A;
        public Furniture(CityAssets a) { A = a; }

        public void Desk(Part p, Vector3 at, float yaw, bool screen)
        {
            p.Local(at, yaw, () =>
            {
                p.Box(A.WoodDark, new Vector3(0, 0.74f, 0), new Vector3(1.6f, 0.06f, 0.8f));
                p.Box(A.Metal, new Vector3(-0.75f, 0.37f, 0), new Vector3(0.05f, 0.74f, 0.75f));
                p.Box(A.Metal, new Vector3(0.75f, 0.37f, 0), new Vector3(0.05f, 0.74f, 0.75f));
                p.Box(A.Metal, new Vector3(0.45f, 0.4f, 0.05f), new Vector3(0.5f, 0.6f, 0.65f));
                if (screen)
                {
                    p.Box(A.Screen, new Vector3(-0.2f, 1.03f, 0.2f), new Vector3(0.55f, 0.35f, 0.04f));
                    p.Box(A.Metal, new Vector3(-0.2f, 0.82f, 0.25f), new Vector3(0.08f, 0.1f, 0.08f));
                    p.Box(A.Trim, new Vector3(-0.2f, 0.78f, -0.15f), new Vector3(0.45f, 0.02f, 0.15f));
                }
                p.Box(A.Paper, new Vector3(0.45f, 0.78f, -0.1f), new Vector3(0.3f, 0.03f, 0.22f));
            });
        }

        public void Chair(Part p, Vector3 at, float yaw)
        {
            p.Local(at, yaw, () =>
            {
                p.Box(A.Fabric, new Vector3(0, 0.46f, 0), new Vector3(0.48f, 0.08f, 0.48f));
                p.Box(A.Fabric, new Vector3(0, 0.8f, 0.22f), new Vector3(0.46f, 0.6f, 0.06f));
                p.Box(A.Metal, new Vector3(0, 0.22f, 0), new Vector3(0.06f, 0.44f, 0.06f));
                p.Box(A.Metal, new Vector3(0, 0.03f, 0), new Vector3(0.5f, 0.05f, 0.5f));
            });
        }

        public void Bench(Part p, Vector3 at, float yaw, float len, Material m = null)
        {
            m = m ?? A.Wood;
            p.Local(at, yaw, () =>
            {
                p.Box(m, new Vector3(0, 0.45f, 0), new Vector3(len, 0.08f, 0.45f));
                p.Box(m, new Vector3(0, 0.8f, 0.2f), new Vector3(len, 0.4f, 0.06f));
                p.Box(A.Metal, new Vector3(-len * 0.45f, 0.22f, 0), new Vector3(0.06f, 0.44f, 0.4f));
                p.Box(A.Metal, new Vector3(len * 0.45f, 0.22f, 0), new Vector3(0.06f, 0.44f, 0.4f));
            });
        }

        public void Shelf(Part p, Vector3 at, float yaw, float len, float h, Material frame, Material goods)
        {
            p.Local(at, yaw, () =>
            {
                p.Box(frame, new Vector3(0, h * 0.5f, 0.2f), new Vector3(len, h, 0.04f));
                p.Box(frame, new Vector3(-len * 0.5f, h * 0.5f, 0), new Vector3(0.04f, h, 0.45f));
                p.Box(frame, new Vector3(len * 0.5f, h * 0.5f, 0), new Vector3(0.04f, h, 0.45f));
                int levels = Mathf.Max(2, Mathf.RoundToInt(h / 0.45f));
                for (int i = 0; i < levels; i++)
                {
                    float y = 0.1f + i * (h - 0.15f) / (levels - 1);
                    p.Box(frame, new Vector3(0, y, 0), new Vector3(len, 0.03f, 0.45f));
                    if (goods != null && i < levels - 1)
                        for (float x = -len * 0.5f + 0.2f; x < len * 0.5f - 0.15f; x += 0.35f)
                            if (Mathf.Repeat(x * 7.3f + i * 3.1f, 1f) > 0.3f)
                                p.Box(goods, new Vector3(x, y + 0.13f, 0), new Vector3(0.22f, 0.22f, 0.3f));
                }
            });
        }

        public void Counter(Part p, Vector3 at, float yaw, float len, Material body, Material top)
        {
            p.Local(at, yaw, () =>
            {
                p.Box(body, new Vector3(0, 0.52f, 0), new Vector3(len, 1.04f, 0.7f));
                p.Box(top, new Vector3(0, 1.07f, -0.05f), new Vector3(len + 0.1f, 0.06f, 0.85f));
            });
        }

        public void Locker(Part p, Vector3 at, float yaw, int doors, Material m)
        {
            p.Local(at, yaw, () =>
            {
                float w = 0.45f;
                for (int i = 0; i < doors; i++)
                {
                    float x = (i - (doors - 1) * 0.5f) * w;
                    p.Box(m, new Vector3(x, 0.95f, 0), new Vector3(w - 0.02f, 1.9f, 0.5f));
                    p.Box(A.Chrome, new Vector3(x + 0.15f, 1.1f, -0.26f), new Vector3(0.03f, 0.15f, 0.03f));
                }
            });
        }

        public void HayBale(Part p, Vector3 at, float yaw)
        {
            p.Local(at, yaw, () =>
            {
                p.Box(A.Hay, new Vector3(0, 0.25f, 0), new Vector3(1.1f, 0.5f, 0.55f));
                p.Box(A.Wood, new Vector3(-0.3f, 0.25f, 0), new Vector3(0.03f, 0.52f, 0.57f));
                p.Box(A.Wood, new Vector3(0.3f, 0.25f, 0), new Vector3(0.03f, 0.52f, 0.57f));
            });
        }

        public void Trough(Part p, Vector3 at, float yaw, float len = 2f)
        {
            p.Local(at, yaw, () =>
            {
                p.Box(A.WoodDark, new Vector3(0, 0.08f, 0), new Vector3(len, 0.16f, 0.7f));
                p.Box(A.WoodDark, new Vector3(0, 0.4f, -0.32f), new Vector3(len, 0.6f, 0.06f));
                p.Box(A.WoodDark, new Vector3(0, 0.4f, 0.32f), new Vector3(len, 0.6f, 0.06f));
                p.Box(A.WoodDark, new Vector3(-len * 0.5f, 0.4f, 0), new Vector3(0.06f, 0.6f, 0.7f));
                p.Box(A.WoodDark, new Vector3(len * 0.5f, 0.4f, 0), new Vector3(0.06f, 0.6f, 0.7f));
                p.Box(A.Water, new Vector3(0, 0.6f, 0), new Vector3(len - 0.1f, 0.02f, 0.58f));
            });
        }

        public void HitchingRail(Part p, Vector3 at, float yaw, float len = 3f)
        {
            p.Local(at, yaw, () =>
            {
                p.Box(A.WoodDark, new Vector3(-len * 0.5f, 0.55f, 0), new Vector3(0.14f, 1.1f, 0.14f));
                p.Box(A.WoodDark, new Vector3(len * 0.5f, 0.55f, 0), new Vector3(0.14f, 1.1f, 0.14f));
                p.Box(A.Wood, new Vector3(0, 1.05f, 0), new Vector3(len + 0.3f, 0.12f, 0.12f));
            });
        }

        public void Crate(Part p, Vector3 at, float yaw, float s = 0.8f)
        {
            p.Local(at, yaw, () =>
            {
                p.Box(A.Wood, new Vector3(0, s * 0.5f, 0), new Vector3(s, s, s));
                p.Box(A.WoodDark, new Vector3(0, s * 0.5f, -s * 0.5f - 0.01f), new Vector3(s * 0.15f, s, 0.02f));
            });
        }

        public void Barrel(Part p, Vector3 at, Material m = null)
        {
            p.Cylinder(m ?? A.Rust, at, 0.3f, 0.9f, 8);
        }

        public void Bucket(Part p, Vector3 at) => p.Cylinder(A.Metal, at, 0.16f, 0.3f, 6);

        public void HangingLamp(Part p, Vector3 at, float cord, Material bulb)
        {
            p.Box(A.Metal, at + Vector3.up * (cord * 0.5f), new Vector3(0.02f, cord, 0.02f));
            p.Box(A.Metal, at + Vector3.up * 0.05f, new Vector3(0.45f, 0.12f, 0.45f));
            p.Box(bulb, at - Vector3.up * 0.04f, new Vector3(0.3f, 0.06f, 0.3f));
        }

        /// <summary>Post and rail fence between two points.</summary>
        public void Fence(Part p, Material m, Vector3 a, Vector3 b, float height = 1.3f, float spacing = 2.5f, int rails = 3)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            int n = Mathf.Max(1, Mathf.CeilToInt(len / spacing));
            for (int i = 0; i <= n; i++)
            {
                Vector3 pt = Vector3.Lerp(a, b, (float)i / n);
                p.Box(m, pt + Vector3.up * (height * 0.5f), new Vector3(0.14f, height, 0.14f));
            }
            for (int r = 0; r < rails; r++)
            {
                float y = height * (0.3f + 0.65f * r / Mathf.Max(1, rails - 1));
                p.Beam(m, a + Vector3.up * y, b + Vector3.up * y, 0.08f);
            }
        }

        /// <summary>Row of vertical bars (cells, stall tops, gates).</summary>
        public void Bars(Part p, Material m, Vector3 a, Vector3 b, float y0, float y1, float gap = 0.18f, float t = 0.04f)
        {
            Vector3 d = b - a;
            int n = Mathf.Max(1, Mathf.RoundToInt(d.magnitude / gap));
            for (int i = 0; i <= n; i++)
            {
                Vector3 pt = Vector3.Lerp(a, b, (float)i / n);
                p.Box(m, pt + Vector3.up * ((y0 + y1) * 0.5f), new Vector3(t, y1 - y0, t));
            }
            p.Beam(m, a + Vector3.up * y0, b + Vector3.up * y0, 0.07f);
            p.Beam(m, a + Vector3.up * y1, b + Vector3.up * y1, 0.07f);
        }

        public void Tree(Part p, Vector3 at, float scale, int seed, Material leaves = null)
        {
            p.Cylinder(A.Bark, at, 0.18f * scale, 2.6f * scale, 5, false);
            p.Blob(leaves ?? A.Leaves, at + Vector3.up * 3.6f * scale, new Vector3(2.2f, 1.7f, 2.2f) * scale, seed);
            p.Blob(leaves ?? A.Leaves, at + new Vector3(0.8f, 4.4f, 0.4f) * scale, new Vector3(1.4f, 1.2f, 1.4f) * scale, seed + 1);
        }

        /// <summary>Indoor plant, about 1.3m tall, so it fits under any ceiling.</summary>
        public void PottedPlant(Part p, Vector3 at, int seed)
        {
            p.Cylinder(A.Concrete, at, 0.25f, 0.45f, 8);
            p.Box(A.Dirt, at + Vector3.up * 0.44f, new Vector3(0.36f, 0.02f, 0.36f));
            p.Box(A.Bark, at + Vector3.up * 0.65f, new Vector3(0.05f, 0.4f, 0.05f));
            p.Blob(A.Leaves, at + Vector3.up * 1.0f, new Vector3(0.4f, 0.35f, 0.4f), seed);
        }

        public void Planter(Part p, Vector3 at, float size, int seed)
        {
            p.Box(A.Concrete, at + Vector3.up * 0.3f, new Vector3(size, 0.6f, size));
            p.Box(A.Dirt, at + Vector3.up * 0.61f, new Vector3(size - 0.2f, 0.02f, size - 0.2f));
            Tree(p, at + Vector3.up * 0.6f, 0.9f, seed);
        }
    }
}
