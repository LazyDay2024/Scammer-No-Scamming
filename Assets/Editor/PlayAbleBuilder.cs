using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using ScammerNoScamming;

// Puts the working gameplay into Assets/Scenes/PlayAble.unity, which is the HorseCity map: player, ridable horse,
// revolver, enemies on F/G, death replay, corrupt police raid, and the case flow (menu, case file, map, raid
// site choice, station raid on a wrong pick, hideout with door guards, seal, lesson).
// Safe to rerun: it removes what it added last time.
public static class PlayAbleBuilder
{
    const string ScenePath = "Assets/Scenes/PlayAble.unity";
    // A hand-made map picture dropped here is used instead of the generated plan (see CityMapView).
    const string MapPicturePath = "Assets/Art/UI/CityMap.png";
    // The hideout interior stands this far under the city, standing in for a separate scene.
    const float InteriorDepth = 300f;
    static readonly string[] Added = { "Player", "Horse", "EnemySpawner", "ActionReplay", "Corrupt police raid", "PlayAbleNavMesh",
        "PoliceStation", "EvidenceCounter", "CaseFlow", "CityMapView", "StationRaid", "MapSites", "AmmoCrates" };

    // One real hideout per case: its id is the case's HideoutId, its gang decides the floor plan and the label on the map.
    struct Site
    {
        public string id, displayName, sign;
        public ScamType scamType;
        public InteriorPlan plan;
        public Vector3 near; // the QP_SuspectHideout marker nearest this point gives the warehouse door
    }

    static readonly Site[] Sites = {
        new Site { id = "east_shophouse", displayName = "โกดังริมเมืองฝั่งตะวันตก", sign = "ร้านแบตม้าเหล็ก ลด 70%",
            scamType = ScamType.OnlineShopping, plan = InteriorPlan.FakeShop, near = new Vector3(-319f, 0f, -103f) },
        new Site { id = "north_tower", displayName = "โกดังริมเมืองฝั่งเหนือ", sign = "งานง่าย รายได้ดี จำกัด",
            scamType = ScamType.InvestmentOrJob, plan = InteriorPlan.CallCenter, near = new Vector3(-269f, 0f, 200f) },
        new Site { id = "south_house", displayName = "โกดังริมเมืองฝั่งใต้", sign = "สำนักงานตรวจสอบพัสดุ",
            scamType = ScamType.FakeOfficial, plan = InteriorPlan.FakeOfficer, near = new Vector3(81f, 0f, -393f) },
    };

    [MenuItem("Tools/Scammer No Scamming/Add Gameplay To PlayAble Scene")]
    public static void Build()
    {
        PropPrefabs.BuildAll(); // models must exist before anything references them
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
            if (System.Array.IndexOf(Added, root.name) >= 0 || root.name.StartsWith("Hideout_")
                || (root.name == "Main Camera" && root.GetComponent<Camera>() != null))
                Object.DestroyImmediate(root);

        Physics.SyncTransforms();
        // The city's own police station (south-west of the tower): the case starts in its lobby, door facing local -Z.
        var station = FindStation();
        var spawn = station.TransformPoint(new Vector3(0f, .25f, -12.5f));
        var horseSpawn = station.TransformPoint(new Vector3(4f, 0f, -22f));
        var mapView = BuildMapView();

        var player = PlayerSandbox.CreatePlayer();
        player.transform.SetPositionAndRotation(spawn, station.rotation);
        var camera = player.GetComponentInChildren<Camera>();
        camera.farClipPlane = 900f;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.allowHDR = true;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true; // the map ships a dusk volume
        player.GetComponent<PlayerWeaponController>().tracerMaterial =
            PlayerSandboxBuilder.GetMaterial("Tracer", new Color(1f, .9f, .4f), "Universal Render Pipeline/Unlit");

        var horse = PlayerSandbox.CreateHorse(CaseFlowSceneBuilder.Flat);
        horse.transform.SetPositionAndRotation(horseSpawn, station.rotation);
        horse.AddComponent<HorseGenes>();
        WorldSceneArt.Horse(horse);

        var spawner = new GameObject("EnemySpawner").AddComponent<EnemySpawner>();
        spawner.gameObject.AddComponent<PlayerHurtKeys>();
        spawner.enemyMaterial = CaseFlowSceneBuilder.Flat(new Color(.75f, .2f, .2f));
        new GameObject("ActionReplay").AddComponent<ActionReplay>();

        var raid = new GameObject("Corrupt police raid");
        var police = raid.AddComponent<AmbushGroup>();
        police.kind = AmbushKind.Riders;
        police.count = 4;
        police.minRadius = 35f;
        police.maxRadius = 45f;
        police.material = CaseFlowSceneBuilder.Flat(new Color(.15f, .2f, .45f));
        raid.AddComponent<CorruptPoliceRaid>();

        // Enemies chase over a NavMesh baked when play starts, after the whole map exists.
        var nav = new GameObject("PlayAbleNavMesh");
        nav.AddComponent<NavMeshSurface>().collectObjects = CollectObjects.All;
        nav.AddComponent<RuntimeNavMesh>();

        BuildCase(station, scene);
        Physics.SyncTransforms();
        AmmoCrates.Build(station, Sites.Select(site => HideoutMarker(site.near).position).ToList());

        EditorSceneManager.SaveScene(scene, ScenePath);
        CaseFlowSceneBuilder.RegisterFirst(ScenePath);
        Debug.Log("Gameplay added to " + ScenePath + ", player at " + spawn + ", map centre " + mapView.center + " half " + mapView.halfSize);
    }

    static Transform FindStation() =>
        Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t => t.name == "PoliceStation" && t.parent != null);

    // The city's edge warehouses are the suspect hideouts. A QP_SuspectHideout marker stands 2 m in front of each
    // warehouse door, in the building's frame (door side is local -Z). Take the marker nearest a wanted spot.
    static Transform HideoutMarker(Vector3 near) =>
        Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name == "QP_SuspectHideout")
            .OrderBy(t => (t.position - near).sqrMagnitude).First();

    static void BuildCase(Transform station, Scene main)
    {
        var counter = CaseFlowSceneBuilder.Prop("EvidenceCounter", station.TransformPoint(new Vector3(-3.6f, .8f, -12.3f)),
            new Vector3(3.2f, 1.2f, 1.2f), station.eulerAngles.y, CaseFlowSceneBuilder.Textured("tex_evidence_seal_table", new Color(.4f, .3f, .2f)));

        // Scammers who storm the station after a wrong pick. Each template is a spawn point outside the front door.
        var stationRaid = new GameObject("StationRaid");
        stationRaid.transform.SetPositionAndRotation(station.position, station.rotation);
        var stationEncounter = stationRaid.AddComponent<RaidEncounter>();
        stationEncounter.enemyRoot = Templates(stationRaid.transform, "RaidSpawnPoints", new[] {
            new Vector3(-8f, .1f, -26f), new Vector3(-3f, .1f, -29f), new Vector3(3f, .1f, -29f), new Vector3(8f, .1f, -26f) }, 0f);

        foreach (var site in Sites) BuildHideout(site, main);

        var flow = new GameObject("CaseFlow").AddComponent<CaseFlowDemo>();
        flow.sealPoint = counter.transform;
        flow.regularCases = Sites.Length; // one case per hideout
        flow.includeBoss = false;
        var briefing = flow.gameObject.AddComponent<CaseBriefing>();
        briefing.flow = flow;
        briefing.stationAnchor = station;
        briefing.deskLocalPosition = new Vector3(4.6f, .2f, -11.5f);
        briefing.stationRaid = stationEncounter;
        briefing.caseFileModel = PropPrefabs.Prefab("CaseFolder");
        briefing.foldedMapModel = PropPrefabs.Prefab("OldMap");
        flow.briefing = briefing;
    }

    // A warehouse door on the street with guards and a map site. The interior behind it is its own scene.
    static void BuildHideout(Site site, Scene main)
    {
        var marker = HideoutMarker(site.near);
        string scenePath = BuildInteriorScene(site, main);

        var door = BuildDoor(marker, site);
        var exit = WorldSceneArt.Empty(door, "ExteriorExit", new Vector3(0f, .3f, -3f), 180f);
        var inPortal = WorldSceneArt.Empty(door, "DoorPortal_" + site.id, new Vector3(0f, 0f, 1.2f)).gameObject.AddComponent<DoorPortal>();
        inPortal.radius = 1.8f; // destination and requiresReveal are bound when the interior scene loads

        var guards = new GameObject("DoorGuards");
        guards.transform.SetParent(door, false);
        var guardRaid = guards.AddComponent<RaidEncounter>();
        guardRaid.enemyRoot = Templates(guards.transform, "GuardTemplates", new[] {
            new Vector3(-3f, .1f, -3f), new Vector3(3f, .1f, -3f), new Vector3(0f, .1f, -6.5f) }, 180f);

        var entrance = door.gameObject.AddComponent<HideoutEntrance>();
        entrance.hideoutId = site.id;
        entrance.sceneName = Path.GetFileNameWithoutExtension(scenePath);
        entrance.guards = guardRaid;
        entrance.enterPortal = inPortal;
        entrance.exitPoint = exit;

        var mapSite = door.gameObject.AddComponent<MapSite>();
        mapSite.siteId = site.id;
        mapSite.displayName = site.displayName;
        mapSite.scamType = site.scamType;
    }

    // Builds the interior in a new scene next to the city, saves it under Assets/Scenes/HideOut, and registers it
    // in Build Settings so it can be loaded by name.
    static string BuildInteriorScene(Site site, Scene main)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        var root = new GameObject("Hideout_" + site.id);
        root.transform.position = new Vector3(0f, -InteriorDepth, 0f); // under the city, clear of anything else
        var hideout = root.AddComponent<Hideout>();
        hideout.hideoutId = site.id;
        hideout.zoneHint = site.displayName;
        HideoutInteriors.Build(root, site.plan, "PlayAble_" + site.id);
        hideout.beaconMaterialAsset = WorldSceneArt.Paint("InvestigationBeacon", new Color(1.1f, .68f, .22f), true);
        hideout.beaconHeight = 120f;

        float doorX = HideoutInteriors.DoorX(site.plan);
        var entry = WorldSceneArt.Empty(root.transform, "InteriorEntry", new Vector3(doorX, .3f, 2.6f));
        var exitPortal = WorldSceneArt.Empty(root.transform, "ExitPortal", new Vector3(doorX, 0f, .8f)).gameObject.AddComponent<DoorPortal>();
        exitPortal.radius = 1f;
        var interior = root.AddComponent<HideoutInterior>();
        interior.hideoutId = site.id;
        interior.hideout = hideout;
        interior.entry = entry;
        interior.exitPortal = exitPortal;

        WorldSceneArt.EnsureFolder("Assets/Scenes/HideOut");
        string path = "Assets/Scenes/HideOut/Hideout_" + site.id + ".unity";
        EditorSceneManager.SaveScene(scene, path);
        SceneManager.SetActiveScene(main);
        EditorSceneManager.CloseScene(scene, true);

        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        return path;
    }

    // Armed scammer clones are made from these inactive templates when a raid begins.
    static Transform Templates(Transform parent, string name, Vector3[] points, float yaw)
    {
        var root = WorldSceneArt.Empty(parent, name, Vector3.zero);
        for (int i = 0; i < points.Length; i++)
            WorldSceneArt.Character(root, i % 2 == 0 ? "Suit_Male" : "Casual3_Male", points[i], true, Faction.Scammer, yaw);
        root.gameObject.SetActive(false);
        return root;
    }

    // A door and sign on the warehouse wall in front of the marker (the door side is the marker's local -Z).
    static Transform BuildDoor(Transform marker, Site site)
    {
        var root = new GameObject("Hideout_door_" + site.id).transform;
        root.SetPositionAndRotation(marker.position, Quaternion.Euler(0f, marker.eulerAngles.y, 0f));
        var wood = WorldSceneArt.Paint("DarkWeatheredWood", new Color(.2f, .13f, .08f));
        WorldSceneArt.Box(root, "Door", new Vector3(0f, 1.6f, 1.85f), new Vector3(2.8f, 3.2f, .25f), wood, false);
        WorldSceneArt.Box(root, "Lintel", new Vector3(0f, 3.4f, 1.8f), new Vector3(3.4f, .4f, .35f), wood, false);
        var sign = WorldSceneArt.Empty(root, "Sign", new Vector3(0f, 4.3f, 1.6f)).gameObject.AddComponent<TextMesh>();
        sign.text = site.sign;
        sign.anchor = TextAnchor.MiddleCenter;
        sign.fontSize = 64;
        sign.characterSize = .1f;
        sign.color = new Color(.95f, .85f, .6f);
        sign.gameObject.AddComponent<WorldLabel>().width = 0f;
        return root;
    }

    // The M map covers the whole round city, centred on the tower (north up). A hand-made picture drawn to the
    // same centre and half-size can replace the generated plan.
    static CityMapView BuildMapView()
    {
        var view = new GameObject("CityMapView").AddComponent<CityMapView>();
        view.center = Vector2.zero;
        view.halfSize = 648f; // the art map's round edge sits at 621 of its 639 px half-width, and the city reaches about 630 m
        if (File.Exists(MapPicturePath)) view.overridePicture = AssetDatabase.LoadAssetAtPath<Texture2D>(MapPicturePath);
        return view;
    }
}
