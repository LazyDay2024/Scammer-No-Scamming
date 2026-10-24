using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HorseCity.EditorTools
{
    /// <summary>
    /// Generates every texture and material the city uses. Textures are small (256-512px)
    /// procedural PNGs so the whole city fits comfortably in low-end VRAM.
    /// </summary>
    public class CityAssets
    {
        public const string Root = "Assets/HorseCity/Generated";
        public const string TexDir = Root + "/Textures";
        public const string MatDir = Root + "/Materials";

        // Facade tile sizes in meters (width of one texture repeat, height of one repeat).
        public static readonly Vector2 GlassTile = new Vector2(12.8f, 28.8f);   // 8 x 8 panes, 1.6m x 3.6m
        public static readonly Vector2 AptTile = new Vector2(12.8f, 12f);       // 4 x 4 windows, 3.2m x 3m
        public static readonly Vector2 ShopTile = new Vector2(16f, 14.4f);      // 4 x 4 bays, 4m x 3.6m
        public static readonly Vector2 OfficeTile = new Vector2(14.4f, 21.6f);  // 6 x 6 windows, 2.4m x 3.6m
        public const float GlassCell = 1.6f, AptCell = 3.2f, ShopCell = 4f, OfficeCell = 2.4f;

        public Material Asphalt, Pavement, Plaza, Curb, Grass, Dirt, Sand, LaneWhite, LaneYellow, Crosswalk;
        public Material GlassBlue, GlassTeal, GlassDark, AptWhite, AptGrey, Office, OfficeDark;
        public Material ShopCream, ShopMint, ShopPeach, ShopGrey;
        public Material Plaster, PlasterYellow, PlasterBlue, RoofTile, RoofMetal, RoofConcrete;
        public Material Trim, Concrete, Metal, Chrome, Rust, Shutter, WindowDark, WindowLit;
        public Material AwningRed, AwningBlue, AwningGreen, SignRed, SignYellow, SignBlue, SignWhite, SignGreen;
        public Material LampBulb, LampWarm, AviationRed, PoliceBlue, PoliceLight, CrossGreen;
        public Material Wood, WoodDark, Hay, FenceWhite, Water;
        public Material Leaves, LeavesDark, Bark, Brick;
        public Material FloorTile, FloorVinyl, RubberMat, WallInterior, WallClinic, WallPolice, Ceiling, Bars;
        public Material Fabric, Leather, Paper, Screen, Gold, Glass;

        Shader _lit;
        bool _urp;

        public static CityAssets Create()
        {
            var a = new CityAssets();
            a.Build();
            return a;
        }

        void Build()
        {
            EnsureFolder(Root);
            EnsureFolder(TexDir);
            EnsureFolder(MatDir);

            _urp = GraphicsSettings.defaultRenderPipeline != null;
            _lit = _urp ? Shader.Find("Universal Render Pipeline/Lit") : null;
            if (_lit == null) { _lit = Shader.Find("Standard"); _urp = false; }

            // ---- textures ----
            var tGlass = FacadeGlass("tex_glass", out var eGlass, new Color(0.13f, 0.19f, 0.28f), 101);
            var tGlassT = FacadeGlass("tex_glass_teal", out var eGlassT, new Color(0.1f, 0.2f, 0.22f), 202);
            var tApt = FacadeApartment("tex_apartment", out var eApt, new Color(0.83f, 0.85f, 0.86f), 303);
            var tAptG = FacadeApartment("tex_apartment_grey", out var eAptG, new Color(0.62f, 0.65f, 0.68f), 404);
            var tOffice = FacadeOffice("tex_office", out var eOffice, new Color(0.56f, 0.55f, 0.53f), 505);
            var tOfficeD = FacadeOffice("tex_office_dark", out var eOfficeD, new Color(0.25f, 0.26f, 0.28f), 606);
            var tShopC = FacadeShop("tex_shop_cream", out var eShopC, new Color(0.86f, 0.8f, 0.66f), 707);
            var tShopM = FacadeShop("tex_shop_mint", out var eShopM, new Color(0.66f, 0.79f, 0.72f), 808);
            var tShopP = FacadeShop("tex_shop_peach", out var eShopP, new Color(0.88f, 0.7f, 0.6f), 909);
            var tShopG = FacadeShop("tex_shop_grey", out var eShopG, new Color(0.7f, 0.7f, 0.68f), 111);

            var tAsphalt = Noise("tex_asphalt", 256, new Color(0.2f, 0.2f, 0.21f), 0.12f, 1, cracks: true);
            var tPave = Tiles("tex_pavement", 256, 4, new Color(0.56f, 0.55f, 0.52f), new Color(0.4f, 0.4f, 0.38f), 2);
            var tPlaza = Tiles("tex_plaza", 256, 2, new Color(0.62f, 0.58f, 0.52f), new Color(0.42f, 0.38f, 0.34f), 3);
            var tGrass = Noise("tex_grass", 256, new Color(0.25f, 0.33f, 0.16f), 0.3f, 4);
            var tDirt = Noise("tex_dirt", 256, new Color(0.42f, 0.34f, 0.25f), 0.25f, 5);
            var tSand = Noise("tex_sand", 256, new Color(0.62f, 0.52f, 0.38f), 0.15f, 6);
            var tConcrete = Noise("tex_concrete", 256, new Color(0.6f, 0.6f, 0.58f), 0.12f, 7, stains: true);
            var tRust = Noise("tex_rust", 256, new Color(0.45f, 0.26f, 0.15f), 0.4f, 8, stains: true);
            var tWood = Stripes("tex_wood", 256, 8, new Color(0.48f, 0.33f, 0.2f), new Color(0.36f, 0.24f, 0.14f), false, 9);
            var tWoodD = Stripes("tex_wood_dark", 256, 8, new Color(0.3f, 0.2f, 0.13f), new Color(0.2f, 0.13f, 0.08f), false, 10);
            var tHay = Noise("tex_hay", 256, new Color(0.78f, 0.66f, 0.36f), 0.35f, 11, streaks: true);
            var tCorr = Stripes("tex_corrugated", 256, 16, new Color(0.5f, 0.56f, 0.56f), new Color(0.34f, 0.38f, 0.38f), true, 12);
            var tRoofTile = Stripes("tex_rooftile", 256, 10, new Color(0.58f, 0.27f, 0.18f), new Color(0.38f, 0.17f, 0.12f), false, 13);
            var tShutter = Stripes("tex_shutter", 256, 32, new Color(0.62f, 0.63f, 0.62f), new Color(0.42f, 0.43f, 0.43f), false, 14);
            var tBrick = Bricks("tex_brick", 256, 15);
            var tFloor = Tiles("tex_floortile", 256, 4, new Color(0.82f, 0.82f, 0.8f), new Color(0.6f, 0.6f, 0.58f), 16);
            var tLeaves = Noise("tex_leaves", 128, new Color(0.2f, 0.3f, 0.14f), 0.4f, 17);
            var tBark = Stripes("tex_bark", 128, 12, new Color(0.3f, 0.23f, 0.17f), new Color(0.2f, 0.15f, 0.11f), true, 18);

            // ---- materials ----
            Asphalt = Mat("Asphalt", Color.white, 0.25f, 0f, tAsphalt, 8f);
            Pavement = Mat("Pavement", Color.white, 0.2f, 0f, tPave, 4f);
            Plaza = Mat("Plaza", Color.white, 0.3f, 0f, tPlaza, 6f);
            Curb = Mat("Curb", new Color(0.66f, 0.66f, 0.64f), 0.15f, 0f, tConcrete, 2f);
            Grass = Mat("Grass", Color.white, 0.05f, 0f, tGrass, 6f);
            Dirt = Mat("Dirt", Color.white, 0.05f, 0f, tDirt, 5f);
            Sand = Mat("ArenaSand", Color.white, 0.02f, 0f, tSand, 4f);
            LaneWhite = Mat("LaneWhite", new Color(0.8f, 0.8f, 0.76f), 0.3f, 0f);
            LaneYellow = Mat("LaneYellow", new Color(0.82f, 0.64f, 0.18f), 0.3f, 0f);
            Crosswalk = Mat("Crosswalk", new Color(0.75f, 0.75f, 0.72f), 0.3f, 0f);

            GlassBlue = Mat("Facade_GlassBlue", Color.white, 0.9f, 0.6f, tGlass, 1f, eGlass, new Color(1.6f, 1.45f, 1.2f));
            GlassTeal = Mat("Facade_GlassTeal", Color.white, 0.88f, 0.6f, tGlassT, 1f, eGlassT, new Color(1.5f, 1.45f, 1.3f));
            GlassDark = Mat("Facade_GlassDark", new Color(0.6f, 0.62f, 0.7f), 0.92f, 0.7f, tGlass, 1f, eGlass, new Color(0.9f, 0.85f, 0.7f));
            AptWhite = Mat("Facade_ApartmentWhite", Color.white, 0.2f, 0f, tApt, 1f, eApt, new Color(1.3f, 1.25f, 1.05f));
            AptGrey = Mat("Facade_ApartmentGrey", Color.white, 0.2f, 0f, tAptG, 1f, eAptG, new Color(1.3f, 1.2f, 1f));
            Office = Mat("Facade_Office", Color.white, 0.3f, 0f, tOffice, 1f, eOffice, new Color(1.4f, 1.3f, 1.05f));
            OfficeDark = Mat("Facade_OfficeDark", Color.white, 0.4f, 0.1f, tOfficeD, 1f, eOfficeD, new Color(1.4f, 1.3f, 1.05f));
            ShopCream = Mat("Facade_ShopCream", Color.white, 0.12f, 0f, tShopC, 1f, eShopC, new Color(1.3f, 1.1f, 0.8f));
            ShopMint = Mat("Facade_ShopMint", Color.white, 0.12f, 0f, tShopM, 1f, eShopM, new Color(1.3f, 1.1f, 0.8f));
            ShopPeach = Mat("Facade_ShopPeach", Color.white, 0.12f, 0f, tShopP, 1f, eShopP, new Color(1.3f, 1.1f, 0.8f));
            ShopGrey = Mat("Facade_ShopGrey", Color.white, 0.12f, 0f, tShopG, 1f, eShopG, new Color(1.3f, 1.1f, 0.8f));

            Plaster = Mat("Plaster", new Color(0.88f, 0.86f, 0.8f), 0.1f, 0f, tConcrete, 3f);
            PlasterYellow = Mat("PlasterYellow", new Color(0.92f, 0.82f, 0.58f), 0.1f, 0f, tConcrete, 3f);
            PlasterBlue = Mat("PlasterBlue", new Color(0.66f, 0.76f, 0.86f), 0.1f, 0f, tConcrete, 3f);
            RoofTile = Mat("RoofTile", Color.white, 0.2f, 0f, tRoofTile, 3f);
            RoofMetal = Mat("RoofMetal", Color.white, 0.45f, 0.5f, tCorr, 3f);
            RoofConcrete = Mat("RoofConcrete", new Color(0.55f, 0.55f, 0.54f), 0.1f, 0f, tConcrete, 6f);
            Trim = Mat("Trim", new Color(0.18f, 0.18f, 0.2f), 0.3f, 0.2f);
            Concrete = Mat("Concrete", Color.white, 0.12f, 0f, tConcrete, 3f);
            Metal = Mat("MetalDark", new Color(0.24f, 0.25f, 0.27f), 0.5f, 0.8f);
            Chrome = Mat("Chrome", new Color(0.7f, 0.72f, 0.74f), 0.8f, 1f);
            Rust = Mat("Rust", Color.white, 0.15f, 0.3f, tRust, 2f);
            Shutter = Mat("Shutter", Color.white, 0.35f, 0.6f, tShutter, 3f);
            WindowDark = Mat("WindowDark", new Color(0.1f, 0.13f, 0.18f), 0.92f, 0.3f);
            WindowLit = Mat("WindowLit", new Color(0.9f, 0.8f, 0.55f), 0.6f, 0f, null, 1f, null, new Color(1.6f, 1.25f, 0.7f));

            AwningRed = Mat("AwningRed", new Color(0.62f, 0.16f, 0.14f), 0.2f, 0f);
            AwningBlue = Mat("AwningBlue", new Color(0.16f, 0.3f, 0.55f), 0.2f, 0f);
            AwningGreen = Mat("AwningGreen", new Color(0.18f, 0.45f, 0.3f), 0.2f, 0f);
            SignRed = Mat("SignRed", new Color(0.8f, 0.15f, 0.12f), 0.5f, 0f, null, 1f, null, new Color(1.6f, 0.2f, 0.15f));
            SignYellow = Mat("SignYellow", new Color(0.9f, 0.75f, 0.2f), 0.5f, 0f, null, 1f, null, new Color(1.6f, 1.2f, 0.25f));
            SignBlue = Mat("SignBlue", new Color(0.2f, 0.4f, 0.9f), 0.5f, 0f, null, 1f, null, new Color(0.25f, 0.6f, 1.8f));
            SignWhite = Mat("SignWhite", new Color(0.9f, 0.9f, 0.88f), 0.5f, 0f, null, 1f, null, new Color(1.5f, 1.5f, 1.4f));
            SignGreen = Mat("SignGreen", new Color(0.2f, 0.8f, 0.4f), 0.5f, 0f, null, 1f, null, new Color(0.3f, 1.6f, 0.6f));
            LampBulb = Mat("LampBulb_Cool", new Color(0.95f, 0.97f, 1f), 0.5f, 0f, null, 1f, null, new Color(3f, 3.1f, 3.3f), true);
            LampWarm = Mat("LampBulb_Warm", new Color(1f, 0.85f, 0.6f), 0.5f, 0f, null, 1f, null, new Color(3.4f, 2.4f, 1.2f), true);
            AviationRed = Mat("AviationRed", new Color(1f, 0.1f, 0.1f), 0.5f, 0f, null, 1f, null, new Color(4f, 0.2f, 0.2f));
            PoliceBlue = Mat("PoliceBlue", new Color(0.12f, 0.2f, 0.4f), 0.25f, 0f);
            PoliceLight = Mat("PoliceLight", new Color(0.2f, 0.4f, 1f), 0.5f, 0f, null, 1f, null, new Color(0.4f, 0.8f, 3.5f));
            CrossGreen = Mat("VetCrossGreen", new Color(0.2f, 0.75f, 0.35f), 0.5f, 0f, null, 1f, null, new Color(0.3f, 2f, 0.6f));

            Wood = Mat("Wood", Color.white, 0.15f, 0f, tWood, 2f);
            WoodDark = Mat("WoodDark", Color.white, 0.2f, 0f, tWoodD, 2f);
            Hay = Mat("Hay", Color.white, 0.02f, 0f, tHay, 1.5f);
            FenceWhite = Mat("FenceWhite", new Color(0.85f, 0.84f, 0.8f), 0.2f, 0f);
            Water = Mat("Water", new Color(0.12f, 0.2f, 0.24f), 0.95f, 0f);
            Leaves = Mat("Leaves", Color.white, 0.1f, 0f, tLeaves, 3f);
            LeavesDark = Mat("LeavesDark", new Color(0.7f, 0.75f, 0.7f), 0.1f, 0f, tLeaves, 3f);
            Bark = Mat("Bark", Color.white, 0.05f, 0f, tBark, 1f);
            Brick = Mat("Brick", Color.white, 0.1f, 0f, tBrick, 2f);

            FloorTile = Mat("Int_FloorTile", Color.white, 0.55f, 0f, tFloor, 2f);
            FloorVinyl = Mat("Int_FloorVinyl", new Color(0.55f, 0.62f, 0.6f), 0.5f, 0f, tConcrete, 3f);
            RubberMat = Mat("Int_RubberMat", new Color(0.18f, 0.2f, 0.2f), 0.1f, 0f, tConcrete, 1f);
            WallInterior = Mat("Int_Wall", new Color(0.86f, 0.84f, 0.78f), 0.1f, 0f, tConcrete, 4f);
            WallClinic = Mat("Int_WallClinic", new Color(0.78f, 0.9f, 0.86f), 0.2f, 0f, tConcrete, 4f);
            WallPolice = Mat("Int_WallPolice", new Color(0.75f, 0.78f, 0.82f), 0.15f, 0f, tConcrete, 4f);
            Ceiling = Mat("Int_Ceiling", new Color(0.9f, 0.9f, 0.88f), 0.05f, 0f);
            Bars = Mat("Int_CellBars", new Color(0.3f, 0.32f, 0.34f), 0.6f, 0.9f);
            Fabric = Mat("Int_Fabric", new Color(0.24f, 0.3f, 0.36f), 0.05f, 0f);
            Leather = Mat("Int_Leather", new Color(0.36f, 0.2f, 0.11f), 0.35f, 0f);
            Paper = Mat("Int_Paper", new Color(0.9f, 0.88f, 0.82f), 0.05f, 0f);
            Screen = Mat("Int_Screen", new Color(0.1f, 0.2f, 0.25f), 0.8f, 0f, null, 1f, null, new Color(0.3f, 0.7f, 0.9f));
            Gold = Mat("Gold", new Color(0.8f, 0.62f, 0.25f), 0.7f, 1f);
            Glass = GlassMat("Glass_Display", new Color(0.75f, 0.88f, 0.92f, 0.16f));

            AssetDatabase.SaveAssets();
        }

        public bool IsUrp => _urp;

        // ------------------------------------------------------------------ materials

        Material Mat(string name, Color color, float smooth, float metal, Texture2D albedo = null, float tile = 1f,
                     Texture2D emissionMap = null, Color? emission = null, bool bakedEmissive = false)
        {
            string path = MatDir + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(_lit);
            else m.shader = _lit;

            // Meshes already carry UVs in meters / tile size, so materials never rescale.
            // 'tile' documents the intended repeat size and is kept for tweaking by hand.
            Vector2 scale = Vector2.one;
            SetColor(m, "_BaseColor", color); SetColor(m, "_Color", color);
            SetFloat(m, "_Smoothness", smooth); SetFloat(m, "_Glossiness", smooth);
            SetFloat(m, "_Metallic", metal);
            SetTex(m, "_BaseMap", albedo, scale); SetTex(m, "_MainTex", albedo, scale);

            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                SetColor(m, "_EmissionColor", emission.Value);
                SetTex(m, "_EmissionMap", emissionMap, scale);
                m.globalIlluminationFlags = bakedEmissive ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                SetColor(m, "_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
            m.enableInstancing = true;

            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>See-through glass for display cases (URP Lit or Standard, transparent).</summary>
        Material GlassMat(string name, Color tint)
        {
            Material m = Mat(name, tint, 0.95f, 0f);
            if (_urp)
            {
                SetFloat(m, "_Surface", 1f);
                SetFloat(m, "_Blend", 0f);
                SetFloat(m, "_SrcBlend", (float)BlendMode.SrcAlpha);
                SetFloat(m, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                SetFloat(m, "_SrcBlendAlpha", (float)BlendMode.One);
                SetFloat(m, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                SetFloat(m, "_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                SetFloat(m, "_Mode", 3f);
                SetFloat(m, "_SrcBlend", (float)BlendMode.One);
                SetFloat(m, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                SetFloat(m, "_ZWrite", 0f);
                m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.enableInstancing = false;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void SetColor(Material m, string p, Color c) { if (m.HasProperty(p)) m.SetColor(p, c); }
        static void SetFloat(Material m, string p, float f) { if (m.HasProperty(p)) m.SetFloat(p, f); }
        static void SetTex(Material m, string p, Texture t, Vector2 scale)
        {
            if (!m.HasProperty(p)) return;
            m.SetTexture(p, t);
            m.SetTextureScale(p, t ? scale : Vector2.one);
        }

        // ------------------------------------------------------------------ textures

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        static float ValueNoise(float x, float y, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float a = Hash(xi, yi, seed), b = Hash(xi + 1, yi, seed), c = Hash(xi, yi + 1, seed), d = Hash(xi + 1, yi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>Tileable fractal noise in [0,1] on a size x size texture.</summary>
        static float Fbm(int px, int py, int size, int seed, int baseFreq = 4)
        {
            float sum = 0, amp = 0.5f, norm = 0;
            for (int o = 0; o < 4; o++)
            {
                int f = baseFreq << o;
                float x = (float)px / size * f, y = (float)py / size * f;
                // wrap lattice for seamless tiling
                int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
                float fx = x - xi, fy = y - yi;
                fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
                float a = Hash(xi % f, yi % f, seed + o), b = Hash((xi + 1) % f, yi % f, seed + o);
                float c = Hash(xi % f, (yi + 1) % f, seed + o), d = Hash((xi + 1) % f, (yi + 1) % f, seed + o);
                sum += Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy) * amp;
                norm += amp; amp *= 0.5f;
            }
            return sum / norm;
        }

        Texture2D Noise(string name, int size, Color baseColor, float amount, int seed, bool cracks = false, bool stains = false, bool streaks = false)
        {
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float n = Fbm(x, y, size, seed);
                    float k = 1f + (n - 0.5f) * 2f * amount;
                    k += (Hash(x, y, seed + 99) - 0.5f) * amount * 0.5f;
                    if (cracks)
                    {
                        float c = Mathf.Abs(Fbm(x, y, size, seed + 7, 2) - 0.5f);
                        if (c < 0.012f) k *= 0.6f;
                    }
                    if (stains) k *= Mathf.Lerp(0.8f, 1.05f, Mathf.SmoothStep(0.3f, 0.7f, Fbm(x, y, size, seed + 3, 2)));
                    if (streaks) k *= 0.85f + 0.3f * Hash(x / 2, 0, seed + 5);
                    px[y * size + x] = baseColor * k;
                }
            return Save(name, size, px, false);
        }

        Texture2D Tiles(string name, int size, int count, Color face, Color grout, int seed)
        {
            var px = new Color[size * size];
            int cell = size / count;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int cx = x / cell, cy = y / cell;
                    bool edge = x % cell < 2 || y % cell < 2;
                    float v = 0.9f + 0.15f * Hash(cx, cy, seed) + (Fbm(x, y, size, seed) - 0.5f) * 0.15f;
                    px[y * size + x] = edge ? grout : face * v;
                }
            return Save(name, size, px, false);
        }

        Texture2D Stripes(string name, int size, int count, Color a, Color b, bool smooth, int seed)
        {
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float t = (float)y / size * count;
                    float s = smooth ? 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f) : (t - Mathf.Floor(t) < 0.18f ? 0f : 1f);
                    float n = 0.92f + (Fbm(x, y, size, seed) - 0.5f) * 0.25f;
                    px[y * size + x] = Color.Lerp(b, a, s) * n;
                }
            return Save(name, size, px, false);
        }

        Texture2D Bricks(string name, int size, int seed)
        {
            var px = new Color[size * size];
            int bh = size / 16, bw = size / 8;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int row = y / bh;
                    int xo = (x + (row % 2) * bw / 2) % size;
                    bool mortar = y % bh < 2 || xo % bw < 2;
                    float v = 0.85f + 0.25f * Hash(xo / bw, row, seed);
                    px[y * size + x] = mortar ? new Color(0.55f, 0.52f, 0.48f) : new Color(0.5f, 0.27f, 0.2f) * v;
                }
            return Save(name, size, px, false);
        }

        // Facades: the texture covers N x N window cells; emission maps light random windows.

        Texture2D FacadeGlass(string name, out Texture2D emission, Color glass, int seed)
        {
            const int size = 512, cells = 8, cs = size / cells;
            var px = new Color[size * size];
            var em = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int cx = x / cs, cy = y / cs, lx = x % cs, ly = y % cs;
                    bool mullion = lx < 3 || lx >= cs - 3;
                    bool slab = ly < 9;
                    float sky = (float)y / size;
                    Color c = glass * (0.85f + 0.35f * sky + 0.12f * Hash(cx, cy, seed));
                    // faint interior silhouettes behind glass
                    if (ly > 12 && ly < 30 && Hash(cx * 3 + lx / 20, cy, seed + 7) > 0.65f) c *= 0.75f;
                    if (slab) c = new Color(0.08f, 0.09f, 0.11f);
                    if (mullion) c = new Color(0.2f, 0.22f, 0.25f);
                    px[y * size + x] = c;

                    float lit = Hash(cx, cy, seed + 13);
                    Color e = Color.black;
                    if (!mullion && !slab && lit > 0.7f)
                    {
                        float warm = Hash(cx, cy, seed + 17);
                        Color tone = warm > 0.3f ? new Color(1f, 0.82f, 0.55f) : new Color(0.85f, 0.92f, 1f);
                        // ceiling panel highlight near the top of the pane
                        float grad = Mathf.Lerp(0.45f, 1f, (float)(ly - 9) / (cs - 9));
                        e = tone * grad * (0.6f + 0.4f * lit);
                    }
                    em[y * size + x] = e;
                }
            emission = Save(name + "_emission", size, em, true);
            return Save(name, size, px, false);
        }

        Texture2D FacadeApartment(string name, out Texture2D emission, Color wall, int seed)
        {
            const int size = 256, cells = 4, cs = size / cells;
            var px = new Color[size * size];
            var em = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int cx = x / cs, cy = y / cs, lx = x % cs, ly = y % cs;
                    float dirt = 0.9f + 0.12f * Fbm(x, y, size, seed);
                    Color c = wall * dirt;
                    if (ly < 3) c *= 0.8f; // floor line
                    bool win = lx > 12 && lx < cs - 12 && ly > 16 && ly < cs - 8;
                    bool frame = win && (lx < 15 || lx > cs - 15 || ly < 19 || ly > cs - 11 || Mathf.Abs(lx - cs / 2) < 1);
                    if (win) c = frame ? new Color(0.55f, 0.56f, 0.57f) : new Color(0.12f, 0.15f, 0.2f) * (0.8f + 0.4f * (float)ly / cs);
                    px[y * size + x] = c;
                    float lit = Hash(cx, cy, seed + 13);
                    em[y * size + x] = (win && !frame && lit > 0.62f)
                        ? (Hash(cx, cy, seed + 3) > 0.35f ? new Color(1f, 0.85f, 0.6f) : new Color(0.75f, 0.95f, 0.85f)) * (0.55f + 0.45f * lit)
                        : Color.black;
                }
            emission = Save(name + "_emission", size, em, true);
            return Save(name, size, px, false);
        }

        Texture2D FacadeOffice(string name, out Texture2D emission, Color stone, int seed)
        {
            const int size = 384, cells = 6, cs = size / cells;
            var px = new Color[size * size];
            var em = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int cx = x / cs, cy = y / cs, lx = x % cs, ly = y % cs;
                    Color c = stone * (0.88f + 0.18f * Fbm(x, y, size, seed, 8));
                    bool win = lx > 14 && lx < cs - 14 && ly > 14 && ly < cs - 10;
                    if (win) c = new Color(0.1f, 0.13f, 0.17f) * (0.9f + 0.3f * Hash(cx, cy, seed));
                    px[y * size + x] = c;
                    float lit = Hash(cx, cy, seed + 13);
                    em[y * size + x] = (win && lit > 0.72f) ? new Color(1f, 0.86f, 0.62f) * (0.5f + 0.5f * lit) : Color.black;
                }
            emission = Save(name + "_emission", size, em, true);
            return Save(name, size, px, false);
        }

        Texture2D FacadeShop(string name, out Texture2D emission, Color wall, int seed)
        {
            const int size = 256, cells = 4, cs = size / cells;
            var px = new Color[size * size];
            var em = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int cx = x / cs, cy = y / cs, lx = x % cs, ly = y % cs;
                    // rain streaks running down from every window sill
                    float streak = Fbm(x * 4 % size, y / 4, size, seed + 5, 8);
                    Color c = wall * (0.82f + 0.2f * Fbm(x, y, size, seed) - 0.12f * Mathf.SmoothStep(0.5f, 0.8f, streak));
                    if (ly < 4) c *= 0.75f;
                    bool win = lx > 10 && lx < cs - 10 && ly > 14 && ly < cs - 12;
                    bool grille = win && (lx % 6 == 0 || ly % 8 == 0);
                    bool varied = Hash(cx, cy, seed + 1) > 0.8f; // some bays are boarded or have AC units
                    if (win) c = varied ? new Color(0.35f, 0.33f, 0.3f) : (grille ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.12f, 0.14f, 0.17f));
                    px[y * size + x] = c;
                    float lit = Hash(cx, cy, seed + 13);
                    em[y * size + x] = (win && !grille && !varied && lit > 0.7f) ? new Color(1f, 0.78f, 0.5f) * (0.5f + 0.5f * lit) : Color.black;
                }
            emission = Save(name + "_emission", size, em, true);
            return Save(name, size, px, false);
        }

        Texture2D Save(string name, int size, Color[] px, bool isEmission)
        {
            var t = new Texture2D(size, size, TextureFormat.RGB24, false);
            t.SetPixels(px);
            t.Apply();
            string path = TexDir + "/" + name + ".png";
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Default;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.mipmapEnabled = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.anisoLevel = 4;
            imp.maxTextureSize = 512;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.sRGBTexture = true;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
