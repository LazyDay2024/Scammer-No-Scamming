using System;
using UnityEditor;
using UnityEngine;
using ScammerNoScamming;

// FrontierLevelBuilder's coordinator calls Build(level) after terrain exists.
public static class RiderAmbush
{
    const string BanditPath = "Assets/Resources/Scammer/ScammerModel.prefab";
    const string HorsePath = "Assets/Resources/Scammer/HorseModel.prefab";
    const string RockPath = "Assets/ThirdParty/PolyHaven/namaqualand_boulder_02/namaqualand_boulder_02_2k.fbx";

    public static void Build(Transform level)
    {
        if (level == null) throw new ArgumentNullException(nameof(level));
        if (level.Find("Canyon rider ambush") != null)
            throw new InvalidOperationException("RiderAmbush already exists; rebuild the level to avoid duplicate riders.");
        var banditPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BanditPath);
        var horsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HorsePath);
        if (banditPrefab == null || horsePrefab == null)
            throw new InvalidOperationException("RiderAmbush needs ScammerModel.prefab and HorseModel.prefab in Assets/Resources/Scammer.");
        if (banditPrefab.GetComponentInChildren<Animator>(true) == null || horsePrefab.GetComponentInChildren<Animator>(true) == null)
            throw new InvalidOperationException("RiderAmbush model prefabs must contain their configured bandit and horse Animators.");

        var root = Child(level, "Canyon rider ambush");
        var templates = Child(root, "Mounted rider templates (immutable)");
        templates.gameObject.SetActive(false);
        Vector3 direction = Flat(FrontierLevelBuilder.Road[3] - FrontierLevelBuilder.Road[2]).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        float yaw = Quaternion.LookRotation(direction).eulerAngles.y;

        for (int i = 0; i < 3; i++)
        {
            float side = i == 1 ? -1f : 1f;
            // The stationary player at the trigger starts 20-30 m away from all three riders.
            Vector3 hidden = Flat(FrontierLevelBuilder.Road[3]) - direction * (18f + i * 5f) + right * (9f * side);
            // Cover is upstream; the first route segment exits forward around it, never through it.
            var cover = FrontierLevelBuilder.Model(root, RockPath, hidden - direction * 3.5f, yaw + 35f * i, 2.2f);
            if (cover == null) Debug.LogWarning("RiderAmbush cover rock is missing; inspect canyon concealment before shipping.");
            else cover.name = "Rider cover " + (i + 1);

            var route = Child(templates, "Route " + (i + 1));
            var points = new Transform[5];
            points[0] = Waypoint(route, "Egress", hidden + direction * 3f - right * (3f * side));
            points[1] = Waypoint(route, "Join road flank", Flat(FrontierLevelBuilder.Road[3]) + direction * 5f + right * (6f * side));
            for (int road = 4; road <= 6; road++)
            {
                Vector3 tangent = Flat(FrontierLevelBuilder.Road[road] - FrontierLevelBuilder.Road[road - 1]).normalized;
                Vector3 flank = Vector3.Cross(Vector3.up, tangent) * (6f * side);
                points[road - 2] = Waypoint(route, "Road " + road, Flat(FrontierLevelBuilder.Road[road]) + flank);
            }
            CreateRider(templates, i + 1, hidden, yaw, side, points, banditPrefab, horsePrefab);
        }

        // Separate from the templates so Trap.ResetForAction never edits their captured state.
        var triggerObject = Child(root, "Road 3 mounted ambush trigger");
        triggerObject.gameObject.SetActive(false);
        FrontierLevelBuilder.Place(triggerObject, Flat(FrontierLevelBuilder.Road[3]), yaw);
        var box = triggerObject.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = Vector3.up * 3f;
        box.size = new Vector3(22f, 12f, 10f);
        var trigger = triggerObject.gameObject.AddComponent<RiderAmbushTrigger>();
        trigger.damage = 0f;
        trigger.templateRoot = templates;
        triggerObject.gameObject.SetActive(true);
    }

    static void CreateRider(Transform templates, int number, Vector3 hidden, float yaw, float side,
        Transform[] route, GameObject banditPrefab, GameObject horsePrefab)
    {
        var actor = Child(templates, "Mounted scammer " + number);
        // Build inactive: runtime Awake must see all health, model, collider and animation references.
        actor.gameObject.SetActive(false);
        FrontierLevelBuilder.Place(actor, hidden, yaw);
        var riderHealth = actor.gameObject.AddComponent<Health>();
        riderHealth.max = 3f;
        riderHealth.deactivateOnDeath = false;
        var horse = Child(actor, "Horse");
        var horseModel = InstantiateModel(horsePrefab, horse);
        var horseHealth = horse.gameObject.AddComponent<Health>();
        horseHealth.max = 6f;
        horseHealth.deactivateOnDeath = false;
        Zone(horse, horseHealth, "Horse body", new Vector3(0f, 1f, 0f), new Vector3(.85f, 1.3f, 2.3f), HitZoneType.Body);
        Zone(horse, horseHealth, "Horse head", new Vector3(0f, 1.65f, 1.05f), new Vector3(.5f, .8f, .7f), HitZoneType.Body);

        var rider = Child(actor, "Rider");
        rider.localPosition = new Vector3(0f, .72f, .08f);
        var banditModel = InstantiateModel(banditPrefab, rider);
        var body = Zone(rider, riderHealth, "Body", new Vector3(0f, 1.22f, 0f), new Vector3(.6f, .65f, .45f), HitZoneType.Body);
        Zone(rider, riderHealth, "Head", new Vector3(0f, 1.68f, 0f), new Vector3(.32f, .32f, .32f), HitZoneType.Head);
        var animation = actor.gameObject.AddComponent<BanditAnimator>();
        animation.animator = banditModel.GetComponentInChildren<Animator>(true);
        animation.Mounted = true;
        var npc = actor.gameObject.AddComponent<MountedScammer>();
        npc.Faction = Faction.Scammer;
        npc.bodyCollider = body;
        npc.horseRoot = horse;
        npc.riderRoot = rider;
        npc.horseHealth = horseHealth;
        npc.horseAnimator = horseModel.GetComponentInChildren<Animator>(true);
        npc.flankSide = side;
        npc.waypoints = route;
        npc.eyeHeight = 2.3f;
        npc.muzzleHeight = 2.1f;
        npc.damage = 8f;
        npc.sightAngle = 360f;
        npc.sightRange = 90f;
        foreach (var child in banditModel.GetComponentsInChildren<Transform>(true))
            if (child.name == "Muzzle") { npc.muzzle = child; break; }
        actor.gameObject.SetActive(true); // Its immutable template parent remains inactive.
    }

    static GameObject InstantiateModel(GameObject prefab, Transform parent)
    {
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        model.transform.SetParent(parent, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
        return model;
    }

    static BoxCollider Zone(Transform parent, Health owner, string name, Vector3 centre, Vector3 size, HitZoneType type)
    {
        var zone = Child(parent, name);
        zone.localPosition = centre;
        var box = zone.gameObject.AddComponent<BoxCollider>();
        box.size = size;
        zone.gameObject.AddComponent<HitZone>().Configure(type, HitZone.DefaultMultiplier(type), owner);
        return box;
    }

    static Transform Waypoint(Transform route, string name, Vector3 position)
    {
        var point = Child(route, name);
        FrontierLevelBuilder.Place(point, Flat(position), 0f);
        return point;
    }

    static Transform Child(Transform parent, string name)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    static Vector3 Flat(Vector3 point) => new Vector3(point.x, 0f, point.z);
}
