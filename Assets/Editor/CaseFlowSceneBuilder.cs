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
        Lighting();

        var player = PlayerSandbox.CreatePlayer();
        player.transform.position = new Vector3(0f, 0.05f, -24f);
        player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        var camera = player.GetComponentInChildren<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.farClipPlane = 700f;
        var horse = PlayerSandbox.CreateHorse(Flat);
        horse.transform.position = new Vector3(4f, 0.05f, -26f);
        horse.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        horse.AddComponent<HorseGenes>();

        var counter = Prop("EvidenceCounter", new Vector3(0f, 0.6f, -16f), new Vector3(3.2f, 1.2f, 1.2f), 0f,
            Textured("tex_evidence_seal_table", new Color(0.4f, 0.3f, 0.2f)));

        var hideouts = new GameObject("Hideouts").transform;
        AddHideout(hideouts, "east_shophouse", CityZone.Shophouse, CityDirection.East);
        AddHideout(hideouts, "north_tower", CityZone.Tower, CityDirection.North);
        AddHideout(hideouts, "south_house", CityZone.House, CityDirection.South);
        AddHideout(hideouts, "west_tower", CityZone.Tower, CityDirection.West);

        AddTraps(new GameObject("Traps").transform);

        var flow = new GameObject("CaseFlow").AddComponent<CaseFlowDemo>();
        flow.sealPoint = counter.transform;

        MarkStatic(city.Root);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Case flow demo scene saved to {ScenePath}");
    }

    static void AddHideout(Transform parent, string id, CityZone zone, CityDirection direction)
    {
        Vector3 pos = CityLayoutPoints.Point(zone, direction);
        var root = new GameObject("Hideout_" + id);
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        var hideout = root.AddComponent<Hideout>();
        hideout.hideoutId = id;
        hideout.zoneHint = CityLayoutPoints.Describe(zone, direction);

        // A reinforced door facing the road, so the player knows where to hold E.
        Vector3 toRoad = -Vector3.Cross(Vector3.up, CityLayoutPoints.Direction(direction));
        float yaw = Quaternion.LookRotation(toRoad).eulerAngles.y;
        var door = Prop("Door", pos + Vector3.up * 2f, new Vector3(3f, 4f, 0.4f), yaw, Textured("tex_hideout_door", new Color(0.2f, 0.22f, 0.24f)));
        door.transform.SetParent(root.transform, true);
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
        var sign = Prop("PhishingSign", north * 42f + Vector3.Cross(Vector3.up, north) * 6f + Vector3.up * 2.2f,
            new Vector3(3f, 1.5f, 0.1f), 180f, Textured("tex_sign_phishing", new Color(0.9f, 0.2f, 0.2f)));
        sign.AddComponent<PhishingSign>();
        sign.transform.SetParent(parent, true);

        // East road: a "villager" in an off-colour uniform who stares too long.
        Vector3 east = CityLayoutPoints.Direction(CityDirection.East);
        var villager = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        villager.name = "DisguisedScammer";
        villager.transform.position = east * 70f + Vector3.Cross(Vector3.up, east) * 5f + Vector3.up;
        villager.GetComponent<Renderer>().sharedMaterial = Textured("tex_uniform_fake_police", new Color(0.3f, 0.35f, 0.5f));
        var health = villager.AddComponent<Health>();
        health.max = 3f;
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
        var rentSign = Prop("RentHorseSign", west * 50f + westSide * 9f + Vector3.up * 2f, new Vector3(2.4f, 1.2f, 0.1f), 90f,
            Textured("tex_sign_rent_horse", new Color(0.8f, 0.7f, 0.4f)));
        rentSign.transform.SetParent(parent, true);

        // South road further out: a phone booth that never stops ringing.
        var booth = Prop("CallCenterBooth", south * 100f - southSide * 7f + Vector3.up * 1.3f, new Vector3(1.2f, 2.6f, 1.2f), 0f,
            Textured("tex_phone_booth", new Color(0.2f, 0.45f, 0.5f)));
        booth.AddComponent<CallCenterBooth>();
        booth.transform.SetParent(parent, true);
    }

    static GameObject HiddenScammer(Transform parent, Vector3 position)
    {
        var scammer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        scammer.name = "HiddenScammer";
        scammer.transform.position = position + Vector3.up;
        scammer.GetComponent<Renderer>().sharedMaterial = Textured("tex_outfit_scammer", new Color(0.66f, 0.29f, 0.2f));
        var health = scammer.AddComponent<Health>();
        health.max = 3f;
        scammer.transform.SetParent(parent, true);
        scammer.SetActive(false);
        return scammer;
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
