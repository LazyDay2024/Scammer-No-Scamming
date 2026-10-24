using UnityEngine;

namespace HorseCity.EditorTools
{
    public enum BuildingStyle { GlassTower, Office, Apartment, Shophouse, House, Warehouse }

    /// <summary>
    /// Generic city buildings, exterior only. Every building is built in its own local frame:
    /// origin at the ground center of the footprint, front facade facing -Z (toward the street).
    /// Widths are snapped to the facade's window cell so windows never get cut at corners.
    /// </summary>
    public class BuildingFactory
    {
        readonly CityAssets A;
        public BuildingFactory(CityAssets assets) { A = assets; }

        public static float Snap(float v, float cell) => Mathf.Max(cell, Mathf.Round(v / cell) * cell);

        float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);
        T Pick<T>(System.Random r, params T[] items) => items[r.Next(items.Length)];

        // ------------------------------------------------------------ glass tower
        public Part GlassTower(float w, float d, float h, System.Random r)
        {
            var p = new Part();
            Material glass = Pick(r, A.GlassBlue, A.GlassTeal, A.GlassDark);
            Vector2 tile = CityAssets.GlassTile;
            float podium = 7.2f;

            // lobby podium, a bit wider, dark glass with lit lobby
            p.Box(A.WindowLit, new Vector3(0, podium * 0.5f, 0), new Vector3(w - 1.2f, podium, d - 1.2f), new Vector2(4, 4));
            p.Box(A.Trim, new Vector3(0, podium - 0.4f, 0), new Vector3(w + 2f, 0.8f, d + 2f));
            for (float x = -w * 0.5f + 0.6f; x <= w * 0.5f; x += 4.8f) // lobby columns
                p.Box(A.Concrete, new Vector3(x, podium * 0.5f, -d * 0.5f + 0.4f), new Vector3(0.7f, podium, 0.7f));
            // entrance canopy
            p.Box(A.Trim, new Vector3(0, 4.2f, -d * 0.5f - 1.8f), new Vector3(Mathf.Min(10f, w * 0.5f), 0.35f, 3.6f));

            int tiers = h > 110 ? 3 : (h > 60 ? 2 : 1);
            float y = podium, tw = w, td = d;
            for (int t = 0; t < tiers; t++)
            {
                float th = (t == tiers - 1) ? h - y : Mathf.Max(7.2f, Mathf.Round((h - y) * 0.6f / 3.6f) * 3.6f);
                p.Box(glass, new Vector3(0, y + th * 0.5f, 0), new Vector3(tw, th, td), tile);
                y += th;
                p.Box(A.Trim, new Vector3(0, y + 0.3f, 0), new Vector3(tw + 0.4f, 0.6f, td + 0.4f));
                if (t < tiers - 1)
                {
                    tw = Snap(tw * R(r, 0.7f, 0.85f), CityAssets.GlassCell);
                    td = Snap(td * R(r, 0.7f, 0.85f), CityAssets.GlassCell);
                }
                if (y >= h - 1f) break;
            }
            Roof(p, tw, td, y + 0.6f, r, true);
            if (h > 90)
            {
                p.Cylinder(A.Metal, new Vector3(tw * 0.2f, y + 0.6f, 0), 0.25f, h * 0.12f, 6);
                p.Box(A.AviationRed, new Vector3(tw * 0.2f, y + 0.6f + h * 0.12f, 0), new Vector3(0.6f, 0.6f, 0.6f));
                p.Box(A.AviationRed, new Vector3(-tw * 0.5f + 0.4f, y + 1f, -td * 0.5f + 0.4f), new Vector3(0.5f, 0.5f, 0.5f));
            }
            return p;
        }

        // ------------------------------------------------------------ stone office block
        public Part Office(float w, float d, float h, System.Random r)
        {
            var p = new Part();
            Material fac = Pick(r, A.Office, A.Office, A.OfficeDark);
            p.Box(A.Concrete, new Vector3(0, 2.4f, 0), new Vector3(w + 0.6f, 4.8f, d + 0.6f), new Vector2(3, 3));
            p.Box(A.WindowLit, new Vector3(0, 2.0f, -d * 0.5f - 0.32f), new Vector3(w * 0.6f, 3.2f, 0.1f), new Vector2(3, 3));
            p.Box(fac, new Vector3(0, 4.8f + (h - 4.8f) * 0.5f, 0), new Vector3(w, h - 4.8f, d), CityAssets.OfficeTile);
            p.Box(A.Concrete, new Vector3(0, h + 0.5f, 0), new Vector3(w + 0.8f, 1f, d + 0.8f), new Vector2(3, 3));
            // vertical pilasters on the front, like the stone tower in the reference
            for (float x = -w * 0.5f; x <= w * 0.5f + 0.01f; x += CityAssets.OfficeCell * 3f)
                p.Box(A.Concrete, new Vector3(x, h * 0.5f, -d * 0.5f - 0.2f), new Vector3(0.5f, h, 0.4f), new Vector2(3, 3));
            Roof(p, w, d, h + 1f, r, false);
            return p;
        }

        // ------------------------------------------------------------ Japanese style apartment block
        public Part Apartment(float w, float d, float h, System.Random r)
        {
            var p = new Part();
            Material fac = Pick(r, A.AptWhite, A.AptWhite, A.AptGrey);
            float floorH = 3f;
            int floors = Mathf.Max(3, Mathf.RoundToInt(h / floorH));
            h = floors * floorH;
            p.Box(fac, new Vector3(0, h * 0.5f, 0), new Vector3(w, h, d), CityAssets.AptTile);

            // ground floor entrance + lit lobby
            p.Box(A.WindowLit, new Vector3(0, 1.3f, -d * 0.5f - 0.05f), new Vector3(3.2f, 2.6f, 0.1f), new Vector2(3, 3));
            p.Box(A.Concrete, new Vector3(0, 2.8f, -d * 0.5f - 1f), new Vector3(4.4f, 0.25f, 2f));

            // balconies along the front on every floor except the ground floor
            bool sideBalcony = r.NextDouble() < 0.4;
            for (int f = 1; f < floors; f++)
            {
                float y = f * floorH;
                p.Box(A.Concrete, new Vector3(0, y + 0.1f, -d * 0.5f - 0.7f), new Vector3(w, 0.2f, 1.4f), new Vector2(3, 3));
                p.Box(A.Metal, new Vector3(0, y + 0.65f, -d * 0.5f - 1.38f), new Vector3(w, 0.9f, 0.05f));
                if (sideBalcony)
                    p.Box(A.Concrete, new Vector3(w * 0.5f + 0.6f, y + 0.55f, 0), new Vector3(1.2f, 0.9f, d * 0.6f), new Vector2(3, 3));
                // a few AC units on the balcony
                if (r.NextDouble() < 0.6)
                    p.Box(A.FenceWhite, new Vector3(R(r, -w * 0.45f, w * 0.45f), y + 0.5f, -d * 0.5f - 0.5f), new Vector3(0.8f, 0.6f, 0.3f));
            }
            // balcony dividers every window bay
            for (float x = -w * 0.5f; x <= w * 0.5f + 0.01f; x += CityAssets.AptCell * 2f)
                p.Box(A.Concrete, new Vector3(x, (h + floorH) * 0.5f, -d * 0.5f - 0.7f), new Vector3(0.15f, h - floorH, 1.4f), new Vector2(3, 3));
            // exterior stair core on one side
            if (r.NextDouble() < 0.5)
                p.Box(fac, new Vector3(w * 0.5f + 1.6f, (h + 3f) * 0.5f, d * 0.2f), new Vector3(3.2f, h + 3f, 4f), CityAssets.AptTile);

            p.Box(A.Concrete, new Vector3(0, h + 0.4f, 0), new Vector3(w, 0.8f, d), new Vector2(3, 3));
            Roof(p, w, d, h + 0.8f, r, false);
            return p;
        }

        // ------------------------------------------------------------ Thai shophouse row
        public Part Shophouse(float w, float d, int floors, System.Random r)
        {
            var p = new Part();
            Material fac = Pick(r, A.ShopCream, A.ShopMint, A.ShopPeach, A.ShopGrey);
            float floorH = 3.6f;
            float h = floors * floorH;
            float ground = 3.6f;

            // upper floors use the facade texture, ground floor is shutters + columns
            p.Box(fac, new Vector3(0, ground + (h - ground) * 0.5f, 0), new Vector3(w, h - ground, d), CityAssets.ShopTile);
            p.Box(A.Concrete, new Vector3(0, ground * 0.5f, 0.4f), new Vector3(w, ground, d - 0.8f), new Vector2(3, 3));
            int bays = Mathf.Max(1, Mathf.RoundToInt(w / CityAssets.ShopCell));
            float bay = w / bays;
            for (int b = 0; b < bays; b++)
            {
                float x = -w * 0.5f + bay * (b + 0.5f);
                bool open = r.NextDouble() < 0.25;
                p.Box(open ? A.WindowLit : A.Shutter, new Vector3(x, 1.45f, -d * 0.5f + 0.45f), new Vector3(bay - 0.5f, 2.9f, 0.1f), new Vector2(3, 3));
                p.Box(A.Concrete, new Vector3(-w * 0.5f + bay * b + 0.2f, ground * 0.5f, -d * 0.5f + 0.2f), new Vector3(0.4f, ground, 0.4f), new Vector2(3, 3));
                // awning over the shop front
                if (r.NextDouble() < 0.6)
                    p.Box(Pick(r, A.AwningRed, A.AwningBlue, A.AwningGreen, A.RoofMetal), new Vector3(x, 3.1f, -d * 0.5f - 0.9f), new Vector3(bay - 0.1f, 0.12f, 1.9f), new Vector2(2, 2), 0f, true, true);
                // shop signs, some lit
                if (r.NextDouble() < 0.55)
                {
                    Material s = Pick(r, A.SignRed, A.SignYellow, A.SignWhite, A.SignBlue, A.SignGreen, A.Plaster, A.Plaster);
                    if (r.NextDouble() < 0.5)
                        p.Box(s, new Vector3(x, ground + 0.5f, -d * 0.5f - 0.15f), new Vector3(bay * 0.8f, 0.9f, 0.2f));
                    else
                        p.Box(s, new Vector3(x + bay * 0.4f, ground + 2.5f, -d * 0.5f - 0.8f), new Vector3(0.2f, 3f, 1.1f));
                }
                // AC units and drain pipes on upper floors
                for (int f = 1; f < floors; f++)
                    if (r.NextDouble() < 0.3)
                        p.Box(A.FenceWhite, new Vector3(x + R(r, -1, 1), f * floorH + 0.6f, -d * 0.5f - 0.2f), new Vector3(0.8f, 0.55f, 0.35f));
            }
            p.Box(A.Trim, new Vector3(0, ground, -d * 0.5f - 0.05f), new Vector3(w + 0.1f, 0.25f, 0.3f));

            // roof: half flat with parapet and clutter, half pitched metal
            if (r.NextDouble() < 0.45)
            {
                p.Box(A.Concrete, new Vector3(0, h + 0.1f, 0), new Vector3(w, 0.2f, d), new Vector2(3, 3));
                p.Gable(Pick(r, A.RoofMetal, A.RoofTile), new Vector3(0, h + 0.2f, 0), w, d, R(r, 1.2f, 2.4f), 0.5f);
            }
            else
            {
                Parapet(p, w, d, h, 1f, A.Concrete);
                Roof(p, w, d, h, r, false);
                if (r.NextDouble() < 0.4) // rooftop shack in corrugated metal
                {
                    float sw = Mathf.Min(4f, w * 0.4f);
                    p.Box(A.RoofMetal, new Vector3(-w * 0.25f, h + 1.2f, d * 0.2f), new Vector3(sw, 2.4f, 3f), new Vector2(3, 3));
                }
            }
            return p;
        }

        // ------------------------------------------------------------ detached house
        public Part House(float w, float d, System.Random r)
        {
            var p = new Part();
            Material wall = Pick(r, A.Plaster, A.PlasterYellow, A.PlasterBlue, A.Brick);
            int floors = r.NextDouble() < 0.7 ? 2 : 1;
            float h = floors * 3f;
            float hw = w * 0.5f, hd = d * 0.5f;
            p.Box(A.Concrete, new Vector3(0, 0.2f, 0), new Vector3(w + 0.4f, 0.4f, d + 0.4f), new Vector2(3, 3));
            p.Box(wall, new Vector3(0, 0.4f + h * 0.5f, 0), new Vector3(w, h, d), new Vector2(3, 3));
            // windows and door as thin insets
            for (int f = 0; f < floors; f++)
            {
                float y = 0.4f + f * 3f + 1.6f;
                for (float x = -hw + 1.6f; x < hw - 1f; x += 2.6f)
                {
                    if (f == 0 && Mathf.Abs(x) < 1.2f) continue;
                    Material win = r.NextDouble() < 0.35 ? A.WindowLit : A.WindowDark;
                    p.Box(win, new Vector3(x, y, -hd - 0.04f), new Vector3(1.1f, 1.2f, 0.08f));
                    p.Box(win, new Vector3(x, y, hd + 0.04f), new Vector3(1.1f, 1.2f, 0.08f));
                }
                p.Box(A.WindowDark, new Vector3(-hw - 0.04f, y, 0), new Vector3(0.08f, 1.2f, 1.1f));
                p.Box(A.WindowDark, new Vector3(hw + 0.04f, y, 0), new Vector3(0.08f, 1.2f, 1.1f));
            }
            p.Box(A.WoodDark, new Vector3(0, 1.5f, -hd - 0.05f), new Vector3(1.1f, 2.2f, 0.1f));
            p.Box(A.RoofTile, new Vector3(0, 2.9f, -hd - 0.8f), new Vector3(2.4f, 0.12f, 1.6f), new Vector2(2, 2), 0f, true, true);
            p.Gable(Pick(r, A.RoofTile, A.RoofTile, A.RoofMetal), new Vector3(0, 0.4f + h, 0), w, d, R(r, 1.6f, 2.6f), 0.6f);
            // garden wall with gate gap
            float fy = 0.6f;
            p.Box(A.Plaster, new Vector3(-hw * 0.6f - 0.6f, fy, -hd - 3f), new Vector3(hw * 1.2f - 0.6f, 1.2f, 0.2f), new Vector2(3, 3));
            p.Box(A.Plaster, new Vector3(hw * 0.6f + 0.6f, fy, -hd - 3f), new Vector3(hw * 1.2f - 0.6f, 1.2f, 0.2f), new Vector2(3, 3));
            p.Box(A.Grass, new Vector3(0, 0.02f, -hd - 1.5f), new Vector3(w + 1f, 0.04f, 3f), new Vector2(3, 3));
            if (r.NextDouble() < 0.5)
            {
                p.Cylinder(A.Bark, new Vector3(hw - 0.8f, 0, -hd - 1.5f), 0.15f, 2.2f, 5);
                p.Blob(A.Leaves, new Vector3(hw - 0.8f, 3f, -hd - 1.5f), new Vector3(1.4f, 1.2f, 1.4f), r.Next());
            }
            return p;
        }

        // ------------------------------------------------------------ warehouse / workshop
        public Part Warehouse(float w, float d, System.Random r)
        {
            var p = new Part();
            float h = R(r, 6f, 9f);
            p.Box(A.Concrete, new Vector3(0, 0.6f, 0), new Vector3(w, 1.2f, d), new Vector2(3, 3));
            p.Box(A.RoofMetal, new Vector3(0, 1.2f + (h - 1.2f) * 0.5f, 0), new Vector3(w, h - 1.2f, d), new Vector2(3, 3));
            p.Box(A.Shutter, new Vector3(0, 2.4f, -d * 0.5f - 0.06f), new Vector3(Mathf.Min(6f, w * 0.5f), 4.8f, 0.1f), new Vector2(3, 3));
            p.Box(A.Rust, new Vector3(w * 0.3f, 1.2f, -d * 0.5f - 0.06f), new Vector3(1f, 2.2f, 0.1f));
            p.Gable(A.Rust, new Vector3(0, h, 0), d, w, R(r, 1f, 2f), 0.4f, 90f);
            p.Box(A.LampWarm, new Vector3(0, 5.3f, -d * 0.5f - 0.3f), new Vector3(0.5f, 0.2f, 0.4f));
            return p;
        }

        // ------------------------------------------------------------ roof clutter
        void Parapet(Part p, float w, float d, float h, float ph, Material m)
        {
            float t = 0.3f;
            p.Box(m, new Vector3(0, h + ph * 0.5f, -d * 0.5f + t * 0.5f), new Vector3(w, ph, t), new Vector2(3, 3));
            p.Box(m, new Vector3(0, h + ph * 0.5f, d * 0.5f - t * 0.5f), new Vector3(w, ph, t), new Vector2(3, 3));
            p.Box(m, new Vector3(-w * 0.5f + t * 0.5f, h + ph * 0.5f, 0), new Vector3(t, ph, d), new Vector2(3, 3));
            p.Box(m, new Vector3(w * 0.5f - t * 0.5f, h + ph * 0.5f, 0), new Vector3(t, ph, d), new Vector2(3, 3));
        }

        void Roof(Part p, float w, float d, float y, System.Random r, bool tower)
        {
            p.Box(A.RoofConcrete, new Vector3(0, y + 0.05f, 0), new Vector3(w - 0.4f, 0.1f, d - 0.4f), new Vector2(6, 6));
            int n = tower ? 3 : r.Next(1, 4);
            for (int i = 0; i < n; i++)
            {
                float bw = R(r, 2f, Mathf.Min(8f, w * 0.4f)), bd = R(r, 2f, Mathf.Min(6f, d * 0.4f)), bh = R(r, 1.5f, tower ? 5f : 3f);
                p.Box(Pick(r, A.Concrete, A.Metal, A.RoofMetal), new Vector3(R(r, -w * 0.3f, w * 0.3f), y + bh * 0.5f, R(r, -d * 0.3f, d * 0.3f)), new Vector3(bw, bh, bd), new Vector2(3, 3));
            }
            if (!tower && r.NextDouble() < 0.6) // water tank on legs, very common on Thai and Japanese roofs
            {
                Vector3 c = new Vector3(R(r, -w * 0.3f, w * 0.3f), y, R(r, -d * 0.3f, d * 0.3f));
                for (int i = 0; i < 4; i++)
                    p.Box(A.Metal, c + new Vector3((i % 2 - 0.5f) * 1.4f, 1f, (i / 2 - 0.5f) * 1.4f), new Vector3(0.12f, 2f, 0.12f));
                p.Cylinder(r.NextDouble() < 0.5 ? A.FenceWhite : A.AwningBlue, c + Vector3.up * 2f, 1f, 1.8f, 8);
            }
            if (r.NextDouble() < 0.5) // TV antenna
            {
                Vector3 c = new Vector3(R(r, -w * 0.4f, w * 0.4f), y, R(r, -d * 0.4f, d * 0.4f));
                p.Box(A.Metal, c + Vector3.up * 1.8f, new Vector3(0.06f, 3.6f, 0.06f));
                p.Box(A.Metal, c + Vector3.up * 3.2f, new Vector3(1.6f, 0.05f, 0.05f));
                p.Box(A.Metal, c + Vector3.up * 2.7f, new Vector3(1.2f, 0.05f, 0.05f));
            }
        }
    }
}
