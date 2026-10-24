using System.Collections.Generic;
using UnityEngine;
using O = HorseCity.EditorTools.Part.Opening;

namespace HorseCity.EditorTools
{
    public struct LightSpec
    {
        public Vector3 pos;
        public Color color;
        public float range, intensity;
        public LightSpec(Vector3 p, Color c, float r, float i) { pos = p; color = c; range = r; intensity = i; }
    }

    /// <summary>Output of a landmark build, all in the landmark's local frame (front faces -Z).</summary>
    public class Landmark
    {
        public string name;
        public Part shell = new Part();     // walls, roof, signs (casts shadows, has collider)
        public Part interior = new Part();  // furniture and fittings (no shadows, has collider)
        public Part yard = new Part();      // outdoor fences, troughs, hay (no shadows)
        public List<LightSpec> interiorLights = new List<LightSpec>();
        public List<LightSpec> outdoorLights = new List<LightSpec>();
        public Bounds interiorBounds;
        public List<(QuestPointKind kind, Vector3 pos, string note)> points = new List<(QuestPointKind, Vector3, string)>();
        /// <summary>Lot rectangle (center, size) in local space, used to keep generic buildings away.</summary>
        public Rect lot;
    }

    /// <summary>
    /// Story locations. The vet, horse auction house, police station and police stable get
    /// full interiors. The central tower and the hidden weapon shop are exterior only.
    /// </summary>
    public class Landmarks
    {
        readonly CityAssets A;
        readonly Furniture F;
        static readonly Color ClinicWhite = new Color(0.92f, 0.97f, 1f);
        static readonly Color Warm = new Color(1f, 0.8f, 0.55f);
        static readonly Color Lantern = new Color(1f, 0.68f, 0.38f);

        public Landmarks(CityAssets a) { A = a; F = new Furniture(a); }

        // Shorthand for walls along X or Z with openings given in world-local coordinates.
        static O D(float at, float w, float h) => O.Door(at, w, h);
        static O W(float at, float w, float sill, float top, Material g = null) => O.Window(at, w, sill, top, g);

        void Floor(Part p, Material m, float x0, float x1, float z0, float z1, float y = 0.2f)
            => p.MinMax(m, new Vector3(x0, y - 0.2f, z0), new Vector3(x1, y, z1));

        void FlatRoof(Part shell, float w, float d, float h, float parapet, Material outside)
        {
            // Ceiling and roof slab stop just inside the outer walls and parapet. If their edges sat on
            // the wall's outer face, the two surfaces would fight over the same pixels and flicker.
            float t = 0.3f, ci = t, ri = 0.05f;
            shell.MinMax(A.Ceiling, new Vector3(-w * 0.5f + ci, h - 0.15f, -d * 0.5f + ci), new Vector3(w * 0.5f - ci, h, d * 0.5f - ci));
            shell.MinMax(A.RoofConcrete, new Vector3(-w * 0.5f + ri, h, -d * 0.5f + ri), new Vector3(w * 0.5f - ri, h + 0.25f, d * 0.5f - ri), 6f);
            shell.MinMax(outside, new Vector3(-w * 0.5f, h, -d * 0.5f), new Vector3(w * 0.5f, h + parapet, -d * 0.5f + t), 3f);
            shell.MinMax(outside, new Vector3(-w * 0.5f, h, d * 0.5f - t), new Vector3(w * 0.5f, h + parapet, d * 0.5f), 3f);
            shell.MinMax(outside, new Vector3(-w * 0.5f, h, -d * 0.5f), new Vector3(-w * 0.5f + t, h + parapet, d * 0.5f), 3f);
            shell.MinMax(outside, new Vector3(w * 0.5f - t, h, -d * 0.5f), new Vector3(w * 0.5f, h + parapet, d * 0.5f), 3f);
        }

        // Openings are given by their x (for walls along X) or z (for walls along Z); these convert to distance along the wall.
        static O[] Along(float start, params O[] o)
        {
            for (int i = 0; i < o.Length; i++) o[i].center -= start;
            return o;
        }

        void WallX(Part p, Material outM, Material inM, float z, float x0, float x1, float h, Vector3 inside, params O[] o)
            => p.Wall(outM, inM, new Vector3(x0, 0, z), new Vector3(x1, 0, z), h, 0.3f, inside, Along(x0, o));

        void WallZ(Part p, Material outM, Material inM, float x, float z0, float z1, float h, Vector3 inside, params O[] o)
            => p.Wall(outM, inM, new Vector3(x, 0, z0), new Vector3(x, 0, z1), h, 0.3f, inside, Along(z0, o));

        // ================================================================== VET
        public Landmark Vet()
        {
            var L = new Landmark { name = "Vet_EquineClinic" };
            Part s = L.shell, i = L.interior, y = L.yard;
            const float H = 5.5f;
            Material ext = A.Plaster, inn = A.WallClinic;
            Vector3 c = Vector3.zero;

            Floor(i, A.FloorTile, -16, -4, -11, 0);
            Floor(i, A.RubberMat, -4, 16, -11, 11);
            Floor(i, A.FloorVinyl, -16, -4, 0, 3);
            Floor(i, A.RubberMat, -16, -4, 3, 11);

            // outer walls
            WallX(s, ext, inn, -11, -16, 16, H, c,
                W(-14, 2f, 1f, 2.4f, A.WindowLit), D(-10, 2f, 2.6f), W(-6.5f, 2.4f, 1f, 2.4f, A.WindowLit),
                D(6, 4.2f, 4.2f), W(12.5f, 2.4f, 1.6f, 3.2f, A.WindowDark));
            WallX(s, ext, inn, 11, -16, 16, H, c, D(6, 4.2f, 4.2f), W(12.5f, 2.4f, 1.6f, 3.2f, A.WindowDark), W(-10, 2f, 1.8f, 2.6f, A.WindowDark));
            WallZ(s, ext, inn, -16, -11, 11, H, c, W(-5, 2f, 1f, 2.4f, A.WindowDark), W(7, 1.6f, 1.8f, 2.6f, A.WindowDark));
            WallZ(s, ext, inn, 16, -11, 11, H, c, W(-5, 3f, 1.4f, 3.4f, A.WindowDark), W(5, 3f, 1.4f, 3.4f, A.WindowDark));
            FlatRoof(s, 32.3f, 22.3f, H, 1.2f, ext);

            // partitions
            WallZ(i, inn, inn, -4, -11, 0, H, new Vector3(-10, 0, -5), D(-5, 1.6f, 2.4f));
            WallX(i, inn, inn, 0, -16, -4, H, new Vector3(-10, 0, -5), D(-10, 1.6f, 2.4f));
            WallZ(i, inn, inn, -4, 0, 11, H, new Vector3(-10, 0, 5), D(1.5f, 2.6f, 3.2f));

            // reception and waiting room
            F.Counter(i, new Vector3(-9.5f, 0.2f, -4f), 0, 5f, A.FenceWhite, A.WoodDark);
            F.Desk(i, new Vector3(-9f, 0.2f, -2.6f), 180, true);
            F.Chair(i, new Vector3(-9f, 0.2f, -1.8f), 180);
            F.Bench(i, new Vector3(-14f, 0.2f, -10.3f), 180, 3f);
            F.Bench(i, new Vector3(-15.4f, 0.2f, -6.5f), 90, 3f);
            F.Shelf(i, new Vector3(-15.6f, 0.2f, -2f), 90, 3f, 2f, A.FenceWhite, A.Paper);
            F.Shelf(i, new Vector3(-6f, 0.2f, -0.4f), 180, 3f, 1.8f, A.FenceWhite, A.SignGreen);
            i.Box(A.Paper, new Vector3(-12f, 2f, -0.2f), new Vector3(2f, 1.2f, 0.05f)); // notice board
            F.PottedPlant(i, new Vector3(-5f, 0.2f, -10f), 7);

            // treatment hall: examination stocks, hoist, cabinets, x-ray viewer
            Vector3 st = new Vector3(6, 0.2f, 2);
            for (int k = 0; k < 4; k++)
                i.Box(A.Chrome, st + new Vector3((k % 2 - 0.5f) * 1.1f, 0.9f, (k / 2 - 0.5f) * 2.4f), new Vector3(0.08f, 1.8f, 0.08f));
            foreach (float ry in new[] { 1.0f, 1.6f })
            {
                i.Box(A.Chrome, st + new Vector3(-0.55f, ry, 0), new Vector3(0.06f, 0.06f, 2.5f));
                i.Box(A.Chrome, st + new Vector3(0.55f, ry, 0), new Vector3(0.06f, 0.06f, 2.5f));
            }
            i.Box(A.Metal, new Vector3(6, 5.1f, 0), new Vector3(0.3f, 0.3f, 20f));       // hoist rail
            i.Box(A.Metal, new Vector3(6, 4.6f, 2), new Vector3(0.6f, 0.6f, 0.6f));      // hoist trolley
            i.Beam(A.Metal, new Vector3(6, 4.3f, 2), new Vector3(6, 2.6f, 2), 0.03f);
            i.Box(A.LampBulb, new Vector3(6, 5.25f, 0.5f), new Vector3(1.2f, 0.08f, 1.2f));
            i.Box(A.LampBulb, new Vector3(6, 5.25f, -6f), new Vector3(1.2f, 0.08f, 1.2f));
            for (float z = -9; z <= 8; z += 2.2f)
                F.Locker(i, new Vector3(15.5f, 0.2f, z), 90, 4, A.FenceWhite);
            i.Box(A.Screen, new Vector3(15.6f, 3f, 0f), new Vector3(0.05f, 1f, 1.8f)); // x-ray light box
            F.Counter(i, new Vector3(-1.5f, 0.2f, 8.5f), 90, 4f, A.FenceWhite, A.Chrome);  // sink bench
            i.Box(A.Chrome, new Vector3(1.5f, 0.6f, -6.5f), new Vector3(1.2f, 0.05f, 0.6f)); // instrument cart
            i.Box(A.Chrome, new Vector3(1.5f, 0.95f, -6.5f), new Vector3(1.2f, 0.05f, 0.6f));
            i.Box(A.Metal, new Vector3(1.5f, 0.6f, -6.5f), new Vector3(0.05f, 0.9f, 0.05f));
            i.MinMax(A.Fabric, new Vector3(9, 0.2f, -10), new Vector3(14, 0.5f, -5)); // padded recovery mat
            F.Bucket(i, new Vector3(4.5f, 0.2f, 3f));

            // recovery stalls
            for (int k = 0; k < 3; k++)
            {
                float x0 = -16 + k * 4;
                if (k > 0) i.MinMax(A.WoodDark, new Vector3(x0 - 0.08f, 0.2f, 3f), new Vector3(x0 + 0.08f, 2.4f, 11f));
                i.MinMax(A.WoodDark, new Vector3(x0, 0.2f, 2.9f), new Vector3(x0 + 1.2f, 1.3f, 3.1f));
                i.MinMax(A.WoodDark, new Vector3(x0 + 2.8f, 0.2f, 2.9f), new Vector3(x0 + 4f, 1.3f, 3.1f));
                F.Bars(i, A.Metal, new Vector3(x0, 0, 3f), new Vector3(x0 + 1.2f, 0, 3f), 1.3f, 2.4f);
                F.Bars(i, A.Metal, new Vector3(x0 + 2.8f, 0, 3f), new Vector3(x0 + 4f, 0, 3f), 1.3f, 2.4f);
                i.MinMax(A.Hay, new Vector3(x0 + 0.1f, 0.2f, 3.2f), new Vector3(x0 + 3.9f, 0.3f, 10.8f));
                F.Trough(i, new Vector3(x0 + 2f, 0.2f, 10.3f), 0, 1.6f);
                F.Bucket(i, new Vector3(x0 + 3.4f, 0.2f, 9.6f));
            }
            i.MinMax(A.Paper, new Vector3(-15.95f, 1.6f, 0.6f), new Vector3(-15.85f, 2.4f, 2.4f)); // patient chart board

            // exterior: canopy, sign, hitching rail, paddock
            s.MinMax(A.AwningGreen, new Vector3(2.8f, 4.4f, -13f), new Vector3(9.2f, 4.6f, -11f));
            BlockFont.Write(s, A.SignGreen, "VET CLINIC", new Vector3(-10f, 4.25f, -11.25f), 0.15f);
            BlockFont.Write(s, A.CrossGreen, "+", new Vector3(12.5f, 4.4f, -11.25f), 0.25f);
            BlockFont.Write(s, A.CrossGreen, "+", new Vector3(0f, H + 0.6f, -11.25f), 0.2f);
            F.HitchingRail(y, new Vector3(-2f, 0, -13.5f), 0, 4f);
            F.Trough(y, new Vector3(-2f, 0, -14.6f), 0, 2.4f);
            F.Fence(y, A.FenceWhite, new Vector3(-16, 0, 11.3f), new Vector3(-16, 0, 26));
            F.Fence(y, A.FenceWhite, new Vector3(16, 0, 11.3f), new Vector3(16, 0, 26));
            F.Fence(y, A.FenceWhite, new Vector3(-16, 0, 26), new Vector3(-3, 0, 26));
            F.Fence(y, A.FenceWhite, new Vector3(3, 0, 26), new Vector3(16, 0, 26));
            y.MinMax(A.Dirt, new Vector3(-16, 0, 11.2f), new Vector3(16, 0.04f, 26));
            y.MinMax(A.WoodDark, new Vector3(9, 2.6f, 18), new Vector3(15, 2.75f, 25)); // shade shelter
            for (int k = 0; k < 4; k++) y.Box(A.WoodDark, new Vector3(9.2f + (k % 2) * 5.6f, 1.3f, 18.2f + (k / 2) * 6.6f), new Vector3(0.15f, 2.6f, 0.15f));
            F.HayBale(y, new Vector3(-12, 0, 20), 20);
            F.HayBale(y, new Vector3(-11, 0, 21.2f), -10);
            F.Trough(y, new Vector3(0, 0, 24.6f), 0);

            L.interiorLights.Add(new LightSpec(new Vector3(-10, 4.8f, -5), Warm, 10f, 1.3f));
            L.interiorLights.Add(new LightSpec(new Vector3(6, 4.9f, 1), ClinicWhite, 13f, 1.8f));
            L.interiorLights.Add(new LightSpec(new Vector3(6, 4.9f, -7), ClinicWhite, 11f, 1.3f));
            L.interiorLights.Add(new LightSpec(new Vector3(-10, 4.6f, 6), Warm, 10f, 1.1f));
            L.outdoorLights.Add(new LightSpec(new Vector3(-10, 3.2f, -12f), Warm, 9f, 1.2f));
            L.outdoorLights.Add(new LightSpec(new Vector3(6, 4.2f, -13f), ClinicWhite, 10f, 1.2f));
            L.interiorBounds = new Bounds(new Vector3(0, 3, 0), new Vector3(32, 6, 22));
            L.points.Add((QuestPointKind.Vet, new Vector3(-10, 0, -13.5f), "Vet reception entrance"));
            L.points.Add((QuestPointKind.Vet, new Vector3(6, 0, -14.5f), "Horse entrance to the treatment hall"));
            L.lot = new Rect(-18, -16, 36, 44);
            return L;
        }

        // ================================================================== HORSE AUCTION HOUSE
        public Landmark AuctionHouse()
        {
            var L = new Landmark { name = "HorseSeller_AuctionHouse" };
            Part s = L.shell, i = L.interior, y = L.yard;
            const float H = 8f;
            Material ext = A.Wood, inn = A.WallInterior;
            Vector3 c = new Vector3(0, 0, 5);

            Floor(i, A.FloorTile, -28, 28, -22, -15);
            Floor(i, A.Concrete, -28, 28, -15, 22);

            WallX(s, ext, inn, -22, -28, 28, H, c,
                W(-22, 3, 1.2f, 3f, A.WindowLit), W(-15, 3, 1.2f, 3f, A.WindowLit), D(-8, 3, 3.2f),
                D(8, 3, 3.2f), W(15, 3, 1.2f, 3f, A.WindowLit), W(22, 3, 1.2f, 3f, A.WindowLit));
            WallX(s, ext, inn, 22, -28, 28, H, c, D(-12, 4.5f, 4.2f), D(12, 4.5f, 4.2f));
            WallZ(s, ext, inn, -28, -22, 22, H, c, W(-12, 4, 5f, 7f), W(-2, 4, 5f, 7f), W(8, 4, 5f, 7f), W(17, 4, 5f, 7f));
            WallZ(s, ext, inn, 28, -22, 22, H, c, W(-12, 4, 5f, 7f), W(-2, 4, 5f, 7f), W(8, 4, 5f, 7f), W(17, 4, 5f, 7f));
            s.Gable(A.RoofMetal, new Vector3(0, H, 0), 56.3f, 44.3f, 6f, 0.8f);
            for (float x = -24; x <= 24; x += 8) // roof trusses seen from inside
                s.Beam(A.WoodDark, new Vector3(x, H - 0.3f, -22), new Vector3(x, H - 0.3f, 22), 0.3f);

            // lobby partition with a big open viewing window into the hall
            WallX(i, inn, inn, -15, -28, 28, H, new Vector3(0, 0, -18), D(-8, 3.5f, 3.2f), D(8, 3.5f, 3.2f), W(0, 8, 1f, 3f));
            WallZ(i, inn, inn, 18, -22, -15, 3.6f, new Vector3(23, 0, -18), D(-18.5f, 1.4f, 2.4f));
            i.MinMax(A.Ceiling, new Vector3(18, 3.6f, -22), new Vector3(28, 3.75f, -15));

            F.Counter(i, new Vector3(-16f, 0.2f, -18.5f), 0, 8f, A.WoodDark, A.Wood);
            F.Desk(i, new Vector3(-17f, 0.2f, -17.2f), 180, true);
            F.Desk(i, new Vector3(-14f, 0.2f, -17.2f), 180, false);
            F.Bench(i, new Vector3(0, 0.2f, -21.3f), 180, 5f);
            F.Bench(i, new Vector3(-26.8f, 0.2f, -18.5f), 90, 4f);
            i.MinMax(A.Paper, new Vector3(-6, 1.2f, -21.84f), new Vector3(-2, 2.8f, -21.8f)); // horse listings board
            i.MinMax(A.Paper, new Vector3(2, 1.2f, -21.84f), new Vector3(6, 2.8f, -21.8f));
            F.Desk(i, new Vector3(23, 0.2f, -19), 0, true); // office
            F.Chair(i, new Vector3(23, 0.2f, -19.9f), 0);
            F.Shelf(i, new Vector3(27.6f, 0.2f, -18.5f), -90, 3f, 2f, A.WoodDark, A.Paper);

            // sale ring
            Vector3 ring = new Vector3(0, 0.2f, 3);
            const float rr = 8f;
            i.Cylinder(A.Sand, ring - Vector3.up * 0.15f, rr, 0.2f, 20);
            const int seg = 20;
            for (int k = 0; k < seg; k++)
            {
                if (k == 2 || k == 7) continue; // gates toward the back entry/exit lanes
                float a0 = k * Mathf.PI * 2 / seg, a1 = (k + 1) * Mathf.PI * 2 / seg;
                Vector3 p0 = ring + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * rr;
                Vector3 p1 = ring + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * rr;
                F.Fence(i, A.FenceWhite, p0, p1, 1.4f, 3f, 3);
            }
            // tiered bleachers on both sides
            for (int t = 0; t < 4; t++)
            {
                float h = 0.45f * (t + 1);
                i.MinMax(A.Wood, new Vector3(-27.7f, 0.2f, -12), new Vector3(-12.5f - t * 3.4f, 0.2f + h, 16), 2f);
                i.MinMax(A.Wood, new Vector3(12.5f + t * 3.4f, 0.2f, -12), new Vector3(27.7f, 0.2f + h, 16), 2f);
            }
            i.Beam(A.Metal, new Vector3(-12.3f, 1.2f, -12), new Vector3(-12.3f, 1.2f, 16), 0.06f);
            i.Beam(A.Metal, new Vector3(12.3f, 1.2f, -12), new Vector3(12.3f, 1.2f, 16), 0.06f);
            // auctioneer booth
            i.MinMax(A.WoodDark, new Vector3(-3, 0.2f, 13.5f), new Vector3(3, 1.4f, 17));
            i.MinMax(A.Wood, new Vector3(-1.5f, 1.4f, 13.6f), new Vector3(1.5f, 2.5f, 14.2f));
            i.Box(A.Gold, new Vector3(0, 2.56f, 13.9f), new Vector3(0.3f, 0.12f, 0.3f)); // the gavel block
            F.Chair(i, new Vector3(0, 1.4f, 15.2f), 180);
            F.Chair(i, new Vector3(-1.8f, 1.4f, 15.6f), 180);
            F.Chair(i, new Vector3(1.8f, 1.4f, 15.6f), 180);
            BlockFont.Write(i, A.SignYellow, "AUCTION", new Vector3(0, 5.6f, 21.7f), 0.3f);
            i.MinMax(A.Screen, new Vector3(-6, 3.2f, 21.75f), new Vector3(6, 4.4f, 21.8f)); // lot / price board
            // entry and exit lanes from the back doors to the ring gates
            F.Fence(i, A.WoodDark, new Vector3(-14.2f, 0.2f, 21.8f), new Vector3(-8.5f, 0.2f, 8.5f), 1.6f);
            F.Fence(i, A.WoodDark, new Vector3(-9.8f, 0.2f, 21.8f), new Vector3(-5.8f, 0.2f, 10.2f), 1.6f);
            F.Fence(i, A.WoodDark, new Vector3(14.2f, 0.2f, 21.8f), new Vector3(8.5f, 0.2f, 8.5f), 1.6f);
            F.Fence(i, A.WoodDark, new Vector3(9.8f, 0.2f, 21.8f), new Vector3(5.8f, 0.2f, 10.2f), 1.6f);
            for (int k = 0; k < 5; k++)
                F.HangingLamp(i, new Vector3(-10 + k * 5, 6.6f, 3), 1.1f, A.LampWarm);

            // front: canopy, sign, hitching rails
            s.MinMax(A.WoodDark, new Vector3(-12, 3.6f, -25), new Vector3(12, 3.8f, -22));
            for (int k = 0; k < 4; k++) s.Box(A.WoodDark, new Vector3(-11.6f + k * 7.73f, 1.8f, -24.7f), new Vector3(0.25f, 3.6f, 0.25f));
            BlockFont.Write(s, A.SignYellow, "HORSE AUCTION", new Vector3(0, 5.8f, -22.25f), 0.28f);
            F.HitchingRail(y, new Vector3(-18, 0, -25.5f), 0, 5f);
            F.HitchingRail(y, new Vector3(18, 0, -25.5f), 0, 5f);
            F.Trough(y, new Vector3(-18, 0, -26.6f), 0, 3f);
            F.Trough(y, new Vector3(18, 0, -26.6f), 0, 3f);

            // holding pens behind the hall
            y.MinMax(A.Dirt, new Vector3(-28, 0, 22.2f), new Vector3(28, 0.04f, 42));
            float[] px = { -28, -9.3f, 9.3f, 28 };
            for (int k = 0; k < 4; k++) F.Fence(y, A.WoodDark, new Vector3(px[k], 0, 22.2f), new Vector3(px[k], 0, 42));
            F.Fence(y, A.WoodDark, new Vector3(-28, 0, 42), new Vector3(28, 0, 42));
            for (int k = 0; k < 3; k++)
            {
                float cx = (px[k] + px[k + 1]) * 0.5f;
                F.Trough(y, new Vector3(cx, 0, 40.8f), 0, 3f);
                F.HayBale(y, new Vector3(cx - 4, 0, 36), 30 * k);
                F.HayBale(y, new Vector3(cx + 3, 0, 33), -20 * k);
            }
            y.MinMax(A.Concrete, new Vector3(-30, 0, 30), new Vector3(-28.2f, 1.1f, 36)); // loading ramp

            L.interiorLights.Add(new LightSpec(new Vector3(-5, 6.3f, 3), Warm, 16f, 1.6f));
            L.interiorLights.Add(new LightSpec(new Vector3(5, 6.3f, 3), Warm, 16f, 1.6f));
            L.interiorLights.Add(new LightSpec(new Vector3(-19, 5.5f, 3), Warm, 14f, 1.1f));
            L.interiorLights.Add(new LightSpec(new Vector3(19, 5.5f, 3), Warm, 14f, 1.1f));
            L.interiorLights.Add(new LightSpec(new Vector3(-12, 3.2f, -18.5f), Warm, 11f, 1.2f));
            L.interiorLights.Add(new LightSpec(new Vector3(8, 3.2f, -18.5f), Warm, 11f, 1.2f));
            L.outdoorLights.Add(new LightSpec(new Vector3(0, 3.4f, -24f), Lantern, 14f, 1.4f));
            L.outdoorLights.Add(new LightSpec(new Vector3(0, 4.5f, 32f), Lantern, 18f, 1.2f));
            L.interiorBounds = new Bounds(new Vector3(0, 4, 0), new Vector3(56, 9, 44));
            L.points.Add((QuestPointKind.HorseSeller, new Vector3(0, 0, -26.5f), "Horse auction house entrance"));
            L.points.Add((QuestPointKind.HorseSeller, new Vector3(0, 0, 32f), "Holding pens (horse pickup)"));
            L.lot = new Rect(-31, -28, 62, 72);
            return L;
        }

        // ================================================================== POLICE STATION
        public Landmark PoliceStation()
        {
            var L = new Landmark { name = "PoliceStation" };
            Part s = L.shell, i = L.interior, y = L.yard;
            const float H = 4.2f;
            Material ext = A.Plaster, inn = A.WallPolice;
            Vector3 c = Vector3.zero;

            Floor(i, A.FloorTile, -20, 20, -15, 15);

            WallX(s, ext, inn, -15, -20, 20, H, c,
                W(-16, 2, 1f, 2.6f, A.WindowLit), W(-12, 2, 1f, 2.6f, A.WindowLit), W(-5, 2, 1f, 2.6f, A.WindowLit),
                D(0, 3f, 2.8f), W(5, 2, 1f, 2.6f, A.WindowLit), W(12, 1.2f, 2.1f, 2.7f, A.WindowDark), W(17, 1.2f, 2.1f, 2.7f, A.WindowDark));
            WallX(s, ext, inn, 15, -20, 20, H, c, W(-14, 2, 1f, 2.6f, A.WindowDark), W(-8, 2, 1f, 2.6f, A.WindowDark), D(6, 2.4f, 2.6f), W(14, 2, 1f, 2.6f, A.WindowDark));
            WallZ(s, ext, inn, -20, -15, 15, H, c, W(-10, 2, 1f, 2.6f, A.WindowDark), W(-4, 2, 1f, 2.6f, A.WindowDark), W(9, 2, 1f, 2.6f, A.WindowDark), W(12, 2, 1f, 2.6f, A.WindowDark));
            WallZ(s, ext, inn, 20, -15, 15, H, c, W(-12.5f, 1f, 2.1f, 2.7f), W(-7.5f, 1f, 2.1f, 2.7f), W(-2.5f, 1f, 2.1f, 2.7f), W(8, 2, 1f, 2.6f, A.WindowDark));
            // ground floor ceiling, then a solid upper office floor (exterior only)
            s.MinMax(A.Ceiling, new Vector3(-20, H - 0.15f, -15), new Vector3(20, H, 15));
            s.Box(A.Office, new Vector3(0, H + 2.1f, 0), new Vector3(40.3f, 4.2f, 30.3f), CityAssets.OfficeTile);
            FlatRoof(s, 40.3f, 30.3f, H * 2f, 1f, A.Plaster);
            s.MinMax(A.PoliceBlue, new Vector3(-20.3f, H, -15.35f), new Vector3(20.3f, H + 0.5f, 15.35f)); // blue belt

            // partitions
            Vector3 lobby = new Vector3(0, 0, -10);
            WallX(i, inn, inn, -5, -8, 8, H, lobby, D(0, 1.8f, 2.4f));
            WallZ(i, inn, inn, -8, -15, 5, H, lobby, D(-10, 1.6f, 2.4f), D(0, 1.6f, 2.4f));
            WallZ(i, inn, inn, 8, -15, 15, H, lobby, D(-10, 1.6f, 2.4f), D(2.5f, 1.6f, 2.4f));
            WallX(i, inn, inn, 5, -20, -8, H, new Vector3(-14, 0, 0), D(-12, 1.6f, 2.4f));
            WallX(i, inn, inn, 5, -4, 8, H, new Vector3(0, 0, 10), D(0, 1.4f, 2.4f), D(6, 1.4f, 2.4f));
            WallZ(i, inn, inn, -4, 5, 15, H, new Vector3(-10, 0, 10), D(7, 1.6f, 2.4f));
            WallZ(i, inn, inn, 4, 5, 15, H, new Vector3(0, 0, 10));
            WallX(i, inn, inn, 0, 8, 20, H, new Vector3(14, 0, -5), D(11, 1.6f, 2.4f));

            // lobby: front desk, waiting chairs, wanted board
            F.Counter(i, new Vector3(-0.7f, 0.2f, -9f), 0, 8.6f, A.PoliceBlue, A.WoodDark);
            F.Desk(i, new Vector3(-2f, 0.2f, -7.5f), 180, true);
            F.Desk(i, new Vector3(1.5f, 0.2f, -7.5f), 180, true);
            F.Chair(i, new Vector3(-2f, 0.2f, -6.7f), 180);
            F.Chair(i, new Vector3(1.5f, 0.2f, -6.7f), 180);
            F.Bench(i, new Vector3(-7.4f, 0.2f, -12f), 90, 3f, A.Fabric);
            F.Bench(i, new Vector3(7.4f, 0.2f, -12.5f), -90, 2.4f, A.Fabric);
            i.MinMax(A.Paper, new Vector3(-7.84f, 1.2f, -8.5f), new Vector3(-7.8f, 2.6f, -6f)); // wanted posters
            for (int k = 0; k < 6; k++)
                i.MinMax(A.Paper, new Vector3(-7.86f, 1.35f + (k / 3) * 0.7f, -8.4f + (k % 3) * 0.8f), new Vector3(-7.76f, 1.9f + (k / 3) * 0.7f, -7.9f + (k % 3) * 0.8f));

            // bullpen
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 2; col++)
                {
                    Vector3 dp = new Vector3(-17f + col * 5f, 0.2f, -11f + row * 5f);
                    F.Desk(i, dp, 0, true);
                    F.Chair(i, dp + new Vector3(0, 0, -0.8f), 0);
                }
            for (float z = -14; z <= 3; z += 1.2f) i.MinMax(A.Metal, new Vector3(-19.8f, 0.2f, z), new Vector3(-19.2f, 1.5f, z + 1f)); // filing cabinets
            i.MinMax(A.Paper, new Vector3(-12, 1.2f, 4.8f), new Vector3(-9, 2.6f, 4.84f)); // case board

            // holding cells
            float[] cz = { -15f, -10f, -5f, 0f };
            for (int k = 0; k < 3; k++)
            {
                float z0 = cz[k], z1 = cz[k + 1];
                F.Bars(i, A.Bars, new Vector3(14, 0.2f, z0 + 0.2f), new Vector3(14, 0.2f, z0 + 1.6f), 0f, 3.2f, 0.14f);
                if (k != 1) F.Bars(i, A.Bars, new Vector3(14, 0.2f, z0 + 1.6f), new Vector3(14, 0.2f, z0 + 2.8f), 0f, 3.2f, 0.14f); // closed door
                else F.Bars(i, A.Bars, new Vector3(14, 0.2f, z0 + 1.6f), new Vector3(15.1f, 0.2f, z0 + 2.4f), 0f, 3.2f, 0.14f); // open door
                F.Bars(i, A.Bars, new Vector3(14, 0.2f, z0 + 2.8f), new Vector3(14, 0.2f, z1 - 0.2f), 0f, 3.2f, 0.14f);
                if (k > 0) i.MinMax(inn, new Vector3(14, 0.2f, z0 - 0.12f), new Vector3(20, H, z0 + 0.12f));
                i.MinMax(A.Concrete, new Vector3(17.6f, 0.2f, z0 + 0.4f), new Vector3(19.8f, 0.65f, z1 - 0.4f)); // bunk
                i.MinMax(A.Chrome, new Vector3(15f, 0.2f, z1 - 0.9f), new Vector3(15.6f, 0.7f, z1 - 0.3f));  // toilet
            }
            i.MinMax(A.Bars, new Vector3(13.95f, 3.4f, -15f), new Vector3(14.05f, 3.5f, 0f));
            F.Desk(i, new Vector3(10.5f, 0.2f, -3f), 90, true); // guard desk
            F.Chair(i, new Vector3(9.7f, 0.2f, -3f), 90);

            // briefing room
            i.MinMax(A.WoodDark, new Vector3(-16, 0.95f, 8.6f), new Vector3(-8, 1.0f, 11.4f));
            i.MinMax(A.Metal, new Vector3(-15.8f, 0.2f, 9f), new Vector3(-15.6f, 0.95f, 11f));
            i.MinMax(A.Metal, new Vector3(-8.4f, 0.2f, 9f), new Vector3(-8.2f, 0.95f, 11f));
            for (int k = 0; k < 5; k++)
            {
                F.Chair(i, new Vector3(-15 + k * 1.7f, 0.2f, 8f), 0);
                F.Chair(i, new Vector3(-15 + k * 1.7f, 0.2f, 12f), 180);
            }
            i.MinMax(A.FenceWhite, new Vector3(-19.84f, 1f, 7f), new Vector3(-19.8f, 2.6f, 13f)); // whiteboard
            i.MinMax(A.Paper, new Vector3(-19.86f, 1.4f, 8f), new Vector3(-19.82f, 1.8f, 9.2f));  // city map pinned up

            // armory
            for (int k = 0; k < 3; k++) F.Locker(i, new Vector3(-2.5f + k * 2.5f, 0.2f, 14.5f), 180, 4, A.Metal);
            i.MinMax(A.WoodDark, new Vector3(-3.7f, 1.2f, 6f), new Vector3(-3.6f, 2.6f, 13f)); // rifle rack board
            for (float z = 6.4f; z < 12.8f; z += 0.45f) i.Box(A.Trim, new Vector3(-3.5f, 1.9f, z), new Vector3(0.08f, 1.2f, 0.08f));
            i.MinMax(A.Wood, new Vector3(-1, 0.2f, 8f), new Vector3(2, 1f, 10f)); // ammo table
            F.Crate(i, new Vector3(0, 1f, 9f), 10, 0.5f);
            F.Crate(i, new Vector3(1.2f, 1f, 9.2f), -15, 0.4f);

            // locker room and back exit
            F.Locker(i, new Vector3(7.6f, 0.2f, 10f), -90, 8, A.PoliceBlue);
            F.Bench(i, new Vector3(6f, 0.2f, 10f), 90, 3f);

            // chief's office
            F.Desk(i, new Vector3(15f, 0.2f, 9f), 180, true);
            F.Chair(i, new Vector3(15f, 0.2f, 9.9f), 180);
            F.Chair(i, new Vector3(14.3f, 0.2f, 7.5f), 0);
            F.Chair(i, new Vector3(15.7f, 0.2f, 7.5f), 0);
            F.Shelf(i, new Vector3(19.6f, 0.2f, 8f), -90, 4f, 2.2f, A.WoodDark, A.Paper);
            i.MinMax(A.Leather, new Vector3(10f, 0.2f, 12.5f), new Vector3(13f, 0.8f, 14.6f)); // sofa
            i.Box(A.Metal, new Vector3(18.5f, 1.2f, 13.5f), new Vector3(0.05f, 2.4f, 0.05f));  // flag pole
            i.Box(A.PoliceBlue, new Vector3(18.5f, 2.1f, 13.1f), new Vector3(0.02f, 0.6f, 0.8f));

            // exterior: steps, canopy, sign, blue lamps, flagpole, hitching rail
            s.MinMax(A.Concrete, new Vector3(-4, 0, -17f), new Vector3(4, 0.2f, -15f));
            s.MinMax(A.PoliceBlue, new Vector3(-4, 3.3f, -17.5f), new Vector3(4, 3.5f, -15f));
            BlockFont.Write(s, A.SignWhite, "POLICE", new Vector3(0, H + 2.3f, -15.35f), 0.3f, 0.15f);
            s.Box(A.PoliceLight, new Vector3(-2.6f, 2.6f, -15.25f), new Vector3(0.4f, 0.5f, 0.2f));
            s.Box(A.PoliceLight, new Vector3(2.6f, 2.6f, -15.25f), new Vector3(0.4f, 0.5f, 0.2f));
            y.Cylinder(A.Chrome, new Vector3(-9, 0, -19f), 0.08f, 10f, 6);
            y.Box(A.PoliceBlue, new Vector3(-8.2f, 9.2f, -19f), new Vector3(1.6f, 1f, 0.02f));
            F.HitchingRail(y, new Vector3(10, 0, -18.5f), 0, 6f);
            F.Trough(y, new Vector3(10, 0, -19.6f), 0, 3f);
            for (int k = 0; k < 5; k++) y.Cylinder(A.Concrete, new Vector3(-6 + k * 3, 0, -20.5f), 0.25f, 0.8f, 6);

            L.interiorLights.Add(new LightSpec(new Vector3(0, 3.8f, -10), ClinicWhite, 10f, 1.4f));
            L.interiorLights.Add(new LightSpec(new Vector3(-14, 3.8f, -8), ClinicWhite, 9f, 1.2f));
            L.interiorLights.Add(new LightSpec(new Vector3(-14, 3.8f, 1), ClinicWhite, 9f, 1.2f));
            L.interiorLights.Add(new LightSpec(new Vector3(0, 3.8f, 0), ClinicWhite, 8f, 1f));
            L.interiorLights.Add(new LightSpec(new Vector3(14, 3.8f, -7.5f), new Color(0.8f, 0.9f, 1f), 9f, 0.9f));
            L.interiorLights.Add(new LightSpec(new Vector3(-12, 3.8f, 10), ClinicWhite, 9f, 1.1f));
            L.interiorLights.Add(new LightSpec(new Vector3(0, 3.8f, 10), Warm, 7f, 1f));
            L.interiorLights.Add(new LightSpec(new Vector3(14, 3.8f, 8), Warm, 9f, 1.1f));
            L.outdoorLights.Add(new LightSpec(new Vector3(0, 3.1f, -17f), new Color(0.6f, 0.75f, 1f), 10f, 1.5f));
            L.interiorBounds = new Bounds(new Vector3(0, 2.1f, 0), new Vector3(40, 4.4f, 30));
            L.points.Add((QuestPointKind.PoliceStation, new Vector3(0, 0, -18f), "Police station front door"));
            L.lot = new Rect(-22, -22, 44, 39);
            return L;
        }

        // ================================================================== POLICE STABLE
        public Landmark PoliceStable()
        {
            var L = new Landmark { name = "PoliceStable" };
            Part s = L.shell, i = L.interior, y = L.yard;
            const float H = 4.5f;
            Material ext = A.Wood, inn = A.WoodDark;
            Vector3 c = Vector3.zero;

            Floor(i, A.Concrete, -18, 18, -3, 3);
            Floor(i, A.Dirt, -18, 18, -12, -3);
            Floor(i, A.Dirt, -18, 18, 3, 12);

            var frontWin = new List<O> { D(0, 4f, 3.5f) };
            var backWin = new List<O>();
            for (int k = 0; k < 8; k++)
            {
                float x = k < 4 ? -16 + k * 4 : 4 + (k - 4) * 4;
                frontWin.Add(W(x, 1.4f, 1.8f, 2.6f));
            }
            for (int k = 0; k < 7; k++) backWin.Add(W(-12 + k * 4, 1.4f, 1.8f, 2.6f));
            WallX(s, ext, inn, -12, -18, 18, H, c, frontWin.ToArray());
            WallX(s, ext, inn, 12, -18, 18, H, c, backWin.ToArray());
            WallZ(s, ext, inn, -18, -12, 12, H, c, D(0, 4.5f, 4f));
            WallZ(s, ext, inn, 18, -12, 12, H, c, D(0, 4.5f, 4f));
            s.Gable(A.RoofTile, new Vector3(0, H, 0), 36.3f, 24.3f, 3.5f, 0.7f);
            s.MinMax(A.Wood, new Vector3(-1.5f, H + 3.1f, -1.5f), new Vector3(1.5f, H + 4.6f, 1.5f)); // cupola
            s.Gable(A.RoofTile, new Vector3(0, H + 4.6f, 0), 3f, 3f, 1f, 0.3f);
            s.Box(A.Metal, new Vector3(0, H + 6.2f, 0), new Vector3(0.04f, 1.2f, 0.04f));
            s.Box(A.Metal, new Vector3(0.3f, H + 6.6f, 0), new Vector3(0.6f, 0.25f, 0.02f)); // weather vane
            for (float x = -16; x <= 16; x += 4) s.Beam(A.WoodDark, new Vector3(x, H - 0.2f, -12), new Vector3(x, H - 0.2f, 12), 0.22f);

            void Stall(float x0, float x1, bool south, bool open, int k)
            {
                float zf = south ? -3f : 3f, zb = south ? -12f : 12f;
                float zLo = Mathf.Min(zf, zb), zHi = Mathf.Max(zf, zb);
                float mid = (x0 + x1) * 0.5f;
                i.MinMax(inn, new Vector3(x1 - 0.06f, 0.2f, zLo), new Vector3(x1 + 0.06f, 2.3f, zHi));
                i.MinMax(inn, new Vector3(x0, 0.2f, zf - 0.06f), new Vector3(mid - 0.8f, 1.2f, zf + 0.06f));
                i.MinMax(inn, new Vector3(mid + 0.8f, 0.2f, zf - 0.06f), new Vector3(x1, 1.2f, zf + 0.06f));
                F.Bars(i, A.Metal, new Vector3(x0, 0, zf), new Vector3(mid - 0.8f, 0, zf), 1.2f, 2.3f, 0.22f);
                F.Bars(i, A.Metal, new Vector3(mid + 0.8f, 0, zf), new Vector3(x1, 0, zf), 1.2f, 2.3f, 0.22f);
                if (!open)
                    i.MinMax(A.WoodDark, new Vector3(mid - 0.8f, 0.4f, zf - 0.05f), new Vector3(mid + 0.8f, 1.3f, zf + 0.05f)); // closed gate
                else
                    i.MinMax(A.WoodDark, new Vector3(mid + 0.75f, 0.4f, south ? zf : zf - 1.6f), new Vector3(mid + 0.85f, 1.3f, south ? zf + 1.6f : zf)); // swung open
                i.MinMax(A.Hay, new Vector3(x0 + 0.1f, 0.2f, zLo + 0.1f), new Vector3(x1 - 0.1f, 0.32f, zHi - 0.1f));
                float zt = south ? zb + 0.5f : zb - 0.5f;
                F.Trough(i, new Vector3(mid - 0.8f, 0.2f, zt), 0, 1.4f);
                F.Bucket(i, new Vector3(x1 - 0.5f, 0.2f, zt));
                i.Blob(A.Hay, new Vector3(x0 + 0.6f, 1.8f, zt), new Vector3(0.35f, 0.45f, 0.35f), k); // hay net
                i.MinMax(A.Paper, new Vector3(mid - 0.3f, 1.4f, zf + (south ? 0.07f : -0.07f) - 0.02f), new Vector3(mid + 0.3f, 1.7f, zf + (south ? 0.07f : -0.07f) + 0.02f)); // name plate
            }

            // south row: 4 + 4 stalls around the front entry passage
            for (int k = 0; k < 4; k++) Stall(-18 + k * 4, -14 + k * 4, true, k == 1, k);
            for (int k = 0; k < 4; k++) Stall(2 + k * 4, 6 + k * 4, true, k == 2, 10 + k);
            i.MinMax(inn, new Vector3(-2.06f, 0.2f, -12), new Vector3(-1.94f, 2.4f, -3));
            i.MinMax(inn, new Vector3(1.94f, 0.2f, -12), new Vector3(2.06f, 2.4f, -3));
            // north row: feed room, 7 stalls, tack room
            for (int k = 0; k < 7; k++) Stall(-14 + k * 4, -10 + k * 4, false, k == 3, 20 + k);
            WallX(i, inn, inn, 3, -18, -14, H, new Vector3(-16, 0, 8), D(-16, 1.4f, 2.4f));
            WallZ(i, inn, inn, -14, 3, 12, H, new Vector3(-16, 0, 8));
            WallX(i, inn, inn, 3, 14, 18, H, new Vector3(16, 0, 8), D(16, 1.4f, 2.4f));
            WallZ(i, inn, inn, 14, 3, 12, H, new Vector3(16, 0, 8));

            // feed room: hay stacks, sacks, barrels
            for (int r = 0; r < 3; r++)
                for (int k = 0; k < 2; k++)
                {
                    F.HayBale(i, new Vector3(-17.2f + k * 1.15f, 0.2f + r * 0.5f, 11.3f), 90);
                    F.HayBale(i, new Vector3(-17.2f + k * 1.15f, 0.2f + r * 0.5f, 10.1f), 90);
                }
            F.Barrel(i, new Vector3(-14.6f, 0.2f, 6.5f), A.WoodDark);
            F.Barrel(i, new Vector3(-14.6f, 0.2f, 5.7f), A.WoodDark);
            for (int k = 0; k < 4; k++) i.Box(A.Paper, new Vector3(-17.3f, 0.45f, 4f + k * 0.7f), new Vector3(0.6f, 0.5f, 0.45f), 8f * k); // feed sacks

            // tack room: saddle racks, bridles, chest
            for (int k = 0; k < 5; k++)
            {
                float z = 4.2f + k * 1.5f;
                i.Box(A.WoodDark, new Vector3(17.5f, 1.3f, z), new Vector3(0.6f, 0.08f, 0.1f));
                i.Gable(A.Leather, new Vector3(17.3f, 1.35f, z), 0.7f, 0.55f, 0.25f, 0.05f, 90f);
                i.Box(A.Leather, new Vector3(17.85f, 2.2f, z), new Vector3(0.05f, 0.5f, 0.08f)); // bridle
            }
            i.MinMax(A.WoodDark, new Vector3(14.4f, 0.2f, 9.8f), new Vector3(15.6f, 0.8f, 11.8f));
            F.Bench(i, new Vector3(15.8f, 0.2f, 5.5f), 90, 2f);
            for (int k = 0; k < 4; k++)
                F.HangingLamp(i, new Vector3(-13.5f + k * 9, 3.6f, 0), 0.7f, A.LampWarm);

            // exterior: sign, hitching rails, paddock behind
            BlockFont.Write(s, A.SignYellow, "POLICE STABLE", new Vector3(0, 4.05f, -12.25f), 0.1f, 0.1f);
            F.HitchingRail(y, new Vector3(-9, 0, -14f), 0, 6f);
            F.HitchingRail(y, new Vector3(9, 0, -14f), 0, 6f);
            F.Trough(y, new Vector3(-9, 0, -15.1f), 0, 3f);
            F.Trough(y, new Vector3(9, 0, -15.1f), 0, 3f);
            y.MinMax(A.Dirt, new Vector3(-18, 0, 12.2f), new Vector3(18, 0.04f, 34));
            F.Fence(y, A.FenceWhite, new Vector3(-18, 0, 12.4f), new Vector3(-18, 0, 34));
            F.Fence(y, A.FenceWhite, new Vector3(18, 0, 12.4f), new Vector3(18, 0, 34));
            F.Fence(y, A.FenceWhite, new Vector3(-18, 0, 34), new Vector3(-4, 0, 34));
            F.Fence(y, A.FenceWhite, new Vector3(4, 0, 34), new Vector3(18, 0, 34));
            for (int k = 0; k < 8; k++) // training poles in the paddock
                y.MinMax(A.SignRed, new Vector3(-12 + k * 3, 0.3f, 24f), new Vector3(-12 + k * 3 + 0.1f, 0.4f, 26f));
            F.Trough(y, new Vector3(14, 0, 33.2f), 0, 3f);
            F.HayBale(y, new Vector3(-15, 0, 15), 15);
            F.HayBale(y, new Vector3(-13.8f, 0, 15.3f), -5);
            F.HayBale(y, new Vector3(-14.4f, 0.5f, 15.1f), 5);
            F.Barrel(y, new Vector3(19, 0, -10), A.Water);

            for (int k = 0; k < 4; k++)
                L.interiorLights.Add(new LightSpec(new Vector3(-13.5f + k * 9, 3.3f, 0), Lantern, 9f, 1.2f));
            L.interiorLights.Add(new LightSpec(new Vector3(-16, 3.4f, 8), Lantern, 6f, 0.9f));
            L.interiorLights.Add(new LightSpec(new Vector3(16, 3.4f, 8), Lantern, 6f, 0.9f));
            L.outdoorLights.Add(new LightSpec(new Vector3(0, 3.8f, -13f), Lantern, 12f, 1.4f));
            L.outdoorLights.Add(new LightSpec(new Vector3(-19, 4f, 0), Lantern, 9f, 1.1f));
            L.interiorBounds = new Bounds(new Vector3(0, 2.5f, 0), new Vector3(36, 6, 24));
            L.points.Add((QuestPointKind.PoliceStable, new Vector3(0, 0, -15f), "Police stable front door"));
            L.points.Add((QuestPointKind.PlayerSpawn, new Vector3(-4, 0, -16.5f), "Player starts here with their police horse"));
            L.lot = new Rect(-20, -18, 40, 53);
            return L;
        }

        // ================================================================== HIDDEN WEAPON SHOP (exterior only)
        public Landmark WeaponShop()
        {
            var L = new Landmark { name = "WeaponShop_Hidden" };
            Part s = L.shell, y = L.yard;
            // a tired two storey concrete shack facing a narrow alley, no signage on purpose
            s.Box(A.ShopGrey, new Vector3(0, 3.6f, 0), new Vector3(7.2f, 7.2f, 7f), CityAssets.ShopTile);
            s.Box(A.Rust, new Vector3(0, 1.5f, -3.56f), new Vector3(4f, 3f, 0.12f));      // shutter, rusted shut
            s.Box(A.WoodDark, new Vector3(2.9f, 1.1f, -3.55f), new Vector3(0.9f, 2.1f, 0.1f)); // side door
            s.Box(A.Rust, new Vector3(0, 3.3f, -4.4f), new Vector3(7.8f, 0.08f, 1.8f), 6f); // corrugated lean-to
            s.Beam(A.Metal, new Vector3(-3.6f, 0, -5.2f), new Vector3(-3.6f, 3.25f, -5.2f), 0.08f);
            s.Beam(A.Metal, new Vector3(3.6f, 0, -5.2f), new Vector3(3.6f, 3.25f, -5.2f), 0.08f);
            s.Box(A.LampWarm, new Vector3(2.9f, 2.6f, -3.7f), new Vector3(0.15f, 0.2f, 0.15f));  // bare bulb
            s.Box(A.Concrete, new Vector3(0, 7.5f, 0), new Vector3(7.4f, 0.6f, 7.2f), new Vector2(3, 3));
            s.Box(A.FenceWhite, new Vector3(-2.5f, 5f, -3.7f), new Vector3(0.8f, 0.55f, 0.35f)); // old AC unit
            s.Box(A.WindowDark, new Vector3(1f, 5f, -3.52f), new Vector3(1.6f, 1.2f, 0.06f));
            F.Bars(s, A.Metal, new Vector3(0.2f, 4.4f, -3.6f), new Vector3(1.8f, 4.4f, -3.6f), 0f, 1.2f, 0.2f);
            F.Crate(y, new Vector3(-3, 0, -5.5f), 12);
            F.Crate(y, new Vector3(-2.2f, 0, -6f), -20, 0.6f);
            F.Crate(y, new Vector3(-2.9f, 0.8f, -5.6f), 30, 0.55f);
            F.Barrel(y, new Vector3(4.6f, 0, -2f));
            F.Barrel(y, new Vector3(4.6f, 0, -1.2f), A.AwningBlue);
            L.outdoorLights.Add(new LightSpec(new Vector3(2.9f, 2.4f, -4.2f), new Color(1f, 0.6f, 0.3f), 6f, 1.4f));
            L.points.Add((QuestPointKind.WeaponShop, new Vector3(2.9f, 0, -5f), "Hidden weapon shop. Knock on the side door."));
            L.lot = new Rect(-6, -8, 12, 14);
            return L;
        }

        // ================================================================== CENTRAL TOWER (the biggest building)
        public Landmark CentralTower()
        {
            var L = new Landmark { name = "CentralTower" };
            Part s = L.shell;
            Vector2 tile = CityAssets.GlassTile;
            // podium
            s.Box(A.WindowLit, new Vector3(0, 6f, 0), new Vector3(60f, 12f, 60f), new Vector2(4, 4));
            s.Box(A.Trim, new Vector3(0, 12.4f, 0), new Vector3(64f, 0.8f, 64f));
            for (int side = 0; side < 4; side++)
            {
                float yaw = side * 90f;
                s.Local(Vector3.zero, yaw, () =>
                {
                    for (float x = -28.8f; x <= 28.9f; x += 4.8f)
                        s.Box(A.Concrete, new Vector3(x, 6f, -30.4f), new Vector3(0.9f, 12f, 0.9f));
                    s.Box(A.Trim, new Vector3(0, 5.5f, -34f), new Vector3(16f, 0.4f, 7f)); // entrance canopy
                    s.Box(A.LampBulb, new Vector3(0, 5.28f, -34f), new Vector3(14f, 0.05f, 5f));
                });
            }
            // stepped glass shaft
            float[] widths = { 51.2f, 44.8f, 35.2f, 25.6f };
            float[] tops = { 140.4f, 212.4f, 262.8f, 284.4f };
            float y0 = 12.8f;
            for (int t = 0; t < widths.Length; t++)
            {
                float h = tops[t] - y0;
                s.Box(t == 3 ? A.GlassTeal : A.GlassBlue, new Vector3(0, y0 + h * 0.5f, 0), new Vector3(widths[t], h, widths[t]), tile);
                s.Box(A.Trim, new Vector3(0, tops[t] + 0.4f, 0), new Vector3(widths[t] + 0.8f, 0.8f, widths[t] + 0.8f));
                // corner fins
                for (int k = 0; k < 4; k++)
                {
                    float sx = (k % 2 == 0 ? -1 : 1) * widths[t] * 0.5f, sz = (k < 2 ? -1 : 1) * widths[t] * 0.5f;
                    s.Box(A.Chrome, new Vector3(sx, y0 + h * 0.5f, sz), new Vector3(0.8f, h, 0.8f));
                }
                y0 = tops[t] + 0.8f;
            }
            // lit crown band and spire
            s.Box(A.WindowLit, new Vector3(0, 290f, 0), new Vector3(22f, 10f, 22f), new Vector2(4, 4));
            s.Box(A.Trim, new Vector3(0, 295.4f, 0), new Vector3(24f, 0.8f, 24f));
            s.Cylinder(A.Chrome, new Vector3(0, 295.8f, 0), 1.2f, 30f, 8);
            s.Cylinder(A.Chrome, new Vector3(0, 325.8f, 0), 0.4f, 18f, 6);
            s.Box(A.AviationRed, new Vector3(0, 344f, 0), new Vector3(1f, 1f, 1f));
            for (int k = 0; k < 4; k++)
                s.Box(A.AviationRed, new Vector3((k % 2 == 0 ? -1 : 1) * 12f, 296.2f, (k < 2 ? -1 : 1) * 12f), new Vector3(0.6f, 0.6f, 0.6f));
            L.points.Add((QuestPointKind.CentralTower, new Vector3(0, 0, -36f), "Central tower main entrance (south)"));
            L.lot = new Rect(-36, -36, 72, 72);
            return L;
        }
    }
}
