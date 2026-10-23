using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using ScammerNoScamming;

// Builds Assets/Scenes/CaseFlowDemo.unity: the ring city, player and horse, the four case hideouts,
// one of every road trap and the station's evidence counter, driven by CaseFlowDemo.
// chanawee's test scene; it does not touch CitySandbox or PlayerSandbox.
public static class CaseFlowSceneBuilder
{
    const string ScenePath = "Assets/Scenes/CaseFlowDemo.unity";
    const string CityMaterialFolder = "Assets/Materials/City";
    const string TrapMaterialFolder = "Assets/Materials/Traps";
    const string TextureFolder = "Assets/Art/Textures/AI";

    [MenuItem("Tools/Scammer No Scamming/Build Case Flow Demo Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var mats = new CityMaterials
        {
            TowerA = LoadCity("Mat_facade_tower_a"),
            TowerB = LoadCity("Mat_facade_tower_b"),
            Mid = LoadCity("Mat_facade_mid"),
            Roof = LoadCity("Mat_roof_house"),
            Ground = LoadCity("Mat_road_dirt"),
            Flat = Flat,
        };
        if (mats.Ground != null)
            mats.Ground.SetTextureScale("_BaseMap", Vector2.one * (CityBuilder.GroundSize / 8f));
        var city = CityBuilder.Build(mats);

        var player = PlayerSandbox.CreatePlayer();
        player.transform.position = new Vector3(0f, 0.05f, -24f);
        player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        var camera = player.GetComponentInChildren<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.farClipPlane = 700f;
        WorldSceneArt.Lighting(camera);
        var horse = PlayerSandbox.CreateHorse(Flat);
        horse.transform.position = new Vector3(4f, 0.05f, -26f);
        horse.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        horse.AddComponent<HorseGenes>();
        WorldSceneArt.Horse(horse);

        var counter = Prop("EvidenceCounter", new Vector3(0f, 0.6f, -16f), new Vector3(3.2f, 1.2f, 1.2f), 0f,
            Textured("tex_evidence_seal_table", new Color(0.4f, 0.3f, 0.2f)));

        var hideouts = new GameObject("Hideouts").transform;
        AddHideout(hideouts, "east_shophouse", CityZone.Shophouse, CityDirection.East, city, 0);
        AddHideout(hideouts, "north_tower", CityZone.Tower, CityDirection.North, city, 1);
        AddHideout(hideouts, "south_house", CityZone.House, CityDirection.South, city, 2);
        AddHideout(hideouts, "west_tower", CityZone.Tower, CityDirection.West, city, 3);

        AddTraps(new GameObject("Traps").transform);

        var flow = new GameObject("CaseFlow").AddComponent<CaseFlowDemo>();
        flow.sealPoint = counter.transform;
        var oldInterior = city.Station.transform.Find("Interior");
        if (oldInterior != null) Object.DestroyImmediate(oldInterior.gameObject);
        var station = flow.gameObject.AddComponent<StationInvestigation>();
        station.stationAnchor = city.Station.transform;
        station.victimPrefabs = new[] {
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Quaternius/Characters/Chef_Female.fbx"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Quaternius/Characters/Casual2_Male.fbx"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Quaternius/Characters/OldClassy_Male.fbx"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Quaternius/Characters/Suit_Female.fbx")
        };
        station.interiorPrefabs = new[] {
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Quaternius/Interior/Bookshelf.fbx"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Quaternius/Interior/Chair_1.fbx"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Quaternius/Interior/Light_Desk.fbx")
        };

        MarkStatic(city.Root);
        EditorSceneManager.SaveScene(scene, ScenePath);
        RegisterFirst(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Case flow demo scene saved to {ScenePath}");
    }

    static void AddHideout(Transform parent, string id, CityZone zone, CityDirection direction, CityResult city, int index)
    {
        Vector3 pos = CityLayoutPoints.Point(zone, direction, 13f);
        var root = new GameObject("Hideout_" + id);
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        // Local +Z goes into the compound; local -Z is the open entrance facing the road.
        root.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, CityLayoutPoints.Direction(direction)));
        var hideout = root.AddComponent<Hideout>();
        hideout.hideoutId = id;
        hideout.zoneHint = CityLayoutPoints.Describe(zone, direction);

        WorldSceneArt.Compound(hideout, city, index == 3, index);
    }

    // Moves a door spot along the road until no building collider overlaps it.
    static Vector3 FreeSpot(Vector3 point, Vector3 along)
    {
        Physics.SyncTransforms();
        for (int i = 0; i < 20; i++)
        {
            Vector3 p = point + along * (i % 2 == 0 ? i * 2f : -i * 2f);
            var hits = Physics.OverlapBox(p + Vector3.up * 2f, new Vector3(2f, 1.8f, 2f));
            bool blocked = false;
            foreach (var h in hits)
                if (h.gameObject.name != "Ground" && !(h is TerrainCollider) && h.bounds.size.y > 0.5f) { blocked = true; break; }
            if (!blocked) return p;
        }
        Debug.LogWarning($"No free spot near {point}");
        return point;
    }

    static void AddTraps(Transform parent)
    {
        // South avenue near the plaza: a real crate (seal) beside a fake one (no seal, "free!").
        Vector3 south = CityLayoutPoints.Direction(CityDirection.South);
        Vector3 southSide = Vector3.Cross(Vector3.up, south);
        var real = Prop("SupplyCrate_Real", south * 44f + southSide * 6f + Vector3.up * 0.5f, Vector3.one, 0f,
            Textured("tex_supply_box_real", new Color(0.6f, 0.5f, 0.35f)));
        real.AddComponent<SupplyCrate>().isFake = false;
        real.transform.SetParent(parent, true);
        var fake = Prop("SupplyCrate_Fake", south * 47f + southSide * 6f + Vector3.up * 0.5f, Vector3.one, 0f,
            Textured("tex_supply_box_fake", new Color(0.9f, 0.5f, 0.2f)));
        var fakeCrate = fake.AddComponent<SupplyCrate>();
        fakeCrate.isFake = true;
        fakeCrate.activateOnSpring = new[] { HiddenScammer(parent, fake.transform.position + southSide * 4f) };
        fake.transform.SetParent(parent, true);

        // North road: a floating ad with a misspelled link.
        Vector3 north = CityLayoutPoints.Direction(CityDirection.North);
        var sign = RoadSign(parent, "PhishingSign", north * 42f + Vector3.Cross(Vector3.up, north) * 7f,
            new Vector2(3f, 1.5f), 0f, "tex_sign_phishing");
        sign.AddComponent<PhishingSign>();
        sign.transform.SetParent(parent, true);

        // East road: a "villager" in an off-colour uniform who stares too long.
        Vector3 east = CityLayoutPoints.Direction(CityDirection.East);
        var villager = WorldSceneArt.Character(parent, "Soldier_Male",
            east * 70f + Vector3.Cross(Vector3.up, east) * 5f + Vector3.up * .12f, true, Faction.Scammer);
        villager.name = "DisguisedScammer";
        // The disguise is a trap until exposed, rather than a guard shooting before its tell.
        Object.DestroyImmediate(villager.GetComponent<ArmedEnemy>());
        Object.DestroyImmediate(villager.GetComponent<UnityEngine.AI.NavMeshAgent>());
        villager.AddComponent<DisguisedScammer>();
        villager.transform.SetParent(parent, true);

        // West road: a money bag in a side alley under a "rent your account" sign.
        Vector3 west = CityLayoutPoints.Direction(CityDirection.West);
        Vector3 westSide = Vector3.Cross(Vector3.up, west);
        var bag = Prop("AmbushBait_MoneyBag", west * 52f + westSide * 7f + Vector3.up * 0.3f, Vector3.one * 0.6f, 0f,
            Textured("tex_money_bag", new Color(0.7f, 0.6f, 0.3f)));
        var bait = bag.AddComponent<AmbushBait>();
        bait.activateOnSpring = new[] { HiddenScammer(parent, bag.transform.position + westSide * 5f), HiddenScammer(parent, bag.transform.position - westSide * 5f + west * 3f) };
        bag.transform.SetParent(parent, true);
        var rentSign = RoadSign(parent, "RentHorseSign", west * 50f + westSide * 9f,
            new Vector2(2.4f, 1.2f), -90f, "tex_sign_rent_horse");
        rentSign.transform.SetParent(parent, true);

        // South road further out: a phone booth that never stops ringing.
        var booth = WorldSceneArt.Empty(parent, "CallCenterBooth", south * 100f - southSide * 8f, 180f).gameObject;
        var teal = WorldSceneArt.Paint("BoothTeal", new Color(.12f, .27f, .27f));
        WorldSceneArt.Box(booth.transform, "BoothBack", new Vector3(0f, 1.35f, .52f), new Vector3(1.45f, 2.7f, .14f), teal);
        for (int side = -1; side <= 1; side += 2)
            WorldSceneArt.Box(booth.transform, "BoothSide", new Vector3(side * .68f, 1.35f, 0f), new Vector3(.12f, 2.7f, 1.1f), teal);
        WorldSceneArt.Box(booth.transform, "BoothRoof", new Vector3(0f, 2.75f, 0f), new Vector3(1.6f, .17f, 1.25f), teal);
        WorldSceneArt.FrontArt(booth.transform, "PhonePanel", new Vector3(0f, 1.35f, .43f), new Vector2(1.15f, 2.2f), Textured("tex_phone_booth", Color.white));
        WorldSceneArt.Model(booth.transform, "Props/Phone", new Vector3(0f, .9f, -.05f), .35f);
        booth.AddComponent<CallCenterBooth>();
        booth.transform.SetParent(parent, true);
    }

    static GameObject HiddenScammer(Transform parent, Vector3 position)
    {
        var scammer = WorldSceneArt.Character(parent, "Casual3_Male", position + Vector3.up * .12f, true, Faction.Scammer);
        scammer.name = "HiddenScammer";
        scammer.transform.SetParent(parent, true);
        scammer.SetActive(false);
        return scammer;
    }

    static GameObject RoadSign(Transform parent, string name, Vector3 position, Vector2 size, float yaw, string texture)
    {
        var sign = WorldSceneArt.Empty(parent, name, position, yaw).gameObject;
        var wood = WorldSceneArt.Paint("RoadSignWood", new Color(.27f, .17f, .1f));
        WorldSceneArt.Box(sign.transform, "SignPost", new Vector3(0f, 1.3f, 0f), new Vector3(.18f, 2.6f, .18f), wood);
        WorldSceneArt.Box(sign.transform, "SignBacking", new Vector3(0f, 2.2f, 0f), new Vector3(size.x + .14f, size.y + .14f, .16f), wood);
        WorldSceneArt.FrontArt(sign.transform, "SignArtwork", new Vector3(0f, 2.2f, -.086f), size, Textured(texture, Color.white));
        return sign;
    }

    static GameObject Prop(string name, Vector3 position, Vector3 scale, float yaw, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    // A URP Lit material asset per texture, created once under Assets/Materials/Traps.
    static Material Textured(string texture, Color fallback)
    {
        string path = $"{TrapMaterialFolder}/Mat_{texture}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{texture}.png");
        if (existing != null && (tex == null || existing.GetTexture("_BaseMap") == tex)) return existing;

        if (!AssetDatabase.IsValidFolder(TrapMaterialFolder)) AssetDatabase.CreateFolder("Assets/Materials", "Traps");
        var mat = existing != null ? existing : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
        }
        else
        {
            mat.SetColor("_BaseColor", fallback);
        }
        if (existing == null) AssetDatabase.CreateAsset(mat, path);
        else EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material Flat(Color color)
    {
        string hex = ColorUtility.ToHtmlStringRGB(color);
        var shared = AssetDatabase.LoadAssetAtPath<Material>($"{CityMaterialFolder}/Flat_{hex}.mat");
        if (shared != null) return shared;  // reuse the city builder's flat colours
        string path = $"{TrapMaterialFolder}/Mat_flat_{hex}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        if (!AssetDatabase.IsValidFolder(TrapMaterialFolder)) AssetDatabase.CreateFolder("Assets/Materials", "Traps");
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // The jam build should open on this flow, so put the scene first in Build Settings (others kept).
    static void RegisterFirst(string path)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(path, true) };
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != path) scenes.Add(s);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static Material LoadCity(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{CityMaterialFolder}/{name}.mat");

    static void Lighting()
    {
        var lightObject = new GameObject("Sun");
        lightObject.transform.rotation = Quaternion.Euler(22f, -140f, 0f);
        var sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.72f, 0.5f);
        sun.intensity = 1.3f;
        RenderSettings.sun = sun;
        RenderSettings.skybox = LoadCity("Mat_sky_dusk");
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.42f, 0.4f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.85f, 0.55f, 0.35f);
        RenderSettings.fogStartDistance = 140f;
        RenderSettings.fogEndDistance = 460f;
    }

    static void MarkStatic(GameObject root)
    {
        const StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;
        foreach (var t in root.GetComponentsInChildren<Transform>())
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
    }
}
