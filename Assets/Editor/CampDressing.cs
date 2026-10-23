using System;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using ScammerNoScamming;
using Object = UnityEngine.Object;

// Hideout camp encounter dressing (owner: Codex). Called by FrontierLevelBuilder after the camp blockout.
public static class CampDressing
{
    const string Textures = "Assets/ThirdParty/OldWest/Textures/";

    public static void Build(Transform level, ScammerNoScamming.Hideout hideout)
    {
        if (level == null || hideout == null) throw new ArgumentNullException("Camp dressing needs a level and hideout.");
        var raid = hideout.GetComponent<RaidEncounter>();
        if (raid == null || raid.enemyRoot == null) throw new InvalidOperationException("Build the compound raid before CampDressing.");
        if (level.Find("CampDressing") != null) throw new InvalidOperationException("CampDressing already exists; rebuild the level to avoid duplicate guards.");

        // All plan coordinates are in the hall's frame: front is -Z, hall is x +/-9.5, z -1.5..15.5.
        // Ground placement must pass y=0 to Place: it ADDS terrain height, rather than accepting world y.
        var root = WorldSceneArt.Empty(level, "CampDressing", Vector3.zero);
        root.SetPositionAndRotation(hideout.transform.position, hideout.transform.rotation);
        var wood = Surface("CampDressing_Wood", "Planks023A_512x512_Color.png", "Planks023A_512x512_NormalGL.png", new Vector2(2f, 2f));
        var roof = Surface("CampDressing_Roof", "Planks023A_512x512_Color_Black.png", "Planks023A_512x512_NormalGL.png", new Vector2(3f, 2f));
        var metal = Surface("CampDressing_Metal", "Metal041C_512x512_Color.png", "Metal041C_512x512_NormalGL.png", new Vector2(2f, 1f));
        var canvas = Surface("CampDressing_Canvas", "Fabric019_512x512_Color.png", "Fabric019_512x512_NormalGL.png", new Vector2(2f, 2f));
        canvas.SetColor("_BaseColor", new Color(.64f, .54f, .36f));
        canvas.SetFloat("_Cull", (float)CullMode.Off); // Thin canvas is visible from both inside and outside.
        EditorUtility.SetDirty(canvas);
        var dust = FrontierLevelBuilder.Tex("CampDressing_Dirt", "Assets/Art/Terrain/dry_ground_rocks_Diffuse.jpg", new Vector2(2f, 2f));
        var particles = ParticleMaterial();

        GroundExistingCamp(level);
        var office = Office(root, wood, roof, metal);
        var hut = GeneratorHut(root, wood, metal);
        Tent(root, wood, canvas);
        Perimeter(root, wood, metal);
        Cover(root, wood, metal, canvas);
        Campfire(root, dust, wood, particles);
        WorkLight(office, new Vector3(0f, 2.8f, -3.8f), metal);
        WorkLight(hut, new Vector3(0f, 2.8f, -2.9f), metal);
        var gateLamp = Ground(root, "Road gate lamp", new Vector3(16f, 0f, -17f));
        WorldSceneArt.Box(gateLamp, "Pole", Vector3.up * 2.1f, new Vector3(.16f, 4.2f, .16f), wood);
        WorkLight(gateLamp, Vector3.up * 4.1f, metal);

        ClearRoad(level, root);
        Populate(level, root, office, hut, raid);
        ArmTanks(level, raid, particles);
        BakeYard(root, hideout);
    }

    static Material Surface(string name, string albedo, string normal, Vector2 tiling)
    {
        var material = FrontierLevelBuilder.Tex(name, Textures + albedo, tiling);
        string path = Textures + normal;
        if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        material.SetTextureScale("_BumpMap", tiling);
        material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_BumpScale", .65f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Transform Ground(Transform frame, string name, Vector3 point, float yaw = 0f)
    {
        var item = WorldSceneArt.Empty(frame, name, Vector3.zero);
        Vector3 world = frame.TransformPoint(new Vector3(point.x, 0f, point.z));
        FrontierLevelBuilder.Place(item, new Vector3(world.x, point.y, world.z), frame.eulerAngles.y + yaw);
        return item;
    }

    static GameObject Prop(Transform frame, string path, Vector3 point, float yaw = 0f, float scale = 1f, bool collide = true)
    {
        Vector3 world = frame.TransformPoint(new Vector3(point.x, 0f, point.z));
        return FrontierLevelBuilder.Model(frame, path, new Vector3(world.x, point.y, world.z), frame.eulerAngles.y + yaw, scale, collide);
    }

    static Transform Office(Transform root, Material wood, Material roof, Material metal)
    {
        // Door faces +X into a 3m passage along the hall. No explosive props in the rescue room.
        var office = Ground(root, "Mine payroll office - captive", new Vector3(-16f, 0f, 4f), -90f);
        WorldSceneArt.Box(office, "Raised plank floor", new Vector3(0f, .08f, 0f), new Vector3(6f, .2f, 7f), wood);
        foreach (float x in new[] { -2.8f, 2.8f })
        {
            WorldSceneArt.Box(office, "Corner pier", new Vector3(x, -.35f, -3.2f), new Vector3(.3f, .9f, .3f), wood);
            WorldSceneArt.Box(office, "Corner pier", new Vector3(x, -.35f, 3.2f), new Vector3(.3f, .9f, .3f), wood);
        }
        WorldSceneArt.Box(office, "Back wall", new Vector3(0f, 1.65f, 3.4f), new Vector3(6f, 3f, .18f), wood);
        WorldSceneArt.Box(office, "Window wall left", new Vector3(-2.9f, 1.65f, 0f), new Vector3(.18f, 3f, 7f), wood);
        WorldSceneArt.Box(office, "Window sill", new Vector3(2.9f, .7f, 0f), new Vector3(.18f, 1.1f, 7f), wood);
        WorldSceneArt.Box(office, "Window header", new Vector3(2.9f, 2.85f, 0f), new Vector3(.18f, .6f, 7f), wood);
        foreach (float z in new[] { -2.8f, 0f, 2.8f })
            WorldSceneArt.Box(office, "Window upright", new Vector3(2.9f, 1.9f, z), new Vector3(.2f, 1.3f, .18f), wood);
        foreach (float x in new[] { -2f, 2f })
            WorldSceneArt.Box(office, "Door side", new Vector3(x, 1.65f, -3.4f), new Vector3(2f, 3f, .18f), wood);
        WorldSceneArt.Box(office, "Door header", new Vector3(0f, 2.95f, -3.4f), new Vector3(2f, .4f, .18f), wood);
        var roofPanel = WorldSceneArt.Box(office, "Sloping plank roof", new Vector3(0f, 3.35f, 0f), new Vector3(6.6f, .2f, 7.6f), roof);
        roofPanel.transform.localRotation = Quaternion.Euler(0f, 0f, -5f);
        WorldSceneArt.Box(office, "Porch step", new Vector3(0f, .04f, -3.8f), new Vector3(2.2f, .12f, .7f), wood);
        WorldSceneArt.Box(office, "Payroll desk", new Vector3(-1.6f, .65f, 2f), new Vector3(1.8f, 1f, .8f), wood);
        WorldSceneArt.Box(office, "Desk safe", new Vector3(-1.6f, 1.4f, 2f), new Vector3(.65f, .55f, .6f), metal);
        Label(office, "PAY OFFICE", new Vector3(0f, 2.6f, -3.51f), .12f);
        return office;
    }

    static Transform GeneratorHut(Transform root, Material wood, Material metal)
    {
        var hut = Ground(root, "Generator and antenna hut", new Vector3(16f, 0f, 10f));
        WorldSceneArt.Box(hut, "Foundation", new Vector3(0f, .08f, 0f), new Vector3(5f, .2f, 5f), wood);
        WorldSceneArt.Box(hut, "Rear cladding", new Vector3(0f, 1.6f, 2.4f), new Vector3(5f, 3f, .2f), metal);
        foreach (float x in new[] { -2.4f, 2.4f })
            WorldSceneArt.Box(hut, "Side cladding", new Vector3(x, 1.6f, 0f), new Vector3(.2f, 3f, 5f), metal);
        foreach (float x in new[] { -1.75f, 1.75f })
            WorldSceneArt.Box(hut, "Door jamb", new Vector3(x, 1.6f, -2.4f), new Vector3(1.5f, 3f, .2f), wood);
        WorldSceneArt.Box(hut, "Door lintel", new Vector3(0f, 2.95f, -2.4f), new Vector3(2f, .3f, .2f), wood);
        WorldSceneArt.Box(hut, "Roof firing platform", new Vector3(0f, 3.25f, 0f), new Vector3(5.6f, .2f, 5.6f), metal);
        // Partial rail leaves the sentry exposed from the front and overlook; never an enclosed firing box.
        WorldSceneArt.Box(hut, "Rear roof rail", new Vector3(0f, 3.8f, 2.5f), new Vector3(5f, .7f, .16f), wood);
        WorldSceneArt.Box(hut, "Generator housing", new Vector3(0f, .8f, 1f), new Vector3(2.2f, 1.3f, 1.2f), metal);
        for (int i = 0; i < 6; i++)
            WorldSceneArt.Box(hut, "Cooling fin", new Vector3(-.8f + i * .32f, .85f, .35f), new Vector3(.08f, .7f, .1f), wood, false);
        WorldSceneArt.Box(hut, "Exhaust", new Vector3(1f, 2.4f, 1.4f), new Vector3(.16f, 3.2f, .16f), metal);
        WorldSceneArt.Box(hut, "Antenna pole", new Vector3(1.8f, 5.2f, 1.8f), new Vector3(.1f, 4f, .1f), metal, false);
        for (int i = 0; i < 3; i++)
            WorldSceneArt.Box(hut, "Antenna crossbar", new Vector3(1.8f, 6f + i * .4f, 1.8f), new Vector3(1.6f - i * .3f, .07f, .07f), metal, false);
        Label(hut, "POWER / RADIO", new Vector3(0f, 2.65f, -2.52f), .1f);
        return hut;
    }

    static void Tent(Transform root, Material wood, Material canvas)
    {
        var tent = Ground(root, "Canvas sleeping tent", new Vector3(-15f, 0f, -12f), 15f);
        // Two textured planes form a pitched canvas roof, with a clear opening at -Z.
        float halfWidth = 2.7f, rise = 2.8f;
        float slope = Mathf.Sqrt(halfWidth * halfWidth + rise * rise);
        float angle = Mathf.Atan2(rise, halfWidth) * Mathf.Rad2Deg;
        foreach (int side in new[] { -1, 1 })
        {
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Canvas roof";
            plane.transform.SetParent(tent, false);
            plane.transform.localPosition = new Vector3(side * halfWidth * .5f, rise * .5f + .15f, 0f);
            plane.transform.localRotation = Quaternion.Euler(0f, 0f, -side * angle);
            plane.transform.localScale = new Vector3(slope / 10f, 1f, .6f);
            plane.GetComponent<Renderer>().sharedMaterial = canvas;
        }
        WorldSceneArt.Box(tent, "Ridge pole", new Vector3(0f, rise + .15f, 0f), new Vector3(.12f, .12f, 6.4f), wood);
        foreach (float z in new[] { -3f, 3f })
            WorldSceneArt.Box(tent, "Tent pole", new Vector3(0f, rise * .5f, z), new Vector3(.12f, rise, .12f), wood);
        WorldSceneArt.Box(tent, "Folded bedding", new Vector3(1.1f, .18f, 1.2f), new Vector3(1f, .3f, 2f), canvas, false);
    }

    static void Perimeter(Transform root, Material wood, Material metal)
    {
        // Exactly two openings: road crosses front at x~9; FlankPath crosses +X at z~8.
        Fence(root, new Vector3(-22f, 0f, -19f), new Vector3(3f, 0f, -19f), wood, metal);
        Fence(root, new Vector3(16f, 0f, -19f), new Vector3(22f, 0f, -19f), wood, metal);
        Fence(root, new Vector3(-22f, 0f, -19f), new Vector3(-22f, 0f, 23f), wood, metal);
        Fence(root, new Vector3(-22f, 0f, 23f), new Vector3(22f, 0f, 23f), wood, metal);
        Fence(root, new Vector3(22f, 0f, -19f), new Vector3(22f, 0f, 3f), wood, metal);
        Fence(root, new Vector3(22f, 0f, 15f), new Vector3(22f, 0f, 23f), wood, metal);
        var sign = Ground(root, "Mine route sign", new Vector3(20.5f, 0f, 16f), 90f);
        WorldSceneArt.Box(sign, "Post", Vector3.up, new Vector3(.15f, 2f, .15f), wood);
        WorldSceneArt.Box(sign, "Board", new Vector3(0f, 1.8f, 0f), new Vector3(2.5f, .5f, .15f), wood, false);
        Label(sign, "MINE  >", new Vector3(0f, 1.8f, -.09f), .1f);
    }

    static void Fence(Transform root, Vector3 a, Vector3 b, Material wood, Material metal)
    {
        int count = Mathf.CeilToInt(Vector3.Distance(a, b) / 2.8f);
        Transform previous = null;
        for (int i = 0; i <= count; i++)
        {
            var post = Ground(root, "Perimeter post", Vector3.Lerp(a, b, (float)i / count));
            WorldSceneArt.Box(post, "Weathered post", Vector3.up * .9f, new Vector3(.18f, 1.8f, .18f), wood);
            if (previous != null)
                foreach (float height in new[] { .65f, 1.25f })
                {
                    Vector3 from = previous.position + Vector3.up * height, to = post.position + Vector3.up * height;
                    var rail = WorldSceneArt.Box(root, "Fence rail", Vector3.zero, new Vector3(.09f, .12f, Vector3.Distance(from, to)), metal);
                    rail.transform.SetPositionAndRotation((from + to) * .5f, Quaternion.LookRotation(to - from));
                }
            previous = post;
        }
    }

    static void Cover(Transform root, Material wood, Material metal, Material canvas)
    {
        // Staggered islands leave a broad entry lane (x -2.5..2.5) and both side passages open.
        Crates(root, new Vector3(6f, 0f, -7.5f), -12f, wood, metal);
        Crates(root, new Vector3(-6f, 0f, -17f), 18f, wood, metal);
        Crates(root, new Vector3(-15f, 0f, 12f), -20f, wood, metal);
        Crates(root, new Vector3(12f, 0f, .5f), 10f, wood, metal);
        var bags = Ground(root, "Sandbag fighting position", new Vector3(15f, 0f, -15f), -15f);
        for (int row = 0; row < 3; row++)
            for (int i = 0; i < 4; i++)
            {
                var bag = WorldSceneArt.Box(bags, "Canvas sandbag", new Vector3((i - 1.5f) * .75f + (row % 2) * .18f, .16f + row * .27f, 0f), new Vector3(.82f, .32f, .65f), canvas);
                // Rounded sacks keep the close-up silhouette softer than cube blockout.
                Object.DestroyImmediate(bag.GetComponent<BoxCollider>());
                var filter = bag.GetComponent<MeshFilter>();
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                filter.sharedMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(sphere);
                bag.AddComponent<BoxCollider>();
            }
        // Photo-scanned boulders are natural cover on the edge, outside both approach corridors.
        Prop(root, "Assets/ThirdParty/PolyHaven/namaqualand_boulder_02/namaqualand_boulder_02_2k.fbx", new Vector3(-19f, -.12f, -4f), 25f, .85f);
        Prop(root, "Assets/ThirdParty/PolyHaven/namaqualand_boulder_03/namaqualand_boulder_03_2k.fbx", new Vector3(19f, -.15f, -9f), -20f, .8f);
        foreach (var p in new[] { new Vector3(18f, 0f, -2f), new Vector3(18.7f, 0f, -1.3f) })
        {
            var barrel = Ground(root, "Water barrel", p);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Wooden barrel";
            body.transform.SetParent(barrel, false);
            body.transform.localPosition = Vector3.up * .55f;
            body.transform.localScale = new Vector3(.75f, .55f, .75f);
            body.GetComponent<Renderer>().sharedMaterial = wood;
            foreach (float y in new[] { .18f, .9f })
            {
                var hoop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hoop.name = "Iron hoop";
                hoop.transform.SetParent(barrel, false);
                hoop.transform.localPosition = Vector3.up * y;
                hoop.transform.localScale = new Vector3(.78f, .035f, .78f);
                hoop.GetComponent<Renderer>().sharedMaterial = metal;
                Object.DestroyImmediate(hoop.GetComponent<Collider>());
            }
        }
    }

    static void Crates(Transform root, Vector3 point, float yaw, Material wood, Material metal)
    {
        var pile = Ground(root, "Supply cover", point, yaw);
        foreach (var p in new[] { new Vector3(-.7f, .6f, 0f), new Vector3(.7f, .6f, .15f), new Vector3(.7f, 1.55f, .15f) })
        {
            var crate = WorldSceneArt.Empty(pile, "Plank crate", p);
            WorldSceneArt.Box(crate, "Box", Vector3.zero, new Vector3(1.25f, 1.1f, 1.15f), wood);
            foreach (float x in new[] { -.45f, .45f })
                WorldSceneArt.Box(crate, "Iron strap", new Vector3(x, 0f, -.59f), new Vector3(.07f, 1.1f, .035f), metal, false);
            var brace = WorldSceneArt.Box(crate, "Diagonal brace", new Vector3(0f, 0f, -.63f), new Vector3(1.3f, .1f, .08f), wood, false);
            brace.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
        }
    }

    static void ClearRoad(Transform level, Transform root)
    {
        var camp = level.Find("MiningCamp");
        if (camp == null) return;
        var barricades = camp.Cast<Transform>().Where(t => t.name == "Barricade").ToArray();
        var positions = new[] { new Vector3(-6f, 0f, -12f), new Vector3(-3.8f, 0f, -15.5f), new Vector3(17f, 0f, -17f), new Vector3(7.5f, 0f, -11f) };
        for (int i = 0; i < Mathf.Min(barricades.Length, positions.Length); i++)
        {
            var p = root.TransformPoint(positions[i]);
            FrontierLevelBuilder.Place(barricades[i], new Vector3(p.x, 0f, p.z), root.eulerAngles.y + (i % 2 == 0 ? 8f : -8f));
        }
    }

    static void GroundExistingCamp(Transform level)
    {
        var camp = level.Find("MiningCamp");
        if (camp == null) return;
        foreach (Transform item in camp)
        {
            bool tower = item.name == "LookoutTower";
            bool imported = item.name.IndexOf("PropaneTank", StringComparison.OrdinalIgnoreCase) >= 0 || item.name.StartsWith("oldwest_06", StringComparison.OrdinalIgnoreCase);
            if (!tower && !imported) continue;
            // BuildCamp.Local returns a world Y, but Place/Model treat Y as a terrain-relative lift.
            // Correct these existing props here without changing the shared builder.
            float baseOffset = 0f;
            var renderers = item.GetComponentsInChildren<Renderer>();
            if (imported && renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                baseOffset = bounds.min.y - item.position.y;
            }
            Quaternion rotation = item.rotation;
            Vector3 position = item.position;
            FrontierLevelBuilder.Place(item, new Vector3(position.x, -baseOffset, position.z), rotation.eulerAngles.y);
            item.rotation = rotation; // Preserve imported FBX orientation as well as yaw.
        }
    }

    static void Populate(Transform level, Transform root, Transform office, Transform hut, RaidEncounter raid)
    {
        var templates = WorldSceneArt.Empty(raid.enemyRoot, "Outdoor encounter", Vector3.zero);
        // Waypoints belong to the same cloned subtree as the guards: no references back into hidden templates.
        Vector3[][] routes = {
            new[] { new Vector3(3.5f, 0f, -9f), new Vector3(9f, 0f, -15f), new Vector3(12f, 0f, -8f) },
            new[] { new Vector3(-11f, 0f, -3f), new Vector3(-11f, 0f, 8f), new Vector3(-16f, 0f, -1f) },
            new[] { new Vector3(11.5f, 0f, 3f), new Vector3(19.5f, 0f, 3f), new Vector3(20f, 0f, 14f) },
            new[] { new Vector3(-4f, 0f, 18.5f), new Vector3(8f, 0f, 19.5f), new Vector3(12f, 0f, 16.5f) }
        };
        for (int i = 0; i < routes.Length; i++)
        {
            var route = WorldSceneArt.Empty(templates, "Yard patrol route " + (i + 1), Vector3.zero);
            var waypoints = routes[i].Select((p, n) => Ground(root, "Waypoint " + n, p)).ToArray();
            foreach (var waypoint in waypoints) waypoint.SetParent(route, true);
            var guard = WorldSceneArt.Character(templates, i % 2 == 0 ? "Soldier_Male" : "Casual3_Male", Vector3.zero, true, Faction.Scammer);
            guard.name = "Yard patrol " + (i + 1);
            guard.transform.SetPositionAndRotation(waypoints[0].position + Vector3.up * .05f, Quaternion.LookRotation(waypoints[1].position - waypoints[0].position));
            guard.GetComponent<ArmedEnemy>().waypoints = waypoints;
        }

        var tower = level.Find("MiningCamp/LookoutTower");
        if (tower == null) throw new InvalidOperationException("The camp lookout tower is missing.");
        Sentry(templates, "Gate tower sentry", tower.TransformPoint(new Vector3(0f, 7.14f, 0f)), FrontierLevelBuilder.Road[5]);
        Sentry(templates, "Radio roof sentry", hut.TransformPoint(new Vector3(-.7f, 3.36f, -.8f)), FrontierLevelBuilder.OverlookPath[3]);

        var captive = WorldSceneArt.Character(templates, "Worker_Male", Vector3.zero, false, Faction.ForcedWorker);
        captive.name = "Captive payroll worker";
        captive.transform.SetPositionAndRotation(office.TransformPoint(new Vector3(.6f, .19f, .8f)), office.rotation);
        var worker = captive.GetComponent<Bystander>();
        // Character creates a default WorkerExit under parent.parent. Replace that with an actual road escape.
        foreach (var oldExit in worker.exitPoints) if (oldExit != null) Object.DestroyImmediate(oldExit.gameObject);
        var exit = Ground(root, "Freed worker road exit", new Vector3(10f, 0f, -22f));
        exit.SetParent(templates, true);
        worker.exitPoints = new[] { exit };
    }

    static void Sentry(Transform templates, string name, Vector3 position, Vector3 lookAt)
    {
        var guard = WorldSceneArt.Character(templates, "Soldier_Male", Vector3.zero, true, Faction.Scammer);
        guard.name = name;
        Vector3 direction = lookAt - position;
        direction.y = 0f;
        guard.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        // Static firing posts have no stairs. Prevent NavMeshAgent snapping them to the ground below.
        guard.GetComponent<NavMeshAgent>().enabled = false;
        var enemy = guard.GetComponent<ArmedEnemy>();
        enemy.sightRange = 95f;
        enemy.sightAngle = 180f;
    }

    static void ArmTanks(Transform level, RaidEncounter raid, Material particles)
    {
        var camp = level.Find("MiningCamp");
        if (camp == null) return;
        var tanks = camp.Cast<Transform>().Where(t => t.name.IndexOf("PropaneTank", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        foreach (var tank in tanks)
        {
            // Keep the imported renderer and collision meshes, but reset them alongside the raid NPCs.
            tank.SetParent(raid.enemyRoot, true);
            tank.name = "Explosive propane tank";
            GameObjectUtility.SetStaticEditorFlags(tank.gameObject, (StaticEditorFlags)0);
            var health = tank.GetComponent<Health>();
            if (health == null) health = tank.gameObject.AddComponent<Health>();
            health.deactivateOnDeath = false;
            var explosive = tank.GetComponent<ExplosiveProp>();
            if (explosive == null) explosive = tank.gameObject.AddComponent<ExplosiveProp>();
            explosive.burstMaterial = particles;
            // Imported root scales/orientations differ; express a world-up blast point in its local frame.
            var renderers = tank.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                explosive.localBlastCenter = tank.InverseTransformPoint(bounds.center);
            }
        }
        if (tanks.Length != 3) Debug.LogWarning("Camp dressing expected three propane tanks; found " + tanks.Length);
    }

    static void BakeYard(Transform root, Hideout hideout)
    {
        var hall = hideout.GetComponent<NavMeshSurface>();
        if (hall == null) throw new InvalidOperationException("Compound navigation surface is missing.");
        // The original volume overlaps the yard. Restrict that surface to the hall hierarchy.
        hall.collectObjects = CollectObjects.Children;
        BakeAsset(hall, hideout.hideoutId);

        // This volume is outside the hall hierarchy, so it cuts only the yard bake, including the roof.
        var exclusion = WorldSceneArt.Empty(root, "Hall footprint excluded from yard NavMesh", Vector3.zero);
        exclusion.SetPositionAndRotation(hideout.transform.position, hideout.transform.rotation);
        var volume = exclusion.gameObject.AddComponent<NavMeshModifierVolume>();
        volume.center = new Vector3(0f, 3f, 7f);
        volume.size = new Vector3(19.5f, 12f, 17.5f);
        volume.area = NavMesh.GetAreaFromName("Not Walkable");
        var surface = root.gameObject.AddComponent<NavMeshSurface>();
        surface.agentTypeID = hall.agentTypeID;
        surface.collectObjects = CollectObjects.Volume;
        surface.center = new Vector3(0f, 4f, 2f);
        surface.size = new Vector3(60f, 32f, 62f);
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.overrideVoxelSize = true;
        surface.voxelSize = .15f;
        BakeAsset(surface, hideout.hideoutId + "_yard");
        // Explicitly join the disjoint surfaces through the 4.4m hall doorway, never through a wall.
        var doorway = WorldSceneArt.Empty(root, "Yard to hall doorway", Vector3.zero);
        doorway.SetPositionAndRotation(hideout.transform.position, hideout.transform.rotation);
        var link = doorway.gameObject.AddComponent<NavMeshLink>();
        link.agentTypeID = hall.agentTypeID;
        Vector3 front = hideout.transform.TransformPoint(new Vector3(0f, 0f, -2.8f));
        var terrain = Terrain.activeTerrains.FirstOrDefault(t => t != null && t.terrainData != null &&
            front.x >= t.transform.position.x && front.z >= t.transform.position.z &&
            front.x <= t.transform.position.x + t.terrainData.size.x && front.z <= t.transform.position.z + t.terrainData.size.z);
        if (terrain != null) front.y = terrain.SampleHeight(front) + terrain.transform.position.y;
        link.startPoint = doorway.InverseTransformPoint(front);
        link.endPoint = new Vector3(0f, .12f, .2f);
        link.width = 2.8f;
        link.bidirectional = true;
        link.UpdateLink();
    }

    static void BakeAsset(NavMeshSurface surface, string name)
    {
        Physics.SyncTransforms();
        surface.BuildNavMesh();
        if (surface.navMeshData == null) throw new InvalidOperationException("Camp NavMesh bake failed: " + name);
        WorldSceneArt.EnsureFolder("Assets/Navigation");
        string path = "Assets/Navigation/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
        if (existing != null && existing != surface.navMeshData)
        {
            // Preserve GUIDs when rebuilding a previously saved level.
            var baked = surface.navMeshData;
            surface.RemoveData();
            EditorUtility.CopySerialized(baked, existing);
            surface.navMeshData = existing;
            Object.DestroyImmediate(baked);
            surface.AddData();
            EditorUtility.SetDirty(existing);
        }
        else if (existing == null) AssetDatabase.CreateAsset(surface.navMeshData, path);
        EditorUtility.SetDirty(surface);
    }

    static void WorkLight(Transform parent, Vector3 position, Material metal)
    {
        WorldSceneArt.Box(parent, "Work lamp hood", position + Vector3.up * .15f, new Vector3(.55f, .18f, .4f), metal, false);
        var lamp = WorldSceneArt.Empty(parent, "Warm work light", position).gameObject.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.color = new Color(1f, .68f, .34f);
        lamp.intensity = 4f;
        lamp.range = 12f;
        lamp.shadows = LightShadows.None;
    }

    static void Label(Transform parent, string text, Vector3 position, float size)
    {
        // TextMesh reads from its -Z side, which is the side these signs face; depth-tested via WorldLabel.
        var label = WorldSceneArt.Empty(parent, text, position, 0f).gameObject.AddComponent<TextMesh>();
        label.gameObject.AddComponent<WorldLabel>().width = 0f;
        label.text = text;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 48;
        label.characterSize = size;
        label.color = new Color(.9f, .81f, .62f);
    }

    static void Campfire(Transform root, Material dirt, Material wood, Material particles)
    {
        var fire = Ground(root, "Campfire rest corner", new Vector3(-15.5f, 0f, -4f));
        var ash = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ash.name = "Ash ring";
        ash.transform.SetParent(fire, false);
        ash.transform.localScale = new Vector3(1.7f, .04f, 1.7f);
        ash.GetComponent<Renderer>().sharedMaterial = dirt;
        Object.DestroyImmediate(ash.GetComponent<Collider>());
        for (int i = 0; i < 3; i++)
        {
            var log = WorldSceneArt.Box(fire, "Charred log", new Vector3(0f, .13f + i * .09f, 0f), new Vector3(1.15f, .15f, .18f), wood, false);
            log.transform.localRotation = Quaternion.Euler(0f, i * 60f, 12f);
        }
        var source = WorldSceneArt.Empty(fire, "Flame and embers", Vector3.up * .25f);
        var flame = source.gameObject.AddComponent<ParticleSystem>();
        flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = flame.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.45f, .9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.5f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(.16f, .38f);
        main.startColor = new Color(1f, .4f, .08f, .8f);
        main.maxParticles = 40;
        var emission = flame.emission;
        emission.rateOverTime = 24f;
        var shape = flame.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = .25f;
        source.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        var fade = flame.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(new Color(1f, .65f, .15f), 0f), new GradientColorKey(new Color(.5f, .08f, .01f), 1f) },
            new[] { new GradientAlphaKey(.9f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        flame.GetComponent<ParticleSystemRenderer>().sharedMaterial = particles;
        var light = fire.gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, .4f, .12f);
        light.intensity = 2.5f;
        light.range = 8f;
        light.shadows = LightShadows.None;
    }

    static Material ParticleMaterial()
    {
        const string folder = "Assets/Materials/World";
        const string texturePath = folder + "/CampDressing_SoftParticle.asset";
        WorldSceneArt.EnsureFolder(folder);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "Camp soft particle", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float alpha = Mathf.Clamp01(1f - new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude);
                    pixels[y * 32 + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            texture.SetPixels(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, texturePath);
        }
        string path = folder + "/CampDressing_Particles.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) throw new InvalidOperationException("URP particle shader missing.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }
}
