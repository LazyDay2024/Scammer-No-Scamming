using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ScammerNoScamming;

// Blockout of the case-one level (references: RDR2 Valentine for the town, Gaptooth Breach for the hideout,
// Ubisoft's 360 approach for the camp). Builds Assets/Scenes/Frontier_Case01.unity:
//   town "Siam Phirom" in a valley: police station closing the north end of one main street, shophouses both sides,
//   temple on the east hill as the landmark; the street leads south through a gate into a canyon that climbs to a
//   mining camp hideout on a mountainside plateau, with a radio mast on the ridge visible from town (the "weenie").
// Everything is placed in the saved scene so the team can move it in the Editor.
public static class FrontierLevelBuilder
{
    const string ScenePath = "Assets/Scenes/Frontier_Case01.unity";
    const string LevelFolder = "Assets/Level";
    const float Size = 600f, MaxHeight = 120f, Ground = 10f;
    static readonly Vector3 Origin = new Vector3(-300f, 0f, -330f);

    // Main road: x, z, height. From the station plaza down the street, out of the gate, up the canyon to the camp.
    public static readonly Vector3[] Road = {
        new Vector3(0f, Ground, 70f), new Vector3(0f, Ground, -45f), new Vector3(2f, Ground + 1f, -95f),
        new Vector3(22f, Ground + 5f, -135f), new Vector3(58f, Ground + 10f, -162f), new Vector3(96f, Ground + 15f, -178f),
        new Vector3(118f, Ground + 16f, -184f) };
    // Side path up to the overlook on the canyon's north rim.
    public static readonly Vector3[] OverlookPath = {
        new Vector3(24f, Ground + 5.5f, -132f), new Vector3(40f, Ground + 14f, -122f), new Vector3(62f, Ground + 24f, -128f),
        new Vector3(78f, Ground + 28f, -140f) };
    // Back route along the ridge behind the camp (flank).
    public static readonly Vector3[] FlankPath = {
        new Vector3(96f, Ground + 15f, -178f), new Vector3(108f, Ground + 22f, -206f), new Vector3(134f, Ground + 26f, -214f),
        new Vector3(150f, Ground + 24f, -200f) };
    public static readonly Vector3 Camp = new Vector3(132f, Ground + 16f, -190f);
    public static readonly Vector3 TempleHill = new Vector3(62f, 0f, 28f);
    public static readonly Vector3 Mast = new Vector3(150f, 0f, -228f);
    static Terrain land;

    [MenuItem("Tools/Scammer No Scamming/Build Frontier Level (Case 01)")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        WorldSceneArt.EnsureFolder(LevelFolder);
        var terrain = BuildTerrain();
        var level = new GameObject("Level").transform;

        var player = PlayerSandbox.CreatePlayer();
        Place(player.transform, new Vector3(0f, 0f, 58f), 180f);
        var camera = player.GetComponentInChildren<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.farClipPlane = 900f;
        // Sun low in the south-west, shining up the street toward the station: long shadows, golden rim light.
        WorldSceneArt.Lighting(camera, 25f, 24f);
        RenderSettings.fogDensity = .0018f;
        SkyLookdev.Apply(0); // photographic sky; the sun follows the sun in the image
        var horse = PlayerSandbox.CreateHorse(CaseFlowSceneBuilder.Flat);
        Place(horse.transform, new Vector3(6f, 0f, 56f), 200f);
        horse.AddComponent<HorseGenes>();
        WorldSceneArt.Horse(horse);

        var station = BuildStation(level, terrain);
        BuildTown(level, terrain);
        TownDressing.Build(level);   // claude2: town art pass
        BuildTemple(level, terrain);
        var hideout = BuildCamp(level, terrain);
        CampDressing.Build(level, hideout);   // Codex: camp encounter dressing
        BuildTraps(level, terrain);
        RiderAmbush.Build(level);
        Scatter(level, terrain);
        MenuCameras(level);

        var counter = CaseFlowSceneBuilder.Prop("EvidenceCounter", Vector3.zero, new Vector3(3.2f, 1.2f, 1.2f), 0f,
            CaseFlowSceneBuilder.Textured("tex_evidence_seal_table", new Color(.4f, .3f, .2f)));
        Place(counter.transform, new Vector3(-6f, .6f, 60f), 0f);
        var flow = new GameObject("CaseFlow").AddComponent<CaseFlowDemo>();
        flow.sealPoint = counter.transform;
        flow.regularCases = 1;
        flow.includeBoss = false;
        var investigation = flow.gameObject.AddComponent<StationInvestigation>();
        investigation.stationAnchor = station;
        investigation.interiorPrefabs = new GameObject[0];
        investigation.victimPrefabs = new[] {
            Load("Assets/ThirdParty/Quaternius/Characters/Chef_Female.fbx"), Load("Assets/ThirdParty/Quaternius/Characters/Casual2_Male.fbx"),
            Load("Assets/ThirdParty/Quaternius/Characters/OldClassy_Male.fbx"), Load("Assets/ThirdParty/Quaternius/Characters/Suit_Female.fbx") };

        EditorSceneManager.SaveScene(scene, ScenePath);
        CaseFlowSceneBuilder.RegisterFirst(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Frontier level saved to " + ScenePath);
    }

    // ---------------------------------------------------------------- terrain

    static Terrain BuildTerrain()
    {
        const int res = 513;
        var data = new TerrainData { heightmapResolution = res };
        data.size = new Vector3(Size, MaxHeight, Size);
        var heights = new float[res, res];
        for (int zi = 0; zi < res; zi++)
            for (int xi = 0; xi < res; xi++)
            {
                float x = Origin.x + xi / (float)(res - 1) * Size, z = Origin.z + zi / (float)(res - 1) * Size;
                heights[zi, xi] = Height(x, z) / MaxHeight;
            }
        data.SetHeights(0, 0, heights);

        // Photo-scanned CC0 surfaces (Poly Haven): desert gravel, rutted red dirt road, two sandstone cliff faces.
        const string pt = "Assets/Art/Terrain/";
        var sand = Layer("Desert", pt + "sandy_gravel_02_Diffuse.jpg", pt + "sandy_gravel_02_nor_gl.jpg", 7f);
        var road = Layer("Road", pt + "red_dirt_mud_01_Diffuse.jpg", pt + "red_dirt_mud_01_nor_gl.jpg", 6f);
        var rockRed = Layer("RockRed", pt + "rock_boulder_cracked_Diffuse.jpg", pt + "rock_boulder_cracked_nor_gl.jpg", 12f);
        var rockPale = Layer("RockPale", pt + "rock_06_Diffuse.jpg", pt + "rock_06_nor_gl.jpg", 14f);
        data.terrainLayers = new[] { sand, road, rockRed, rockPale };
        data.alphamapResolution = 512;
        int ar = data.alphamapResolution;
        var alpha = new float[ar, ar, 4];
        for (int zi = 0; zi < ar; zi++)
            for (int xi = 0; xi < ar; xi++)
            {
                float u = xi / (float)(ar - 1), v = zi / (float)(ar - 1);
                float x = Origin.x + u * Size, z = Origin.z + v * Size;
                float steep = Mathf.InverseLerp(26f, 38f, data.GetSteepness(u, v));
                // Sandstone strata: alternate red and pale rock every few metres of height, so cliffs read as mesas.
                float height = data.GetInterpolatedHeight(u, v);
                float band = Mathf.PerlinNoise(x * .03f, z * .03f) * 2f;
                bool red = Mathf.FloorToInt((height + band) / 4f) % 2 == 0;
                float onRoad = 1f - Mathf.InverseLerp(3.5f, 6.5f, Mathf.Min(PathDistance(Road, x, z, out _), PathDistance(OverlookPath, x, z, out _) + 2f, PathDistance(FlankPath, x, z, out _) + 2.5f));
                onRoad = Mathf.Max(onRoad, 1f - Mathf.InverseLerp(10f, 14f, Vector2.Distance(new Vector2(x, z), new Vector2(0f, 66f)))); // station plaza
                float r = steep, d = (1f - steep) * onRoad, s = 1f - r - d;
                alpha[zi, xi, 0] = s; alpha[zi, xi, 1] = d; alpha[zi, xi, red ? 2 : 3] = r;
            }
        data.SetAlphamaps(0, 0, alpha);

        string path = LevelFolder + "/FrontierTerrain.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(data, path);
        var go = Terrain.CreateTerrainGameObject(data);
        go.name = "Terrain";
        go.transform.position = Origin;
        var terrain = go.GetComponent<Terrain>();
        land = terrain;
        terrain.materialTemplate = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/TerrainLit.mat");
        terrain.basemapDistance = 400f;
        terrain.heightmapPixelError = 3f;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic);
        return terrain;
    }

    // Tileable rock painted in code: value noise with cracks, tinted per stratum.
    static string RockTexture(string name, Color tint)
    {
        WorldSceneArt.EnsureFolder(LevelFolder);
        string path = $"{LevelFolder}/{name}.png";
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size * Mathf.PI * 2f, v = y / (float)size * Mathf.PI * 2f;
                // Periodic noise (sampled on a torus) so the texture tiles without seams.
                float n = Mathf.PerlinNoise(3f + Mathf.Cos(u) * 2f, 3f + Mathf.Sin(u) * 2f + Mathf.Cos(v) * 2f) * .6f
                        + Mathf.PerlinNoise(9f + Mathf.Cos(u) * 6f, 9f + Mathf.Sin(v) * 6f) * .4f;
                float strata = .9f + .1f * Mathf.Sin(v * 6f + n * 4f);
                float k = (.72f + n * .45f) * strata;
                tex.SetPixel(x, y, new Color(tint.r * k, tint.g * k, tint.b * k));
            }
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        return path;
    }

    static TerrainLayer Layer(string name, string albedo, string normal, float tile)
    {
        string path = $"{LevelFolder}/Layer_{name}.terrainlayer";
        var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (layer == null) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, path); }
        layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(albedo);
        if (normal != null && AssetImporter.GetAtPath(normal) is TextureImporter ni && ni.textureType != TextureImporterType.NormalMap)
        {
            ni.textureType = TextureImporterType.NormalMap;
            ni.SaveAndReimport();
        }
        layer.normalMapTexture = normal != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(normal) : null;
        layer.normalScale = 1f;
        layer.tileSize = new Vector2(tile, tile);
        layer.smoothness = .05f;
        EditorUtility.SetDirty(layer);
        return layer;
    }

    // Height in metres. Playable ground (valley, roads, plateau, overlook) is carved out of terraced mesas.
    static float Height(float x, float z)
    {
        float mountain = Mesa(x, z);
        float flat = Ground;
        float mask = 0f;

        // Town valley (ellipse), temple hill, station plaza.
        float valley = Ellipse(x, z, 0f, 5f, 62f, 88f);
        mask = Mathf.Max(mask, Soft(valley, 1f, 1.12f));
        float hill = Vector2.Distance(new Vector2(x, z), new Vector2(TempleHill.x, TempleHill.z));
        float hillHeight = Ground + 13f * Smooth(1f - Mathf.Clamp01(hill / 30f));
        mask = Mathf.Max(mask, Soft(hill, 26f, 40f));
        flat = Mathf.Max(flat, hill < 40f ? hillHeight : Ground);

        // Roads: flat profile along the path, blending into the mesas over a few metres (canyon walls).
        float dRoad = PathDistance(Road, x, z, out float roadHeight);
        float dLook = PathDistance(OverlookPath, x, z, out float lookHeight);
        float dFlank = PathDistance(FlankPath, x, z, out float flankHeight);
        float camp = Vector2.Distance(new Vector2(x, z), new Vector2(Camp.x, Camp.z));
        float wRoad = Soft(dRoad, 10f, 15f), wLook = Soft(dLook, 3.5f, 7f), wFlank = Soft(dFlank, 3f, 6.5f), wCamp = Soft(camp, 30f, 36f);
        float wLookPad = Soft(Vector2.Distance(new Vector2(x, z), new Vector2(OverlookPath[3].x, OverlookPath[3].z)), 7f, 10f);
        if (valley < 1.12f && z > -60f) roadHeight = Ground;
        float carved = flat;
        float total = 0f, sum = 0f;
        void Add(float w, float h) { if (w <= 0f) return; total += w; sum += w * h; }
        Add(Soft(valley, 1f, 1.12f) * (hill < 40f ? 0f : 1f), Ground);
        Add(hill < 40f ? Soft(hill, 26f, 40f) : 0f, hillHeight);
        Add(wRoad, roadHeight);
        Add(wLook, lookHeight);
        Add(wLookPad, OverlookPath[3].y);
        Add(wFlank, flankHeight);
        Add(wCamp, Camp.y);
        if (total > 0f) carved = sum / total;
        mask = Mathf.Max(mask, Mathf.Max(wRoad, Mathf.Max(wLook, Mathf.Max(wFlank, Mathf.Max(wCamp, wLookPad)))));

        // A dry riverbed crossing the valley south of the gate.
        float river = Mathf.Abs(z - (-70f + 6f * Mathf.Sin(x * .05f)));
        if (river < 7f && mask > .9f) carved -= 2.2f * Smooth(1f - river / 7f) * (1f - wRoad * .6f);

        float h = Mathf.Lerp(mountain, carved, mask);
        return Mathf.Clamp(h, 0f, MaxHeight - 1f);
    }

    static float Mesa(float x, float z)
    {
        float n = Mathf.PerlinNoise(x * .006f + 13f, z * .006f + 7f) * .65f + Mathf.PerlinNoise(x * .02f + 3f, z * .02f + 11f) * .35f;
        // Low rims along the route (so the overlook sits on top of the canyon wall), tall mesas further out.
        float near = Mathf.Min(PathDistance(Road, x, z, out _), PathDistance(OverlookPath, x, z, out _));
        float tall = Mathf.InverseLerp(35f, 110f, near);
        float h = Ground + Mathf.Lerp(16f, 22f, tall) + n * Mathf.Lerp(14f, 55f, tall);
        // Terraces read as sandstone mesas: quantise with a short ramp between ledges.
        // Near-vertical risers (12% of each step) so every ledge shows a sandstone cliff face.
        const float step = 11f;
        float k = Mathf.Floor(h / step), f = h / step - k;
        h = (k + Smooth(Mathf.Clamp01((f - .88f) / .12f))) * step;
        // The ridge behind the camp rises higher for the radio mast silhouette.
        float ridge = Vector2.Distance(new Vector2(x, z), new Vector2(Mast.x, Mast.z));
        h += 22f * Smooth(1f - Mathf.Clamp01(ridge / 45f));
        return h;
    }

    static float Ellipse(float x, float z, float cx, float cz, float rx, float rz)
    {
        float dx = (x - cx) / rx, dz = (z - cz) / rz;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    static float Soft(float d, float inner, float outer) => 1f - Smooth(Mathf.Clamp01((d - inner) / (outer - inner)));
    static float Smooth(float t) => t * t * (3f - 2f * t);

    public static float PathDistance(Vector3[] path, float x, float z, out float height)
    {
        float best = float.MaxValue; height = path[0].y;
        var p = new Vector2(x, z);
        for (int i = 0; i < path.Length - 1; i++)
        {
            var a = new Vector2(path[i].x, path[i].z); var b = new Vector2(path[i + 1].x, path[i + 1].z);
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            float d = Vector2.Distance(p, a + ab * t);
            if (d < best) { best = d; height = Mathf.Lerp(path[i].y, path[i + 1].y, Smooth(t)); }
        }
        return best;
    }

    public static void Place(Transform t, Vector3 xz, float yaw, float lift = 0f)
    {
        var terrain = land;
        float y = terrain != null ? terrain.SampleHeight(new Vector3(xz.x, 0f, xz.z)) + terrain.transform.position.y : Ground;
        t.SetPositionAndRotation(new Vector3(xz.x, y + xz.y + lift, xz.z), Quaternion.Euler(0f, yaw, 0f));
    }

    static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

    // Instance of an imported model, planted on the terrain, colliders added for blocking.
    public static GameObject Model(Transform parent, string path, Vector3 xz, float yaw, float scale = 1f, bool collide = true)
    {
        var prefab = Load(path);
        if (prefab == null) { Debug.LogWarning("Missing model " + path); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.transform.SetParent(parent, false);
        go.transform.localScale *= scale;
        Place(go.transform, xz, yaw);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation;
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
            float groundY = land.SampleHeight(go.transform.position) + land.transform.position.y;
            go.transform.position += Vector3.up * (groundY + xz.y - b.min.y);
            if (collide)
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    if (mf.GetComponent<Collider>() == null) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }
        if (path.Contains("/PolyHaven/"))
        {
            var material = ScanMaterial(path);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = System.Linq.Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
        }
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    // URP material for a Poly Haven scan: colour (with cut-out alpha for foliage) + OpenGL normal map.
    static Material ScanMaterial(string fbxPath)
    {
        string folder = System.IO.Path.GetDirectoryName(fbxPath).Replace(System.IO.Path.DirectorySeparatorChar, '/');
        string id = System.IO.Path.GetFileName(folder);
        string matPath = $"{folder}/{id}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material != null) return material;
        string tex = folder + "/textures/" + id;
        var alpha = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "_diffalpha_2k.png");
        string normalPath = tex + "_nor_gl_2k.png";
        if (AssetImporter.GetAtPath(normalPath) is TextureImporter ni && ni.textureType != TextureImporterType.NormalMap)
        { ni.textureType = TextureImporterType.NormalMap; ni.SaveAndReimport(); }
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetTexture("_BaseMap", alpha != null ? alpha : AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "_diff_2k.jpg"));
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
        material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_Smoothness", .12f);
        if (alpha != null)
        {
            material.SetFloat("_AlphaClip", 1f); material.SetFloat("_Cutoff", .45f); material.EnableKeyword("_ALPHATEST_ON");
            material.SetFloat("_Cull", 0f); material.doubleSidedGI = true;
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
        AssetDatabase.CreateAsset(material, matPath);
        return material;
    }

    // ---------------------------------------------------------------- town

    static Transform BuildStation(Transform level, Terrain terrain)
    {
        // A 26 x 16 m hall like CityBuilder's (StationInvestigation/StationDressing use its local layout); door faces the street.
        var root = new GameObject("PoliceStation").transform;
        root.SetParent(level, false);
        Place(root, new Vector3(0f, .2f, 78f), 0f);
        var planks = Tex("OldWest_StationPlanks", "Assets/ThirdParty/OldWest/Textures/Planks012_512x512_Color.png", new Vector2(6f, 2f));
        var roof = Tex("OldWest_StationRoof", "Assets/ThirdParty/OldWest/Textures/Planks023A_512x512_Color_Black.png", new Vector2(8f, 4f));
        var floor = WorldSceneArt.Paint("StationShellFloor", new Color(.36f, .27f, .2f));
        const float w = 26f, d = 16f, h = 6f, t = .5f, door = 4.5f;
        float side = (w - door) * .5f;
        WorldSceneArt.Box(root, "Floor", new Vector3(0f, .1f, 0f), new Vector3(w, .2f, d), floor);
        WorldSceneArt.Box(root, "WallBack", new Vector3(0f, h / 2f, d / 2f - t / 2f), new Vector3(w, h, t), planks);
        WorldSceneArt.Box(root, "WallLeft", new Vector3(-(w / 2f - t / 2f), h / 2f, 0f), new Vector3(t, h, d - 2f * t), planks);
        WorldSceneArt.Box(root, "WallRight", new Vector3(w / 2f - t / 2f, h / 2f, 0f), new Vector3(t, h, d - 2f * t), planks);
        WorldSceneArt.Box(root, "WallFrontLeft", new Vector3(-(door / 2f + side / 2f), h / 2f, -(d / 2f - t / 2f)), new Vector3(side, h, t), planks);
        WorldSceneArt.Box(root, "WallFrontRight", new Vector3(door / 2f + side / 2f, h / 2f, -(d / 2f - t / 2f)), new Vector3(side, h, t), planks);
        WorldSceneArt.Box(root, "Lintel", new Vector3(0f, 4.4f + (h - 4.4f) / 2f, -(d / 2f - t / 2f)), new Vector3(door, h - 4.4f, t), planks);
        WorldSceneArt.Box(root, "Roof", new Vector3(0f, h + .25f, 0f), new Vector3(w + 1.2f, .5f, d + 1.2f), roof);
        // False front: the tall western facade that makes the station read from the far end of the street.
        WorldSceneArt.Box(root, "FalseFront", new Vector3(0f, h + 2.2f, -(d / 2f) - .05f), new Vector3(16f, 4.4f, .4f), planks);
        WorldSceneArt.Box(root, "Porch", new Vector3(0f, .15f, -(d / 2f) - 2f), new Vector3(16f, .3f, 4f), floor);
        WorldSceneArt.Box(root, "PorchRoof", new Vector3(0f, 4.2f, -(d / 2f) - 2.1f), new Vector3(16f, .2f, 4.2f), roof, false);
        foreach (float x in new[] { -7.6f, -2.6f, 2.6f, 7.6f })
            WorldSceneArt.Box(root, "PorchPost", new Vector3(x, 2.1f, -(d / 2f) - 4f), new Vector3(.25f, 4.2f, .25f), roof);
        var sign = WorldSceneArt.Empty(root, "StationSign", new Vector3(0f, h + 2.4f, -(d / 2f) - .3f)).gameObject.AddComponent<TextMesh>();
        sign.text = "สถานีตำรวจสยามภิรมย์"; sign.anchor = TextAnchor.MiddleCenter; sign.fontSize = 64; sign.characterSize = .12f;
        sign.color = new Color(.95f, .85f, .6f);
        sign.gameObject.AddComponent<WorldLabel>().width = 0f;
        StationDressing.Build(root);
        return root;
    }

    public static Material Tex(string name, string texture, Vector2 tiling)
    {
        var material = WorldSceneArt.Paint(name, Color.white);
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
        material.SetTextureScale("_BaseMap", tiling);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void BuildTown(Transform level, Terrain terrain)
    {
        var town = new GameObject("Town").transform;
        town.SetParent(level, false);
        // Shophouse rows (Thai wooden market street x frontier town). Old West buildings where we have them,
        // blockout shophouses (2 storeys, balcony, false front) to fill the rest.
        string ow = "Assets/ThirdParty/OldWest/";
        var west = new (string, float)[] { (ow + "oldwest_07.obj", 12f), (null, 9f), (ow + "oldwest_01.obj", 11f), (null, 9f), (ow + "oldwest_02.obj", 8f), (null, 9f) };
        var east = new (string, float)[] { (null, 9f), (ow + "oldwest_00.obj", 9f), (null, 9f), (ow + "oldwest_03.obj", 7f), (null, 9f), (null, 9f) };
        // Fronts face the street: local -Z of a lot points at x = 0.
        Row(town, west, -14f, 52f, -90f);
        Row(town, east, 14f, 52f, 90f);
        // Props that sell the street: hitching rails, water troughs, wagon, crates.
        Model(town, ow + "oldwest_06.obj", new Vector3(-7f, 0f, 12f), 0f);
        Model(town, ow + "oldwest_12.obj", new Vector3(8.5f, 0f, 36f), 0f);
        Model(town, ow + "oldwest_09.obj", new Vector3(-8.5f, 0f, -6f), 90f);
        Model(town, ow + "oldwest_04.obj", new Vector3(8.5f, 0f, -14f), 0f);
        Model(town, ow + "oldwest_05.obj", new Vector3(-8.5f, 0f, 30f), 0f);
        // Gate at the south end: two posts and a beam with the town name.
        var wood = Tex("OldWest_GateWood", "Assets/ThirdParty/OldWest/Textures/Planks023A_512x512_Color.png", new Vector2(1f, 4f));
        var gate = WorldSceneArt.Empty(town, "TownGate", Vector3.zero);
        Place(gate, new Vector3(0f, 0f, -50f), 0f);
        foreach (float x in new[] { -6f, 6f }) WorldSceneArt.Box(gate, "GatePost", new Vector3(x, 3.5f, 0f), new Vector3(.5f, 7f, .5f), wood);
        WorldSceneArt.Box(gate, "GateBeam", new Vector3(0f, 6.4f, 0f), new Vector3(13.5f, .6f, .6f), wood);
        // Water tower at the edge of town (secondary landmark).
        var tower = WorldSceneArt.Empty(town, "WaterTower", Vector3.zero);
        Place(tower, new Vector3(-30f, 0f, -36f), 0f);
        foreach (var p in new[] { new Vector3(-1.8f, 0, -1.8f), new Vector3(1.8f, 0, -1.8f), new Vector3(-1.8f, 0, 1.8f), new Vector3(1.8f, 0, 1.8f) })
            WorldSceneArt.Box(tower, "Leg", p + Vector3.up * 5f, new Vector3(.35f, 10f, .35f), wood);
        var tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder); tank.name = "Tank";
        tank.transform.SetParent(tower, false); tank.transform.localPosition = new Vector3(0f, 12f, 0f); tank.transform.localScale = new Vector3(5f, 2.2f, 5f);
        tank.GetComponent<Renderer>().sharedMaterial = wood;
    }

    static void Row(Transform parent, (string model, float width)[] lots, float x, float startZ, float yaw)
    {
        float z = startZ;
        var planksA = Tex("Shophouse_PlanksA", "Assets/ThirdParty/OldWest/Textures/Planks012_512x512_Color.png", new Vector2(2f, 2f));
        var planksB = Tex("Shophouse_PlanksB", "Assets/ThirdParty/OldWest/Textures/Planks019_512x512_Color_Red.png", new Vector2(2f, 2f));
        var planksC = Tex("Shophouse_PlanksC", "Assets/ThirdParty/OldWest/Textures/Planks012_512x512_Color_Blue.png", new Vector2(2f, 2f));
        var roof = Tex("Shophouse_Roof", "Assets/ThirdParty/OldWest/Textures/Planks023A_512x512_Color_Black.png", new Vector2(3f, 2f));
        int i = 0;
        foreach (var (model, width) in lots)
        {
            float centre = z - width / 2f;
            if (model != null) Model(parent, model, new Vector3(x + Mathf.Sign(x) * 2f, 0f, centre), yaw);
            else Shophouse(parent, new Vector3(x + Mathf.Sign(x) * 3.5f, 0f, centre), yaw, width - .6f, i % 3 == 0 ? planksA : i % 3 == 1 ? planksB : planksC, roof, 6f + (i % 2) * 1.6f);
            z -= width + 1.5f;
            i++;
        }
    }

    // Two-storey wooden shophouse blockout: shop front open to a covered walkway, balcony above, false front.
    static void Shophouse(Transform parent, Vector3 xz, float yaw, float width, Material walls, Material roof, float height)
    {
        var root = WorldSceneArt.Empty(parent, "Shophouse", Vector3.zero);
        Place(root, xz, yaw);
        const float depth = 8f;
        WorldSceneArt.Box(root, "Body", new Vector3(0f, height / 2f, 1f), new Vector3(width, height, depth), walls);
        WorldSceneArt.Box(root, "FalseFront", new Vector3(0f, height + .9f, -3.05f), new Vector3(width, 1.8f, .3f), walls);
        WorldSceneArt.Box(root, "Walkway", new Vector3(0f, .15f, -4.2f), new Vector3(width, .3f, 2.4f), roof);
        WorldSceneArt.Box(root, "Balcony", new Vector3(0f, 3.1f, -4.2f), new Vector3(width, .25f, 2.4f), roof);
        WorldSceneArt.Box(root, "BalconyRail", new Vector3(0f, 3.7f, -5.3f), new Vector3(width, .1f, .1f), roof, false);
        foreach (float px in new[] { -width / 2f + .3f, width / 2f - .3f })
            WorldSceneArt.Box(root, "Post", new Vector3(px, 1.6f, -5.3f), new Vector3(.2f, 3.2f, .2f), roof);
        var dark = WorldSceneArt.Paint("ShopWindowDark", new Color(.08f, .07f, .06f));
        WorldSceneArt.Box(root, "ShopOpening", new Vector3(0f, 1.4f, -3.06f), new Vector3(width * .55f, 2.4f, .1f), dark, false);
        WorldSceneArt.Box(root, "UpperWindow", new Vector3(0f, 4.6f, -3.06f), new Vector3(width * .4f, 1.3f, .1f), dark, false);
    }

    static void BuildTemple(Transform level, Terrain terrain)
    {
        // Thai temple on the east hill: stacked, steep gold-trimmed roofs, the town's main landmark.
        var temple = WorldSceneArt.Empty(level, "TempleOnHill", Vector3.zero);
        Place(temple, new Vector3(TempleHill.x, 0f, TempleHill.z), -90f);
        var white = WorldSceneArt.Paint("TempleWhite", new Color(.92f, .88f, .8f));
        var red = WorldSceneArt.Paint("TempleRoofRed", new Color(.62f, .16f, .1f));
        var gold = WorldSceneArt.Paint("TempleGold", new Color(.95f, .72f, .25f));
        WorldSceneArt.Box(temple, "Platform", new Vector3(0f, .5f, 0f), new Vector3(16f, 1f, 24f), white);
        WorldSceneArt.Box(temple, "Hall", new Vector3(0f, 3.5f, 0f), new Vector3(10f, 5f, 18f), white);
        for (int k = 0; k < 3; k++)
        {
            float y = 6.5f + k * 2.4f, wdt = 12f - k * 3.2f, len = 20f - k * 4f;
            var left = WorldSceneArt.Box(temple, "Roof", new Vector3(-wdt / 4f, y, 0f), new Vector3(wdt / 2f + .6f, .3f, len), red);
            left.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
            var right = WorldSceneArt.Box(temple, "Roof", new Vector3(wdt / 4f, y, 0f), new Vector3(wdt / 2f + .6f, .3f, len), red);
            right.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
        }
        WorldSceneArt.Box(temple, "Ridge", new Vector3(0f, 13.4f, 0f), new Vector3(.4f, .4f, 12f), gold);
        WorldSceneArt.Box(temple, "Finial", new Vector3(0f, 14.5f, 6f), new Vector3(.3f, 2.4f, .3f), gold);
        WorldSceneArt.Box(temple, "Finial", new Vector3(0f, 14.5f, -6f), new Vector3(.3f, 2.4f, .3f), gold);
        var chedi = GameObject.CreatePrimitive(PrimitiveType.Capsule); chedi.name = "Chedi";
        chedi.transform.SetParent(temple, false); chedi.transform.localPosition = new Vector3(0f, 5f, -16f); chedi.transform.localScale = new Vector3(4f, 5f, 4f);
        chedi.GetComponent<Renderer>().sharedMaterial = white;
    }

    // ---------------------------------------------------------------- hideout (Gaptooth-style mining camp)

    static Hideout BuildCamp(Transform level, Terrain terrain)
    {
        var root = new GameObject("Hideout_east_shophouse");
        root.transform.SetParent(level, false);
        // Entrance faces back down the canyon road (north-west).
        Place(root.transform, new Vector3(Camp.x, 0f, Camp.z), 140f);
        var hideout = root.AddComponent<Hideout>();
        hideout.hideoutId = "east_shophouse";
        hideout.zoneHint = "เหมืองร้างท้ายหุบเขา";
        WorldSceneArt.Compound(hideout, null, false, 0);
        var camp = WorldSceneArt.Empty(level, "MiningCamp", Vector3.zero);
        var wood = Tex("Camp_Wood", "Assets/ThirdParty/OldWest/Textures/Planks023A_512x512_Color.png", new Vector2(1f, 3f));
        var metal = Tex("Camp_Metal", "Assets/ThirdParty/OldWest/Textures/Metal041C_512x512_Color.png", new Vector2(2f, 2f));
        // Lookout tower by the gate (the first threat you see), barricade, tents, propane tanks to shoot.
        Vector3 Local(float x, float z) => root.transform.TransformPoint(new Vector3(x, 0f, z));
        var lookout = WorldSceneArt.Empty(camp, "LookoutTower", Vector3.zero);
        Place(lookout, Local(-9f, -8f), 140f);
        foreach (var p in new[] { new Vector3(-1.2f, 0, -1.2f), new Vector3(1.2f, 0, -1.2f), new Vector3(-1.2f, 0, 1.2f), new Vector3(1.2f, 0, 1.2f) })
            WorldSceneArt.Box(lookout, "Leg", p + Vector3.up * 3.5f, new Vector3(.3f, 7f, .3f), wood);
        WorldSceneArt.Box(lookout, "Deck", new Vector3(0f, 7f, 0f), new Vector3(3.2f, .25f, 3.2f), wood);
        WorldSceneArt.Box(lookout, "RoofCap", new Vector3(0f, 9.4f, 0f), new Vector3(3.6f, .2f, 3.6f), metal, false);
        for (int i = 0; i < 4; i++)
        {
            var bar = WorldSceneArt.Empty(camp, "Barricade", Vector3.zero);
            Place(bar, Local(-6f + i * 4f, -12f), 140f + (i % 2 == 0 ? 8f : -6f));
            WorldSceneArt.Box(bar, "Planks", new Vector3(0f, .7f, 0f), new Vector3(3f, 1.4f, .3f), wood);
        }
        Model(camp, "Assets/ThirdParty/OldWest/oldwest_06.obj", Local(14f, -4f), 60f);
        foreach (var p in new[] { Local(12f, 6f), Local(13f, 7.5f), Local(-12f, 10f) })
            Model(camp, "Assets/ThirdParty/Quaternius/Props/PropaneTank.fbx", p, 0f, 1f);
        // Mine entrance in the slope behind the camp (back route / flank exit).
        var mine = WorldSceneArt.Empty(camp, "MineEntrance", Vector3.zero);
        Place(mine, new Vector3(FlankPath[3].x, 0f, FlankPath[3].z), 200f);
        WorldSceneArt.Box(mine, "FrameL", new Vector3(-1.8f, 1.8f, 0f), new Vector3(.4f, 3.6f, .4f), wood);
        WorldSceneArt.Box(mine, "FrameR", new Vector3(1.8f, 1.8f, 0f), new Vector3(.4f, 3.6f, .4f), wood);
        WorldSceneArt.Box(mine, "FrameTop", new Vector3(0f, 3.7f, 0f), new Vector3(4.2f, .4f, .4f), wood);
        WorldSceneArt.Box(mine, "Dark", new Vector3(0f, 1.8f, .4f), new Vector3(3.2f, 3.4f, .1f), WorldSceneArt.Paint("MineDark", new Color(.02f, .02f, .02f)), false);
        // Radio mast on the ridge above: the scam call centre's antenna, visible from town with a blinking red light.
        var mast = WorldSceneArt.Empty(level, "RadioMast", Vector3.zero);
        Place(mast, new Vector3(Mast.x, 0f, Mast.z), 0f);
        for (int k = 0; k < 4; k++)
            WorldSceneArt.Box(mast, "Leg", new Vector3((k % 2 - .5f) * 2.4f, 14f, (k / 2 - .5f) * 2.4f), new Vector3(.25f, 28f, .25f), metal);
        for (int k = 1; k < 9; k++)
            WorldSceneArt.Box(mast, "Brace", new Vector3(0f, k * 3.2f, 0f), new Vector3(2.6f, .15f, 2.6f), metal, false);
        WorldSceneArt.Box(mast, "Antenna", new Vector3(0f, 31f, 0f), new Vector3(.15f, 6f, .15f), metal, false);
        var beacon = WorldSceneArt.Empty(mast, "WarningLight", new Vector3(0f, 34.2f, 0f)).gameObject.AddComponent<Light>();
        beacon.type = LightType.Point; beacon.color = new Color(1f, .15f, .1f); beacon.intensity = 6f; beacon.range = 25f;
        var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere); bulb.name = "Bulb";
        bulb.transform.SetParent(beacon.transform, false); bulb.transform.localScale = Vector3.one * .7f;
        Object.DestroyImmediate(bulb.GetComponent<Collider>());
        bulb.GetComponent<Renderer>().sharedMaterial = WorldSceneArt.Paint("MastRed", new Color(2f, .2f, .15f), true);
        return hideout;
    }

    static void BuildTraps(Transform level, Terrain terrain)
    {
        var traps = new GameObject("Traps").transform;
        traps.SetParent(level, false);
        // Roadside camp in the canyon: a real crate beside a fake one ("free!"), scammer hidden behind the rocks.
        var real = CaseFlowSceneBuilder.Prop("SupplyCrate_Real", Vector3.zero, Vector3.one, 20f, CaseFlowSceneBuilder.Textured("tex_supply_box_real", new Color(.6f, .5f, .35f)));
        Place(real.transform, new Vector3(-6f, .5f, -112f), 20f); real.transform.SetParent(traps, true);
        real.AddComponent<SupplyCrate>().isFake = false;
        var fake = CaseFlowSceneBuilder.Prop("SupplyCrate_Fake", Vector3.zero, Vector3.one, -10f, CaseFlowSceneBuilder.Textured("tex_supply_box_fake", new Color(.9f, .5f, .2f)));
        Place(fake.transform, new Vector3(-7.5f, .5f, -116f), -10f); fake.transform.SetParent(traps, true);
        var fakeCrate = fake.AddComponent<SupplyCrate>();
        fakeCrate.isFake = true;
        var hidden = CaseFlowSceneBuilder.HiddenScammer(traps, Vector3.zero);
        Place(hidden.transform, new Vector3(-12f, .12f, -120f), 60f);
        fakeCrate.activateOnSpring = new[] { hidden };
        Model(traps, "Assets/ThirdParty/Quaternius/Props/Bonfire.fbx", new Vector3(-9f, 0f, -110f), 0f, 1f, false);
        // Phishing sign where the overlook path leaves the road: a shortcut that "saves time".
        var sign = CaseFlowSceneBuilder.RoadSign(traps, "PhishingSign", Vector3.zero, new Vector2(3f, 1.5f), 0f, "tex_sign_phishing");
        Place(sign.transform, new Vector3(30f, 0f, -128f), 200f); sign.transform.SetParent(traps, true);
        sign.AddComponent<PhishingSign>();
        // Disguised scammer loitering at the gate.
        var villager = WorldSceneArt.Character(traps, "Soldier_Male", Vector3.zero, true, Faction.Scammer);
        Place(villager.transform, new Vector3(4f, .12f, -54f), 0f);
        villager.name = "DisguisedScammer";
        Object.DestroyImmediate(villager.GetComponent<ArmedEnemy>());
        Object.DestroyImmediate(villager.GetComponent<UnityEngine.AI.NavMeshAgent>());
        villager.AddComponent<DisguisedScammer>();
        // Money bag by the riverbed with an ambush.
        var bag = CaseFlowSceneBuilder.Prop("AmbushBait_MoneyBag", Vector3.zero, Vector3.one * .6f, 0f, CaseFlowSceneBuilder.Textured("tex_money_bag", new Color(.7f, .6f, .3f)));
        Place(bag.transform, new Vector3(14f, .3f, -72f), 0f); bag.transform.SetParent(traps, true);
        var bait = bag.AddComponent<AmbushBait>();
        var a1 = CaseFlowSceneBuilder.HiddenScammer(traps, Vector3.zero); Place(a1.transform, new Vector3(22f, .12f, -66f), -90f);
        var a2 = CaseFlowSceneBuilder.HiddenScammer(traps, Vector3.zero); Place(a2.transform, new Vector3(20f, .12f, -80f), -60f);
        bait.activateOnSpring = new[] { a1, a2 };
    }

    // Dead trees, rocks, dry grass and cacti on the valley floor and canyon edges, never on roads or lots.
    static void Scatter(Transform level, Terrain terrain)
    {
        var root = new GameObject("Vegetation").transform;
        root.SetParent(level, false);
        var rng = new System.Random(2035);
        // Photo-scanned Karoo desert set (Poly Haven, CC0) plus the Old West cacti.
        string n = "Assets/ThirdParty/PolyHaven/";
        var items = new (string path, float weight, float scale, bool collide)[] {
            (n + "namaqualand_boulder_02/namaqualand_boulder_02_2k.fbx", 2f, 1.6f, true), (n + "namaqualand_boulder_03/namaqualand_boulder_03_2k.fbx", 2f, 1.4f, true),
            (n + "dead_tree_trunk_02/dead_tree_trunk_02_2k.fbx", .8f, 1f, true),
            (n + "searsia_lucida/searsia_lucida_2k.fbx", 3f, 1f, false), (n + "othonna_cerarioides/othonna_cerarioides_2k.fbx", 3f, 1.2f, false),
            (n + "dry_branches_medium_01/dry_branches_medium_01_2k.fbx", 2f, 1f, false),
            ("Assets/ThirdParty/OldWest/oldwest_10.obj", 1.5f, 1f, true) };
        float total = 0f; foreach (var it in items) total += it.weight;
        int placed = 0;
        for (int attempt = 0; attempt < 6000 && placed < 420; attempt++)
        {
            float x = (float)rng.NextDouble() * 360f - 140f, z = (float)rng.NextDouble() * 360f - 280f;
            if (PathDistance(Road, x, z, out _) < 8f || PathDistance(OverlookPath, x, z, out _) < 3f || PathDistance(FlankPath, x, z, out _) < 3f) continue;
            if (Mathf.Abs(x) < 26f && z > -52f && z < 92f) continue; // street, lots and station
            if (Vector2.Distance(new Vector2(x, z), new Vector2(Camp.x, Camp.z)) < 26f) continue;
            if (Vector2.Distance(new Vector2(x, z), new Vector2(TempleHill.x, TempleHill.z)) < 16f) continue;
            float u = (x - Origin.x) / Size, v = (z - Origin.z) / Size;
            if (terrain.terrainData.GetSteepness(u, v) > 32f) continue;
            float pick = (float)rng.NextDouble() * total; int k = 0;
            while (pick > items[k].weight) { pick -= items[k].weight; k++; }
            // Rocks and dead trees gather near the cliff feet; the open flats keep only dry grass and bushes.
            float slopeNear = Mathf.Max(terrain.terrainData.GetSteepness(Mathf.Clamp01(u + .015f), v), terrain.terrainData.GetSteepness(Mathf.Clamp01(u - .015f), v),
                terrain.terrainData.GetSteepness(u, Mathf.Clamp01(v + .015f)), terrain.terrainData.GetSteepness(u, Mathf.Clamp01(v - .015f)));
            bool big = items[k].path.Contains("boulder") || items[k].path.Contains("cliff") || items[k].path.Contains("trunk");
            if (big && slopeNear < 30f) continue;
            var go = Model(root, items[k].path, new Vector3(x, 0f, z), (float)rng.NextDouble() * 360f, items[k].scale * (.8f + (float)rng.NextDouble() * .5f), items[k].collide);
            if (go != null) { go.transform.position -= Vector3.up * .15f; placed++; }
        }
        Debug.Log("Scattered " + placed);
    }

    static void MenuCameras(Transform level)
    {
        // Title screen drift: high over the town looking down the street toward the canyon and the mast.
        var a = new GameObject("MenuCam_A").transform; a.SetParent(level, false);
        a.SetPositionAndRotation(new Vector3(-22f, 32f, 96f), Quaternion.Euler(14f, 160f, 0f));
        var b = new GameObject("MenuCam_B").transform; b.SetParent(level, false);
        b.SetPositionAndRotation(new Vector3(18f, 28f, 92f), Quaternion.Euler(12f, 195f, 0f));
    }
}
