using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ScammerNoScamming;

// Floor plans of the three scammer hideouts, one per kind of gang. Each is a 26 x 18 m building in the hideout
// root's frame: x from -13 to 13, z from 0 (front wall, the door) to 18 (back wall). Enemies, forced workers and
// the evidence are placed from the plan data below, so a layout is changed by editing numbers, not code.
public enum InteriorPlan { FakeShop, CallCenter, FakeOfficer }

public static class HideoutInteriors
{
    const float Width = 26f, Depth = 18f, WallHeight = 3.2f, WallThickness = .3f, FloorTop = .12f;

    struct Wall
    {
        public bool alongX;       // true: runs along X at z = at; false: runs along Z at x = at
        public float at, from, to;
        public float[] gaps;      // door openings as (centre, width) pairs along the wall
        public Wall(bool alongX, float at, float from, float to, params float[] gaps) { this.alongX = alongX; this.at = at; this.from = from; this.to = to; this.gaps = gaps; }
    }

    struct Prop
    {
        public string name; public float x, z, w, d, h;
        public Prop(string name, float x, float z, float w, float d, float h) { this.name = name; this.x = x; this.z = z; this.w = w; this.d = d; this.h = h; }
    }

    class Plan
    {
        public float doorX;
        public Color wall, floor, trim;
        public Wall[] walls;
        public Prop[] props;
        public Vector2[] enemies;   // x, z
        public Vector2[] workers;
        public Vector2 evidence;
        public Vector2[] lights;
    }

    static Plan Get(InteriorPlan plan)
    {
        switch (plan)
        {
            case InteriorPlan.CallCenter: return CallCenter();
            case InteriorPlan.FakeOfficer: return FakeOfficer();
            default: return FakeShop();
        }
    }

    // Front shop, packing hall, then a stockroom and an office with the evidence: one line, three doors.
    static Plan FakeShop() => new Plan
    {
        doorX = 0f,
        wall = new Color(.52f, .44f, .33f), floor = new Color(.3f, .26f, .21f), trim = new Color(.2f, .13f, .08f),
        walls = new[] {
            new Wall(true, 4f, -13f, 13f, 0f, 2.4f),
            new Wall(true, 12.125f, -13f, 13f, -8f, 2.6f, 9.5f, 2.6f),
            new Wall(false, .5f, 12.125f, 18f),
        },
        props = new[] {
            new Prop("PackingTable", -8.6f, 9.3f, 3.75f, 1.1f, .9f), new Prop("PackingTable", -1.1f, 8.7f, 3.75f, 1.1f, .9f),
            new Prop("PackingTable", 6.4f, 9.3f, 3.75f, 1.1f, .9f),
            new Prop("ShopCounter", -6.4f, 1.5f, 6.9f, .75f, 1.1f), new Prop("ShopCounter", 6.7f, 1.5f, 6.9f, .75f, 1.1f),
            new Prop("CrateStack", -11f, 16.3f, 1.6f, 1.6f, 1.6f), new Prop("CrateStack", -9.4f, 16.9f, 1.2f, 1.2f, 1.2f),
            new Prop("CrateStack", -5f, 16.6f, 1.6f, 1.6f, 2.2f), new Prop("OfficeDesk", 6f, 15.2f, 3f, 1.2f, .9f),
        },
        enemies = new[] { new Vector2(-6.1f, .8f), new Vector2(7f, .8f), new Vector2(-9.25f, 6.4f), new Vector2(.1f, 5.8f),
            new Vector2(10.75f, 6.4f), new Vector2(-8f, 14.9f), new Vector2(5.75f, 15f) },
        workers = new[] { new Vector2(-4.9f, 7.25f), new Vector2(5.1f, 7.25f) },
        evidence = new Vector2(11.5f, 16.2f),
        lights = new[] { new Vector2(0f, 2f), new Vector2(0f, 8f), new Vector2(-6.5f, 15f), new Vector2(7f, 15f) },
    };

    // One huge floor of desks with forced workers, a server room back left, supervisors back right.
    static Plan CallCenter() => new Plan
    {
        doorX = -8f,
        wall = new Color(.36f, .44f, .46f), floor = new Color(.2f, .24f, .26f), trim = new Color(.12f, .16f, .17f),
        walls = new[] {
            new Wall(true, 4f, -13f, 13f, -8f, 2.5f, 4.5f, 2.5f),
            new Wall(true, 13.25f, -13f, 13f, -8f, 2.5f, 9.5f, 2.5f),
            new Wall(false, -2.5f, 13.25f, 18f),
            new Wall(false, -2.5f, 0f, 4f),
        },
        props = DeskRows().Concat(new[] {
            new Prop("ServerRack", -11.5f, 17.2f, 1.2f, .9f, 2.2f), new Prop("ServerRack", -10f, 17.2f, 1.2f, .9f, 2.2f),
            new Prop("ServerRack", -8.5f, 17.2f, 1.2f, .9f, 2.2f), new Prop("SupervisorDesk", 7f, 16.2f, 3f, 1.2f, .9f),
            new Prop("BreakTable", 6f, 1.6f, 2f, 1f, .8f),
        }).ToArray(),
        enemies = new[] { new Vector2(11.6f, 9.75f), new Vector2(-11.9f, 7.5f), new Vector2(.1f, 5.25f), new Vector2(11.4f, 5.25f),
            new Vector2(-11.1f, 14.9f), new Vector2(5.75f, 15.5f), new Vector2(10.75f, 15.5f), new Vector2(5.1f, 1.1f) },
        workers = new[] { new Vector2(-9.75f, 9.9f), new Vector2(-2.25f, 7.6f), new Vector2(5.25f, 9.9f), new Vector2(-6f, 5.5f), new Vector2(9f, 7.6f) },
        evidence = new Vector2(-4.5f, 15.75f),
        lights = new[] { new Vector2(-8f, 2f), new Vector2(5f, 2f), new Vector2(-6f, 8.5f), new Vector2(6f, 8.5f), new Vector2(-8f, 15.5f), new Vector2(7f, 15.5f) },
    };

    static Prop[] DeskRows()
    {
        var desks = new List<Prop>();
        foreach (float z in new[] { 10.7f, 8.55f, 6.4f })
            for (int i = 0; i < 6; i++) desks.Add(new Prop("Workstation", -9.75f + i * 3.75f, z, 2.75f, .9f, .9f));
        return desks.ToArray();
    }

    // A lobby along the front, then cells, a corridor to the boss office, and a video call studio.
    static Plan FakeOfficer() => new Plan
    {
        doorX = 0f,
        wall = new Color(.42f, .48f, .6f), floor = new Color(.3f, .31f, .34f), trim = new Color(.1f, .14f, .28f),
        walls = new[] {
            new Wall(true, 5.25f, -13f, 13f, -8.6f, 2.5f, 0f, 2.5f, 8.9f, 2.5f),
            new Wall(false, -4f, 5.25f, 18f),
            new Wall(false, 4f, 5.25f, 18f),
            new Wall(true, 12.25f, -4f, 4f, 0f, 2.5f),
        },
        props = new[] {
            new Prop("Bunk", -10.4f, 15.4f, 3.1f, .6f, .5f), new Prop("Bunk", -10.4f, 11.7f, 3.1f, .6f, .5f), new Prop("Bunk", -10.4f, 7.9f, 3.1f, .6f, .5f),
            new Prop("GreenScreen", 8.6f, 17.7f, 7f, .2f, 2.5f), new Prop("StudioTable", 8.6f, 9.4f, 4.4f, 1f, .8f),
            new Prop("ReceptionCounter", 7f, 2f, 7.5f, .75f, 1.1f), new Prop("Bench", -9.25f, 1.2f, 5f, .6f, .5f),
            new Prop("BossDesk", 1.5f, 15.2f, 2.4f, 1f, .9f),
        },
        enemies = new[] { new Vector2(-4.25f, 1.1f), new Vector2(11.75f, 3.5f), new Vector2(-2.1f, 8.3f), new Vector2(6.4f, 13f),
            new Vector2(11.4f, 7.4f), new Vector2(-2.1f, 14.25f) },
        workers = new[] { new Vector2(-6.75f, 13.9f), new Vector2(-6.75f, 10.1f), new Vector2(-6.75f, 6.4f) },
        evidence = new Vector2(2.1f, 16.9f),
        lights = new[] { new Vector2(0f, 2.5f), new Vector2(-8.5f, 11f), new Vector2(0f, 8.5f), new Vector2(0f, 15f), new Vector2(8.5f, 11f) },
    };

    // ---- Dressing: the imported office packs under Assets/Interiors (VNB Office Set, nappin Office Essentials, Next-gen
    // Electronics) placed per prop. Each prop keeps an invisible footprint collider so the NavMesh and bullets still see
    // the plan's numbers; the models are visual only. Things the packs lack (server racks, bunks, green screen, glowing
    // lamps) stay simple boxes. ----

    const string V = "VNB - Office Set/Prefabs/", N = "nappin/OfficeEssentialsPack/Prefabs/(Prb)", E = "Next-gen Electronics Pack/Prefabs/";
    // The VNB / nappin models face +Z; "toward -Z" (into the room from a back wall) is a 180 degree turn.
    const float Back = 180f;

    static Material M(string name, Color c, bool unlit = false) => WorldSceneArt.Paint("Dress_" + name, c, unlit, "Assets/Interiors/Materials");
    static Material Metal => M("Metal", new Color(.3f, .32f, .34f));
    static Material Black => M("Black", new Color(.05f, .05f, .06f));
    static Material Navy => M("Navy", new Color(.1f, .14f, .3f));
    static Material Paper => M("Paper", new Color(.9f, .88f, .8f));
    static Material Screen => M("ScreenOn", new Color(.6f, .95f, .75f), true);
    static Material Warm => M("WarmGlow", new Color(1f, .8f, .45f), true);
    static Material Tube => M("Tube", new Color(1f, .98f, .9f), true);

    // y is the bottom of the box above the floor; x, z are its centre.
    static GameObject B(Transform parent, string name, float x, float y, float z, float w, float h, float d, Material m, bool solid = false) =>
        WorldSceneArt.Box(parent, name, new Vector3(x, FloorTop + y, z) + Vector3.up * h / 2f, new Vector3(w, h, d), m, solid);

    static GameObject Put(Transform g, string path, float x, float y, float z, float yaw = 0f, float sx = 1f, float sy = 1f, float sz = 1f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Interiors/" + path + ".prefab");
        if (prefab == null) throw new InvalidOperationException("Interior prefab missing: " + path);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.transform.SetParent(g, false);
        go.transform.localPosition = new Vector3(x, FloorTop + y, z);
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = Vector3.Scale(go.transform.localScale, new Vector3(sx, sy, sz));
        Solidify(go);
        return go;
    }

    // Every visible piece gets a convex MeshCollider (no gaps under desks or between chair legs for a capsule to jam in);
    // flat things (rugs, paper, keyboards) stay walkable and get none.
    static void Solidify(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
        // Seating stays soft: with chair hulls in the aisles the 1 m gaps between desks no longer fit a NavMesh agent.
        if (go.name.Contains("chair") || go.name.Contains("stool")) return;
        foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
        {
            var renderer = filter.GetComponent<Renderer>();
            if (filter.sharedMesh == null || renderer == null || renderer.bounds.size.y < .05f) continue;
            var collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = true;
        }
    }

    // A row of `count` identical modules, each `step` wide along X, centred on x = 0.
    static void Row(Transform g, string path, int count, float step, float y, float z, float yaw = 0f, float sz = 1f)
    {
        for (int i = 0; i < count; i++) Put(g, path, (i - (count - 1) / 2f) * step, y, z, yaw, 1f, 1f, sz);
    }

    // The VNB desk_2 (2.37 x .95 x .75 m) stretched over the prop's footprint.
    static void FitDesk(Transform g, Prop p, string desk = V + "Office/desk_2") =>
        Put(g, desk, 0f, 0f, 0f, 0f, p.w / 2.37f, p.h / .95f, p.d / .75f);

    // Stacked VNB cardboard boxes (.65 m cube) filling a footprint; every other column one box lower.
    static void CrateGrid(Transform g, Prop p)
    {
        float s = Mathf.Min(.7f, p.d * .9f);
        int nx = Mathf.Max(1, Mathf.RoundToInt(p.w / s)), ny = Mathf.Max(1, Mathf.RoundToInt(p.h / s));
        for (int ix = 0; ix < nx; ix++)
            for (int iy = 0; iy < ny - (ix % 2); iy++)
                Put(g, V + "Deco/box", (ix - (nx - 1) / 2f) * s, iy * s, 0f, ix * 17f % 12f - 6f, s / .65f, s / .65f, s / .65f);
    }

    static void Boxes(Transform g, float x, float y, float z, int n, float step)
    {
        for (int i = 0; i < n; i++) Put(g, V + "Deco/box", x + i * step, y, z, i * 20f - 20f, .55f - (i % 2) * .08f, .55f - (i % 2) * .08f, .55f);
    }

    static void Workstation(Transform g, Prop p, float hd)
    {
        FitDesk(g, p, V + "Office/desk_1");
        Put(g, E + "monitor", 0f, p.h, hd - .3f, Back);
        Put(g, E + "keyboard", 0f, p.h, -.1f, Back);
        Put(g, E + "mouse", .5f, p.h, -.1f, Back);
        Put(g, V + "Office/phone", -p.w * .35f, p.h, 0f, 20f);
        Put(g, V + "Office/pile_of_paper_small", p.w * .3f, p.h, .05f, 10f);
        Put(g, V + "Office/office_chair", 0f, 0f, -hd - .35f, 0f);
    }

    // What the raid seals: the boss's computer and laptop, collected in DressProp from the boss desk.
    static readonly List<Transform> evidenceItems = new List<Transform>();

    static Transform Evidence(Transform desk, string label, string prefab, Vector3 at, float yaw)
    {
        var holder = WorldSceneArt.Empty(desk, label, at);
        var model = Put(holder, prefab, 0f, 0f, 0f, yaw);
        model.transform.localPosition = Vector3.zero;
        holder.gameObject.AddComponent<EvidenceItem>().label = label;
        var box = holder.gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, .25f, 0f);
        box.size = new Vector3(.75f, .55f, .7f);
        evidenceItems.Add(holder);
        return holder;
    }

    static void DressProp(Transform root, Prop p)
    {
        var g = WorldSceneArt.Empty(root, p.name, new Vector3(p.x, 0f, p.z));
        float hw = p.w / 2f, hd = p.d / 2f;
        switch (p.name)
        {
            case "PackingTable":
                FitDesk(g, p);
                Boxes(g, -hw * .55f, p.h, 0f, 3, .75f);
                Put(g, V + "Office/notepad", hw * .55f, p.h, -.1f, 15f);
                Put(g, V + "Office/tape", hw * .35f, p.h, .15f, 40f);
                Put(g, V + "Furniture/stool_1", 0f, 0f, -hd - .6f);
                Put(g, V + "Deco/box", hw * .3f, 0f, 0f, 10f, 1.1f, .6f, 1f);
                break;
            case "ShopCounter":
                Row(g, V + "Furniture/counter_a_1", Mathf.RoundToInt(p.w), 1f, 0f, 0f, 0f, p.d / .67f);
                for (float x = -hw + .8f; x < hw - .5f; x += 1.3f)
                    Put(g, V + (((int)(x * 10f) & 1) == 0 ? "Deco/briefcaase" : "Deco/box"), x, p.h - .15f, 0f, 10f * x, .5f, .5f, .5f);
                break;
            case "CrateStack":
                CrateGrid(g, p);
                break;
            case "OfficeDesk":
            case "SupervisorDesk":
            case "BossDesk":
                FitDesk(g, p, V + "Office/desk_2");
                Evidence(g, "คอมพิวเตอร์ของบอส", V + "Office/computer_new", new Vector3(hw * .35f, FloorTop + p.h, hd * .3f), Back);
                Evidence(g, "แล็ปท็อปของบอส", E + "notebook", new Vector3(-hw * .38f, FloorTop + p.h, -.1f), Back);
                Put(g, V + "Office/pile_of_paper_small", -hw * .02f, p.h, .2f, 12f);
                Put(g, V + "Furniture/lamp_small", -hw * .65f, p.h, .2f, 0f);
                Put(g, V + "Office/phone", hw * .05f, p.h, -.25f, 160f);
                Put(g, V + "Office/standard_office_chair_1", 0f, 0f, hd + .45f, Back);
                break;
            case "Workstation":
                Workstation(g, p, hd);
                foreach (float sx in new[] { -1f, 1f }) B(g, "Partition", sx * hw, 0f, 0f, .06f, 1.25f, p.d, M("Partition", new Color(.4f, .43f, .43f)));
                break;
            case "ServerRack":
                B(g, "Body", 0f, 0f, 0f, p.w, p.h, p.d, Black, true);
                for (int r = 0; r < 10; r++) B(g, "Led", 0f, .2f + r * .19f, -hd - .01f, p.w * .8f, .06f, .01f, M(r % 3 == 0 ? "LedBlue" : "LedGreen", r % 3 == 0 ? new Color(.2f, .5f, 1f) : new Color(.2f, 1f, .3f), true));
                break;
            case "BreakTable":
                FitDesk(g, p, V + "Office/desk_1");
                Put(g, N + "CoffePot", -.3f, p.h, 0f);
                Put(g, V + "Food/cup_filled", .1f, p.h, .1f);
                Put(g, N + "Mug", .5f, p.h, -.1f);
                Put(g, V + "Furniture/stool_1", -hw - .1f, 0f, 0f, 90f);
                Put(g, V + "Furniture/stool_1", hw + .1f, 0f, 0f, -90f);
                break;
            case "Bunk":
                foreach (float y in new[] { 0f, 1.1f })
                {
                    B(g, "Frame", 0f, y + .25f, 0f, p.w, .06f, 1f, Metal, true);
                    B(g, "Mattress", 0f, y + .31f, 0f, p.w - .1f, .16f, .94f, Navy, true);
                    B(g, "Pillow", -hw + .4f, y + .47f, 0f, .5f, .1f, .6f, Paper);
                }
                foreach (float sx in new[] { -1f, 1f })
                    foreach (float sz in new[] { -.5f, .5f }) B(g, "Post", sx * (hw - .05f), 0f, sz, .06f, 1.9f, .06f, Metal, true);
                break;
            case "GreenScreen":
                B(g, "Cloth", 0f, .15f, 0f, p.w - .6f, p.h - .3f, .04f, M("Chroma", new Color(.1f, .75f, .15f), true), true);
                foreach (float sx in new[] { -1f, 1f })
                {
                    B(g, "Pole", sx * (hw - .15f), 0f, 0f, .06f, p.h + .1f, .06f, Black, true);
                    B(g, "StandLeg", sx * (hw + .35f), 0f, -.6f, .05f, 1.9f, .05f, Black, true);
                    B(g, "Softbox", sx * (hw + .35f), 1.5f, -.6f, .7f, .7f, .2f, Tube, true);
                }
                B(g, "Bar", 0f, p.h, 0f, p.w, .06f, .06f, Black);
                break;
            case "StudioTable":
                B(g, "Rug", 0f, 0f, 0f, p.w + 1.6f, .02f, p.d + 1.8f, M("Rug", new Color(.12f, .12f, .13f)));
                FitDesk(g, p, V + "Office/desk_2");
                Put(g, E + "notebook", -hw * .2f, p.h, 0f, Back);
                Put(g, V + "Office/pile_of_paper_small", hw * .6f, p.h, 0f, 10f);
                Put(g, V + "Office/standard_office_chair_1", 0f, 0f, hd + .45f, Back);
                break;
            case "ReceptionCounter":
                Row(g, V + "Furniture/counter_b_1", Mathf.RoundToInt(p.w), 1f, 0f, 0f, 0f, p.d / .67f);
                B(g, "Badge", -p.w * .2f, .3f, -hd - .01f, .55f, .55f, .02f, M("Gold", new Color(.9f, .7f, .2f)));
                Put(g, V + "Office/new_monitor", hw * .35f, p.h - .1f, .05f, Back);
                Put(g, V + "Office/phone", hw * .1f, p.h - .1f, -.2f, 160f);
                Put(g, V + "Office/pile_of_paper_small", -hw * .6f, p.h - .1f, 0f, 8f);
                break;
            case "Bench":
                for (int i = 0; i < 4; i++) Put(g, V + "Furniture/modern_chair", (i - 1.5f) * 1.2f, 0f, 0f, Back);
                break;
            default:
                FitDesk(g, p);
                break;
        }
    }

    // Plants, lamps and tubes, mats, boards, cabinets: the set dressing that is not part of the plan's numbers.
    static void Extras(Transform t, InteriorPlan which, Plan plan)
    {
        bool wood = which == InteriorPlan.FakeShop;
        foreach (var l in plan.lights)
        {
            if (wood)
            {
                B(t, "Cord", l.x, 2.55f, l.y, .02f, .6f, .02f, Black);
                B(t, "Bulb", l.x, 2.4f, l.y, .18f, .22f, .18f, Warm);
            }
            else B(t, "Tube", l.x, 3.1f, l.y, 2.2f, .06f, .25f, Tube);
        }
        Put(t, V + "Deco/rug_a", plan.doorX, .01f, 1.4f, 0f, 2.4f, 1f, 2.4f);
        void Plant(float x, float z) => Put(t, V + "Deco/nature_deco_plant", x, 0f, z);
        void Cabinet(float x, float z, float yaw) => Put(t, V + "Furniture/file_cabinet_medium", x, 0f, z, yaw);
        switch (which)
        {
            case InteriorPlan.FakeShop:
                Plant(-12.2f, 2.8f); Plant(12.2f, 2.8f); Plant(-12.2f, 11f);
                Put(t, V + "Furniture/whiteboard_wall", 3.5f, 1.7f, 11.9f, Back, .9f, 1f, 1f);
                Put(t, V + "Deco/big_painting", -3.5f, 1.8f, 11.9f, Back, 1.6f, 1.6f, 1f);
                Cabinet(2.3f, 17.4f, Back);
                Put(t, V + "Furniture/shelf", -12.3f, 0f, 14f, 90f);
                Put(t, V + "Furniture/shelf", 12.3f, 0f, 14.5f, -90f);
                break;
            case InteriorPlan.CallCenter:
                Plant(-12.3f, 12.4f); Plant(12.3f, 12.4f);
                Put(t, V + "Furniture/whiteboard_wall", 7f, 1.8f, 17.8f, Back, 2.2f, 1.2f, 1f);
                Put(t, V + "Furniture/whiteboard_wall", -12.78f, 1.8f, 8.5f, 90f, 1.2f, 1.2f, 1f);
                for (int i = 0; i < 4; i++) Cabinet(-12.4f, 1f + i * .85f, 90f);
                Put(t, N + "WaterDispenser", 12.3f, 0f, 6.5f, -90f);
                Put(t, N + "VendingMachine", 12.2f, 0f, 3f, -90f);
                Put(t, V + "Office/coffee_machine", 11.4f, .95f, .8f, Back);
                Put(t, V + "Office/microwave_new", 12f, 0f, 1.6f, -90f, .8f, 1f, .8f);
                Put(t, V + "Office/air_conditioner", -5f, 2.4f, 17.7f, Back);
                break;
            case InteriorPlan.FakeOfficer:
                Plant(-12.4f, 3.8f); Plant(12.4f, 3.8f); Plant(12.3f, 17f);
                Put(t, V + "Furniture/whiteboard_wall", -1f, 1.8f, 17.8f, Back, .8f, 1f, 1f);
                Cabinet(-3.3f, 17.3f, Back); Cabinet(3.3f, 17.3f, Back);
                Put(t, V + "Furniture/bookshelf", -4.7f, 0f, 17.4f, Back, .6f, 1f, 1f);
                Put(t, V + "Furniture/lamp", 3.4f, 0f, 12.8f);
                break;
        }
    }

    // Door position inside the building (x); the entry and exit points are built from it.
    public static float DoorX(InteriorPlan plan) => Get(plan).doorX;

    // Builds the interior under `root` (the Hideout object). Returns the evidence-carrying RaidEncounter.
    public static RaidEncounter Build(GameObject root, InteriorPlan which, string navName)
    {
        var plan = Get(which);
        var t = root.transform;
        var wall = Surface(which, "Wall", plan.wall);
        var floor = Surface(which, "Floor", plan.floor);
        var trim = Surface(which, "Trim", plan.trim);
        var ceiling = Surface(which, "Ceiling", plan.trim);
        var shell = WorldSceneArt.Empty(t, "Shell", Vector3.zero);

        Slab(shell, "Floor", new Vector3(0f, FloorTop - .08f, Depth / 2f), new Vector3(Width + .6f, .16f, Depth + .6f), floor);
        Slab(shell, "Ceiling", new Vector3(0f, WallHeight + FloorTop + .15f, Depth / 2f), new Vector3(Width + .6f, .3f, Depth + .6f), ceiling);
        BuildWall(shell, new Wall(true, 0f, -13f, 13f, plan.doorX, 3f), wall, trim);
        BuildWall(shell, new Wall(true, Depth, -13f, 13f), wall, trim);
        BuildWall(shell, new Wall(false, -13f, 0f, Depth), wall, trim);
        BuildWall(shell, new Wall(false, 13f, 0f, Depth), wall, trim);
        foreach (var w in plan.walls) BuildWall(shell, w, wall, trim);
        // The front door is shut: the doorway is crossed through DoorPortal, not walked out of.
        Slab(shell, "FrontDoor", new Vector3(plan.doorX, FloorTop + 1.3f, .1f), new Vector3(2.8f, 2.6f, .2f), trim);

        var props = WorldSceneArt.Empty(t, "Props", Vector3.zero);
        evidenceItems.Clear();
        foreach (var p in plan.props) DressProp(props, p);
        if (evidenceItems.Count == 0) throw new InvalidOperationException("No boss desk in plan " + which + ": nothing to seal");
        Extras(WorldSceneArt.Empty(t, "Dressing", Vector3.zero), which, plan);

        BuildLighting(t, which, plan);

        // A tight warm spot over the boss's desk so the objective reads from across the room.
        var spotLight = WorldSceneArt.Empty(evidenceItems[0], "EvidenceSpot", new Vector3(0f, 1.9f, -.3f)).gameObject.AddComponent<Light>();
        spotLight.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        spotLight.type = LightType.Spot;
        spotLight.color = new Color(1f, .9f, .7f);
        spotLight.intensity = 25f;
        spotLight.range = 5f;
        spotLight.spotAngle = 80f;
        spotLight.innerSpotAngle = 40f;
        spotLight.shadows = LightShadows.None;

        var raid = root.AddComponent<RaidEncounter>();
        raid.evidencePoint = evidenceItems[0];
        raid.extraEvidence = evidenceItems.Skip(1).ToArray();
        var enemies = WorldSceneArt.Empty(t, "RaidTemplates", Vector3.zero);
        raid.enemyRoot = enemies;
        for (int i = 0; i < plan.enemies.Length; i++)
            WorldSceneArt.Character(enemies, i % 2 == 0 ? "Suit_Male" : "Casual3_Male", Spot(t, plan.enemies[i]), true, Faction.Scammer);
        for (int i = 0; i < plan.workers.Length; i++)
            WorldSceneArt.Character(enemies, i % 2 == 0 ? "Worker_Female" : "Worker_Male", Spot(t, plan.workers[i]), false, Faction.ForcedWorker);
        // Corrupt police reinforcements come in at the front door when Trouble fills.
        var police = WorldSceneArt.Empty(t, "CorruptPoliceTemplates", Vector3.zero);
        raid.corruptPoliceRoot = police;
        for (int i = 0; i < 3; i++)
            WorldSceneArt.Character(police, "Soldier_Male", Spot(t, new Vector2(plan.doorX + (i - 1) * 2f, 2.2f)), true, Faction.CorruptPolice, 0f);
        enemies.gameObject.SetActive(false);
        police.gameObject.SetActive(false);

        Bake(root, navName);
        return raid;
    }

    // The plan's spawn points were chosen against the old footprints; the real meshes are bigger, so move a spawn point to the
    // nearest spot where a standing capsule fits, otherwise a character could start inside furniture and be stuck.
    static Vector3 Spot(Transform root, Vector2 p)
    {
        Physics.SyncTransforms();
        for (float r = 0f; r <= 2.5f; r += .25f)
            for (int a = 0; a < (r == 0f ? 1 : 12); a++)
            {
                var q = p + new Vector2(Mathf.Cos(a * Mathf.PI / 6f), Mathf.Sin(a * Mathf.PI / 6f)) * r;
                if (Mathf.Abs(q.x) > 12.3f || q.y < .8f || q.y > 17.2f) continue; // keep inside the outer walls
                var world = root.TransformPoint(new Vector3(q.x, FloorTop + .03f, q.y));
                if (!Physics.CheckCapsule(world + Vector3.up * .5f, world + Vector3.up * 1.4f, .45f, ~0, QueryTriggerInteraction.Ignore))
                    return new Vector3(q.x, FloorTop + .03f, q.y);
            }
        return new Vector3(p.x, FloorTop + .03f, p.y); // shortcut: no free spot within 2.5 m, keep the plan's point; the spawn check in the builder log shows it
    }

    // ---- Room surfaces: Codex's tileable textures (Assets/Interiors/Textures), each covering 2 x 2 m. ----

    const float TextureMetres = 2f;
    static readonly Dictionary<InteriorPlan, string> TexturePrefix = new Dictionary<InteriorPlan, string>
    {
        { InteriorPlan.FakeShop, "fakeshop" }, { InteriorPlan.CallCenter, "callcenter" }, { InteriorPlan.FakeOfficer, "fakeofficer" },
    };

    // The room's wall / floor / trim / ceiling material. Without the texture file it stays the flat plan colour.
    static Material Surface(InteriorPlan which, string kind, Color flat)
    {
        var material = WorldSceneArt.Paint("Interior_" + which + "_" + kind, flat, false, "Assets/Interiors/Materials");
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Interiors/Textures/{TexturePrefix[which]}_{kind.ToLowerInvariant()}.png");
        if (texture == null) return material;
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white); // the texture carries the colour
        EditorUtility.SetDirty(material);
        return material;
    }

    // A box whose UVs run in metres, so a texture keeps its scale on a 26 m wall as on a 1 m lintel
    // (a stock cube maps the whole texture onto every face however it is stretched).
    static GameObject Slab(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var box = WorldSceneArt.Box(parent, name, position, size, material);
        var filter = box.GetComponent<MeshFilter>();
        var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
        mesh.name = name + "_Uv";
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var uv = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            var p = Vector3.Scale(vertices[i] + Vector3.one * .5f, size) / TextureMetres; // 0..size along each axis, in tiles
            var n = normals[i];
            uv[i] = Mathf.Abs(n.x) > .5f ? new Vector2(p.z, p.y) : Mathf.Abs(n.y) > .5f ? new Vector2(p.x, p.z) : new Vector2(p.x, p.y);
        }
        mesh.uv = uv;
        filter.sharedMesh = mesh;
        return box;
    }

    // ---- Lighting. The hideout scenes load additively into the city, whose ambient light and dusk sun are the ones that
    // apply, and the ceiling shades the sun out: indoors is lit only by the lights below, a local post volume and a probe. ----

    struct Look { public Color lamp, tint; public float intensity, range; }

    static Look LookOf(InteriorPlan which) => which switch
    {
        InteriorPlan.CallCenter => new Look { lamp = new Color(.82f, .93f, 1f), tint = new Color(.9f, 1f, .98f), intensity = 7f, range = 11f },     // cold fluorescent
        InteriorPlan.FakeOfficer => new Look { lamp = new Color(.9f, .95f, 1f), tint = new Color(.92f, .96f, 1.05f), intensity = 7f, range = 11f }, // office white
        _ => new Look { lamp = new Color(1f, .76f, .45f), tint = new Color(1.05f, .95f, .85f), intensity = 8f, range = 11f },                         // tungsten bulbs
    };

    static Light PointLight(Transform parent, string name, Vector3 at, Color color, float intensity, float range)
    {
        var light = WorldSceneArt.Empty(parent, name, at).gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        return light;
    }

    static void BuildLighting(Transform t, InteriorPlan which, Plan plan)
    {
        var look = LookOf(which);
        var lights = WorldSceneArt.Empty(t, "Lights", Vector3.zero);
        foreach (var l in plan.lights) PointLight(lights, "HangingLamp", new Vector3(l.x, 2.9f, l.y), look.lamp, look.intensity, look.range);
        // Dim fill on a grid so no corner of the building is pitch black (the plan's lamps leave the walls dark).
        foreach (float x in new[] { -9f, 0f, 9f })
            foreach (float z in new[] { 3f, 9f, 15f })
                PointLight(lights, "Fill", new Vector3(x, 2.7f, z), look.lamp * .8f, 2.5f, 8f);
        PointLight(lights, "DoorGlow", new Vector3(plan.doorX, 2.4f, 1.3f), new Color(.75f, .9f, 1f), 5f, 5f);

        // Light that belongs to things in the room.
        switch (which)
        {
            case InteriorPlan.FakeShop:
                PointLight(lights, "WindowGlowL", new Vector3(-6.4f, 1.1f, 1.2f), new Color(1f, .7f, .35f), 4f, 4.5f);
                PointLight(lights, "WindowGlowR", new Vector3(6.7f, 1.1f, 1.2f), new Color(1f, .7f, .35f), 4f, 4.5f);
                break;
            case InteriorPlan.CallCenter:
                PointLight(lights, "ServerGlow", new Vector3(-10f, 1.6f, 15.8f), new Color(.2f, .65f, 1f), 7f, 6f);
                foreach (float z in new[] { 6.4f, 8.55f, 10.7f })
                    PointLight(lights, "MonitorGlow", new Vector3(0f, 1.5f, z + .3f), new Color(.6f, 1f, .8f), 3.5f, 9f);
                break;
            case InteriorPlan.FakeOfficer:
                PointLight(lights, "ChromaGlow", new Vector3(8.6f, 1.8f, 16.2f), new Color(.25f, 1f, .35f), 8f, 7f);
                PointLight(lights, "SoftboxL", new Vector3(4.75f, 1.7f, 16.9f), new Color(1f, 1f, .95f), 6f, 7f);
                PointLight(lights, "SoftboxR", new Vector3(12.45f, 1.7f, 16.9f), new Color(1f, 1f, .95f), 6f, 7f);
                break;
        }

        // Local grade: a touch of exposure, bloom for the emissive screens and lamps, a heavier vignette than the street.
        const string folder = "Assets/Interiors/Volumes";
        WorldSceneArt.EnsureFolder(folder);
        string path = $"{folder}/Hideout_{which}_Look.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
        foreach (var existing in profile.components.ToArray()) UnityEngine.Object.DestroyImmediate(existing, true);
        profile.components.Clear();
        var color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(.7f);
        color.contrast.Override(12f);
        color.colorFilter.Override(look.tint);
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(.55f);
        bloom.threshold.Override(.85f);
        bloom.scatter.Override(.65f);
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(.32f);
        vignette.smoothness.Override(.5f);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        var volume = WorldSceneArt.Empty(t, "IndoorGrade", new Vector3(0f, 1.6f, Depth / 2f)).gameObject.AddComponent<Volume>();
        volume.isGlobal = false;
        volume.priority = 20f;
        volume.blendDistance = 1.5f;
        volume.sharedProfile = profile;
        var box = volume.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(Width + .5f, WallHeight + 1.5f, Depth + .5f);

        // One box-projected probe so the glossier props reflect the room, not the city sky.
        var probe = WorldSceneArt.Empty(t, "IndoorProbe", new Vector3(0f, 1.6f, Depth / 2f)).gameObject.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        probe.resolution = 64;
        probe.size = new Vector3(Width, WallHeight + 1f, Depth);
        probe.boxProjection = true;
        probe.importance = 10;
        probe.intensity = .8f;
    }

    static void BuildWall(Transform parent, Wall w, Material wall, Material trim)
    {
        var gaps = new List<(float lo, float hi)>();
        for (int i = 0; i + 1 < w.gaps.Length; i += 2) gaps.Add((w.gaps[i] - w.gaps[i + 1] / 2f, w.gaps[i] + w.gaps[i + 1] / 2f));
        gaps.Sort((a, b) => a.lo.CompareTo(b.lo));

        float cursor = w.from;
        foreach (var gap in gaps)
        {
            Segment(parent, w, cursor, gap.lo, wall);
            // Lintel over the opening.
            float lintelH = WallHeight - 2.6f;
            Piece(parent, w, (gap.lo + gap.hi) / 2f, gap.hi - gap.lo, FloorTop + 2.6f + lintelH / 2f, lintelH, trim);
            cursor = gap.hi;
        }
        Segment(parent, w, cursor, w.to, wall);
    }

    static void Segment(Transform parent, Wall w, float a, float b, Material material)
    {
        if (b - a < .01f) return;
        Piece(parent, w, (a + b) / 2f, b - a, FloorTop + WallHeight / 2f, WallHeight, material);
    }

    static void Piece(Transform parent, Wall w, float centre, float length, float y, float height, Material material)
    {
        var size = w.alongX ? new Vector3(length, height, WallThickness) : new Vector3(WallThickness, height, length);
        var position = w.alongX ? new Vector3(centre, y, w.at) : new Vector3(w.at, y, centre);
        Slab(parent, w.alongX ? "WallX" : "WallZ", position, size, material);
    }

    // Each interior bakes its own NavMesh asset, named for the scene that owns it, so no other scene's asset is replaced.
    static void Bake(GameObject root, string navName)
    {
        Physics.SyncTransforms();
        var surface = root.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Volume;
        surface.center = new Vector3(0f, 2f, Depth / 2f);
        surface.size = new Vector3(Width + 4f, 8f, Depth + 4f);
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.overrideVoxelSize = true;
        surface.voxelSize = .15f;
        surface.BuildNavMesh();
        if (surface.navMeshData == null) throw new InvalidOperationException("NavMesh bake failed: " + navName);
        WorldSceneArt.EnsureFolder("Assets/Interiors/Navigation");
        string path = "Assets/Interiors/Navigation/" + navName + ".asset";
        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(path) != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(surface.navMeshData, path);
    }
}
