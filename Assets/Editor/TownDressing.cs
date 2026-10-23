using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ScammerNoScamming;
using Object = UnityEngine.Object;
using Random = System.Random;

// Town art pass (owner: claude2). Called by FrontierLevelBuilder after the street blockout.
// Siam Phirom: one main street (x = 0, z 62 .. -46), raised board walkways, two-storey shophouses with balconies,
// false fronts, Thai signboards, lamps, hitching rails, barrels and power lines. Everything is generated from
// photo-scanned Poly Haven PBR textures (Assets/Art/Town) on combined meshes with world-scale UVs: one mesh per
// material per building, so the whole town stays cheap to render and every wall board keeps its real-world size.
public static class TownDressing
{
    const string Art = "Assets/Art/Town/";
    const float Y0 = .28f;            // boardwalk / shop floor height above the ground (<= the player's step offset, so it can be climbed anywhere)
    const float FrontX = 11.4f;       // facade line: |x| of the front wall
    const float WalkDepth = 3.7f;     // boardwalk, facade to street edge
    const float PorchDepth = 2.7f;    // covered part of the walkway (posts stand here)
    const float H1 = 3.5f, H2 = 3.1f; // storey heights

    static Terrain terrain;
    const float Gain = 1.4f;          // scans are photographed in shade; lift them so the street reads at golden hour

    public static void Build(Transform level)
    {
        AssetDatabase.Refresh();
        terrain = Terrain.activeTerrain;
        matCache.Clear(); tiles.Clear();
        var old = level.Find("TownArt");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var town = level.Find("Town");
        if (town != null)
            for (int i = town.childCount - 1; i >= 0; i--)
            {
                var child = town.GetChild(i);
                if (child.name != "TownGate" && child.name != "WaterTower") Object.DestroyImmediate(child.gameObject);
            }
        var root = new GameObject("TownArt").transform;
        root.SetParent(level, false);
        InitMaterials();
        // Soft cool fill from the shade side (sky bounce), so facades turned away from the low sun stay readable.
        var fill = new GameObject("TownFill").AddComponent<Light>();
        fill.transform.SetParent(root, false);
        fill.transform.rotation = Quaternion.LookRotation(new Vector3(.6f, -.4f, -.6f));
        fill.type = LightType.Directional; fill.color = new Color(.62f, .72f, .95f); fill.intensity = 1.2f; fill.shadows = LightShadows.None;
        BuildStreet(root);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Scammer No Scamming/Town: Rebuild Dressing Only")]
    static void RebuildOnly()
    {
        var level = GameObject.Find("Level");
        if (level == null) { Debug.LogError("Open Frontier_Case01 first"); return; }
        Build(level.transform);
        Debug.Log("Town dressing rebuilt");
    }

    public static void DebugFillBoost()
    {
        var f = GameObject.Find("TownFill");
        if (f != null) { var l = f.GetComponent<Light>(); l.intensity = l.intensity > 5f ? 1.2f : 9f; Debug.Log("fill " + l.intensity + " type " + l.type + " mask " + l.cullingMask + " enabled " + l.enabled); }
    }

    static float GroundY(float x, float z) =>
        terrain != null ? terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y : 10f;

    // ================================================================ materials

    static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();
    static readonly Dictionary<Material, float> tiles = new Dictionary<Material, float>();

    static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

    static void Import(string path, bool normal, bool linear = false)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        bool dirty = false;
        if (normal && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
        if (linear && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
        if (ti.anisoLevel < 8) { ti.anisoLevel = 8; dirty = true; }
        if (dirty) ti.SaveAndReimport();
    }

    // Metallic (R) + smoothness (A) packed from the scan's roughness map, so every wall keeps its real gloss pattern.
    static Texture2D Mask(string id, float metal, float smooth)
    {
        string path = $"{Art}gen/{id}_m{Mathf.RoundToInt(metal * 100)}_s{Mathf.RoundToInt(smooth * 100)}.png";
        var existing = Load<Texture2D>(path);
        if (existing != null) return existing;
        WorldSceneArt.EnsureFolder(Art + "gen");
        var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        src.LoadImage(File.ReadAllBytes($"{Art}{id}_rough.jpg"));
        var px = src.GetPixels32();
        for (int i = 0; i < px.Length; i++)
            px[i] = new Color32((byte)(metal * 255f), 255, 0, (byte)(Mathf.Clamp01((1f - px[i].r / 255f) * smooth) * 255f));
        var dst = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        dst.SetPixels32(px);
        File.WriteAllBytes(path, dst.EncodeToPNG());
        Object.DestroyImmediate(src); Object.DestroyImmediate(dst);
        AssetDatabase.ImportAsset(path);
        Import(path, false, true);
        return Load<Texture2D>(path);
    }

    static Material NewMaterial(string key, string shader)
    {
        WorldSceneArt.EnsureFolder(Art + "Materials");
        string path = $"{Art}Materials/T_{key}.mat";
        var m = Load<Material>(path);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, path); }
        else if (m.shader.name != shader) m.shader = Shader.Find(shader);
        return m;
    }

    // Scanned PBR material. tile = metres of wall per texture repeat.
    static Material Pbr(string key, string id, Color tint, float tile, double smooth = .6, double metal = 0, double bump = 1)
    {
        if (matCache.TryGetValue(key, out var cached)) return cached;
        var m = NewMaterial(key, "Universal Render Pipeline/Lit");
        Import($"{Art}{id}_diff.jpg", false); Import($"{Art}{id}_nor.jpg", true);
        m.SetTexture("_BaseMap", Load<Texture2D>($"{Art}{id}_diff.jpg"));
        m.SetColor("_BaseColor", new Color(tint.r * Gain, tint.g * Gain, tint.b * Gain, 1f));
        m.SetTexture("_BumpMap", Load<Texture2D>($"{Art}{id}_nor.jpg"));
        m.EnableKeyword("_NORMALMAP");
        m.SetFloat("_BumpScale", (float)bump);
        m.SetTexture("_MetallicGlossMap", Mask(id, (float)metal, (float)smooth));
        m.EnableKeyword("_METALLICSPECGLOSSMAP");
        m.SetFloat("_Smoothness", 1f);
        m.SetFloat("_SmoothnessTextureChannel", 0f);
        EditorUtility.SetDirty(m);
        matCache[key] = m; tiles[m] = tile;
        return m;
    }

    static Material Flat(string key, Color c, float smooth = .3f, float metal = 0f, Color? emission = null, bool unlit = false)
    {
        if (matCache.TryGetValue(key, out var cached)) return cached;
        var m = NewMaterial(key, unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", c);
        if (!unlit)
        {
            m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
        }
        EditorUtility.SetDirty(m);
        matCache[key] = m; tiles[m] = 1f;
        return m;
    }

    // Small painted textures (posters, QR codes, clock face) generated once and cached as PNG.
    static Material Painted(string key, int w, int h, Func<int, int, Color> pixel, bool unlit = true, bool transparent = false)
    {
        if (matCache.TryGetValue(key, out var cached)) return cached;
        string path = $"{Art}gen/{key}.png";
        if (Load<Texture2D>(path) == null)
        {
            WorldSceneArt.EnsureFolder(Art + "gen");
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) t.SetPixel(x, y, pixel(x, y));
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
        }
        var m = NewMaterial(key, unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        m.SetTexture("_BaseMap", Load<Texture2D>(path));
        m.SetColor("_BaseColor", Color.white);
        if (!unlit) m.SetFloat("_Smoothness", .15f);
        if (transparent) SetTransparent(m);
        EditorUtility.SetDirty(m);
        matCache[key] = m; tiles[m] = 1f;
        return m;
    }

    static Material mDeck, mSkirt, mRoofIron, mRoofRust, mRoofWood, mShutter, mDoor, mGlass, mInterior, mGlow, mIron, mBrass, mCanvasA, mCanvasB,
        mCurtain, mPost, mRail, mGold, mWater, mStone, mHay, mSack, mLampGlass, mDark, mRope, mBarrel, mCrate, mLanternRed, mLeaf, mLeafB, mBloom, mRedPaint, mWhitePaint, mBluePaint, mSolar, mClock, mQr, mPaper, mInsulator, mWire, mInteriorGrad, mInteriorOld, mBulb, mRoofTile, mRoofMoss;
    static Dictionary<string, Material> walls;

    static void InitMaterials()
    {
        walls = new Dictionary<string, Material>
        {
            ["brown"] = Pbr("wall_brown", "planks_brown_10", new Color(.86f, .78f, .68f), 1.3f, .45),
            ["dark"] = Pbr("wall_dark", "wood_plank_wall", new Color(.8f, .74f, .68f), 1.3f, .5),
            ["blue"] = Pbr("wall_blue", "blue_painted_planks", new Color(.78f, .86f, .85f), 1.2f, .55),
            ["grey"] = Pbr("wall_grey", "distressed_painted_planks", new Color(.82f, .85f, .76f), 1.3f, .5),
            ["red"] = Pbr("wall_red", "red_plaster_weathered", new Color(.78f, .66f, .62f), 2.4f, .35),
            ["raw"] = Pbr("wall_raw", "raw_plank_wall", new Color(.7f, .62f, .55f), 1.4f, .4),
            ["log"] = Pbr("wall_log", "weathered_plank_siding", new Color(.85f, .75f, .65f), 1.3f, .5),
            ["weather"] = Pbr("wall_weather", "weathered_planks", new Color(.9f, .82f, .72f), 1.4f, .45),
        };
        mDeck = Pbr("deck", "wooden_planks", new Color(.66f, .55f, .45f), 1.2f, .45);
        mSkirt = Pbr("skirt", "weathered_planks", new Color(.72f, .62f, .52f), 1.4f, .3);
        mRoofIron = Pbr("roof_iron", "worn_corrugated_iron", new Color(.66f, .62f, .57f), 1.6f, .5, .3f);
        mRoofTile = Pbr("roof_tile", "clay_roof_tiles_02", new Color(.95f, .8f, .72f), 1.6f, .3);
        mRoofMoss = Pbr("roof_moss", "roof_tiles_14", new Color(1.1f, 1.05f, .95f), 1.5f, .3);
        mRoofRust = Pbr("roof_rust", "rusty_corrugated_iron", new Color(.95f, .82f, .74f), 1.6f, .45, .2f);
        mRoofWood = Pbr("roof_wood", "roof_planks", new Color(.55f, .45f, .38f), 1.5f, .35);
        mShutter = Pbr("shutter", "wood_shutter", new Color(.55f, .6f, .52f), 1.0f, .4);
        mDoor = Pbr("door", "wooden_gate", new Color(.8f, .66f, .54f), 1.4f, .4);
        mPost = Pbr("post", "weathered_brown_planks", new Color(.74f, .64f, .55f), 1.3f, .35);
        mRail = Pbr("rail", "dark_wooden_planks", new Color(.8f, .72f, .64f), 1.3f, .4);
        mCanvasA = Pbr("canvas_a", "hessian_230", new Color(.86f, .84f, .78f), .5f, .1);
        mCanvasB = Pbr("canvas_b", "hessian_230", new Color(.62f, .22f, .16f), .5f, .1);
        mCurtain = Pbr("curtain", "hessian_230", new Color(.62f, .5f, .38f), .4f, .1);
        mHay = Pbr("hay", "thatch_roof_angled", new Color(1.2f, 1.0f, .6f), .7f, .1);
        mSack = Pbr("sack", "hessian_230", new Color(.8f, .72f, .55f), .6f, .1);
        mGlass = Glass();
        mInterior = Flat("interior", new Color(.05f, .035f, .025f), 0f, 0f, null, true);
        mIron = Flat("iron", new Color(.07f, .07f, .075f), .45f, .85f);
        mBrass = Flat("brass", new Color(.62f, .45f, .16f), .6f, .9f);
        mGold = Flat("gold", new Color(.85f, .62f, .2f), .55f, .8f);
        mWater = Flat("water", new Color(.1f, .15f, .15f), .95f, 0f);
        mStone = Pbr("stone", "stonerock", new Color(.8f, .74f, .66f), 1.6f, .25);
        mLampGlass = Flat("lamp_glass", new Color(1f, .75f, .4f), .8f, 0f, new Color(3.2f, 1.7f, .6f));
        mDark = Flat("dark", new Color(.03f, .025f, .02f), .1f, 0f);
        mRope = Flat("rope", new Color(.45f, .36f, .22f), .1f, 0f);
        mBulb = Flat("bulb", new Color(2.2f, 1.25f, .5f), 0f, 0f, null, true);
        mBarrel = Pbr("barrel", "weathered_brown_planks", new Color(.85f, .7f, .55f), .8f, .35);
        mCrate = Pbr("crate", "raw_plank_wall", new Color(.62f, .52f, .42f), 1f, .3);
        mLanternRed = Flat("lantern_red", new Color(.7f, .08f, .04f), .5f, 0f, new Color(2.6f, .4f, .14f));
        mLeaf = Flat("leaf", new Color(.12f, .26f, .07f), .35f);
        mLeafB = Flat("leaf_b", new Color(.24f, .33f, .09f), .35f);
        mBloom = Flat("bloom", new Color(.85f, .2f, .28f), .3f);
        mRedPaint = Flat("red_paint", new Color(.5f, .07f, .05f), .5f);
        mWhitePaint = Flat("white_paint", new Color(.75f, .72f, .64f), .5f);
        mBluePaint = Flat("blue_paint", new Color(.08f, .18f, .4f), .5f);
        mSolar = Flat("solar", new Color(.03f, .06f, .13f), .92f, .3f);
        mInsulator = Flat("insulator", new Color(.3f, .42f, .38f), .75f);
        mWire = Flat("wire", new Color(.015f, .015f, .015f), .3f, .5f);
        mInteriorGrad = Painted("interior_dim", 128, 128, (x, y) => InteriorPixel(x, y, false));
        mGlow = Painted("interior_lit", 128, 128, (x, y) => InteriorPixel(x, y, true));
        mGlow.SetColor("_BaseColor", new Color(1.7f, 1.55f, 1.4f, 1f));
        mInteriorOld = Painted("interior_grad", 4, 64, (x, y) =>
        {
            float t = y / 63f, warm = Mathf.Exp(-Mathf.Pow((t - .45f) / .3f, 2f));
            return new Color(.05f + .24f * warm, .032f + .13f * warm, .02f + .07f * warm, 1f);
        });
        mClock = Painted("clock_face", 128, 128, (x, y) =>
        {
            float dx = (x - 63.5f) / 64f, dy = (y - 63.5f) / 64f, d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > 1f) return new Color(0, 0, 0, 0);
            if (d > .9f) return new Color(.15f, .1f, .06f, 1f);
            float ang = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg; if (ang < 0) ang += 360f;
            if (d > .76f && Mathf.Abs(Mathf.Repeat(ang + 15f, 30f) - 15f) < 2.2f) return new Color(.1f, .07f, .05f, 1f);
            if (d < .55f && Mathf.Abs(Mathf.DeltaAngle(ang, 305f)) < 1f / Mathf.Max(d, .2f) * 1.2f * .9f) return new Color(.08f, .05f, .04f, 1f);
            if (d < .78f && Mathf.Abs(Mathf.DeltaAngle(ang, 60f)) < 1f / Mathf.Max(d, .25f) * .7f) return new Color(.08f, .05f, .04f, 1f);
            if (d < .07f) return new Color(.5f, .1f, .08f, 1f);
            return new Color(.9f, .86f, .72f, 1f);
        });
        mQr = Painted("qr_prompt", 100, 125, (x, y) =>
        {
            if (y < 25) return new Color(.06f, .22f, .45f, 1f);
            int mx = x / 4, my = (y - 25) / 4;
            bool Finder(int fx, int fy) { int m = Mathf.Max(Mathf.Abs(mx - fx), Mathf.Abs(my - fy)); return m <= 3 && m != 2; }
            if (mx < 7 && my < 7) return Finder(3, 3) ? Color.black : Color.white;
            if (mx > 17 && my < 7) return Finder(21, 3) ? Color.black : Color.white;
            if (mx < 7 && my > 17) return Finder(3, 21) ? Color.black : Color.white;
            int h = (mx * 73856093) ^ (my * 19349663); h = (h >> 3) & 7;
            return h < 4 ? Color.black : Color.white;
        });
        mPaper = Painted("poster_paper", 64, 96, (x, y) =>
        {
            float n = Mathf.PerlinNoise(x * .13f, y * .13f) * .18f;
            float dx = (x - 32f) / 18f, dy = (y - 52f) / 22f;
            if (dx * dx + dy * dy < 1f) return new Color(.25f, .2f, .15f, 1f);
            if (y > 80 && y < 90 && x > 8 && x < 56) return new Color(.18f, .12f, .08f, 1f);
            if (y > 8 && y < 14 && x > 14 && x < 50) return new Color(.18f, .12f, .08f, 1f);
            if (y > 20 && y < 24 && x > 20 && x < 44) return new Color(.3f, .2f, .12f, 1f);
            return new Color(.76f - n, .66f - n, .47f - n, 1f);
        });
    }

    // Shop interior seen through a window: back wall with shelves and jars, warm light falling off toward the top.
    static Color InteriorPixel(int x, int y, bool lit)
    {
        float t = y / 127f, glow = lit ? Mathf.Exp(-Mathf.Pow((t - .45f) / .45f, 2f)) : .25f;
        Color wall = lit ? new Color(.95f, .5f, .2f) * (.35f + .65f * glow) : new Color(.1f, .06f, .035f) * (.5f + glow);
        int shelfY = y % 34;
        bool shelf = shelfY < 3 && y > 12 && y < 122;
        if (shelf) return (lit ? new Color(.35f, .18f, .08f) : new Color(.06f, .035f, .02f));
        if (x % 40 < 2) return wall * .45f;
        if (shelfY >= 3 && shelfY < 17 && y > 12 && y < 122)
        {
            int cell = x / 9, h = (cell * 73856093 + (y / 34) * 19349663) & 15;
            if (h < 8 && x % 9 < 6 && shelfY < 4 + 6 + h % 7)
            {
                Color[] pal = { new Color(.7f, .15f, .1f), new Color(.2f, .45f, .2f), new Color(.2f, .3f, .6f), new Color(.75f, .55f, .15f), new Color(.5f, .4f, .3f) };
                var c = pal[h % 5];
                return lit ? c * .75f : c * .22f;
            }
        }
        return wall;
    }

    static Material Glass()
    {
        if (matCache.TryGetValue("glass_t", out var cached)) return cached;
        var m = NewMaterial("glass_t", "Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", new Color(.55f, .68f, .72f, .16f));
        m.SetFloat("_Smoothness", .97f); m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_SrcBlend", 5f); m.SetFloat("_DstBlend", 10f); m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetShaderPassEnabled("ShadowCaster", false);
        m.renderQueue = 3000;
        EditorUtility.SetDirty(m);
        matCache["glass_t"] = m; tiles[m] = 1f;
        return m;
    }

    static Material Paint(Color c, float smooth = .35f)
    {
        string key = "paint_" + ColorUtility.ToHtmlStringRGB(c);
        if (matCache.TryGetValue(key, out var m)) return m;
        m = Pbr(key, "paintbase", c, 1.2f, smooth);
        return m;
    }

    // ================================================================ combined-mesh builder

    sealed class Batch
    {
        sealed class Data { public List<Vector3> v = new List<Vector3>(), n = new List<Vector3>(); public List<Vector4> t = new List<Vector4>(); public List<Vector2> uv = new List<Vector2>(); public List<int> i = new List<int>(); }
        public readonly Transform root;
        readonly string name;
        readonly Dictionary<Material, Data> data = new Dictionary<Material, Data>();
        readonly List<(Vector3 c, Vector3 s)> boxes = new List<(Vector3, Vector3)>();
        readonly List<(string id, Vector3 p, float yaw, float scale)> scans = new List<(string, Vector3, float, float)>();
        readonly List<(Vector3 p, Color c, float i, float r)> lights = new List<(Vector3, Color, float, float)>();
        readonly Stack<Matrix4x4> stack = new Stack<Matrix4x4>();
        Matrix4x4 m = Matrix4x4.identity;

        public Batch(Transform root, string name) { this.root = root; this.name = name; }

        public void Push(Vector3 pos, Quaternion rot) { stack.Push(m); m = m * Matrix4x4.TRS(pos, rot, Vector3.one); }
        public void Pop() { m = stack.Pop(); }

        Data D(Material mat) { if (!data.TryGetValue(mat, out var d)) data[mat] = d = new Data(); return d; }
        static float TileOf(Material mat) => tiles.TryGetValue(mat, out var t) ? t : 1f;

        static readonly Vector3[] FN = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down };
        static readonly Vector3[] FU = { Vector3.right, Vector3.right, Vector3.forward, Vector3.forward, Vector3.right, Vector3.right };
        static readonly Vector3[] FV = { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.forward, Vector3.forward };

        // skip: bit per face (0 +z, 1 -z, 2 +x, 3 -x, 4 +y, 5 -y). swap: planks run along the other axis.
        public void Box(Material mat, Vector3 c, Vector3 s, Quaternion? rot = null, bool swap = false, int skip = 0, float tile = 0f, Vector2 off = default)
        {
            var d = D(mat); float t = tile > 0f ? tile : TileOf(mat);
            var q = rot ?? Quaternion.identity;
            for (int f = 0; f < 6; f++)
            {
                if ((skip & (1 << f)) != 0) continue;
                Vector3 n = FN[f], tu = FU[f], tv = FV[f];
                if (swap) { var tmp = tu; tu = tv; tv = tmp; }
                float hu = Mathf.Abs(Vector3.Dot(tu, s)) * .5f, hv = Mathf.Abs(Vector3.Dot(tv, s)) * .5f;
                Vector3 fc = c + Vector3.Scale(n, s) * .5f;
                Vector3[] p = { fc - tu * hu - tv * hv, fc + tu * hu - tv * hv, fc + tu * hu + tv * hv, fc - tu * hu + tv * hv };
                Add(d, p, n, tu, tv, t, off, c, q);
            }
        }

        public void BoxAB(Material mat, float x0, float x1, float y0, float y1, float z0, float z1, bool swap = false, int skip = 0, float tile = 0f, Vector2 off = default) =>
            Box(mat, new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, (z0 + z1) / 2f), new Vector3(x1 - x0, y1 - y0, z1 - z0), null, swap, skip, tile, off);

        void Add(Data d, Vector3[] p, Vector3 n, Vector3 tu, Vector3 tv, float t, Vector2 off, Vector3 pivot, Quaternion q)
        {
            int b0 = d.v.Count;
            Vector3 wn = m.MultiplyVector(q * n).normalized, wt = m.MultiplyVector(q * tu).normalized;
            float w = Vector3.Dot(Vector3.Cross(n, tu), tv) >= 0f ? 1f : -1f;
            for (int k = 0; k < 4; k++)
            {
                d.uv.Add(new Vector2(Vector3.Dot(p[k], tu), Vector3.Dot(p[k], tv)) / t + off);
                d.v.Add(m.MultiplyPoint3x4(pivot + q * (p[k] - pivot)));
                d.n.Add(wn); d.t.Add(new Vector4(wt.x, wt.y, wt.z, w));
            }
            // winding so that Cross(b - a, c - a) points along the normal (Unity front face)
            var cross = Vector3.Cross(d.v[b0 + 1] - d.v[b0], d.v[b0 + 2] - d.v[b0]);
            if (Vector3.Dot(cross, wn) >= 0f) { d.i.Add(b0); d.i.Add(b0 + 1); d.i.Add(b0 + 2); d.i.Add(b0); d.i.Add(b0 + 2); d.i.Add(b0 + 3); }
            else { d.i.Add(b0); d.i.Add(b0 + 2); d.i.Add(b0 + 1); d.i.Add(b0); d.i.Add(b0 + 3); d.i.Add(b0 + 2); }
        }

        // Single-sided plane facing 'n'. tile <= 0: texture fills the quad once (signs, posters).
        public void Quad(Material mat, Vector3 c, Vector3 n, Vector3 up, float w, float h, float tile = 0f, bool both = false)
        {
            var d = D(mat);
            for (int side = 0; side < (both ? 2 : 1); side++)
            {
                Vector3 nn = side == 0 ? n : -n;
                Vector3 r = Vector3.Cross(nn, up).normalized;
                Vector3 u = Vector3.Cross(r, nn).normalized; // true up
                u = Vector3.Dot(u, up) >= 0f ? u : -u;
                Vector3 uu = tile > 0f ? new Vector3(1f / tile, 1f / tile, 0f) : new Vector3(1f / w, 1f / h, 0f);
                Vector3[] p = { c - r * w / 2f - u * h / 2f, c + r * w / 2f - u * h / 2f, c + r * w / 2f + u * h / 2f, c - r * w / 2f + u * h / 2f };
                int b0 = d.v.Count;
                for (int k = 0; k < 4; k++)
                {
                    float lu = (k == 1 || k == 2) ? w : 0f, lv = (k >= 2) ? h : 0f;
                    d.uv.Add(new Vector2(lu * uu.x, lv * uu.y));
                    d.v.Add(m.MultiplyPoint3x4(p[k]));
                    d.n.Add(m.MultiplyVector(nn).normalized);
                    var wt = m.MultiplyVector(r).normalized;
                    d.t.Add(new Vector4(wt.x, wt.y, wt.z, 1f));
                }
                var cross = Vector3.Cross(d.v[b0 + 1] - d.v[b0], d.v[b0 + 2] - d.v[b0]);
                if (Vector3.Dot(cross, m.MultiplyVector(nn)) >= 0f) { d.i.Add(b0); d.i.Add(b0 + 1); d.i.Add(b0 + 2); d.i.Add(b0); d.i.Add(b0 + 2); d.i.Add(b0 + 3); }
                else { d.i.Add(b0); d.i.Add(b0 + 2); d.i.Add(b0 + 1); d.i.Add(b0); d.i.Add(b0 + 3); d.i.Add(b0 + 2); }
            }
        }

        // Triangular gable wall: base from x0..x1 at y0, apex at (xa, y0 + h); front at z1, back at z0.
        public void Gable(Material mat, float x0, float x1, float xa, float y0, float h, float z0, float z1)
        {
            var d = D(mat); float t = TileOf(mat);
            for (int side = 0; side < 2; side++)
            {
                float z = side == 0 ? z1 : z0; Vector3 n = side == 0 ? Vector3.forward : Vector3.back;
                Vector3[] p = { new Vector3(x0, y0, z), new Vector3(x1, y0, z), new Vector3(xa, y0 + h, z) };
                int b0 = d.v.Count;
                foreach (var q in p)
                {
                    d.uv.Add(new Vector2(q.x, q.y) / t); d.v.Add(m.MultiplyPoint3x4(q)); d.n.Add(m.MultiplyVector(n).normalized);
                    d.t.Add(new Vector4(1f, 0f, 0f, 1f));
                }
                var cross = Vector3.Cross(d.v[b0 + 1] - d.v[b0], d.v[b0 + 2] - d.v[b0]);
                if (Vector3.Dot(cross, m.MultiplyVector(n)) >= 0f) { d.i.Add(b0); d.i.Add(b0 + 1); d.i.Add(b0 + 2); }
                else { d.i.Add(b0); d.i.Add(b0 + 2); d.i.Add(b0 + 1); }
            }
        }

        // Surface of revolution around local +Y: profile of (radius, height) points, smooth around, faceted along.
        public void Lathe(Material mat, Vector3 origin, (float r, float y)[] prof, int seg = 14, float tile = 0f, Quaternion? rot = null, float squash = 1f, bool swap = false)
        {
            var d = D(mat); float t = tile > 0f ? tile : TileOf(mat);
            var q = rot ?? Quaternion.identity;
            float maxR = prof.Max(p => p.r);
            for (int k = 0; k < prof.Length - 1; k++)
            {
                var a = prof[k]; var b = prof[k + 1];
                float dr = b.r - a.r, dy = b.y - a.y; float len = Mathf.Sqrt(dr * dr + dy * dy); if (len < 1e-5f) continue;
                Vector2 nrm = new Vector2(dy / len, -dr / len); // (radial, up) outward
                int b0 = d.v.Count;
                for (int s = 0; s <= seg; s++)
                {
                    float ang = s / (float)seg * Mathf.PI * 2f, ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                    for (int e = 0; e < 2; e++)
                    {
                        var pr = e == 0 ? a : b;
                        var local = new Vector3(ca * pr.r * squash, pr.y, sa * pr.r);
                        var nl = new Vector3(ca * nrm.x, nrm.y, sa * nrm.x).normalized;
                        d.v.Add(m.MultiplyPoint3x4(origin + q * local)); d.n.Add(m.MultiplyVector(q * nl).normalized);
                        var tg = m.MultiplyVector(q * new Vector3(-sa, 0f, ca)).normalized;
                        d.t.Add(new Vector4(tg.x, tg.y, tg.z, 1f));
                        d.uv.Add(swap ? new Vector2(pr.y / t, ang * maxR / t) : new Vector2(ang * maxR / t, pr.y / t));
                    }
                }
                for (int s = 0; s < seg; s++)
                {
                    int i0 = b0 + s * 2;
                    // verts: i0 (a, s) i0+1 (b, s) i0+2 (a, s+1) i0+3 (b, s+1)
                    var tri0 = (i0, i0 + 1, i0 + 2); var tri1 = (i0 + 2, i0 + 1, i0 + 3);
                    foreach (var tri in new[] { tri0, tri1 })
                    {
                        var cr = Vector3.Cross(d.v[tri.Item2] - d.v[tri.Item1], d.v[tri.Item3] - d.v[tri.Item1]);
                        bool ok = Vector3.Dot(cr, d.n[tri.Item1]) >= 0f;
                        d.i.Add(tri.Item1); d.i.Add(ok ? tri.Item2 : tri.Item3); d.i.Add(ok ? tri.Item3 : tri.Item2);
                    }
                }
            }
        }

        // Square-section bar from a to b.
        public void Between(Material mat, Vector3 a, Vector3 b, float thick, float tile = 0f, float thick2 = 0f)
        {
            var dir = b - a; if (dir.sqrMagnitude < 1e-8f) return;
            Box(mat, (a + b) / 2f, new Vector3(thick, thick2 > 0f ? thick2 : thick, dir.magnitude), Quaternion.LookRotation(dir.normalized, Mathf.Abs(Vector3.Dot(dir.normalized, Vector3.up)) > .98f ? Vector3.forward : Vector3.up), true, 0, tile);
        }

        public void Col(Vector3 c, Vector3 s) => boxes.Add((c, s));
        public void Scan(string id, Vector3 p, float yaw, float scale = 1f) => scans.Add((id, m.MultiplyPoint3x4(p), yaw, scale));
        public void Light(Vector3 p, Color c, float intensity, float range) => lights.Add((m.MultiplyPoint3x4(p), c, intensity, range));

        public Transform Build()
        {
            foreach (var kv in data)
            {
                var d = kv.Value; if (d.v.Count == 0) continue;
                var go = new GameObject(kv.Key.name.Replace("T_", ""));
                go.transform.SetParent(root, false);
                var mesh = new Mesh { name = name + "_" + kv.Key.name };
                if (d.v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(d.v); mesh.SetNormals(d.n); mesh.SetTangents(d.t); mesh.SetUVs(0, d.uv); mesh.SetTriangles(d.i, 0);
                mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = kv.Key;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
            }
            foreach (var (id, sp, yaw, sc) in scans) TownDressing.Scan(root, id, root.TransformPoint(sp), root.eulerAngles.y + yaw, sc);
            foreach (var (p, c, i, rg) in lights)
            {
                var lg = new GameObject("Light").AddComponent<Light>();
                lg.transform.SetParent(root, false); lg.transform.localPosition = p;
                lg.type = LightType.Point; lg.color = c; lg.intensity = i; lg.range = rg; lg.shadows = LightShadows.None;
            }
            if (boxes.Count > 0)
            {
                var col = new GameObject("Colliders");
                col.transform.SetParent(root, false);
                foreach (var (c, s) in boxes) { var bc = col.AddComponent<BoxCollider>(); bc.center = c; bc.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)); }
            }
            return root;
        }
    }

    // ================================================================ street layout

    sealed class Lot
    {
        public string sign, blade, kind = "gable", lo = "brown", hi = "brown";
        public float w = 9f, d = 10f;
        public int floors = 2, seed;
        public bool balcony = true, alley;
        public Color trim = new Color(.78f, .72f, .58f), board = new Color(.12f, .28f, .22f), ink = new Color(.95f, .86f, .6f), awning;
        public string roof = "iron", extra = "";
    }

    static Lot L(string sign, string blade, float w, string kind, int floors, string lo, string hi, Color trim, Color board, Color ink, string roof = "iron", bool balcony = true, string extra = "") =>
        new Lot { sign = sign, blade = blade, w = w, kind = kind, floors = floors, lo = lo, hi = hi, trim = trim, board = board, ink = ink, roof = roof, balcony = balcony, extra = extra };
    static Lot Alley(float w, string extra = "") => new Lot { alley = true, w = w, extra = extra };
    static Color C(int hex) => new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f);

    static void BuildStreet(Transform root)
    {
        var cream = C(0xC9BD9A); var teal = C(0x4F7F78); var oxblood = C(0x7A3228); var ochre = C(0xB88A3A); var paleblue = C(0x8FA7AD); var brownT = C(0x5B4330);
        var gold = C(0xF0D890); var white = C(0xF2EAD6);
        var west = new List<Lot>
        {
            L("ร้านชำสยามภิรมย์", "ของชำ", 12f, "false", 2, "brown", "brown", cream, C(0x1F3A2E), gold, "iron", true, "store"),
            L("ตัดผม ลุงชัย", "ตัดผม", 7.5f, "false", 1, "blue", "blue", white, C(0x20384A), white, "rust", false, "barber"),
            Alley(4f, "alley"),
            L("โรงแรมสยามภิรมย์", "โรงแรม", 17f, "false", 3, "grey", "grey", cream, C(0x3A1D1A), gold, "iron", true, "hotel"),
            L("น้ำชา ขนมไทย ป้าจันทร์", "น้ำชา", 9f, "gable", 2, "blue", "weather", teal, C(0x2C5A52), white, "tile", true, "tea"),
            Alley(3.2f),
            L("ร้านขายยา หมอสมชาย", "ยา", 9.5f, "false", 2, "dark", "dark", paleblue, C(0x22354A), white, "moss", true, "pharmacy"),
            L("โรงช่างไม้ ตาเหลี่ยม", "ช่างไม้", 11f, "low", 1, "raw", "raw", brownT, C(0x4A3320), cream, "rust", false, "carpenter"),
            L("ร้านทองเยาวราช", "ทอง", 8.5f, "gable", 2, "red", "red", ochre, C(0x5A1F1B), gold, "tile", true, "gold"),
            Alley(4f, "alley"),
            L("คอกม้าไทยสบาย", "คอกม้า", 15f, "low", 1, "log", "log", brownT, C(0x3B2A1C), cream, "wood", false, "stable"),
        };
        var east = new List<Lot>
        {
            L("โรงเตี๊ยมศรีสยาม", "เหล้า-ที่พัก", 15f, "false", 2, "dark", "dark", oxblood, C(0x2A1512), gold, "iron", true, "saloon"),
            L("ร้านเครื่องเหล็ก ตาสี", "เหล็ก", 9.5f, "false", 1, "log", "log", brownT, C(0x1D1D1F), cream, "rust", false, "hardware"),
            Alley(4f, "alley"),
            L("ไปรษณีย์โทรเลข", "โทรเลข", 8f, "gable", 2, "grey", "grey", white, C(0x233B52), white, "iron", true, "post"),
            L("ก๋วยเตี๋ยวเรือ ป้าแดง", "ก๋วยเตี๋ยว", 8.5f, "gable", 2, "weather", "weather", oxblood, C(0x7A2A1E), gold, "tile", true, "noodle"),
            Alley(3.4f),
            L("ร้านตัดเสื้อ นางแก้ว", "ตัดเสื้อ", 8.5f, "false", 2, "brown", "blue", teal, C(0x2F5A55), white, "iron", true, "tailor"),
            L("โรงรับจำนำ ทรัพย์สิน", "จำนำ", 9f, "false", 2, "red", "brown", ochre, C(0x2A2A2A), gold, "rust", true, "pawn"),
            L("ห้างขายยาจีน ฮั่วเฮง", "ยาจีน", 10f, "gable", 2, "red", "red", oxblood, C(0x1C1C1C), gold, "tile", true, "chinese"),
            Alley(4f, "alley"),
            L("ธนาคารสยามภิรมย์", "ธนาคาร", 13f, "false", 2, "red", "red", cream, C(0x14261F), gold, "moss", false, "bank"),
        };
        int seed = 100;
        foreach (int side in new[] { -1, 1 })
        {
            float z = 60f; int index = 0;
            foreach (var lot in side < 0 ? west : east)
            {
                lot.seed = seed++;
                lot.d = 8.5f + ((lot.seed * 7) % 5) * .9f;
                if (lot.alley) { BuildAlley(root, lot, side, z - lot.w / 2f, index); z -= lot.w; index++; continue; }
                float zc = z - lot.w / 2f;
                BuildShop(root, lot, side, zc, index++);
                z -= lot.w + 1f;
            }
        }
        Decals(root);
        Plaza(root);
        StationRoof(root);
        StreetFurniture(root);
        Horses(root);
    }

    // ================================================================ building

    static Quaternion Yaw(float a) => Quaternion.Euler(0f, a, 0f);

    static void BuildShop(Transform parent, Lot s, int side, float zc, int index)
    {
        var go = new GameObject($"Shop_{(side < 0 ? "W" : "E")}{index}_{s.extra}").transform;
        go.SetParent(parent, false);
        go.SetPositionAndRotation(new Vector3(side * FrontX, GroundY(side * (FrontX - 2f), zc), zc), Quaternion.Euler(0f, side < 0 ? 90f : -90f, 0f));
        var b = new Batch(go, go.name);
        var rng = new Random(s.seed);
        float W = s.w, D = s.d, hw = W / 2f;
        var wallLo = walls[s.lo]; var wallHi = walls[s.hi]; var trim = Paint(s.trim);
        float Y1 = Y0 + H1, Yt = Y1 + (s.floors - 1) * H2;

        // ---- boardwalk (planks run across the walk), front fascia and under-deck skirt
        b.Box(mDeck, new Vector3(0f, Y0 - .09f, WalkDepth / 2f), new Vector3(W + .9f, .18f, WalkDepth), null, true);
        b.Box(mSkirt, new Vector3(0f, (Y0 - .18f - .5f) / 2f, WalkDepth - .04f), new Vector3(W + .9f, Y0 + .32f, .08f));
        b.Box(mPost, new Vector3(0f, Y0 - .04f, WalkDepth + .03f), new Vector3(W + .96f, .14f, .1f));   // edge board
        b.Col(new Vector3(0f, (Y0 - .5f) / 2f, WalkDepth / 2f), new Vector3(W + .9f, Y0 + .5f, WalkDepth));
        // steps from the street at the door and at both ends
        float stepX = (rng.Next(2) == 0 ? -1f : 1f) * Mathf.Min(hw - 1.2f, 1.6f + (float)rng.NextDouble() * 2f);
        b.Box(mDeck, new Vector3(stepX, .14f, WalkDepth + .35f), new Vector3(1.6f, .28f, .62f), null, true);
        b.Box(mSkirt, new Vector3(stepX, .14f, WalkDepth + .67f), new Vector3(1.64f, .28f, .04f));

        // ---- shell: side walls, back wall, skirt below the floor
        float wy0 = -.5f;
        SideWalls(b, s, wallLo, wallHi, trim, rng, W, D, Yt, wy0);
        BackWall(b, s, wallLo, wallHi, trim, rng, W, D, Yt, wy0);
        b.Col(new Vector3(0f, (wy0 + Yt) / 2f, -D / 2f), new Vector3(W, Yt - wy0, D));

        // ---- facade, storey by storey
        var groundOps = GroundOps(W, rng, s);
        FacadeBand(b, wallLo, Y0 - .05f, Y1, groundOps, W);
        for (int f = 1; f < s.floors; f++)
        {
            float yf = Y1 + (f - 1) * H2;
            var ops = UpperOps(W, yf, s, f, rng);
            FacadeBand(b, wallHi, yf, yf + H2, ops, W);
            foreach (var op in ops) Opening(b, op, trim, rng, s);
            // belt course between storeys
            b.BoxAB(trim, -hw - .05f, hw + .05f, yf - .14f, yf + .1f, -.02f, .07f);
        }
        foreach (var op in groundOps) Opening(b, op, trim, rng, s);
        // corner boards
        foreach (float sx in new[] { -1f, 1f })
            b.BoxAB(trim, sx < 0 ? -hw - .03f : hw - .2f, sx < 0 ? -hw + .2f : hw + .03f, Y0 - .05f, Yt, -.22f, .05f);

        // ---- porch: posts, beams, balcony, roof
        Porch(b, s, W, Y1, Yt, trim, rng);
        // ---- roof and top of the facade
        Roof(b, s, W, D, Yt, trim, wallHi, rng);
        // ---- signs
        SignBoard(b, s, W, Y1, Yt, trim, rng);
        // ---- lived-in dressing
        Dress(b, s, W, D, Y1, rng, groundOps);
        Backyard(b, s, W, D, Y1, rng);
        {
            var door = groundOps.First(o => o.door); float dxl = (door.x0 + door.x1) / 2f;
            float lt = s.balcony ? Y1 - .3f : Y1 + .05f;
            HangLantern(b, new Vector3(dxl - 1.4f, lt, PorchDepth - .55f), s.seed % 2 == 0);
            HangLantern(b, new Vector3(dxl + 1.4f, lt, PorchDepth - .55f), s.seed % 2 == 1);
        }

        b.Build();
        Texts(go, s, W, Y1, Yt, rng);
    }

    // Side walls as small facades so alleys show windows, shutters and a back door instead of blank boards.
    static void SideWalls(Batch b, Lot s, Material wallLo, Material wallHi, Material trim, Random r, float W, float D, float Yt, float wy0)
    {
        float hw = W / 2f, L = D - .2f, Y1 = Y0 + H1;
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Push(new Vector3(sx * hw, 0f, -.2f - L / 2f), Quaternion.Euler(0f, sx * 90f, 0f));
            int n = Mathf.Max(2, Mathf.RoundToInt(L / 3.3f));
            float slot = L / n;
            var ground = new List<Op>(); var upper = new List<List<Op>>();
            for (int f = 1; f < s.floors; f++) upper.Add(new List<Op>());
            for (int i = 0; i < n; i++)
            {
                float cx = -L / 2f + slot * (i + .5f);
                bool backDoor = i == n - 1 && sx > 0f;
                ground.Add(backDoor
                    ? new Op { x0 = cx - .6f, x1 = cx + .6f, y0 = Y0 - .05f, y1 = Y0 + 2.45f, door = true }
                    : new Op { x0 = cx - .55f, x1 = cx + .55f, y0 = Y0 + .95f, y1 = Y0 + 2.5f, glow = r.Next(4) == 0, variant = 2 });
                for (int f = 1; f < s.floors; f++)
                {
                    float yf = Y1 + (f - 1) * H2;
                    upper[f - 1].Add(new Op { x0 = cx - .5f, x1 = cx + .5f, y0 = yf + .7f, y1 = yf + 2.4f, glow = r.Next(4) == 0, variant = r.Next(3) });
                }
            }
            FacadeBand(b, wallLo, wy0, Y1, ground, L);
            foreach (var op in ground) Opening(b, op, trim, r, s);
            for (int f = 1; f < s.floors; f++)
            {
                float yf = Y1 + (f - 1) * H2;
                FacadeBand(b, wallHi, yf, yf + H2, upper[f - 1], L);
                foreach (var op in upper[f - 1]) Opening(b, op, trim, r, s);
            }
            if (s.kind == "low") FacadeBand(b, wallHi, Y1, Yt + .9f, new List<Op>(), L);
            // back door step and a lantern
            if (sx > 0f) { b.Box(mDeck, new Vector3(L / 2f - slot / 2f, .2f, .45f), new Vector3(1.4f, .4f, .8f), null, true); }
            b.Pop();
        }
    }

    // Back wall with windows, a rear door (with an outside stair up to the first floor on two-storey shops) and a lean-to kitchen.
    static void BackWall(Batch b, Lot s, Material wallLo, Material wallHi, Material trim, Random r, float W, float D, float Yt, float wy0)
    {
        float L = W - .4f, Y1 = Y0 + H1;
        b.Push(new Vector3(0f, 0f, -D), Quaternion.Euler(0f, 180f, 0f));
        int n = Mathf.Max(2, Mathf.RoundToInt(L / 3.4f));
        float slot = L / n;
        int doorSlot = n / 2;
        var ground = new List<Op>(); var upper = new List<List<Op>>();
        for (int f = 1; f < s.floors; f++) upper.Add(new List<Op>());
        float doorX = 0f;
        for (int i = 0; i < n; i++)
        {
            float cx = -L / 2f + slot * (i + .5f);
            if (i == doorSlot) { doorX = cx; ground.Add(new Op { x0 = cx - .6f, x1 = cx + .6f, y0 = Y0 - .05f, y1 = Y0 + 2.45f, door = true }); }
            else ground.Add(new Op { x0 = cx - .55f, x1 = cx + .55f, y0 = Y0 + .95f, y1 = Y0 + 2.5f, glow = r.Next(4) == 0, variant = 2 });
            for (int f = 1; f < s.floors; f++)
            {
                float yf = Y1 + (f - 1) * H2;
                bool bd = f == 1 && i == doorSlot;
                upper[f - 1].Add(new Op { x0 = cx - (bd ? .6f : .5f), x1 = cx + (bd ? .6f : .5f), y0 = bd ? yf + .08f : yf + .7f, y1 = yf + (bd ? 2.3f : 2.4f), balconyDoor = bd, glow = r.Next(4) == 0, variant = r.Next(3) });
            }
        }
        FacadeBand(b, wallLo, wy0, Y1, ground, L);
        foreach (var op in ground) Opening(b, op, trim, r, s);
        for (int f = 1; f < s.floors; f++)
        {
            float yf = Y1 + (f - 1) * H2;
            FacadeBand(b, wallHi, yf, yf + H2, upper[f - 1], L);
            foreach (var op in upper[f - 1]) Opening(b, op, trim, r, s);
        }
        if (s.kind == "low") FacadeBand(b, wallHi, Y1, Yt + .9f, new List<Op>(), L);
        if (s.floors > 1)
        {
            // landing at the upper door and a straight stair down to the yard (runs away from the wall)
            float top = Y1, run = 3.8f; int steps = 14; float rise = top / steps, tread = run / steps;
            float sx = doorX;
            b.Box(mDeck, new Vector3(sx, top - .05f, .65f), new Vector3(1.7f, .1f, 1.3f), null, true);
            foreach (float dx in new[] { -.8f, .8f }) b.Box(mPost, new Vector3(sx + dx, top / 2f, 1.25f), new Vector3(.14f, top, .14f), null, true, 0, 1f);
            for (int i = 0; i < steps; i++)
                b.Box(mDeck, new Vector3(sx, rise * (steps - 1 - i) + rise * .5f, 1.3f + i * tread + tread / 2f), new Vector3(1.1f, .06f, tread), null, true);
            foreach (float dx in new[] { -.58f, .58f })
            {
                b.Between(mPost, new Vector3(sx + dx, top - .1f, 1.3f), new Vector3(sx + dx, 0f, 1.3f + run), .08f, 1f, .22f);
                b.Between(mPost, new Vector3(sx + dx, top + .9f, 1.3f), new Vector3(sx + dx, .9f, 1.3f + run), .06f, 1f);
                b.Box(mPost, new Vector3(sx + dx, top + .45f, 1.3f), new Vector3(.08f, .95f, .08f), null, true, 0, 1f);
                b.Box(mPost, new Vector3(sx + dx, .45f, 1.3f + run), new Vector3(.08f, .95f, .08f), null, true, 0, 1f);
            }
            b.Col(new Vector3(sx, top / 2f, 1.3f + run / 2f), new Vector3(1.2f, top * .6f, run));
        }
        b.Pop();
    }

    static void Backyard(Batch b, Lot s, float W, float D, float Y1, Random r)
    {
        float hw = W / 2f, wallTop = Y0 + H1;
        // lean-to kitchen on the back wall with a stove pipe
        float kw = Mathf.Min(W * .4f, 4.2f), kx = (r.Next(2) == 0 ? -1f : 1f) * (hw - kw / 2f - .9f), kd = 2.8f, kh = 2.7f;
        if (s.floors == 1 || r.Next(2) == 0)
        {
            kx = (kx > 0 ? -1f : 1f) * (hw - kw / 2f - .9f);   // keep clear of the stair side
            var wall = walls["weather"];
            b.BoxAB(wall, kx - kw / 2f, kx + kw / 2f, -.4f, kh, -D - kd, -D - kd + .15f);
            b.BoxAB(wall, kx - kw / 2f, kx - kw / 2f + .15f, -.4f, kh, -D - kd, -D);
            b.BoxAB(wall, kx + kw / 2f - .15f, kx + kw / 2f, -.4f, kh, -D - kd, -D);
            b.Box(RoofMat(s), new Vector3(kx, kh + .35f, -D - kd / 2f + .2f), new Vector3(kw + .5f, .1f, kd + .9f), Quaternion.Euler(-12f, 0f, 0f));
            b.Between(mIron, new Vector3(kx + kw * .25f, kh + .2f, -D - kd * .55f), new Vector3(kx + kw * .25f, kh + 1.9f, -D - kd * .55f), .16f);
            b.Lathe(mIron, new Vector3(kx + kw * .25f, kh + 1.9f, -D - kd * .55f), new[] { (.2f, 0f), (.2f, .06f), (.12f, .14f) }, 8, 1f);
            b.Quad(mInteriorGrad, new Vector3(kx, 1.6f, -D - kd + .17f), Vector3.forward, Vector3.up, .9f, .9f);
            b.Col(new Vector3(kx, kh / 2f, -D - kd / 2f), new Vector3(kw, kh, kd));
        }
        // fence across the back with a gap, woodpile, rain barrel, hay, clothes line
        float fz = -D - 6.2f;
        int posts = Mathf.CeilToInt((W + 2f) / 1.9f);
        for (int i = 0; i <= posts; i++)
        {
            float x = -hw - 1f + i * (W + 2f) / posts;
            b.Box(mPost, new Vector3(x, .6f, fz), new Vector3(.12f, 1.2f, .12f), null, true, 0, 1f);
        }
        b.Box(mPost, new Vector3(0f, 1.0f, fz), new Vector3(W + 2.1f, .09f, .06f), null, true, 0, 1f);
        b.Box(mPost, new Vector3(0f, .55f, fz), new Vector3(W + 2.1f, .09f, .06f), null, true, 0, 1f);
        b.Col(new Vector3(0f, .6f, fz), new Vector3(W + 2.1f, 1.2f, .12f));
        float wx = (r.Next(2) == 0 ? -1f : 1f) * (hw - 1.6f);
        for (int row = 0; row < 3; row++)
            for (int i = 0; i < 5 - row; i++)
                b.Lathe(mPost, new Vector3(wx - 1f + row * .2f + i * .21f, .1f + row * .19f, -D - 3.7f), new[] { (.1f, -.5f), (.1f, .5f) }, 8, .5f, Quaternion.Euler(90f, 0f, 0f));
        Barrel(b, new Vector3(-wx, 0f, -D - .9f), .92f, .31f);
        if (r.Next(2) == 0) Hay(b, new Vector3(wx * -.4f, 0f, -D - 3.6f), r);
        // clothes line
        float lx0 = -hw + 1.2f, lx1 = lx0 + Mathf.Min(W - 2.4f, 6f), lz = -D - 4.6f;
        b.Box(mPost, new Vector3(lx0, 1.3f, lz), new Vector3(.08f, 2.6f, .08f), null, true, 0, 1f);
        b.Box(mPost, new Vector3(lx1, 1.3f, lz), new Vector3(.08f, 2.6f, .08f), null, true, 0, 1f);
        b.Between(mRope, new Vector3(lx0, 2.4f, lz), new Vector3(lx1, 2.3f, lz), .012f);
        int cloths = Mathf.RoundToInt((lx1 - lx0) / .75f);
        for (int i = 0; i < cloths; i++)
        {
            float t = (i + .5f) / cloths;
            var mat = i % 3 == 0 ? mCanvasB : i % 3 == 1 ? mCanvasA : mCurtain;
            b.Box(mat, new Vector3(Mathf.Lerp(lx0, lx1, t), 1.95f - (i % 2) * .15f, lz), new Vector3(.5f, .8f - (i % 2) * .3f, .02f), Quaternion.Euler(0f, (i % 2) * 8f, 0f));
        }
        // gable-end vent on the back for two-storey front gables
        // outhouse for some
        if (s.seed % 3 == 0)
        {
            float ox = (hw - .9f) * (wx > 0 ? -1f : 1f) * .5f - 2.5f, oz = -D - 5f;
            b.BoxAB(walls["raw"], ox - .55f, ox + .55f, -.1f, 2.1f, oz - .55f, oz + .55f);
            b.Box(RoofMat(s), new Vector3(ox, 2.3f, oz), new Vector3(1.4f, .08f, 1.5f), Quaternion.Euler(-10f, 0f, 0f));
            b.BoxAB(mDoor, ox - .35f, ox + .35f, 0f, 1.8f, oz + .55f, oz + .6f);
            b.Col(new Vector3(ox, 1f, oz), new Vector3(1.1f, 2.2f, 1.1f));
        }
    }

    sealed class Op { public float x0, x1, y0, y1; public bool door, balconyDoor, glow; public int variant; }

    static List<Op> GroundOps(float W, Random r, Lot s)
    {
        var ops = new List<Op>();
        float margin = .55f;
        int n = Mathf.Max(2, Mathf.RoundToInt((W - 2f * margin) / 3.5f));
        float slot = (W - 2f * margin) / n;
        int door = s.kind == "low" ? (n > 2 ? 1 : 0) : (n % 2 == 1 ? n / 2 : (r.Next(2) == 0 ? n / 2 - 1 : n / 2));
        for (int i = 0; i < n; i++)
        {
            float cx = -W / 2f + margin + slot * (i + .5f);
            if (i == door) ops.Add(new Op { x0 = cx - .8f, x1 = cx + .8f, y0 = Y0 - .05f, y1 = Y0 + 2.95f, door = true });
            else
            {
                float ww = Mathf.Min(slot - 1f, s.kind == "low" ? 2.4f : 2.6f);
                ops.Add(new Op { x0 = cx - ww / 2f, x1 = cx + ww / 2f, y0 = Y0 + .95f, y1 = Y0 + 2.8f, glow = r.Next(3) == 0, variant = r.Next(3) });
            }
        }
        return ops;
    }

    static List<Op> UpperOps(float W, float yf, Lot s, int floor, Random r)
    {
        var ops = new List<Op>();
        int n = Mathf.Max(2, Mathf.RoundToInt((W - 1.2f) / 3f));
        float slot = (W - 1.2f) / n;
        for (int i = 0; i < n; i++)
        {
            float cx = -W / 2f + .6f + slot * (i + .5f);
            bool bd = s.balcony && floor == 1 && (i % 2 == 0 || n == 2);
            float ww = bd ? 1.3f : 1.1f;
            ops.Add(new Op { x0 = cx - ww / 2f, x1 = cx + ww / 2f, y0 = bd ? yf + .08f : yf + .7f, y1 = yf + (bd ? 2.55f : 2.45f), balconyDoor = bd, glow = r.Next(3) == 0, variant = r.Next(3) });
        }
        return ops;
    }

    static void FacadeBand(Batch b, Material wall, float yA, float yB, List<Op> ops, float W)
    {
        float cur = -W / 2f;
        foreach (var op in ops.OrderBy(o => o.x0))
        {
            if (op.x0 > cur + .001f) b.BoxAB(wall, cur, op.x0, yA, yB, -.2f, 0f);
            if (op.y0 > yA + .001f) b.BoxAB(wall, op.x0, op.x1, yA, op.y0, -.2f, 0f);
            if (op.y1 < yB - .001f) b.BoxAB(wall, op.x0, op.x1, op.y1, yB, -.2f, 0f);
            cur = op.x1;
        }
        if (cur < W / 2f - .001f) b.BoxAB(wall, cur, W / 2f, yA, yB, -.2f, 0f);
    }

    // Window / door in the facade plane (z = 0, facing +z): reveal trim, glass, interior card, curtain, shutters.
    static void Opening(Batch b, Op op, Material trim, Random r, Lot s)
    {
        float w = op.x1 - op.x0, h = op.y1 - op.y0, cx = (op.x0 + op.x1) / 2f, cy = (op.y0 + op.y1) / 2f;
        const float tw = .1f;
        // casing: head, jambs (sill for windows)
        b.BoxAB(trim, op.x0 - tw, op.x1 + tw, op.y1, op.y1 + tw + .02f, -.02f, .07f);
        b.BoxAB(trim, op.x0 - tw, op.x0, op.y0, op.y1, -.02f, .05f);
        b.BoxAB(trim, op.x1, op.x1 + tw, op.y0, op.y1, -.02f, .05f);
        if (!op.door && !op.balconyDoor)
        {
            b.BoxAB(trim, op.x0 - tw - .03f, op.x1 + tw + .03f, op.y0 - .07f, op.y0, -.02f, .16f);       // sill
            b.BoxAB(trim, op.x0 - tw - .02f, op.x1 + tw + .02f, op.y0 - .17f, op.y0 - .07f, -.02f, .05f);   // apron
        }
        // interior card and glass
        b.Quad(mInteriorGrad, new Vector3(cx, cy, -.62f), Vector3.forward, Vector3.up, w + .8f, h + .8f);
        if (op.glow) b.Quad(mGlow, new Vector3(cx, cy + h * .05f, -.58f), Vector3.forward, Vector3.up, w * .8f, h * .8f);
        float gz = -.1f;
        if (!op.door)
        {
            b.Quad(mGlass, new Vector3(cx, cy, gz), Vector3.forward, Vector3.up, w, h);
            // muntins
            b.BoxAB(mPost, cx - .025f, cx + .025f, op.y0, op.y1, gz + .005f, gz + .05f);
            b.BoxAB(mPost, op.x0, op.x1, cy - .025f, cy + .025f, gz + .005f, gz + .05f);
            if (op.variant == 1) b.BoxAB(mPost, op.x0, op.x1, op.y1 - h * .22f - .02f, op.y1 - h * .22f + .02f, gz + .005f, gz + .05f);
            // curtain
            if (op.variant != 2) b.Quad(mCurtain, new Vector3(cx, op.y1 - h * .3f, gz + .12f), Vector3.forward, Vector3.up, w * .96f, h * .6f, .5f);
        }
        else
        {
            // door leaf set back in the reveal, transom above, brass handle
            float leafTop = op.y0 + 2.35f;
            b.BoxAB(mDoor, op.x0, op.x1, op.y0, leafTop, -.16f, -.09f);
            b.BoxAB(trim, op.x0 + .12f, op.x1 - .12f, op.y0 + .22f, leafTop - .12f, -.09f, -.07f);
            b.BoxAB(mDoor, op.x0 + .2f, op.x1 - .2f, op.y0 + .3f, leafTop - .22f, -.07f, -.05f);
            b.Quad(mGlass, new Vector3(cx, (leafTop + op.y1) / 2f, gz), Vector3.forward, Vector3.up, w, op.y1 - leafTop);
            b.BoxAB(mPost, op.x0, op.x1, leafTop, leafTop + .06f, -.16f, -.02f);
            b.Box(mBrass, new Vector3(op.x1 - .17f, op.y0 + 1.05f, -.04f), new Vector3(.06f, .06f, .06f));
            b.Box(mBrass, new Vector3(op.x1 - .17f, op.y0 + 1.05f, -.0f), new Vector3(.025f, .025f, .08f));
        }
        if (op.balconyDoor) b.Quad(mGlass, new Vector3(cx, cy, gz), Vector3.forward, Vector3.up, w, h);
        // shutters (windows and balcony doors only), alternately ajar
        if (!op.door && s.kind != "low")
        {
            float lw = Mathf.Min(w / 2f, .55f);
            Material sh = Shutter(s);
            foreach (float sx in new[] { -1f, 1f })
            {
                float hinge = sx < 0 ? op.x0 - tw : op.x1 + tw;
                float ang = op.variant == 0 ? 0f : (op.variant == 1 ? 38f : 78f);
                b.Push(new Vector3(hinge, 0f, .08f), Quaternion.Euler(0f, sx * -ang, 0f));
                b.BoxAB(sh, sx < 0 ? -lw : 0f, sx < 0 ? 0f : lw, op.y0 - .02f, op.y1 + .02f, 0f, .04f, false, 0, 0f, new Vector2(sx * .31f, 0f));
                b.Pop();
            }
        }
    }

    static Material Shutter(Lot s)
    {
        string key = "shutter_" + ColorUtility.ToHtmlStringRGB(s.board);
        if (matCache.TryGetValue(key, out var m)) return m;
        var c = Color.Lerp(s.board, Color.white, .25f);
        return Pbr(key, "wood_shutter", new Color(c.r * 1.1f, c.g * 1.1f, c.b * 1.1f), 1f, .4f);
    }

    // ---- porch, balcony, posts
    static void Porch(Batch b, Lot s, float W, float Y1, float Yt, Material trim, Random r)
    {
        float hw = W / 2f;
        int n = Mathf.Max(2, Mathf.CeilToInt((W - .8f) / 3.4f) + 1);
        float px0 = -hw + .22f, px1 = hw - .22f;
        float roofFront = s.balcony ? Y1 + H2 - .95f : Y1 + .1f;
        if (s.kind == "low") roofFront = Y1 - .25f;
        // post tops
        float postTop = s.balcony ? roofFront - .02f : Y1 + .1f;
        // awning / porch roof slab: from the wall down to the front
        float zFront = PorchDepth + .35f;
        float wallY = s.kind == "low" ? Y1 + .55f : roofFront + (zFront * Mathf.Tan(13f * Mathf.Deg2Rad));
        if (!s.balcony && s.kind != "low") wallY = Y1 + .55f;
        float ang = Mathf.Atan2(wallY - roofFront, zFront) * Mathf.Rad2Deg;
        Material roofMat = s.balcony || s.kind == "low" ? RoofMat(s) : (s.awning.a > 0 ? mCanvasB : (r.Next(2) == 0 ? mCanvasA : mCanvasB));
        float slabLen = Mathf.Sqrt(zFront * zFront + (wallY - roofFront) * (wallY - roofFront));
        float th = s.balcony || s.kind == "low" ? .1f : .06f;
        // slab, centred on its mid-line; rotated about x so the front edge drops
        Vector3 mid = new Vector3(0f, (wallY + roofFront) / 2f, zFront / 2f);
        b.Box(roofMat, mid + new Vector3(0f, th / 2f, 0f), new Vector3(W + .6f, th, slabLen), Quaternion.Euler(ang, 0f, 0f), false, 0, 0f);
        // fascia / valance with cut-out pattern
        b.Box(trim, new Vector3(0f, roofFront - .12f, zFront - .02f), new Vector3(W + .62f, .22f, .06f));
        int teeth = Mathf.FloorToInt((W + .4f) / .34f);
        for (int i = 0; i < teeth; i++)
            b.Box(trim, new Vector3(-W / 2f - .2f + (i + .5f) * ((W + .4f) / teeth), roofFront - .3f, zFront - .02f), new Vector3(.2f, .2f, .05f), Quaternion.Euler(0f, 0f, 45f));
        // posts at the porch edge
        for (int i = 0; i < n; i++)
        {
            float x = Mathf.Lerp(px0, px1, i / (float)(n - 1));
            b.Box(mPost, new Vector3(x, (postTop + 0f) / 2f, PorchDepth), new Vector3(.2f, postTop, .2f), null, true, 0, 1.2f);
            b.Box(trim, new Vector3(x, postTop - .1f, PorchDepth), new Vector3(.28f, .2f, .28f));
            b.Box(trim, new Vector3(x, .3f + Y0, PorchDepth), new Vector3(.27f, .3f, .27f));
            b.Col(new Vector3(x, postTop / 2f, PorchDepth), new Vector3(.22f, postTop, .22f));
            // diagonal brackets to the beam
            foreach (float sx in new[] { -1f, 1f })
            {
                if ((i == 0 && sx < 0) || (i == n - 1 && sx > 0)) continue;
                float bl = .55f;
                b.Between(mPost, new Vector3(x + sx * .1f, postTop - .55f, PorchDepth), new Vector3(x + sx * (.1f + bl), postTop - .06f, PorchDepth), .06f, 1f);
            }
        }
        // beam under the roof front
        b.Box(mPost, new Vector3(0f, roofFront - .02f, PorchDepth), new Vector3(W + .4f, .16f, .2f), null, true);
        if (s.balcony && s.floors > 1)
        {
            // balcony floor at the first-floor level, joists below, balustrade at the front and ends
            b.Box(mDeck, new Vector3(0f, Y1 - .01f, PorchDepth / 2f + .05f), new Vector3(W + .2f, .1f, PorchDepth + .1f), null, true);
            for (int i = 0; i < Mathf.FloorToInt(W / .65f); i++)
                b.Box(mPost, new Vector3(-W / 2f + .35f + i * .65f, Y1 - .2f, PorchDepth / 2f), new Vector3(.1f, .2f, PorchDepth), null, true);
            b.Box(trim, new Vector3(0f, Y1 - .24f, PorchDepth + .02f), new Vector3(W + .24f, .3f, .08f));
            Balustrade(b, new Vector3(-hw + .1f, Y1 + .04f, PorchDepth - .06f), new Vector3(hw - .1f, Y1 + .04f, PorchDepth - .06f), trim);
            Balustrade(b, new Vector3(-hw + .1f, Y1 + .04f, .12f), new Vector3(-hw + .1f, Y1 + .04f, PorchDepth - .06f), trim);
            Balustrade(b, new Vector3(hw - .1f, Y1 + .04f, .12f), new Vector3(hw - .1f, Y1 + .04f, PorchDepth - .06f), trim);
            b.Col(new Vector3(0f, Y1 - .1f, PorchDepth / 2f), new Vector3(W + .2f, .3f, PorchDepth));
        }
        else if (!s.balcony && s.kind != "low")
        {
            // low awning: hanging scallop edge handled by the valance; add a hitching post out front
        }
    }

    static Material RoofMat(Lot s) => s.roof == "wood" ? mRoofWood : s.roof == "rust" ? mRoofRust : s.roof == "tile" ? mRoofTile : s.roof == "moss" ? mRoofMoss : mRoofIron;

    static void Balustrade(Batch b, Vector3 a, Vector3 c, Material trim)
    {
        Vector3 dir = c - a; float len = dir.magnitude; Vector3 u = dir / len;
        b.Between(trim, a + Vector3.up * 1.0f, c + Vector3.up * 1.0f, .09f, 1f, .06f);
        b.Between(mPost, a + Vector3.up * .12f, c + Vector3.up * .12f, .07f, 1f, .06f);
        int n = Mathf.Max(2, Mathf.RoundToInt(len / .13f));
        for (int i = 0; i <= n; i++)
        {
            Vector3 p = a + u * (len * i / n);
            bool post = i == 0 || i == n;
            b.Box(post ? trim : mPost, p + Vector3.up * (post ? .53f : .55f), post ? new Vector3(.12f, 1.06f, .12f) : new Vector3(.04f, .86f, .04f), null, false, 0, 1f);
        }
    }

    // Two roof planes meeting at a ridge along the building depth. front: z of the front eave line; gableFront: draw the
    // front triangle flush with the facade (Thai front gable) instead of hiding it behind a parapet.
    static void GableRoof(Batch b, Lot s, float W, float D, float Yt, Material trim, Material wall, float pitch, float ov, float front, bool gableFront, bool finials)
    {
        float hw = W / 2f; var rm = RoofMat(s);
        float rad = pitch * Mathf.Deg2Rad, run = hw + ov, rise = Mathf.Tan(rad) * hw;
        b.Gable(wall, -hw, hw, 0f, Yt, rise, -D, gableFront ? 0f : front - .02f);
        float zBack = -D - .35f, zLen = front - zBack;
        foreach (float sx in new[] { -1f, 1f })
        {
            float len = run / Mathf.Cos(rad);
            Vector3 ridge = new Vector3(0f, Yt + rise + .02f, 0f), eave = new Vector3(sx * run, Yt - ov * Mathf.Tan(rad) + .02f, 0f);
            Vector3 midp = (ridge + eave) / 2f;
            Vector3 nrm = new Vector3(sx * Mathf.Sin(rad), Mathf.Cos(rad), 0f);
            b.Box(rm, midp + nrm * .05f + new Vector3(0f, 0f, (front + zBack) / 2f), new Vector3(len, .1f, zLen), Quaternion.Euler(0f, 0f, -sx * pitch), false, 0, 0f);
            // fascia along the eave and the front rake
            b.Box(trim, eave + new Vector3(0f, -.06f, (front + zBack) / 2f), new Vector3(.06f, .2f, zLen), Quaternion.Euler(0f, 0f, -sx * pitch));
            if (gableFront) b.Box(trim, midp + new Vector3(0f, -.1f, front - .04f), new Vector3(len + .1f, .28f, .05f), Quaternion.Euler(0f, 0f, -sx * pitch));
        }
        b.Box(rm, new Vector3(0f, Yt + rise + .1f, (front + zBack) / 2f), new Vector3(.3f, .12f, zLen + .06f));
        if (!finials) return;
        // vent in the gable and ridge finials ("chofa")
        b.Box(trim, new Vector3(0f, Yt + rise * .38f, .04f), new Vector3(.9f, .9f, .05f), Quaternion.Euler(0f, 0f, 45f));
        b.Quad(mDark, new Vector3(0f, Yt + rise * .38f, .075f), Vector3.forward, Vector3.up, .5f, .5f);
        b.Between(mGold, new Vector3(0f, Yt + rise + .0f, front - .02f), new Vector3(0f, Yt + rise + .7f, front + .5f), .07f);
        b.Between(mGold, new Vector3(0f, Yt + rise + .0f, zBack + .05f), new Vector3(0f, Yt + rise + .7f, zBack - .45f), .07f);
    }

    // ---- roof, parapet, gable
    static void Roof(Batch b, Lot s, float W, float D, float Yt, Material trim, Material wall, Random r)
    {
        float hw = W / 2f; var rm = RoofMat(s);
        if (s.kind == "gable")
        {
            GableRoof(b, s, W, D, Yt, trim, wall, 33f, .5f, .7f, true, true);
        }
        else if (s.kind == "false")
        {
            float ph = s.floors >= 3 ? 2.6f : 1.9f;
            float pw = W + .12f;
            // parapet with cornice, dentils and a framed sign panel (extra stepped / pediment variants)
            b.BoxAB(wall, -pw / 2f, pw / 2f, Yt, Yt + ph, -.28f, .09f);
            b.BoxAB(trim, -pw / 2f - .12f, pw / 2f + .12f, Yt + ph, Yt + ph + .22f, -.3f, .34f);
            b.BoxAB(trim, -pw / 2f - .05f, pw / 2f + .05f, Yt + ph - .1f, Yt + ph, -.3f, .22f);
            int dents = Mathf.FloorToInt(pw / .3f);
            for (int i = 0; i < dents; i++)
                b.Box(trim, new Vector3(-pw / 2f + (i + .5f) * pw / dents, Yt + ph - .16f, .24f), new Vector3(.12f, .14f, .1f));
            // pediment / raised centre
            float cw = Mathf.Min(W * .5f, 6f);
            if (s.floors >= 2 && (s.seed % 2 == 0 || s.floors == 3))
            {
                float up = s.floors == 3 ? 2.6f : 1.1f;
                b.BoxAB(wall, -cw / 2f, cw / 2f, Yt + ph + .22f, Yt + ph + .22f + up, -.28f, .09f);
                b.BoxAB(trim, -cw / 2f - .12f, cw / 2f + .12f, Yt + ph + .22f + up, Yt + ph + .42f + up, -.3f, .34f);
            }
            // real gable roof behind the parapet (ridge along the depth), flat-looking from the street, rich from above
            float pitch = Mathf.Min(30f, Mathf.Atan(2.3f / hw) * Mathf.Rad2Deg);
            GableRoof(b, s, W, D, Yt, trim, wall, Mathf.Max(pitch, 12f), .3f, -.3f, false, false);
            // chimney stack
            if (r.Next(3) != 0)
            {
                float cx = (r.Next(2) == 0 ? -1f : 1f) * (W / 2f - 1.6f);
                float cy = Yt + Mathf.Tan(Mathf.Max(pitch, 12f) * Mathf.Deg2Rad) * (hw - Mathf.Abs(cx));
                b.BoxAB(mStone, cx - .45f, cx + .45f, Yt - 1f, cy + 1.3f, -D + 1.2f, -D + 2.1f);
                b.BoxAB(mDark, cx - .5f, cx + .5f, cy + 1.3f, cy + 1.42f, -D + 1.15f, -D + 2.15f);
            }
        }
        else // low: wide shed roof sloping to the back, big eaves
        {
            float drop = 1.3f, back = D + .8f, front = .1f;
            float ang = Mathf.Atan2(drop, back + front) * Mathf.Rad2Deg;
            float len = Mathf.Sqrt((back + front) * (back + front) + drop * drop);
            b.Box(rm, new Vector3(0f, Yt + .55f - drop / 2f, (front - back) / 2f), new Vector3(W + 1f, .1f, len), Quaternion.Euler(-ang, 0f, 0f), false, 0, 0f);
            b.Box(trim, new Vector3(0f, Yt + .52f, front + .0f), new Vector3(W + 1f, .24f, .06f), Quaternion.Euler(-ang, 0f, 0f));
            b.BoxAB(wall, -hw, hw, Yt, Yt + .9f, -.2f, 0f);
        }
    }

    // ---- sign board with panel (text is added by Texts)
    static void SignBoard(Batch b, Lot s, float W, float Y1, float Yt, Material trim, Random r)
    {
        var board = Paint(s.board);
        if (s.kind == "false")
        {
            float ph = s.floors >= 3 ? 2.6f : 1.9f;
            b.BoxAB(board, -W / 2f + .5f, W / 2f - .5f, Yt + .3f, Yt + ph - .3f, .09f, .13f);
            b.BoxAB(trim, -W / 2f + .42f, W / 2f - .42f, Yt + ph - .3f, Yt + ph - .22f, .09f, .16f);
            b.BoxAB(trim, -W / 2f + .42f, W / 2f - .42f, Yt + .22f, Yt + .3f, .09f, .16f);
            b.BoxAB(trim, -W / 2f + .42f, -W / 2f + .5f, Yt + .22f, Yt + ph - .22f, .09f, .16f);
            b.BoxAB(trim, W / 2f - .5f, W / 2f - .42f, Yt + .22f, Yt + ph - .22f, .09f, .16f);
        }
        else
        {
            // low and gable shops carry the board over the ground-floor openings
            float y = Y1 - .62f;
            b.BoxAB(board, -W / 2f + .5f, W / 2f - .5f, y - .22f, y + .22f, .07f, .11f);
            b.BoxAB(trim, -W / 2f + .44f, W / 2f - .44f, y + .22f, y + .27f, .07f, .14f);
            b.BoxAB(trim, -W / 2f + .44f, W / 2f - .44f, y - .27f, y - .22f, .07f, .14f);
        }
        // hanging blade sign under the porch roof (visible face-on from the street axis)
        float bx = BladeX(s, W);
        float top = s.balcony && s.floors > 1 ? Y1 - .5f : Y1 - .05f;
        if (s.kind == "low") top = Y1 - .2f;
        b.Box(mIron, new Vector3(bx, top + .02f, PorchDepth - .85f), new Vector3(.04f, .04f, 1.4f));
        foreach (float dz in new[] { -.3f, .3f })
            b.Box(mIron, new Vector3(bx, top - .12f, PorchDepth - .85f + dz), new Vector3(.015f, .24f, .015f));
        b.BoxAB(board, bx - .035f, bx + .035f, top - .76f, top - .24f, PorchDepth - 1.35f, PorchDepth - .35f);
        b.BoxAB(trim, bx - .045f, bx + .045f, top - .79f, top - .76f, PorchDepth - 1.37f, PorchDepth - .33f);
        b.BoxAB(trim, bx - .045f, bx + .045f, top - .24f, top - .21f, PorchDepth - 1.37f, PorchDepth - .33f);
    }

    static float BladeX(Lot s, float W) => (s.seed % 2 == 0 ? -1f : 1f) * (W / 2f - 1.1f);

    static void Texts(Transform go, Lot s, float W, float Y1, float Yt, Random r)
    {
        var parent = go;
        // main board text, readable from the street (text front faces local +Z)
        if (s.kind == "false")
        {
            float ph = s.floors >= 3 ? 2.6f : 1.9f;
            Sign(parent, s.sign, new Vector3(0f, Yt + ph / 2f, .145f), Yaw(180f), new Vector2(W - 1.4f, ph - .8f), s.ink, .22f);
        }
        else Sign(parent, s.sign, new Vector3(0f, Y1 - .62f, .125f), Yaw(180f), new Vector2(W - 1.3f, .36f), s.ink, .16f);
        // blade sign on both faces
        float bx = BladeX(s, W);
        float top = s.balcony && s.floors > 1 ? Y1 - .5f : Y1 - .05f;
        if (s.kind == "low") top = Y1 - .2f;
        foreach (float sx in new[] { -1f, 1f })
            Sign(parent, s.blade, new Vector3(bx + sx * .05f, top - .5f, PorchDepth - .85f), Yaw(sx > 0 ? -90f : 90f), new Vector2(.8f, .42f), s.ink, .12f);
    }

    static GameObject Sign(Transform parent, string text, Vector3 pos, Quaternion rot, Vector2 area, Color color, float maxSize)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localRotation = rot;
        var tm = go.AddComponent<TextMesh>();
        tm.fontSize = 64; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = color; tm.richText = false;
        var wl = go.AddComponent<WorldLabel>();
        wl.width = area.x; wl.height = area.y; wl.maxSize = maxSize;
        wl.Text = text;
        WorldText.Apply(tm);
        return go;
    }

    // ================================================================ photo-scanned props (Poly Haven, CC0)

    static readonly Dictionary<string, Material> scanMats = new Dictionary<string, Material>();
    static readonly HashSet<string> scanLogged = new HashSet<string>();

    static Material ScanMaterial(string id)
    {
        if (scanMats.TryGetValue(id, out var cached) && cached != null) return cached;
        string dir = $"Assets/ThirdParty/PolyHaven/{id}/textures/";
        string diff = dir + id + "_diff_1k.jpg", nor = dir + id + "_nor_gl_1k.png", rough = dir + id + "_rough_1k.jpg";
        string cut = dir + id + "_diffalpha_1k.png";
        bool foliage = File.Exists(cut);
        if (foliage) diff = cut;
        Import(diff, false); Import(nor, true);
        if (foliage && AssetImporter.GetAtPath(diff) is TextureImporter ti && !ti.alphaIsTransparency) { ti.alphaIsTransparency = true; ti.SaveAndReimport(); }
        var m = NewMaterial("scan_" + id, "Universal Render Pipeline/Lit");
        m.SetTexture("_BaseMap", Load<Texture2D>(diff));
        m.SetColor("_BaseColor", new Color(1.15f, 1.15f, 1.15f, 1f));
        m.SetTexture("_BumpMap", Load<Texture2D>(nor)); m.EnableKeyword("_NORMALMAP");
        string maskPath = $"{Art}gen/scan_{id}_ms.png";
        var mask = Load<Texture2D>(maskPath);
        if (mask == null && File.Exists(rough))
        {
            WorldSceneArt.EnsureFolder(Art + "gen");
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false); src.LoadImage(File.ReadAllBytes(rough));
            var px = src.GetPixels32();
            for (int i = 0; i < px.Length; i++) px[i] = new Color32((byte)(id.Contains("brass") ? 220 : 0), 255, 0, (byte)((255 - px[i].r) * .8f));
            var dst = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false); dst.SetPixels32(px);
            File.WriteAllBytes(maskPath, dst.EncodeToPNG());
            Object.DestroyImmediate(src); Object.DestroyImmediate(dst);
            AssetDatabase.ImportAsset(maskPath); Import(maskPath, false, true);
            mask = Load<Texture2D>(maskPath);
        }
        if (mask != null) { m.SetTexture("_MetallicGlossMap", mask); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_Smoothness", 1f); }
        else m.SetFloat("_Smoothness", .25f);
        if (foliage)
        {
            m.SetColor("_BaseColor", new Color(.9f, .95f, .85f, 1f));
            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", .45f); m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", 0f); m.doubleSidedGI = true;
            m.SetOverrideTag("RenderType", "TransparentCutout"); m.renderQueue = 2450;
        }
        EditorUtility.SetDirty(m);
        scanMats[id] = m;
        return m;
    }

    static GameObject Scan(Transform parent, string id, Vector3 worldPos, float worldYaw, float scale)
    {
        var prefab = Load<GameObject>($"Assets/ThirdParty/PolyHaven/{id}/{id}_1k.fbx");
        if (prefab == null) { Debug.LogWarning("Missing scan " + id); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = id;
        go.transform.SetParent(parent, false);
        go.transform.localScale = prefab.transform.localScale * scale;
        go.transform.rotation = Quaternion.Euler(0f, worldYaw, 0f) * prefab.transform.localRotation;
        go.transform.position = worldPos;
        var mat = ScanMaterial(id);
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return go;
        var bounds = rends[0].bounds;
        foreach (var r in rends) { r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray(); bounds.Encapsulate(r.bounds); }
        go.transform.position += Vector3.up * (worldPos.y - bounds.min.y);
        go.transform.position += new Vector3(worldPos.x - bounds.center.x, 0f, worldPos.z - bounds.center.z);
        if (scanLogged.Add(id)) Debug.Log($"scan {id} bounds size {bounds.size:F2}");
        return go;
    }

    // ================================================================ props (building-local coordinates, y = 0 at the ground)

    static float BarrelR(float y, float h, float R) { float t = y / h; return R * (.84f + .16f * (1f - Mathf.Pow(2f * t - 1f, 2f))); }

    static void Barrel(Batch b, Vector3 p, float h = .92f, float R = .31f, Material mat = null)
    {
        var prof = new List<(float, float)> { (0f, 0f) };
        for (int i = 0; i <= 8; i++) { float y = h * i / 8f; prof.Add((BarrelR(y, h, R), y)); }
        prof.Add((0f, h));
        b.Lathe(mat ?? mBarrel, p, prof.ToArray(), 14, .8f, null, 1f, true);
        foreach (float f in new[] { .1f, .28f, .72f, .9f })
        {
            float y = h * f, rr = BarrelR(y, h, R) + .012f;
            b.Lathe(mIron, p, new[] { (rr, y - .025f), (rr, y + .025f) }, 14);
        }
        b.Col(p + Vector3.up * h / 2f, new Vector3(R * 2f, h, R * 2f));
    }

    static void Sack(Batch b, Vector3 p, float yaw, float tilt = 0f, float s = 1f)
    {
        var prof = new[] { (0f, 0f), (.15f * s, 0f), (.21f * s, .06f * s), (.24f * s, .2f * s), (.21f * s, .34f * s), (.12f * s, .42f * s), (.07f * s, .46f * s), (.1f * s, .5f * s), (.04f * s, .54f * s), (0f, .54f * s) };
        b.Lathe(mSack, p, prof, 10, .5f, Quaternion.Euler(tilt, yaw, 0f), 1.2f);
    }

    static void Crate(Batch b, Vector3 p, Vector3 size, float yaw)
    {
        b.Push(p, Quaternion.Euler(0f, yaw, 0f));
        b.Box(mCrate, new Vector3(0f, size.y / 2f, 0f), size, null, false, 0, 1f);
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
            b.Box(mPost, new Vector3(sx * (size.x / 2f - .017f), size.y / 2f, sz * (size.z / 2f - .017f)), new Vector3(.075f, size.y + .01f, .075f), null, true, 0, 1f);
        foreach (float sz in new[] { -1f, 1f })
            foreach (float fy in new[] { .22f, .78f })
                b.Box(mPost, new Vector3(0f, size.y * fy, sz * (size.z / 2f + .008f)), new Vector3(size.x - .1f, .07f, .016f), null, false, 0, 1f);
        b.Pop();
        b.Col(p + Vector3.up * size.y / 2f, new Vector3(Mathf.Max(size.x, size.z), size.y, Mathf.Max(size.x, size.z)));
    }

    static void HitchRail(Batch b, Vector3 a, float len)
    {
        foreach (float x in new[] { 0f, len })
        {
            b.Box(mPost, a + new Vector3(x, .62f, 0f), new Vector3(.14f, 1.24f, .14f), null, true, 0, 1f);
            b.Box(mIron, a + new Vector3(x, 1.2f, 0f), new Vector3(.17f, .03f, .17f));
        }
        b.Box(mPost, a + new Vector3(len / 2f, 1.04f, 0f), new Vector3(len + .12f, .1f, .1f), null, true, 0, 1f);
        b.Box(mPost, a + new Vector3(len / 2f, .66f, 0f), new Vector3(len, .07f, .07f), null, true, 0, 1f);
        b.Col(a + new Vector3(len / 2f, .6f, 0f), new Vector3(len + .14f, 1.2f, .14f));
    }

    static void Trough(Batch b, Vector3 c, float len)
    {
        const float h = .56f, w = .56f;
        b.Box(mPost, c + new Vector3(0f, .22f, 0f), new Vector3(len, .1f, w), null, true, 0, 1f);
        foreach (float sz in new[] { -1f, 1f }) b.Box(mCrate, c + new Vector3(0f, h / 2f + .1f, sz * (w / 2f - .03f)), new Vector3(len, h - .1f, .06f), null, true, 0, 1f);
        foreach (float sx in new[] { -1f, 1f }) b.Box(mCrate, c + new Vector3(sx * (len / 2f - .03f), h / 2f + .1f, 0f), new Vector3(.06f, h - .1f, w - .06f), null, true, 0, 1f);
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
            b.Box(mPost, c + new Vector3(sx * (len / 2f - .12f), .12f, sz * (w / 2f - .1f)), new Vector3(.1f, .24f, .1f), null, true, 0, 1f);
        b.Quad(mWater, c + new Vector3(0f, h - .08f, 0f), Vector3.up, Vector3.forward, len - .1f, w - .1f);
        b.Col(c + new Vector3(0f, h / 2f + .1f, 0f), new Vector3(len, h, w));
    }

    static void Bench(Batch b, Vector3 p, float len, float yaw)
    {
        b.Scan("painted_wooden_bench", p, yaw + BenchYaw, Mathf.Min(len / 1.09f, 1.25f));
        b.Col(p + Vector3.up * .4f, new Vector3(len, .8f, .5f));
    }

    const float BenchYaw = 0f, StoolYaw = 0f;

    static void Stool(Batch b, Vector3 p) => b.Scan("painted_wooden_stool", p, StoolYaw, 1f);

    static void Table(Batch b, Vector3 p, float lx, float lz)
    {
        b.Box(mCrate, p + new Vector3(0f, .74f, 0f), new Vector3(lx, .045f, lz), null, false, 0, 1f);
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
            b.Box(mPost, p + new Vector3(sx * (lx / 2f - .06f), .36f, sz * (lz / 2f - .06f)), new Vector3(.06f, .72f, .06f), null, true, 0, 1f);
        b.Col(p + Vector3.up * .4f, new Vector3(lx, .8f, lz));
    }

    static void Planter(Batch b, Vector3 p, float len, Random r)
    {
        b.Box(mCrate, p + new Vector3(0f, .17f, 0f), new Vector3(len, .34f, .3f), null, false, 0, 1f);
        b.Box(mDark, p + new Vector3(0f, .335f, 0f), new Vector3(len - .06f, .02f, .24f));
        int n = Mathf.RoundToInt(len / .22f);
        for (int i = 0; i < n; i++)
        {
            var leaf = r.Next(4) == 0 ? mLeafB : mLeaf;
            float x = -len / 2f + .12f + i * (len - .24f) / Mathf.Max(1, n - 1), hgt = .25f + (float)r.NextDouble() * .2f;
            b.Lathe(leaf, p + new Vector3(x, .34f, 0f), new[] { (0f, 0f), (.1f, .04f), (.12f, hgt * .5f), (.07f, hgt), (0f, hgt + .05f) }, 7, 1f, Quaternion.Euler((float)r.NextDouble() * 12f, 0f, 0f), 1f);
            if (r.Next(3) == 0) b.Lathe(mBloom, p + new Vector3(x + .04f, .34f + hgt, 0f), new[] { (0f, 0f), (.05f, .02f), (.045f, .06f), (0f, .08f) }, 6, 1f);
        }
    }

    static void PaperLantern(Batch b, Vector3 top, float s = 1f)
    {
        b.Box(mIron, top - Vector3.up * .08f * s, new Vector3(.012f, .16f * s, .012f));
        var prof = new[] { (0f, 0f), (.06f * s, 0f), (.14f * s, .05f * s), (.18f * s, .15f * s), (.18f * s, .27f * s), (.14f * s, .37f * s), (.06f * s, .42f * s), (0f, .42f * s) };
        b.Lathe(mLanternRed, top - Vector3.up * (.16f + .42f) * s, prof, 10, 1f);
        b.Box(mGold, top - Vector3.up * .165f * s, new Vector3(.12f * s, .025f, .12f * s));
        b.Box(mGold, top - Vector3.up * (.16f + .42f) * s, new Vector3(.12f * s, .025f, .12f * s));
    }

    static void HangLantern(Batch b, Vector3 top, bool light)
    {
        b.Box(mIron, top - Vector3.up * .1f, new Vector3(.012f, .2f, .012f));
        b.Lathe(mIron, top - Vector3.up * .22f, new[] { (0f, 0f), (.1f, 0f), (.02f, .08f), (0f, .08f) }, 8, 1f);
        b.Lathe(mLampGlass, top - Vector3.up * .56f, new[] { (0f, 0f), (.09f, 0f), (.115f, .05f), (.115f, .22f), (.07f, .3f), (0f, .3f) }, 10, 1f);
        b.Lathe(mIron, top - Vector3.up * .58f, new[] { (0f, 0f), (.1f, 0f), (.1f, .02f), (0f, .02f) }, 8, 1f);
        if (light) b.Light(top - Vector3.up * .42f, new Color(1f, .62f, .28f), 2.4f, 7f);
    }

    static void Wheel(Batch b, Vector3 c, float radius, float yaw)
    {
        b.Push(c, Quaternion.Euler(0f, yaw, 0f));
        int seg = 16;
        for (int i = 0; i < seg; i++)
        {
            float a0 = i * 360f / seg * Mathf.Deg2Rad, a1 = (i + 1) * 360f / seg * Mathf.Deg2Rad;
            Vector3 p0 = new Vector3(0f, Mathf.Sin(a0) * radius, Mathf.Cos(a0) * radius), p1 = new Vector3(0f, Mathf.Sin(a1) * radius, Mathf.Cos(a1) * radius);
            b.Between(mPost, p0, p1, .07f, 1f, .1f);
            b.Between(mIron, p0 * 1.0f, p1 * 1.0f, .08f, 1f, .02f);
        }
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            b.Between(mPost, Vector3.zero, new Vector3(0f, Mathf.Sin(a) * radius, Mathf.Cos(a) * radius), .045f, 1f);
        }
        b.Lathe(mPost, new Vector3(-.09f, 0f, 0f), new[] { (.08f, 0f), (.08f, .18f), (0f, .18f) }, 8, 1f, Quaternion.Euler(0f, 0f, -90f));
        b.Pop();
    }

    // Open farm wagon: plank bed, sideboards, four spoked wheels, tongue.
    static void Wagon(Batch b, Vector3 p, float yaw, Random r, bool canopy)
    {
        b.Push(p, Quaternion.Euler(0f, yaw, 0f));
        float L = 3.4f, Wd = 1.5f;
        b.Box(mCrate, new Vector3(0f, .95f, 0f), new Vector3(Wd, .12f, L), null, true, 0, 1f);
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Box(mCrate, new Vector3(sx * (Wd / 2f - .03f), 1.28f, 0f), new Vector3(.06f, .55f, L), null, true, 0, 1f);
            b.Box(mPost, new Vector3(sx * (Wd / 2f), .85f, 0f), new Vector3(.1f, .12f, L + .2f), null, true, 0, 1f);
        }
        foreach (float sz in new[] { -1f, 1f }) b.Box(mCrate, new Vector3(0f, 1.28f, sz * (L / 2f - .03f)), new Vector3(Wd, .55f, .06f), null, false, 0, 1f);
        foreach (float sz in new[] { -1f, 1f }) foreach (float sx in new[] { -1f, 1f }) Wheel(b, new Vector3(sx * (Wd / 2f + .1f), .62f, sz * 1.0f), .62f, 0f);
        b.Box(mPost, new Vector3(0f, .75f, 0f), new Vector3(.12f, .12f, L + 1f), null, true, 0, 1f);
        b.Between(mPost, new Vector3(0f, .75f, L / 2f + .1f), new Vector3(0f, .9f, L / 2f + 2.6f), .12f, 1f);
        if (canopy)
        {
            for (int i = 0; i < 5; i++)
            {
                float z = -L / 2f + .3f + i * (L - .6f) / 4f;
                for (int k = 0; k < 8; k++)
                {
                    float a0 = (-90f + k * 22.5f) * Mathf.Deg2Rad, a1 = (-90f + (k + 1) * 22.5f) * Mathf.Deg2Rad;
                    b.Between(mPost, new Vector3(Mathf.Sin(a0) * .75f, 1.55f + Mathf.Cos(a0) * .75f, z), new Vector3(Mathf.Sin(a1) * .75f, 1.55f + Mathf.Cos(a1) * .75f, z), .035f);
                }
            }
            for (int k = 0; k < 8; k++)
            {
                float a0 = (-90f + k * 22.5f) * Mathf.Deg2Rad, a1 = (-90f + (k + 1) * 22.5f) * Mathf.Deg2Rad;
                Vector3 q0 = new Vector3(Mathf.Sin(a0) * .8f, 1.55f + Mathf.Cos(a0) * .8f, 0f), q1 = new Vector3(Mathf.Sin(a1) * .8f, 1.55f + Mathf.Cos(a1) * .8f, 0f);
                b.Box(mCanvasA, (q0 + q1) / 2f, new Vector3(.02f, (q1 - q0).magnitude, L - .1f), Quaternion.Euler(0f, 0f, Mathf.Atan2(q1.y - q0.y, q1.x - q0.x) * Mathf.Rad2Deg - 90f), false, 0, 0f);
            }
        }
        else
        {
            // cargo: sacks and a crate
            for (int i = 0; i < 4; i++) Sack(b, new Vector3(-.3f + (i % 2) * .6f, 1.01f, -.9f + (i / 2) * .7f), r.Next(360), 0f, 1f);
            Crate(b, new Vector3(0f, 1.01f, 1.0f), new Vector3(.8f, .6f, .6f), 8f);
        }
        b.Pop();
        b.Col(p + Vector3.up * .9f, new Vector3(2.2f, 1.8f, 3.4f));
    }

    static void Barber(Batch b, Vector3 p)
    {
        b.Lathe(mIron, p, new[] { (.1f, 0f), (.1f, .08f), (.05f, .12f), (.05f, .2f), (0f, .2f) }, 10, 1f);
        for (int i = 0; i < 9; i++)
            b.Lathe(i % 3 == 0 ? mRedPaint : i % 3 == 1 ? mWhitePaint : mBluePaint, p + Vector3.up * (.2f + i * .12f), new[] { (.07f, 0f), (.07f, .12f) }, 10, 1f);
        b.Lathe(mLampGlass, p + Vector3.up * 1.28f, new[] { (0f, 0f), (.09f, 0f), (.09f, .22f), (.05f, .28f), (0f, .3f) }, 10, 1f);
        b.Col(p + Vector3.up * .8f, new Vector3(.2f, 1.6f, .2f));
    }

    static void SolarPanel(Batch b, Vector3 p, float w, float d, float tilt, float yaw)
    {
        b.Push(p, Quaternion.Euler(tilt, yaw, 0f));
        b.Box(mIron, new Vector3(0f, 0f, 0f), new Vector3(w + .06f, .04f, d + .06f));
        b.Quad(mSolar, new Vector3(0f, .025f, 0f), Vector3.up, Vector3.forward, w, d, 0f);
        b.Pop();
        b.Between(mIron, p + new Vector3(-w / 2f + .1f, -.5f, -d * .3f), p + new Vector3(-w / 2f + .1f, 0f, 0f), .04f);
        b.Between(mIron, p + new Vector3(w / 2f - .1f, -.5f, -d * .3f), p + new Vector3(w / 2f - .1f, 0f, 0f), .04f);
    }

    static void Dish(Batch b, Vector3 p, float yaw, float tilt)
    {
        b.Between(mIron, p, p + Vector3.up * .8f, .05f);
        var prof = new[] { (0f, 0f), (.18f, .015f), (.36f, .06f), (.5f, .14f), (.5f, .15f), (.36f, .08f), (.18f, .03f), (0f, .02f) };
        b.Lathe(mWhitePaint, p + Vector3.up * .85f, prof, 14, 1f, Quaternion.Euler(tilt, yaw, 0f));
        b.Between(mIron, p + Vector3.up * .85f, p + Vector3.up * .85f + Quaternion.Euler(tilt, yaw, 0f) * Vector3.up * .42f, .018f);
    }

    static void Clock(Batch b, Vector3 c, float r)
    {
        b.Lathe(mGold, c, new[] { (r + .1f, -.06f), (r + .1f, .0f), (r + .1f, .06f), (r + .02f, .08f) }, 24, 1f, Quaternion.Euler(90f, 0f, 0f));
        b.Quad(mClock, c + new Vector3(0f, 0f, .05f), Vector3.forward, Vector3.up, r * 2f, r * 2f);
    }

    static void QrPlacard(Batch b, Vector3 c, Vector3 n, float size)
    {
        b.Box(mWhitePaint, c - n * .015f, new Vector3(size + .03f, size * 1.25f + .03f, .02f) , Quaternion.LookRotation(n));
        b.Quad(mQr, c + n * .002f, n, Vector3.up, size, size * 1.25f);
    }

    static void Poster(Batch b, Vector3 c, Vector3 n, float w, float h, float tiltDeg = 0f)
    {
        b.Quad(mPaper, c, n, Quaternion.AngleAxis(tiltDeg, n) * Vector3.up, w, h);
    }

    static void Hay(Batch b, Vector3 p, Random r)
    {
        b.Box(mHay, p + new Vector3(0f, .22f, 0f), new Vector3(.9f, .45f, .5f), Quaternion.Euler(0f, (float)r.NextDouble() * 20f - 10f, 0f), false, 0, .6f);
        foreach (float sy in new[] { .12f, .32f }) b.Box(mRope, p + new Vector3(0f, sy, 0f), new Vector3(.92f, .015f, .52f));
        b.Col(p + Vector3.up * .22f, new Vector3(.9f, .45f, .5f));
    }

    // Shop-specific stuff on the boardwalk. x along the street, z toward the road; deck top at Y0.
    static void Dress(Batch b, Lot s, float W, float D, float Y1, Random r, List<Op> ops)
    {
        float hw = W / 2f;
        var door = ops.First(o => o.door);
        float dx = (door.x0 + door.x1) / 2f;
        bool doorLeft = dx < 0f;
        float edgeX = doorLeft ? hw - .55f : -hw + .55f;        // the far corner from the door
        float nearX = doorLeft ? -hw + .5f : hw - .5f;
        float side = doorLeft ? 1f : -1f;
        // QR payment placard beside every door (2035 Thailand)
        QrPlacard(b, new Vector3(door.x1 + .4f, Y0 + 1.35f, .03f), Vector3.forward, .3f);
        if (s.kind != "low")
        {
            b.Scan("ceramic_pot", new Vector3(door.x0 - .5f, Y0, 1.05f), 20f, .85f);
            b.Scan(s.seed % 2 == 0 ? "calathea_orbifolia_01" : "fern_02", new Vector3(door.x0 - .5f, Y0 + .26f, 1.05f), 0f, s.seed % 2 == 0 ? .5f : .55f);
            if (s.seed % 3 == 0)
            {
                b.Scan("planter_box_01", new Vector3(nearX, Y0, 2.2f), 90f, 1f);
                b.Scan("fern_02", new Vector3(nearX, Y0 + .3f, 2.2f), 0f, .55f);
            }
            if (s.seed % 2 == 0) b.Scan("potted_plant_04", new Vector3(door.x1 + .55f, Y0, 1.0f), 0f, 1.1f);
            else b.Scan("jug_01", new Vector3(door.x1 + .5f, Y0, .8f), 40f, 1.1f);
        }
        // generic: barrel pair and crates at the far corner
        switch (s.extra)
        {
            case "store":
                Barrel(b, new Vector3(edgeX, Y0, 1.1f)); Barrel(b, new Vector3(edgeX - side * .1f, Y0, 1.85f), .92f, .31f);
                Crate(b, new Vector3(edgeX - side * 1.1f, Y0, 1.3f), new Vector3(.75f, .55f, .55f), 6f);
                Crate(b, new Vector3(edgeX - side * 1.1f, Y0 + .55f, 1.3f), new Vector3(.6f, .45f, .5f), -12f);
                for (int i = 0; i < 4; i++) Sack(b, new Vector3(edgeX - side * (2.0f + i * .38f), Y0, 1.0f), r.Next(360), 0f);
                Sack(b, new Vector3(edgeX - side * 2.2f, Y0 + .5f, 1.0f), r.Next(360), 12f);
                Bench(b, new Vector3(nearX + side * 1.4f, Y0, 1.0f), 1.6f, 0f);
                // tools and pans hung on the front
                for (int i = 0; i < 5; i++) b.Box(mIron, new Vector3(nearX + side * (.2f + i * .28f), Y0 + 2.3f, .06f), new Vector3(.2f, .2f, .03f), Quaternion.Euler(0f, 0f, i * 20f));
                break;
            case "barber":
                Barber(b, new Vector3(dx + (doorLeft ? 1.55f : -1.55f), Y0, 2.0f));
                Bench(b, new Vector3(edgeX - side * .3f, Y0, 1.0f), 1.5f, 0f);
                break;
            case "hotel":
                Planter(b, new Vector3(-hw + 1.3f, Y0 + .0f, 2.45f), 1.6f, r);
                Planter(b, new Vector3(hw - 1.3f, Y0 + .0f, 2.45f), 1.6f, r);
                Bench(b, new Vector3(dx - 3.2f, Y0, 1.0f), 1.7f, 0f); Bench(b, new Vector3(dx + 3.2f, Y0, 1.0f), 1.7f, 0f);
                Clock(b, new Vector3(0f, Y1 + 2f * H2 + 2.6f + .22f + 1.2f, .1f), .85f);
                break;
            case "tea":
            case "noodle":
                for (int i = 0; i < 2; i++)
                {
                    float tx = edgeX - side * (.9f + i * 2.2f);
                    Table(b, new Vector3(tx, Y0, 1.5f), 1.1f, .7f);
                    Stool(b, new Vector3(tx - .4f, Y0, 1.5f + .62f)); Stool(b, new Vector3(tx + .4f, Y0, 1.5f + .62f)); Stool(b, new Vector3(tx, Y0, .85f));
                    b.Quad(mCanvasB, new Vector3(tx, Y0 + .765f, 1.5f), Vector3.up, Vector3.forward, .5f, .5f, .5f);
                }
                for (int i = 0; i < 5; i++) PaperLantern(b, new Vector3(-hw + 1.1f + i * ((W - 2.2f) / 4f), Y1 - .2f - (s.balcony ? .45f : 0f), PorchDepth - .35f), .8f);
                if (s.extra == "noodle")
                {   // steaming pot cart
                    b.Box(mCrate, new Vector3(nearX + side * 1.1f, Y0 + .8f, 1.5f), new Vector3(1.3f, .75f, .8f), null, false, 0, 1f);
                    b.Lathe(mIron, new Vector3(nearX + side * 1.1f, Y0 + 1.15f, 1.5f), new[] { (0f, 0f), (.3f, 0f), (.34f, .05f), (.33f, .3f), (.3f, .32f), (0f, .3f) }, 12, 1f);
                    foreach (float wx in new[] { -.5f, .5f }) Wheel(b, new Vector3(nearX + side * (1.1f + wx), Y0 + .4f, 1.95f), .4f, 0f);
                    b.Col(new Vector3(nearX + side * 1.1f, Y0 + .6f, 1.5f), new Vector3(1.3f, 1.2f, .9f));
                }
                break;
            case "saloon":
                // batwing doors in the doorway
                foreach (float sx in new[] { -1f, 1f })
                {
                    b.Push(new Vector3(dx + sx * .78f, Y0 + .95f, -.01f), Quaternion.Euler(0f, sx * -14f, 0f));
                    b.Box(mShutter, new Vector3(-sx * .39f, .5f, 0f), new Vector3(.78f, .95f, .04f), null, true, 0, 1f);
                    b.Pop();
                }
                Barrel(b, new Vector3(edgeX, Y0, 1.1f)); Barrel(b, new Vector3(edgeX - side * .75f, Y0, 1.15f), .92f, .31f);
                Bench(b, new Vector3(nearX + side * 1.2f, Y0, 1.0f), 1.8f, 0f);
                Bench(b, new Vector3(nearX + side * 4f, Y0, 1.0f), 1.8f, 0f);
                Poster(b, new Vector3(door.x1 + .9f, Y0 + 1.7f, .035f), Vector3.forward, .42f, .6f, 3f);
                break;
            case "hardware":
                Barrel(b, new Vector3(edgeX, Y0, 1.2f)); Barrel(b, new Vector3(edgeX - side * .65f, Y0, 1.0f), .92f, .31f);
                Crate(b, new Vector3(edgeX - side * 1.6f, Y0, 1.2f), new Vector3(.9f, .6f, .6f), 4f);
                Wheel(b, new Vector3(nearX + side * .9f, Y0 + .62f, .55f), .6f, 90f);
                for (int i = 0; i < 6; i++) b.Box(mIron, new Vector3(-hw + 1f + i * .32f, Y0 + 2.2f, .05f), new Vector3(.05f, .6f - (i % 2) * .15f, .03f));
                break;
            case "carpenter":
                for (int i = 0; i < 3; i++) b.Box(mPost, new Vector3(edgeX - side * (.2f + i * .05f), Y0 + .1f + i * .18f, 1.4f), new Vector3(.2f, .15f, 2.2f), null, true, 0, 1f);
                foreach (float sx in new[] { -1f, 1f }) b.Between(mPost, new Vector3(nearX + side * (1.2f + sx * .5f), Y0, 1.2f - .3f), new Vector3(nearX + side * (1.2f + sx * .1f), Y0 + .85f, 1.2f), .08f);
                b.Box(mPost, new Vector3(nearX + side * 1.2f, Y0 + .85f, 1.2f), new Vector3(1.4f, .08f, .12f), null, true, 0, 1f);
                for (int i = 0; i < 3; i++) Barrel(b, new Vector3(edgeX - side * (1.6f + i * .7f), Y0, 2.3f), .5f, .26f);
                break;
            case "stable":
                for (int i = 0; i < 4; i++) Hay(b, new Vector3(edgeX - side * (.7f + (i % 2) * .6f), Y0 + (i / 2) * .45f, 1.1f), r);
                Trough(b, new Vector3(nearX + side * 1.8f, Y0, 1.6f), 2.2f);
                break;
            case "pharmacy":
            case "tailor":
            case "gold":
            case "post":
            case "pawn":
            case "chinese":
            case "bank":
                Planter(b, new Vector3(edgeX - side * .5f, Y0, 2.2f), 1.2f, r);
                Bench(b, new Vector3(nearX + side * 1.4f, Y0, 1.0f), 1.6f, 0f);
                if (s.extra == "chinese") for (int i = 0; i < 4; i++) PaperLantern(b, new Vector3(-hw + 1.2f + i * ((W - 2.4f) / 3f), Y1 - .2f - .45f, PorchDepth - .35f), 1f);
                if (s.extra == "pawn") for (int i = 0; i < 3; i++) b.Lathe(mGold, new Vector3(-hw + .8f + i * .3f, Y1 - .5f, PorchDepth + .1f), new[] { (0f, 0f), (.08f, .03f), (.11f, .11f), (.08f, .19f), (0f, .22f) }, 10, 1f);
                if (s.extra == "pharmacy") Barrel(b, new Vector3(edgeX, Y0, 1.1f), .6f, .26f);
                if (s.extra == "post") b.Box(mRedPaint, new Vector3(nearX + side * .9f, Y0 + 1.0f, .35f), new Vector3(.5f, .9f, .4f), null, false, 0, 1f);
                if (s.extra == "bank") Poster(b, new Vector3(door.x0 - .8f, Y0 + 1.6f, .035f), Vector3.forward, .42f, .6f, -4f);
                break;
        }
        // roof extras: solar panels, dishes, antennas (2035), only above false fronts and flat roofs
        float Yt = Y1 + (s.floors - 1) * H2;
        if (s.kind == "false" && (s.seed % 2 == 0 || s.floors == 3))
        {
            float rx = (r.Next(2) == 0 ? -1f : 1f) * (W / 2f - 2.2f);
            if (s.floors == 3) Dish(b, new Vector3(rx, Yt + .5f, -D + 2.8f), 160f, 55f);
            else SolarPanel(b, new Vector3(rx, Yt + .72f, -3.2f), 2.6f, 1.2f, -20f, 0f);
        }
    }

    // Alley between two shops: clutter plus a rear cart (building-local = alley centre line).
    static void BuildAlley(Transform parent, Lot s, int side, float zc, int index)
    {
        var go = new GameObject($"Alley_{(side < 0 ? "W" : "E")}{index}").transform;
        go.SetParent(parent, false);
        go.SetPositionAndRotation(new Vector3(side * FrontX, GroundY(side * (FrontX + 2f), zc), zc), Quaternion.Euler(0f, side < 0 ? 90f : -90f, 0f));
        var b = new Batch(go, go.name);
        var r = new Random(s.seed + 7);
        float w = s.w;
        // boardwalk end caps are the shops' own; here: packed dirt, crates and barrels, a wagon in the wide ones
        for (int i = 0; i < 3; i++) Barrel(b, new Vector3((float)(r.NextDouble() - .5) * (w - 1.2f), 0f, -3.6f - i * .9f - (float)r.NextDouble() * .5f), .92f, .31f);
        Crate(b, new Vector3(-w / 2f + .7f, 0f, -2.2f), new Vector3(.8f, .6f, .6f), 14f);
        Crate(b, new Vector3(-w / 2f + .75f, .6f, -2.25f), new Vector3(.6f, .5f, .5f), -9f);
        Sack(b, new Vector3(w / 2f - .6f, 0f, -2.6f), 30f, 0f); Sack(b, new Vector3(w / 2f - .9f, 0f, -2.9f), 90f, 8f);
        b.Scan("planter_pot_clay", new Vector3(w / 2f - .5f, 0f, -1.2f), 0f, 1.2f);
        b.Scan("fern_02", new Vector3(w / 2f - .5f, .24f, -1.2f), 0f, .6f);
        b.Scan("fern_02", new Vector3(-w / 2f + .5f, 0f, -1.6f), 30f, .7f);
        if (w >= 3.9f) Wagon(b, new Vector3(0f, 0f, -10.5f), 0f, r, index % 2 == 0);
        else Hay(b, new Vector3(0f, 0f, -6.5f), r);
        // laundry / bunting line across the alley
        for (int i = 0; i < 5; i++) b.Box(i % 2 == 0 ? mCanvasB : mCanvasA, new Vector3(-w / 2f + .5f + i * (w - 1f) / 4f, 3.4f + Mathf.Sin(i * 2f) * .06f, -4.5f), new Vector3(.45f, .5f, .02f), Quaternion.Euler(0f, 0f, (i - 2) * 4f));
        b.Between(mRope, new Vector3(-w / 2f, 3.75f, -4.5f), new Vector3(w / 2f, 3.75f, -4.5f), .015f);
        b.Build();
    }

    // ================================================================ street furniture (lamps, poles, wires, horses)

    static Batch furniture;

    static void StreetFurniture(Transform root)
    {
        var go = new GameObject("StreetFurniture").transform;
        go.SetParent(root, false);
        furniture = new Batch(go, "StreetFurniture");
        var r = new Random(77);
        float[] lampZ = { 52f, 28f, 4f, -20f, -44f };
        for (int i = 0; i < lampZ.Length; i++)
        {
            foreach (int side in new[] { -1, 1 })
            {
                float z = lampZ[i] + (side < 0 ? -12f : 0f);
                if (side < 0 && i == lampZ.Length - 1) continue;
                LampPost(furniture, new Vector3(side * 7.3f, GroundY(side * 7.3f, z) + 0f, z), side);
            }
        }
        // hitching rails with troughs on the dirt just beyond the boardwalk edge
        foreach (var (x, z) in new[] { (-6.9f, 46f), (6.9f, 40f), (-6.9f, 10f), (6.9f, -6f), (-6.9f, -30f), (6.9f, -34f) })
        {
            float y = GroundY(x, z);
            furniture.Push(new Vector3(x, y, z - 1.5f), Quaternion.Euler(0f, 90f, 0f));   // rail runs along the street
            HitchRail(furniture, Vector3.zero, 3f);
            furniture.Pop();
        }
        // telegraph poles along the east side, wires to the next pole
        var poles = new List<Vector3>();
        float[] poleZ = { 60f, 36f, 12f, -12f, -36f, -60f };
        foreach (float z in poleZ) poles.Add(new Vector3(7.9f, GroundY(7.9f, z), z));
        foreach (var p in poles) Pole(furniture, p);
        for (int i = 0; i < poles.Count - 1; i++)
            foreach (float off in new[] { -.95f, -.32f, .32f, .95f })
                Wire(furniture, poles[i] + new Vector3(0f, 8.7f, off), poles[i + 1] + new Vector3(0f, 8.7f, off), 1.1f);
        // strings of festival lights across the street
        foreach (float z in new[] { 40f, 6f, -26f })
            StringLights(furniture, new Vector3(-FrontX + .6f, GroundY(-FrontX, z) + 6.4f, z), new Vector3(FrontX - .6f, GroundY(FrontX, z) + 6.4f, z), 1.6f, r);
        furniture.Build();
    }

    static void LampPost(Batch b, Vector3 p, int side)
    {
        float arm = -side * .75f;
        b.Lathe(mIron, p, new[] { (.2f, 0f), (.2f, .12f), (.12f, .3f), (.075f, .55f), (.05f, 3.3f), (.07f, 3.4f), (0f, 3.4f) }, 10, 1f);
        b.Between(mIron, p + new Vector3(0f, 3.0f, 0f), p + new Vector3(arm, 3.4f, 0f), .045f);
        b.Between(mIron, p + new Vector3(0f, 3.4f, 0f), p + new Vector3(arm, 3.55f, 0f), .035f);
        Vector3 l = p + new Vector3(arm, 3.0f, 0f);
        b.Lathe(mIron, l + Vector3.up * .56f, new[] { (.2f, 0f), (.15f, .1f), (0f, .17f) }, 8, 1f);
        b.Lathe(mLampGlass, l + Vector3.up * .12f, new[] { (0f, 0f), (.1f, 0f), (.15f, .06f), (.15f, .34f), (.1f, .44f), (0f, .44f) }, 10, 1f);
        b.Lathe(mIron, l + Vector3.up * .1f, new[] { (0f, 0f), (.12f, 0f), (.12f, .02f), (0f, .02f) }, 8, 1f);
        b.Light(l + Vector3.up * .34f, new Color(1f, .66f, .32f), 3.2f, 11f);
        b.Col(p + Vector3.up * 1.7f, new Vector3(.26f, 3.4f, .26f));
    }

    static void Pole(Batch b, Vector3 p)
    {
        b.Lathe(mPost, p, new[] { (.16f, -.3f), (.16f, 0f), (.12f, 4f), (.1f, 9.4f), (0f, 9.4f) }, 10, 1.2f, null, 1f, true);
        b.Box(mPost, p + new Vector3(0f, 8.9f, 0f), new Vector3(.12f, .14f, 2.4f), null, true, 0, 1f);
        b.Box(mPost, p + new Vector3(0f, 8.35f, 0f), new Vector3(.1f, .12f, 1.4f), null, true, 0, 1f);
        foreach (float z in new[] { -.95f, -.32f, .32f, .95f })
            b.Lathe(mInsulator, p + new Vector3(0f, 8.97f, z), new[] { (0f, 0f), (.05f, 0f), (.06f, .04f), (.045f, .09f), (.06f, .13f), (0f, .15f) }, 8, 1f);
        foreach (float sz in new[] { -1f, 1f }) b.Between(mPost, p + new Vector3(0f, 8.3f, sz * .1f), p + new Vector3(0f, 8.85f, sz * .75f), .07f, 1f);
        b.Col(p + Vector3.up * 2f, new Vector3(.3f, 4f, .3f));
    }

    static void Wire(Batch b, Vector3 a, Vector3 c, float sag, float thick = .022f, int n = 10)
    {
        Vector3 prev = a;
        for (int i = 1; i <= n; i++)
        {
            float t = i / (float)n;
            Vector3 p = Vector3.Lerp(a, c, t) + Vector3.down * sag * 4f * t * (1f - t);
            b.Between(mWire, prev, p, thick);
            prev = p;
        }
    }

    static void StringLights(Batch b, Vector3 a, Vector3 c, float sag, Random r)
    {
        Wire(b, a, c, sag, .014f, 22);
        int bulbs = 21;
        for (int i = 1; i < bulbs; i++)
        {
            float t = i / (float)bulbs;
            Vector3 p = Vector3.Lerp(a, c, t) + Vector3.down * sag * 4f * t * (1f - t);
            b.Lathe(i % 4 == 0 ? mLanternRed : mBulb, p + Vector3.down * .18f, new[] { (0f, -.06f), (.045f, -.03f), (.06f, .03f), (.035f, .08f), (0f, .1f) }, 6, 1f);
            b.Between(mWire, p, p + Vector3.down * .13f, .012f);
        }
        b.Light(Vector3.Lerp(a, c, .5f) + Vector3.down * (sag + .3f), new Color(1f, .6f, .3f), 2.2f, 9f);
    }

    // ================================================================ ground wear and the station plaza

    static void SetTransparent(Material m)
    {
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_SrcBlend", 5f); m.SetFloat("_DstBlend", 10f); m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetShaderPassEnabled("ShadowCaster", false);
        m.renderQueue = 3000;
    }

    static Material mRut, mTrample, mFlag;

    static void Decals(Transform root)
    {
        mRut = Painted("rut_strip", 64, 256, (x, y) =>
        {
            float u = x / 63f, across = 1f - Mathf.Pow(Mathf.Abs(u * 2f - 1f), 2.2f);
            float n = Mathf.PerlinNoise(u * 3f + 4f, y * .045f) * .6f + Mathf.PerlinNoise(u * 9f, y * .2f) * .4f;
            float ends = Mathf.SmoothStep(0f, 1f, Mathf.Min(y, 255f - y) / 40f);
            return new Color(.09f, .05f, .03f, Mathf.Clamp01(across * (.08f + n * .42f) * ends));
        }, true, true);
        mTrample = Painted("trample_strip", 64, 256, (x, y) =>
        {
            float u = x / 63f, across = Mathf.Clamp01(1.4f * (1f - Mathf.Pow(Mathf.Abs(u * 2f - 1f), 1.6f)));
            float n = Mathf.PerlinNoise(u * 4f + 9f, y * .05f) * .7f + Mathf.PerlinNoise(u * 14f, y * .3f) * .3f;
            float ends = Mathf.SmoothStep(0f, 1f, Mathf.Min(y, 255f - y) / 60f);
            return new Color(.16f, .1f, .06f, Mathf.Clamp01(across * (.06f + n * .4f) * ends));
        }, true, true);
        var go = new GameObject("GroundWear").transform;
        go.SetParent(root, false);
        var b = new Batch(go, "GroundWear");
        var r = new Random(5);
        // cart ruts and a hoof-trampled centre line along the whole street, in 10 m pieces so each hugs the ground
        float zTop = 64f, zBot = -52f, step = 10f;
        for (float z = zTop; z > zBot; z -= step)
        {
            float zc = z - step / 2f;
            foreach (float lane in new[] { -2.1f, 1.45f })
            {
                float x = lane + ((float)r.NextDouble() - .5f) * .5f;
                b.Quad(mRut, new Vector3(x, GroundY(x, zc) + .075f, zc), Vector3.up, Vector3.forward, .7f, step + .4f);
                if (r.Next(2) == 0) b.Quad(mRut, new Vector3(x + 1.25f, GroundY(x, zc) + .085f, zc + 1f), Vector3.up, Vector3.forward, .62f, step + .4f);
            }
            b.Quad(mTrample, new Vector3(0f, GroundY(0f, zc) + .06f, zc), Vector3.up, Vector3.forward, 3.4f, step + .4f);
            foreach (int side in new[] { -1, 1 })
                b.Quad(mTrample, new Vector3(side * 6.4f, GroundY(side * 6.4f, zc) + .065f, zc), Vector3.up, Vector3.forward, 2.2f, step + .4f);
        }
        b.Build();
    }

    // Flag and plaza by the station: a tall pole at the head of the street, Thai tricolour.
    static void Plaza(Transform root)
    {
        mFlag = Painted("thai_flag", 90, 60, (x, y) =>
        {
            float t = y / 59f;
            if (t < 1f / 6f || t > 5f / 6f) return new Color(.65f, .08f, .12f, 1f);
            if (t < 2f / 6f || t > 4f / 6f) return new Color(.92f, .92f, .9f, 1f);
            return new Color(.1f, .14f, .38f, 1f);
        }, false);
        var go = new GameObject("StationPlaza").transform;
        go.SetParent(root, false);
        var b = new Batch(go, "StationPlaza");
        float x0 = 9.6f, z0 = 62f, y0 = GroundY(x0, z0);
        b.Lathe(mIron, new Vector3(x0, y0, z0), new[] { (.35f, 0f), (.35f, .25f), (.14f, .5f), (.07f, 1f), (.05f, 9.5f), (.08f, 9.6f), (0f, 9.6f) }, 12, 1f);
        b.Lathe(mGold, new Vector3(x0, y0 + 9.6f, z0), new[] { (0f, 0f), (.12f, .1f), (.1f, .24f), (0f, .3f) }, 10, 1f);
        // flag hangs from the pole on a short spar, facing down the street
        b.Quad(mFlag, new Vector3(x0 - 1.3f, y0 + 8.7f, z0), Vector3.forward, Vector3.up, 2.4f, 1.6f, 0f, true);
        b.Between(mIron, new Vector3(x0, y0 + 9.4f, z0), new Vector3(x0 - 2.5f, y0 + 9.4f, z0), .03f);
        b.Col(new Vector3(x0, y0 + 2f, z0), new Vector3(.4f, 4f, .4f));
        // stone well with a little roof, west of the station steps
        {
            float wx = -10f, wz = 60.5f, wy = GroundY(wx, wz);
            b.Lathe(mStone, new Vector3(wx, wy, wz), new[] { (.0f, 0f), (1.0f, 0f), (1.0f, .85f), (.78f, .9f), (.78f, .3f), (.0f, .3f) }, 16, 1.6f);
            b.Lathe(mWater, new Vector3(wx, wy + .35f, wz), new[] { (0f, 0f), (.77f, 0f) }, 16, 1f);
            foreach (float dx in new[] { -.9f, .9f }) b.Box(mPost, new Vector3(wx + dx, wy + 1.6f, wz), new Vector3(.14f, 1.5f, .14f), null, true, 0, 1f);
            b.Box(mPost, new Vector3(wx, wy + 2.2f, wz), new Vector3(2.1f, .12f, .14f), null, true, 0, 1f);
            b.Box(mRoofWood, new Vector3(wx - .5f, wy + 2.45f, wz), new Vector3(1.4f, .08f, 1.6f), Quaternion.Euler(0f, 0f, 24f));
            b.Box(mRoofWood, new Vector3(wx + .5f, wy + 2.45f, wz), new Vector3(1.4f, .08f, 1.6f), Quaternion.Euler(0f, 0f, -24f));
            b.Lathe(mBarrel, new Vector3(wx + .6f, wy + .9f, wz + .85f), new[] { (0f, 0f), (.16f, 0f), (.19f, .1f), (.19f, .3f), (.16f, .34f), (0f, .34f) }, 10, .5f);
            b.Col(new Vector3(wx, wy + .5f, wz), new Vector3(2f, 1f, 2f));
        }
        // hitching rails and a trough in front of the station porch
        foreach (float x in new[] { -9f, 9f })
        {
            b.Push(new Vector3(x - 1.5f, GroundY(x, 61f), 59.5f), Quaternion.identity);
            HitchRail(b, Vector3.zero, 3f);
            b.Pop();
        }
        Trough(b, new Vector3(-9f, GroundY(-9f, 56f), 56f), 2.2f);
        b.Build();
    }

    // The blockout station has a black flat roof; put a proper low gable of corrugated iron on it.
    static void StationRoof(Transform root)
    {
        var station = root.parent != null ? root.parent.Find("PoliceStation") : null;
        if (station == null) return;
        var go = new GameObject("StationRoof").transform;
        go.SetParent(root, false);
        go.SetPositionAndRotation(new Vector3(0f, station.position.y + 6.5f, 70.5f), Quaternion.Euler(0f, 180f, 0f));
        var b = new Batch(go, "StationRoof");
        var lot = new Lot { roof = "iron", trim = new Color(.45f, .35f, .26f) };
        GableRoof(b, lot, 26.6f, 16.6f, 0f, Paint(new Color(.4f, .3f, .22f)), walls["dark"], 13f, .6f, 0f, true, false);
        b.Build();
    }

    static Mesh Smoothed(Mesh source)
    {
        if (source == null) return null;
        var mesh = Object.Instantiate(source);
        mesh.name = source.name + "_smooth";
        var v = mesh.vertices; var n = mesh.normals;
        var sum = new Dictionary<Vector3Int, Vector3>();
        Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x * 400f), Mathf.RoundToInt(p.y * 400f), Mathf.RoundToInt(p.z * 400f));
        for (int i = 0; i < v.Length; i++) { var k = Key(v[i]); sum[k] = sum.TryGetValue(k, out var a) ? a + n[i] : n[i]; }
        for (int i = 0; i < v.Length; i++) n[i] = sum[Key(v[i])].normalized;
        mesh.normals = n;
        return mesh;
    }

    public static void DebugStats()
    {
        var root = GameObject.Find("TownArt");
        if (root == null) return;
        int rend = 0, tris = 0, lights = 0, cols = 0;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>()) { rend++; if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3; }
        foreach (var l in root.GetComponentsInChildren<Light>()) lights++;
        foreach (var c in root.GetComponentsInChildren<Collider>()) cols++;
        var byName = new Dictionary<string, (int n, int t)>();
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            string key = mf.transform.parent != null && mf.transform.parent.name.StartsWith("Shop_") ? "(batched)" : mf.sharedMesh.name;
            byName.TryGetValue(key, out var e); byName[key] = (e.n + 1, e.t + mf.sharedMesh.triangles.Length / 3);
        }
        foreach (var kv in byName.OrderByDescending(k => k.Value.t).Take(12)) Debug.Log($"  {kv.Key}: {kv.Value.n} objects, {kv.Value.t} tris");
        Debug.Log($"TownArt stats: {rend} mesh objects, {tris} triangles, {lights} lights, {cols} colliders, {root.GetComponentsInChildren<Transform>().Length} transforms");
    }

    static void Horses(Transform root)
    {
        var coats = new[] { new Color(.5f, .27f, .12f), new Color(.38f, .2f, .1f), new Color(.6f, .55f, .5f) };
        var spots = new (float x, float z, float yaw, int coat)[] { (-6.2f, 44.5f, 90f, 0), (-6.2f, 47.2f, 80f, 1), (6.2f, 38.8f, -95f, 2), (6.2f, -4.8f, -88f, 0), (-6.2f, -31f, 85f, 1) };
        int k = 0;
        foreach (var h in spots)
        {
            var horse = WorldSceneArt.Model(root, "Animals/Horse", new Vector3(h.x, GroundY(h.x, h.z), h.z), 2.0f, h.yaw);
            if (horse == null) continue;
            horse.name = "TetheredHorse_" + k++;
            var coat = WorldSceneArt.Paint("HorseCoat_" + h.coat, coats[h.coat]);
            var mane = WorldSceneArt.Paint("TownHorseMane", new Color(.1f, .07f, .05f));
            foreach (var rend in horse.GetComponentsInChildren<Renderer>())
            {
                var mats = rend.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = mats[i] != null && mats[i].GetColor("_BaseColor").maxColorComponent < .05f ? mane : coat;
                rend.sharedMaterials = mats;
            }
            var animator = horse.GetComponentInChildren<Animator>();
            if (animator != null) Object.DestroyImmediate(animator);
            // the CC0 horse is flat-shaded low poly; weld normals so it reads as a smooth animal
            foreach (var smr in horse.GetComponentsInChildren<SkinnedMeshRenderer>()) smr.sharedMesh = Smoothed(smr.sharedMesh);
            foreach (var mf in horse.GetComponentsInChildren<MeshFilter>()) mf.sharedMesh = Smoothed(mf.sharedMesh);
        }
    }

}
