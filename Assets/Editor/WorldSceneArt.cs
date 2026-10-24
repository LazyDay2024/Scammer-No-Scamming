using System;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ScammerNoScamming;
using Object = UnityEngine.Object;

// Editor-only construction: every shader, animation and NavMesh is serialized into the scene/build.
public static class WorldSceneArt
{
    const string Folder = "Assets/Materials/World";
    const string ModelRoot = "Assets/ThirdParty/Quaternius/";
    static readonly Color Wood = new Color(.24f, .145f, .09f);
    static readonly Color Iron = new Color(.12f, .16f, .17f);

    public static Material Paint(string name, Color color, bool unlit = false, string folder = Folder)
    {
        EnsureFolder(folder);
        string path = $"{folder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .18f);
        EditorUtility.SetDirty(material);
        return material;
    }

    public static Material Texture(string name, Color tint)
    {
        var material = Paint(name, tint);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Textures/AI/{name}.png");
        if (texture != null) { material.SetTexture("_BaseMap", texture); material.SetColor("_BaseColor", Color.white); }
        return material;
    }

    public static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        int slash = folder.LastIndexOf('/');
        EnsureFolder(folder.Substring(0, slash));
        AssetDatabase.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
    }

    public static Transform Empty(Transform parent, string name, Vector3 position, float yaw = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return go.transform;
    }

    public static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collision = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!collision) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    // Artwork belongs only on the visible face; the back and edges keep their own wood/metal material.
    public static GameObject FrontArt(Transform parent, string name, Vector3 position, Vector2 size, Material material, float yaw = 0f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        go.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    public static GameObject Model(Transform parent, string relativePath, Vector3 position, float height, float yaw = 0f, bool byWidth = false)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelRoot + relativePath + ".fbx");
        if (prefab == null) throw new InvalidOperationException("Required CC0 model missing: " + relativePath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        // Unpack so converted URP materials and clip references are stable scene overrides.
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        // Keep the FBX root rotation (some Blender exports lie flat without it) and add our yaw on top.
        instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation;
        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return instance;
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var source = materials[i];
                if (source == null) { materials[i] = Paint("ModelFallback", new Color(.54f, .39f, .24f)); continue; }
                if (source.shader != null && source.shader.name.StartsWith("Universal Render Pipeline/")) continue;
                string safe = source.name.Replace(' ', '_').Replace('/', '_');
                Color color = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                var converted = Paint("CC0_" + safe, color);
                if (source.HasProperty("_MainTex") && source.GetTexture("_MainTex") != null)
                    converted.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
                materials[i] = converted;
            }
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.On;
        }
        // byWidth: "height" is the largest horizontal size instead (rugs, flat props).
        float factor = height / Mathf.Max(.001f, byWidth ? Mathf.Max(bounds.size.x, bounds.size.z) : bounds.size.y);
        instance.transform.localScale *= factor;
        // Plant its feet on the intended floor even when the FBX pivot lies in its centre.
        instance.transform.position += Vector3.up * ((parent.TransformPoint(position).y - bounds.min.y) * factor);
        foreach (var collider in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
        return instance;
    }

    public static AnimationClip Clip(string relativePath, string name)
    {
        var clip = AssetDatabase.LoadAllAssetsAtPath(ModelRoot + relativePath + ".fbx").OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview") && (c.name == name || c.name.EndsWith("|" + name)));
        if (clip == null) throw new InvalidOperationException("Missing animation " + relativePath + ":" + name);
        return clip;
    }

    public static void Horse(GameObject horse)
    {
        foreach (var renderer in horse.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        const string path = "Animals/Horse";
        var realistic = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Scammer/HorseModel.prefab");
        if (realistic != null)
        {
            var horseModel = (GameObject)PrefabUtility.InstantiatePrefab(realistic);
            horseModel.transform.SetParent(horse.transform, false);
            var parts = horseModel.GetComponentsInChildren<Renderer>();
            if (parts.Length > 0)
            {
                Bounds hb = parts[0].bounds; foreach (var r in parts) hb.Encapsulate(r.bounds);
                horseModel.transform.localScale *= 2.2f / Mathf.Max(.01f, hb.size.y);
            }
            var drive = horse.AddComponent<HorseVisual>();
            drive.horse = horse.GetComponent<HorseController>();
            drive.animator = horseModel.GetComponentInChildren<Animator>();
            return;
        }
        var model = Model(horse.transform, path, Vector3.zero, 2.15f);
        // The CC0 horse is nearly black brown and read as a silhouette at golden hour; repaint it chestnut.
        var coat = Paint("HorseCoat", new Color(.46f, .25f, .13f));
        var mane = Paint("HorseMane", new Color(.12f, .08f, .06f));
        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                materials[i] = materials[i] != null && materials[i].GetColor("_BaseColor").maxColorComponent < .05f ? mane : coat;
            renderer.sharedMaterials = materials;
        }
        var visual = horse.AddComponent<HorseVisual>();
        visual.horse = horse.GetComponent<HorseController>();
        visual.animator = model.GetComponentInChildren<Animator>();
        visual.idle = Clip(path, "Idle");
        visual.walk = Clip(path, "Walk");
        visual.run = Clip(path, "Run");
    }

    public static GameObject Character(Transform parent, string modelName, Vector3 position, bool armed, Faction faction, float yaw = 180f)
    {
        var root = Empty(parent, armed ? "Guard_" + modelName : "Worker_" + modelName, position, yaw).gameObject;
        string path = "Characters/" + modelName;
        GameObject model;
        // Armed scammers use Nithis's realistic bandit (animated revolver stance, death); others keep the CC0 rigs.
        var bandit = armed ? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Scammer/ScammerModel.prefab") : null;
        if (bandit != null)
        {
            model = (GameObject)PrefabUtility.InstantiatePrefab(bandit);
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            var parts = model.GetComponentsInChildren<Renderer>();
            if (parts.Length > 0)
            {
                Bounds b = parts[0].bounds; foreach (var r in parts) b.Encapsulate(r.bounds);
                model.transform.localScale *= 1.8f / Mathf.Max(.01f, b.size.y);
            }
            var driver = root.AddComponent<BanditAnimator>();
            driver.animator = model.GetComponentInChildren<Animator>();
        }
        else
        {
            model = Model(root.transform, path, Vector3.zero, 1.82f);
            var animation = root.AddComponent<NpcAnimator>();
            animation.animator = model.GetComponentInChildren<Animator>();
            animation.idle = Clip(path, "Idle");
            animation.walk = Clip(path, "Walk");
            animation.shoot = Clip(path, "Shoot_OneHanded");
            animation.receiveHit = Clip(path, "RecieveHit");
            animation.defeat = Clip(path, "Defeat");
            animation.victory = Clip(path, "Victory");
            animation.sitDown = Clip(path, "SitDown");
        }
        var health = root.AddComponent<Health>();
        health.max = armed ? 3f : 2f;
        health.deactivateOnDeath = false;
        var agent = root.AddComponent<NavMeshAgent>();
        agent.height = 1.8f;
        agent.radius = .32f;
        agent.speed = armed ? 3f : 4f;
        agent.stoppingDistance = .3f;
        var body = Zone(root.transform, health, "Body", HitZoneType.Body, new Vector3(0f, .99f, 0f), new Vector3(.55f, 1.15f, .38f));
        Zone(root.transform, health, "Head", HitZoneType.Head, new Vector3(0f, 1.65f, 0f), new Vector3(.36f, .36f, .36f));
        Zone(root.transform, health, "Feet", HitZoneType.Feet, new Vector3(0f, .22f, 0f), new Vector3(.45f, .42f, .38f));
        Npc npc;
        if (armed)
        {
            var enemy = root.AddComponent<ArmedEnemy>();
            enemy.minimumAttackDistance = 3f;
            enemy.maximumAttackDistance = 16f;
            enemy.preferredAttackDistance = 8f;
            enemy.sightAngle = 150f;
            npc = enemy;
            Transform gunMount = bandit != null ? null : model.GetComponentsInChildren<Transform>().FirstOrDefault(t =>
                t.name.EndsWith("Hand.R") || t.name.IndexOf("RightHand", StringComparison.OrdinalIgnoreCase) >= 0 || t.name == "hand_r");
            if (gunMount != null)
            {
                var gun = Model(gunMount, "Props/Revolver_1", Vector3.zero, .2f, 90f);
                // Hand skeleton uses the FBX scale; compensate to keep a pistol rather than a rifle.
                gun.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            var worker = root.AddComponent<Bystander>();
            worker.exitPoints = new[] { Empty(parent.parent, "WorkerExit", new Vector3(0f, .14f, -8f)) };
            npc = worker;
        }
        npc.Faction = faction;
        npc.bodyCollider = body;
        var modifier = root.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;
        return root;
    }

    static Collider Zone(Transform parent, Health health, string name, HitZoneType type, Vector3 position, Vector3 size)
    {
        var zone = Empty(parent, name, position).gameObject;
        var collider = zone.AddComponent<BoxCollider>();
        collider.size = size;
        zone.AddComponent<HitZone>().Configure(type, HitZone.DefaultMultiplier(type), health);
        return collider;
    }

    public static void ClearBuildings(CityResult city, Vector3 centre)
    {
        if (city == null || city.Root == null) return; // hand-built levels have no generated city
        var groups = new[] { city.Root.transform.Find("Towers"), city.Root.transform.Find("MidRise"), city.Root.transform.Find("Houses"), city.Root.transform.Find("Props") };
        foreach (var group in groups)
        {
            if (group == null) continue;
            foreach (Transform child in group.Cast<Transform>().ToArray())
            {
                var renderers = child.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                Vector3 closest = bounds.ClosestPoint(new Vector3(centre.x, bounds.center.y, centre.z));
                if (Vector2.Distance(new Vector2(closest.x, closest.z), new Vector2(centre.x, centre.z)) < 18f)
                    Object.DestroyImmediate(child.gameObject);
            }
        }
        if (city.Hideout != null && (city.Hideout.transform.position - centre).sqrMagnitude < 35f * 35f)
            Object.DestroyImmediate(city.Hideout);
    }

    public static void Compound(Hideout hideout, CityResult city, bool boss, int index)
    {
        Transform root = hideout.transform;
        ClearBuildings(city, root.TransformPoint(new Vector3(0f, 0f, 7f)));
        var shell = Empty(root, "CallCenterCompound", Vector3.zero);
        var wood = Paint("DarkWeatheredWood", Wood);
        var wall = Texture("tex_callcenter_wall", new Color(.58f, .44f, .29f));
        var iron = Paint("ForgedIron", Iron);
        var plaster = Paint("SandstonePlaster", new Color(.6f, .48f, .32f));
        var roof = Paint("WeatheredRoof", new Color(.24f, .21f, .16f));
        var monitor = Paint("MonitorGlow", new Color(.12f, .42f, .37f), true);
        const float width = 19f, depth = 17f, h = 4.6f;
        Box(shell, "Floor", new Vector3(0f, .04f, 7f), new Vector3(width, .16f, depth), plaster);
        Box(shell, "BackWall", new Vector3(0f, h / 2f, 15.35f), new Vector3(width, h, .35f), plaster);
        Box(shell, "WallLeft", new Vector3(-9.35f, h / 2f, 7f), new Vector3(.35f, h, depth), plaster);
        Box(shell, "WallRight", new Vector3(9.35f, h / 2f, 7f), new Vector3(.35f, h, depth), plaster);
        // Cube faces mirror their texture from inside the room, so the poster wall art goes on inward-facing quads.
        const float art = 3.4f;
        foreach (float x in new[] { -4.6f, 4.6f })
            FrontArt(shell, "WallArt", new Vector3(x, 2.3f, 15.16f), new Vector2(art, art), wall);
        foreach (float z in new[] { 3.6f, 10.4f })
        {
            FrontArt(shell, "WallArt", new Vector3(-9.16f, 2.3f, z), new Vector2(art, art), wall, -90f);
            FrontArt(shell, "WallArt", new Vector3(9.16f, 2.3f, z), new Vector2(art, art), wall, 90f);
        }
        Box(shell, "FrontWallLeft", new Vector3(-5.85f, h / 2f, -1.35f), new Vector3(7.3f, h, .35f), plaster);
        Box(shell, "FrontWallRight", new Vector3(5.85f, h / 2f, -1.35f), new Vector3(7.3f, h, .35f), plaster);
        Box(shell, "DoorLintel", new Vector3(0f, 4.05f, -1.35f), new Vector3(4.4f, 1.1f, .4f), wood);
        Box(shell, "Roof", new Vector3(0f, 4.8f, 7f), new Vector3(20f, .4f, 18f), roof);
        // A shaded wooden porch and a physically open double doorway make the entrance readable.
        Box(shell, "PorchRoof", new Vector3(0f, 3.9f, -3.1f), new Vector3(10f, .22f, 3.5f), wood);
        for (int side = -1; side <= 1; side += 2)
        {
            Box(shell, "PorchPost", new Vector3(side * 4.7f, 1.9f, -4.4f), new Vector3(.25f, 3.8f, .25f), wood);
            var door = Box(shell, "OpenDoor", new Vector3(side * 2.15f, 1.65f, -.25f), new Vector3(.18f, 3.3f, 2.1f), wood);
            Box(door.transform, "IronBrace", new Vector3(0f, .22f, 0f), new Vector3(1.1f, .07f, .9f), iron, false);
        }
        Box(shell, "CompanySign", new Vector3(0f, 4.7f, -1.65f), new Vector3(7f, .9f, .15f), iron, false);
        var letters = Empty(shell, "CompanyName", new Vector3(0f, 4.7f, -1.74f)).gameObject.AddComponent<TextMesh>();
        letters.text = boss ? "CLOUD BARON  /  PRIVATE OFFICE" : "FRONTIER  /  COMMUNICATIONS";
        letters.anchor = TextAnchor.MiddleCenter;
        letters.alignment = TextAlignment.Center;
        letters.fontSize = 48;
        letters.characterSize = .14f;
        letters.color = new Color(.94f, .76f, .43f);
        letters.gameObject.AddComponent<WorldLabel>().width = 0f; // depth-tested material only
        letters.transform.localRotation = Quaternion.identity;
        // Rows leave a broad centre aisle and gaps around each cover desk.
        for (int row = 0; row < 3; row++)
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 p = new Vector3(side * 5f, 0f, 3f + row * 3.4f);
                var desk = Empty(shell, "Workstation", p);
                Box(desk, "Desktop", new Vector3(0f, .95f, 0f), new Vector3(3.5f, .13f, 1.3f), wood);
                for (int leg = -1; leg <= 1; leg += 2)
                    Box(desk, "DeskLeg", new Vector3(leg * 1.45f, .46f, 0f), new Vector3(.15f, .92f, 1.05f), iron);
                Box(desk, "Monitor", new Vector3(0f, 1.48f, .3f), new Vector3(1.04f, .72f, .12f), iron, false);
                Box(desk, "Screen", new Vector3(0f, 1.48f, .23f), new Vector3(.9f, .57f, .025f), monitor, false);
                Box(desk, "MonitorStand", new Vector3(0f, 1.12f, .3f), new Vector3(.15f, .3f, .15f), iron, false);
                Box(desk, "Keyboard", new Vector3(0f, 1.05f, -.25f), new Vector3(.75f, .04f, .25f), iron, false);
                Model(desk, "Props/Phone", new Vector3(.95f, 1.03f, -.1f), .22f);
                Model(desk, "Interior/Chair_1", new Vector3(0f, .13f, -1.15f), .95f, 180f);
            }
        for (int i = 0; i < 2; i++)
            Box(shell, "ServerRack", new Vector3(6.7f - i * 1.5f, 1.3f, 14.5f), new Vector3(1.25f, 2.6f, 1f), Texture("tex_server_rack", Iron));
        Model(shell, "Interior/Bookshelf", new Vector3(-7.8f, .13f, 14.3f), 2.5f);
        var evidenceTable = Empty(shell, "EvidenceDesk", new Vector3(0f, 0f, 13.5f));
        Box(evidenceTable, "Desk", new Vector3(0f, .65f, 0f), new Vector3(3.2f, 1.3f, 1.3f), Texture("tex_evidence_seal_table", Wood));
        var evidence = Model(evidenceTable, "Props/Battery_Big", new Vector3(0f, 1.33f, 0f), .4f);
        // Interactor raycasts need a real collider; the visible model is otherwise deliberately collider-free.
        var evidenceTarget = Empty(evidenceTable, "CollectibleEvidence", new Vector3(0f, 1.53f, 0f));
        evidence.transform.SetParent(evidenceTarget, true);
        evidenceTarget.gameObject.AddComponent<BoxCollider>().size = new Vector3(.65f, .45f, .65f);
        FrontArt(evidenceTable, "EvidenceLabel", new Vector3(0f, 1.2f, -.66f), new Vector2(1.1f, .55f), Texture("tex_evidence_drive", Color.white));
        var raid = root.gameObject.AddComponent<RaidEncounter>();
        raid.evidencePoint = evidenceTarget;
        Transform enemies = Empty(root, "RaidTemplates", Vector3.zero);
        raid.enemyRoot = enemies;
        int count = boss ? 7 : index == 1 ? 5 : 4;
        var points = new[] { new Vector3(-2.4f, .15f, 2f), new Vector3(2.4f, .15f, 5f), new Vector3(-2.4f, .15f, 8f), new Vector3(2.4f, .15f, 11f), new Vector3(-7.8f, .15f, 11f), new Vector3(7.8f, .15f, 7f), new Vector3(0f, .15f, 12f) };
        for (int i = 0; i < count; i++) Character(enemies, i % 2 == 0 ? "Suit_Male" : "Casual3_Male", points[i], true, Faction.Scammer);
        for (int i = 0; i < (boss || index == 2 ? 3 : 2); i++)
            Character(enemies, i % 2 == 0 ? "Worker_Female" : "Worker_Male", new Vector3(-7.8f + i * 7.5f, .15f, 3f), false, Faction.ForcedWorker);
        Transform police = Empty(root, "CorruptPoliceTemplates", Vector3.zero);
        raid.corruptPoliceRoot = police;
        for (int i = 0; i < 3; i++) Character(police, "Soldier_Male", new Vector3((i - 1) * 2f, .15f, -10f), true, Faction.CorruptPolice, 0f);
        enemies.gameObject.SetActive(false);
        police.gameObject.SetActive(false);
        hideout.beaconMaterialAsset = Paint("InvestigationBeacon", new Color(1.1f, .68f, .22f), true);
        hideout.beaconHeight = 42f;
        hideout.beaconOffset = new Vector3(0f, 5f, 7f);
        // Warm pools of light contrast with teal shadows and reveal the enemies under the solid roof.
        for (int z = 3; z <= 13; z += 5)
        {
            Model(shell, "Interior/Light_Ceiling1", new Vector3(0f, 3.9f, z), .35f);
            var light = Empty(shell, "HangingLamp", new Vector3(0f, 3.65f, z)).gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, .74f, .43f);
            light.intensity = 5.5f;
            light.range = 13f;
            light.shadows = LightShadows.None;
        }
        BakeNavigation(root, hideout.hideoutId);
    }

    static void BakeNavigation(Transform root, string id)
    {
        Physics.SyncTransforms();
        var surface = root.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Volume;
        surface.center = new Vector3(0f, 2f, 4f);
        surface.size = new Vector3(43f, 8f, 45f);
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.overrideVoxelSize = true;
        surface.voxelSize = .15f;
        surface.BuildNavMesh();
        if (surface.navMeshData == null) throw new InvalidOperationException("NavMesh bake failed: " + id);
        EnsureFolder("Assets/Navigation");
        string path = "Assets/Navigation/" + id + ".asset";
        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(path) != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(surface.navMeshData, path);
    }

    public static void Lighting(Camera camera, float sunYaw = -125f, float sunPitch = 30f)
    {
        var sun = Empty(null, "GoldenHourSun", Vector3.zero).gameObject.AddComponent<Light>();
        sun.transform.rotation = Quaternion.Euler(sunPitch, sunYaw, 0f); // low enough for long shadows, high enough to light the streets
        sun.type = LightType.Directional;
        sun.color = new Color(1f, .8f, .58f);
        sun.intensity = 2f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = .8f;
        sun.shadowBias = .045f;
        RenderSettings.sun = sun;
        RenderSettings.skybox = GoldenSky();
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.45f, .5f, .54f);
        RenderSettings.ambientEquatorColor = new Color(.42f, .36f, .28f);
        RenderSettings.ambientGroundColor = new Color(.19f, .15f, .11f);
        RenderSettings.ambientIntensity = 1f;
        sun.gameObject.AddComponent<FrontierAtmosphere>();
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(.62f, .48f, .32f);
        RenderSettings.fogDensity = .0028f;
        camera.allowHDR = true;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        camera.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        EnsureFolder("Assets/Settings");
        const string path = "Assets/Settings/FrontierLook.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
        foreach (var existing in profile.components.ToArray()) Object.DestroyImmediate(existing, true);
        profile.components.Clear();
        Add<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);
        var bloom = Add<Bloom>(profile);
        bloom.intensity.Override(.2f);
        bloom.threshold.Override(1.1f);
        bloom.scatter.Override(.6f);
        var color = Add<ColorAdjustments>(profile);
        color.postExposure.Override(.4f);
        color.contrast.Override(10f);
        color.saturation.Override(-12f);
        color.colorFilter.Override(new Color(1f, .96f, .88f));
        var balance = Add<WhiteBalance>(profile);
        balance.temperature.Override(9f);
        balance.tint.Override(-3f);
        var lift = Add<LiftGammaGain>(profile);
        lift.lift.Override(new Vector4(.94f, 1.02f, 1.04f, -.018f));
        lift.gamma.Override(new Vector4(1.02f, 1f, .97f, 0f));
        lift.gain.Override(new Vector4(1.04f, 1f, .93f, .02f));
        var vignette = Add<Vignette>(profile);
        vignette.intensity.Override(.2f);
        vignette.smoothness.Override(.55f);
        var grain = Add<FilmGrain>(profile);
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(.12f);
        grain.response.Override(.8f);
        EditorUtility.SetDirty(profile);
        var volume = Empty(null, "FrontierColorGrade", Vector3.zero).gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
        Dust(new Vector3(0f, 2f, -45f));
    }

    // Our own copy of the city's dusk sky: brighter and warmer (golden hour), leaving the map's material untouched.
    static Material GoldenSky()
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/City/Mat_sky_dusk.mat");
        if (source == null) return null;
        string path = Folder + "/Sky_GoldenHour.mat";
        var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (sky == null) { sky = new Material(source); AssetDatabase.CreateAsset(sky, path); }
        else sky.CopyPropertiesFromMaterial(source);
        sky.SetColor("_Tint", new Color(.56f, .53f, .47f));
        sky.SetFloat("_Exposure", 1.3f);
        EditorUtility.SetDirty(sky);
        return sky;
    }

    static T Add<T>(VolumeProfile profile) where T : VolumeComponent
    {
        var component = profile.Add<T>(true);
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    static void Dust(Vector3 centre)
    {
        var particles = Empty(null, "WindblownDust", centre).gameObject.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.startLifetime = 14f;
        main.startSpeed = .2f;
        main.startSize = new ParticleSystem.MinMaxCurve(.015f, .055f);
        main.startColor = new Color(.85f, .69f, .42f, .22f);
        main.maxParticles = 300;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = particles.emission;
        emission.rateOverTime = 18f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(75f, 5f, 120f);
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = .35f;
        velocity.y = .03f;
        velocity.z = .12f;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = Paint("DustMotes", new Color(.85f, .69f, .42f, .22f), true);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }
}
