using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HorseCity.EditorTools
{
    /// <summary>
    /// HORSE menu tools that dress the scene with the imported asset packs:
    ///  - police station furniture (front desk, offices, control room, interrogation, armory, booking)
    ///  - the hidden weapon shop (slat walls with rifles, glass display cases, ammo shelves)
    /// Everything is placed in each building's own floor plan (metres, x = along the street front,
    /// y = from the front wall to the back wall), so it lines up with the Blender export.
    /// Re-running a menu item replaces what it built before. All of it is undoable.
    /// </summary>
    public static class InteriorDresser
    {
        const string VNB = "Assets/Interiors/VNB - Office Set/Prefabs/";
        const string NAP = "Assets/Interiors/nappin/OfficeEssentialsPack/Prefabs/(Prb)";
        const string NGE = "Assets/Interiors/Next-gen Electronics Pack/Prefabs/";
        const string WPN = "Assets/Interiors/Low Poly Weapons VOL.1/Prefabs/";
        const string QUA = "Assets/ThirdParty/Quaternius/";
        const string PRP = "Assets/Models/Props/Prefabs/";
        const string SCI = "Assets/ScifiOfficeLite/Prefabs/";
        const string GEN = "Assets/HorseCity/Generated/";
        const float Floor = 0.17f; // interior floor height above the quest building origin

        // Which way each model's "front" points, relative to +Z. Tuned by looking at them in the scene.
        static readonly Dictionary<string, float> FrontOffset = new Dictionary<string, float>();

        // ------------------------------------------------------------------ police station
        [MenuItem("HORSE/Interiors: Furnish Police Station")]
        static void FurnishPolice()
        {
            var f = Frame("QL_Police", "Police_Interior", 206f, 214f, true);
            if (!f) return;
            const string desk = NAP + "Desk1.prefab";

            // Lobby (x -6..6, y -14..-6) — front desk facing the entrance, waiting chairs
            var lobby = Group(f, "Lobby");
            Workstation(lobby, desk, -1.3f, -8.9f, 180f, 1);
            Workstation(lobby, desk, 1.3f, -8.9f, 180f, 1);
            for (int i = 0; i < 4; i++) Put(lobby, VNB + "Furniture/simple_chair_1.prefab", -5.5f, -12.6f + i * 0.85f, 90f, fitH: 0.9f, snap: Snap.MinX, at: -5.9f);
            for (int i = 0; i < 4; i++) Put(lobby, VNB + "Furniture/simple_chair_1.prefab", 5.5f, -12.6f + i * 0.85f, -90f, fitH: 0.9f, snap: Snap.MaxX, at: 5.9f);
            Put(lobby, NAP + "Plant1.prefab", -5.3f, -13.3f, 0f, fitH: 1.2f);
            Put(lobby, NAP + "Plant1.prefab", 5.3f, -6.7f, 0f, fitH: 1.2f);
            Put(lobby, NAP + "WaterDispenser.prefab", 5.4f, -13.3f, -90f, fitH: 1.3f);
            Put(lobby, VNB + "Deco/big_painting.prefab", 0f, -6.1f, 180f, elev: 1.4f, fitLen: 2.0f, snap: Snap.MaxZ, at: -6.08f);

            // Front-left office (x -18..-6, y -14..-6) — like the office reference
            var off = Group(f, "Office");
            Workstation(off, desk, -15.4f, -11.7f, 180f, 2);
            Workstation(off, desk, -8.8f, -11.7f, 180f, 2);
            Workstation(off, desk, -15.4f, -8.4f, 180f, 1);
            Workstation(off, desk, -8.8f, -8.4f, 180f, 1);
            for (int i = 0; i < 3; i++) Put(off, VNB + "Furniture/file_cabinet_medium.prefab", -17.5f, -13.2f + i * 0.75f, 90f, fitH: 1.3f, snap: Snap.MinX, at: -17.85f);
            Put(off, VNB + "Furniture/whiteboard_wall.prefab", -15.4f, -6.1f, 180f, elev: 0.9f, fitLen: 2.2f, snap: Snap.MaxZ, at: -6.08f);
            Put(off, NAP + "Printer.prefab", -6.6f, -13.3f, -90f, fitH: 1.1f);
            Put(off, NAP + "TrashCan.prefab", -13.0f, -10.0f, 0f, fitH: 0.4f);
            Put(off, NAP + "Plant2.prefab", -17.3f, -6.6f, 0f, fitH: 1.2f);

            // Chief's office (x 6..18, y -14..-6)
            var chief = Group(f, "ChiefOffice");
            Workstation(chief, VNB + "Office/desk_5.prefab", 12f, -11.2f, 0f, 1);
            Put(chief, VNB + "Furniture/simple_chair_1.prefab", 11.2f, -9.6f, 180f, fitH: 0.9f);
            Put(chief, VNB + "Furniture/simple_chair_1.prefab", 12.8f, -9.6f, 180f, fitH: 0.9f);
            Put(chief, VNB + "Furniture/bookshelf.prefab", 7.4f, -13.5f, 0f, fitH: 2.0f, snap: Snap.MinZ, at: -13.85f);
            Put(chief, VNB + "Furniture/file_cabinet_small.prefab", 17.5f, -13.2f, -90f, fitH: 1.0f, snap: Snap.MaxX, at: 17.85f);
            Put(chief, VNB + "Furniture/file_cabinet_small.prefab", 17.5f, -12.4f, -90f, fitH: 1.0f, snap: Snap.MaxX, at: 17.85f);
            Put(chief, NAP + "Sofa1.prefab", 6.5f, -9.0f, 90f, fitH: 0.85f, snap: Snap.MinX, at: 6.08f);
            Put(chief, NAP + "Plant1.prefab", 17.3f, -6.7f, 0f, fitH: 1.2f);
            Put(chief, NAP + "Clock.prefab", 12f, -6.1f, 180f, elev: 2.6f, fitLen: 0.4f, snap: Snap.MaxZ, at: -6.08f);

            // Corridor (y -6..-3)
            var cor = Group(f, "Corridor");
            Put(cor, NAP + "Plant2.prefab", -17.3f, -4.5f, 0f, fitH: 1.2f);
            Put(cor, NAP + "VendingMachine.prefab", 17.3f, -4.5f, -90f, fitH: 1.9f, snap: Snap.MaxX, at: 17.85f);

            // Room A (x -18..-12) — interrogation
            var ia = Group(f, "Interrogation");
            Put(ia, VNB + "Furniture/table.prefab", -15f, 7f, 0f, fitH: 0.76f);
            Put(ia, VNB + "Furniture/simple_chair_1.prefab", -15.4f, 6.1f, 0f, fitH: 0.9f);
            Put(ia, VNB + "Furniture/simple_chair_1.prefab", -14.6f, 6.1f, 0f, fitH: 0.9f);
            Put(ia, VNB + "Furniture/simple_chair_1.prefab", -15f, 7.9f, 180f, fitH: 0.9f);
            Put(ia, PRP + "CaseFolder.prefab", -15f, 7f, 0f, elev: 0.77f, fitLen: 0.35f);

            // Room B (x -12..-6) — evidence store
            var ev = Group(f, "Evidence");
            for (int i = 0; i < 4; i++) Put(ev, VNB + "Furniture/shelf.prefab", -11.6f, 1.5f + i * 3f, 90f, fitH: 2.0f, snap: Snap.MinX, at: -11.9f);
            for (int i = 0; i < 4; i++) Put(ev, VNB + "Furniture/shelf.prefab", -6.4f, 1.5f + i * 3f, -90f, fitH: 2.0f, snap: Snap.MaxX, at: -6.1f);
            Put(ev, VNB + "Furniture/table.prefab", -9f, 8f, 0f, fitH: 0.76f);
            Put(ev, PRP + "OldMap.prefab", -9f, 8f, 0f, elev: 0.77f, fitLen: 0.7f);
            Put(ev, VNB + "Deco/box.prefab", -9.6f, 12.8f, 15f, fitH: 0.4f);
            Put(ev, VNB + "Deco/box.prefab", -8.6f, 12.9f, -10f, fitH: 0.4f);

            // Room C (x -6..2) — CCTV / control room like the reference
            var cc = Group(f, "ControlRoom");
            for (int r = 0; r < 2; r++)
                for (int c = 0; c < 4; c++)
                    Put(cc, NGE + "tv.prefab", -4.4f + c * 1.6f, 13.8f, 180f, elev: 1.25f + r * 0.95f, fitLen: 1.55f, snap: Snap.MaxZ, at: 13.85f);
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    Workstation(cc, desk, -4.0f + c * 2.0f, 9.4f - r * 3.2f, 0f, 2);
            Put(cc, SCI + "Tech Accessories/Server Rack.prefab", 1.5f, -1.9f, -90f, fitH: 2.0f, snap: Snap.MaxX, at: 1.9f);
            Put(cc, NAP + "Clock.prefab", 1.2f, 13.8f, 180f, elev: 3.0f, fitLen: 0.4f, snap: Snap.MaxZ, at: 13.85f);

            // Room D (x 2..6) — armory
            var ar = Group(f, "Armory");
            Box(ar, "GunRackBoard", new Vector3(2.1f, 1.9f, 6.5f), new Vector3(0.05f, 1.6f, 9.5f), Mat("WoodDark", new Color(0.28f, 0.18f, 0.1f), 0.2f));
            string[] rifles = { "AK74", "M4_8", "Bennelli_M4", "M249", "AK74", "M4_8", "Bennelli_M4", "M4_8" };
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 8; i++)
                    Gun(ar, WPN + rifles[(i + row) % rifles.Length] + ".prefab", along: 'z', x: 2.2f, y: 2.6f + i * 1.15f, h: 1.5f + row * 0.75f, len: 0.95f, snap: Snap.MinX, at: 2.14f);
            for (int i = 0; i < 4; i++)
            {
                var sh = Put(ar, NAP + "Shelves2.prefab", 5.6f, 1.8f + i * 3f, -90f, fitH: 1.8f, snap: Snap.MaxX, at: 5.9f);
                if (sh) StackOn(ar, sh, PRP + "AmmoBox.prefab", 3, 0.3f);
            }
            Put(ar, PRP + "AmmoBoxLarge.prefab", 4.2f, 12.8f, 0f, fitLen: 0.8f);

            // Room E (x 6..18, y -3..9) — booking, in front of the cells
            var bk = Group(f, "Booking");
            Workstation(bk, desk, 10f, 3.5f, 0f, 1);
            Put(bk, VNB + "Furniture/simple_chair_1.prefab", 10f, 5.0f, 180f, fitH: 0.9f);
            for (int i = 0; i < 3; i++) Put(bk, VNB + "Furniture/simple_chair_1.prefab", 17.4f, 0f + i * 0.85f, -90f, fitH: 0.9f, snap: Snap.MaxX, at: 17.85f);
            Put(bk, VNB + "Furniture/file_cabinet_medium.prefab", 6.5f, -1.8f, 90f, fitH: 1.3f, snap: Snap.MinX, at: 6.1f);
            Put(bk, VNB + "Furniture/file_cabinet_medium.prefab", 6.5f, -1.05f, 90f, fitH: 1.3f, snap: Snap.MinX, at: 6.1f);

            Finish(f, "police station");
        }

        // ------------------------------------------------------------------ weapon shop
        [MenuItem("HORSE/Interiors: Furnish Weapon Shop")]
        static void FurnishWeaponShop()
        {
            var f = Frame("QL_WeaponShop", "WeaponShop_Furnishing", 140f, 28f, false);
            if (!f) return;
            // Room: x -3.75..3.75, y -6.75..6.75, 4 m high, door at the front right (x 0.9..3.1).
            var wood = Mat("SlatWood", new Color(0.72f, 0.55f, 0.36f), 0.25f);
            var groove = Mat("SlatGroove", new Color(0.22f, 0.15f, 0.09f), 0.1f);
            var black = Mat("CaseBlack", new Color(0.04f, 0.04f, 0.045f), 0.55f);
            var glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/HorseCity/Generated/Materials/Glass_Display.mat") ?? Mat("CaseGlass", new Color(0.8f, 0.9f, 0.95f, 0.18f), 0.95f, glass: true);
            var lamp = Mat("TrackLight", new Color(1f, 0.85f, 0.6f), 0.5f, emissive: true);

            // Slat walls (left and back), like the reference
            var walls = Group(f, "SlatWalls");
            Box(walls, "Slat_Left", new Vector3(-3.73f, 2.0f, 1.9f), new Vector3(0.03f, 3.2f, 9.6f), wood);
            Box(walls, "Slat_Back", new Vector3(0f, 2.0f, 6.73f), new Vector3(7.46f, 3.2f, 0.03f), wood);
            for (float h = 0.55f; h < 3.6f; h += 0.16f)
            {
                Box(walls, "Groove_L", new Vector3(-3.71f, h, 1.9f), new Vector3(0.012f, 0.018f, 9.6f), groove, collider: false);
                Box(walls, "Groove_B", new Vector3(0f, h, 6.71f), new Vector3(7.46f, 0.018f, 0.012f), groove, collider: false);
            }

            // Long guns on the slat walls
            var rack = Group(f, "WallGuns");
            string[] longGuns = { WPN + "AK74.prefab", WPN + "M4_8.prefab", WPN + "Bennelli_M4.prefab", QUA + "Props/Shotgun_1.fbx", WPN + "M249.prefab", QUA + "Props/Shotgun_2.fbx", WPN + "AK74.prefab" };
            int g = 0;
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 6; i++)
                    Gun(rack, longGuns[g++ % longGuns.Length], along: 'z', x: -3.6f, y: -1.6f + i * 1.25f, h: 1.35f + row * 0.6f, len: 0.95f, snap: Snap.MinX, at: -3.7f);
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 5; i++)
                    Gun(rack, longGuns[g++ % longGuns.Length], along: 'x', x: -2.6f + i * 1.3f, y: 6.6f, h: 1.6f + row * 0.65f, len: 0.95f, snap: Snap.MaxZ, at: 6.7f);
            Gun(rack, WPN + "M107.prefab", along: 'x', x: 0f, y: 6.6f, h: 3.15f, len: 1.35f, snap: Snap.MaxZ, at: 6.7f);
            Gun(rack, WPN + "RPG7.prefab", along: 'z', x: -3.6f, y: 2.2f, h: 3.25f, len: 1.1f, snap: Snap.MinX, at: -3.7f);

            // Glass display cases in an L, with handguns inside
            var cases = Group(f, "DisplayCases");
            string[] small = { WPN + "M1911.prefab", QUA + "Props/Pistol_1.fbx", QUA + "Props/Revolver_1.fbx", WPN + "Uzi.prefab", QUA + "Props/Revolver_2.fbx", "Assets/Art/Models/WesternRevolver/WesternRevolver.fbx", WPN + "RGD-5.prefab" };
            int s = 0;
            void Case(Vector3 c, bool alongZ, float len)
            {
                Vector3 size = alongZ ? new Vector3(0.65f, 0f, len) : new Vector3(len, 0f, 0.65f);
                Box(cases, "Case_Base", c + Vector3.up * 0.28f, new Vector3(size.x, 0.56f, size.z), black);
                Box(cases, "Case_Glass", c + Vector3.up * 0.77f, new Vector3(size.x, 0.42f, size.z), glass, collider: false);
                Box(cases, "Case_Rim", c + Vector3.up * 0.985f, new Vector3(size.x + 0.02f, 0.02f, size.z + 0.02f), black, collider: false);
                int n = Mathf.FloorToInt(len / 0.42f);
                for (int k = 0; k < n; k++)
                {
                    float t = -len / 2f + 0.25f + k * (len - 0.5f) / Mathf.Max(1, n - 1);
                    Vector3 p = c + (alongZ ? new Vector3(0, 0, t) : new Vector3(t, 0, 0));
                    Gun(cases, small[s++ % small.Length], along: alongZ ? 'x' : 'z', x: p.x, y: p.z, h: 0.62f, len: 0.24f, flat: true);
                }
            }
            Case(new Vector3(-1.5f, 0f, 0.3f), true, 3.0f);
            Case(new Vector3(-1.5f, 0f, 3.3f), true, 3.0f);
            Case(new Vector3(0.15f, 0f, 4.65f), false, 2.65f);

            // Ammo shelves on the right wall + boxes
            var ammo = Group(f, "Ammo");
            for (int i = 0; i < 3; i++)
            {
                var sh = Put(ammo, NAP + "Shelves2.prefab", 3.4f, 0.4f + i * 2.0f, -90f, fitH: 1.9f, snap: Snap.MaxX, at: 3.72f);
                if (sh) StackOn(ammo, sh, PRP + "AmmoBox.prefab", 4, 0.28f);
            }
            Put(ammo, PRP + "AmmoBoxLarge.prefab", 3.1f, 6.1f, -90f, fitLen: 0.8f);
            Put(ammo, PRP + "AmmoBoxLarge.prefab", 3.1f, 6.1f, -85f, elev: 0.45f, fitLen: 0.8f);

            // Front of the shop stays a normal-looking local shop (the disguise)
            var front = Group(f, "ShopFront");
            Put(front, NAP + "VendingMachine.prefab", -3.2f, -6.2f, 0f, fitH: 1.9f, snap: Snap.MinX, at: -3.72f);
            Put(front, NAP + "Shelves1.prefab", -3.3f, -3.8f, 90f, fitH: 1.8f, snap: Snap.MinX, at: -3.72f);
            Put(front, VNB + "Furniture/counter_a_1.prefab", 0.2f, -3.0f, 180f, fitH: 0.95f);
            Put(front, VNB + "Furniture/stool_1.prefab", 0.2f, -2.3f, 0f, fitH: 0.7f);

            // Track lighting (emissive, bakeable) + one small real light
            var lights = Group(f, "TrackLights");
            foreach (float x in new[] { -2.2f, 1.2f })
            {
                Box(lights, "Track", new Vector3(x, 3.93f, 0.6f), new Vector3(0.05f, 0.04f, 11.5f), black, collider: false);
                for (float y = -4.6f; y <= 6.0f; y += 1.3f)
                {
                    Box(lights, "Spot", new Vector3(x, 3.82f, y), new Vector3(0.09f, 0.16f, 0.09f), black, collider: false);
                    Box(lights, "Bulb", new Vector3(x, 3.735f, y), new Vector3(0.07f, 0.012f, 0.07f), lamp, collider: false);
                }
            }
            var lgo = new GameObject("ShopLight");
            Undo.RegisterCreatedObjectUndo(lgo, "Shop light");
            lgo.transform.SetParent(lights, false);
            lgo.transform.localPosition = new Vector3(-0.8f, 3.4f, 2.5f);
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Point; l.range = 9f; l.intensity = 2.2f; l.color = new Color(1f, 0.86f, 0.66f); l.shadows = LightShadows.None;

            Finish(f, "weapon shop");
        }


        // ------------------------------------------------------------------ quick views (Scene view, eye level)
        [MenuItem("HORSE/View Inside/Weapon Shop")] static void V1() => View("WeaponShop_Furnishing", new Vector3(2.4f, 1.6f, -5.2f), new Vector3(-1.5f, 1.4f, 4.5f));
        [MenuItem("HORSE/View Inside/Police Control Room")] static void V2() => View("Police_Interior", new Vector3(-2f, 1.7f, -1.8f), new Vector3(-2f, 1.4f, 13f));
        [MenuItem("HORSE/View Inside/Police Office")] static void V3() => View("Police_Interior", new Vector3(-7f, 1.7f, -6.6f), new Vector3(-16f, 0.9f, -12f));
        [MenuItem("HORSE/View Inside/Police Lobby")] static void V4() => View("Police_Interior", new Vector3(0f, 1.7f, -13.4f), new Vector3(0f, 1.0f, -6f));
        [MenuItem("HORSE/View Inside/Police Armory")] static void V5() => View("Police_Interior", new Vector3(4.6f, 1.7f, -2.2f), new Vector3(2.2f, 1.6f, 9f));
        [MenuItem("HORSE/View Inside/Police Chief Office")] static void V6() => View("Police_Interior", new Vector3(12f, 1.7f, -6.6f), new Vector3(12f, 0.9f, -13f));

        static void View(string frameName, Vector3 eye, Vector3 look)
        {
            var root = GameObject.Find("Quest_Locations_Separated");
            Transform f = null;
            if (root) foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == frameName) { f = t; break; }
            var sv = SceneView.lastActiveSceneView;
            if (!f || !sv) { Debug.LogWarning("View: run the furnish menu first and keep a Scene view open."); return; }
            Vector3 a = f.TransformPoint(eye), b = f.TransformPoint(look);
            var rot = Quaternion.LookRotation(b - a, Vector3.up);
            sv.orthographic = false;
            sv.LookAt(a + (b - a).normalized * 0.6f, rot, 0.25f, false, true);
            sv.Repaint();
        }

        // ------------------------------------------------------------------ helpers
        enum Snap { None, MinX, MaxX, MinZ, MaxZ }

        static Transform Frame(string quest, string name, float r, float aDeg, bool facesIn)
        {
            var root = GameObject.Find("Quest_Locations_Separated");
            if (!root) { Debug.LogError("Run HORSE > Swap In Separated Quest Locations first."); return null; }
            Transform ql = null;
            foreach (Transform c in root.transform) if (c.name.StartsWith(quest)) { ql = c; break; }
            if (!ql) { Debug.LogError("Can't find " + quest + " under Quest_Locations_Separated."); return null; }
            var old = ql.Find(name);
            if (old) Undo.DestroyObjectImmediate(old.gameObject);

            float a = aDeg * Mathf.Deg2Rad, rot = a + (facesIn ? -Mathf.PI / 2 : Mathf.PI / 2);
            float cs = Mathf.Cos(rot), sn = Mathf.Sin(rot);
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Furnish");
            go.transform.SetParent(ql, false);
            // Blender (x, y, z) -> Unity (-x, z, -y); building x axis -> (-cos, 0, -sin), y axis -> (sin, 0, -cos)
            go.transform.localPosition = new Vector3(-r * Mathf.Cos(a), Floor, -r * Mathf.Sin(a));
            go.transform.localRotation = Quaternion.LookRotation(new Vector3(sn, 0, -cs), Vector3.up);
            return go.transform;
        }

        static Transform Group(Transform f, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(f, false);
            return go.transform;
        }

        static void Finish(Transform f, string what)
        {
            int n = 0;
            foreach (var t in f.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<Light>()) continue;
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
                if (t.GetComponent<Renderer>()) n++;
            }
            Selection.activeGameObject = f.gameObject;
            EditorSceneManager.MarkSceneDirty(f.gameObject.scene);
            Debug.Log($"InteriorDresser: furnished the {what} ({n} renderers) under '{f.name}'.");
        }

        static GameObject Load(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!go) Debug.LogWarning("InteriorDresser: missing asset " + path);
            return go;
        }

        static Bounds Measure(GameObject go, Transform space)
        {
            bool has = false; var b = new Bounds();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                Bounds lb;
                if (r is SkinnedMeshRenderer sk) lb = sk.localBounds;
                else { var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue; lb = mf.sharedMesh.bounds; }
                var m = space.worldToLocalMatrix * r.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var p = m.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? lb.min.x : lb.max.x, (i & 2) == 0 ? lb.min.y : lb.max.y, (i & 4) == 0 ? lb.min.z : lb.max.z));
                    if (!has) { b = new Bounds(p, Vector3.zero); has = true; } else b.Encapsulate(p);
                }
            }
            return b;
        }

        static float Front(string path) => FrontOffset.TryGetValue(System.IO.Path.GetFileNameWithoutExtension(path), out var v) ? v : 0f;

        /// <summary>Place a model with its footprint centred on (x, y), its base at elev, facing yaw (0 = towards the back wall).</summary>
        static GameObject Put(Transform p, string path, float x, float y, float yaw, float elev = 0f, float fitH = 0f, float fitLen = 0f, Snap snap = Snap.None, float at = 0f)
        {
            var prefab = Load(path);
            if (!prefab) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, p);
            Undo.RegisterCreatedObjectUndo(go, "Furnish");
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0, yaw + Front(path), 0);
            var b = Measure(go, p);
            // Only rescale models whose imported size is clearly wrong (e.g. centimetre exports).
            if (fitH > 0 && b.size.y > 1e-4f) { float k = fitH / b.size.y; if (k < 0.6f || k > 1.6f) go.transform.localScale *= k; }
            if (fitLen > 0) { float L = Mathf.Max(b.size.x, b.size.y, b.size.z); if (L > 1e-4f) { float k = fitLen / L; if (k < 0.6f || k > 1.6f) go.transform.localScale *= k; } }
            Align(go, p, x, y, elev, snap, at);
            return go;
        }

        static void Align(GameObject go, Transform p, float x, float y, float elev, Snap snap, float at, bool centreY = false)
        {
            var b = Measure(go, p);
            var off = new Vector3(x - b.center.x, centreY ? elev - b.center.y : elev - b.min.y, y - b.center.z);
            if (snap == Snap.MinX) off.x = at - b.min.x;
            if (snap == Snap.MaxX) off.x = at - b.max.x;
            if (snap == Snap.MinZ) off.z = at - b.min.z;
            if (snap == Snap.MaxZ) off.z = at - b.max.z;
            go.transform.localPosition += off;
        }

        /// <summary>Gun with its barrel running along the given axis, centred at height h. flat = lying on its side.</summary>
        static GameObject Gun(Transform p, string path, char along, float x, float y, float h, float len, Snap snap = Snap.None, float at = 0f, bool flat = false)
        {
            var prefab = Load(path);
            if (!prefab) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, p);
            Undo.RegisterCreatedObjectUndo(go, "Gun");
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            var b = Measure(go, p);
            bool longX = b.size.x >= b.size.z;
            float yaw = along == 'x' ? (longX ? 0f : 90f) : (longX ? 90f : 0f);
            var q = Quaternion.Euler(0, yaw, 0);
            if (flat) q = Quaternion.AngleAxis(90f, along == 'x' ? Vector3.right : Vector3.forward) * q;
            go.transform.localRotation = q;
            b = Measure(go, p);
            float L = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (L > 1e-4f) go.transform.localScale *= len / L; // guns are scaled to an exact display length
            Align(go, p, x, y, h, snap, at, centreY: !flat);
            return go;
        }

        static void StackOn(Transform p, GameObject shelf, string path, int count, float fitLen)
        {
            var b = Measure(shelf, p);
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                bool alongZ = b.size.z > b.size.x;
                float x = alongZ ? b.center.x : Mathf.Lerp(b.min.x + 0.1f, b.max.x - 0.1f, t);
                float y = alongZ ? Mathf.Lerp(b.min.z + 0.1f, b.max.z - 0.1f, t) : b.center.z;
                Put(p, path, x, y, alongZ ? 90f : 0f, elev: b.max.y, fitLen: fitLen);
            }
        }

        static GameObject Box(Transform p, string name, Vector3 centre, Vector3 size, Material m, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(go, "Box");
            go.name = name;
            go.transform.SetParent(p, false);
            go.transform.localPosition = centre;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void Workstation(Transform p, string desk, float x, float y, float face, int monitors)
        {
            // face = direction the person looks (0 = towards back wall, 180 = towards the street)
            float r = face * Mathf.Deg2Rad;
            var d = new Vector3(Mathf.Sin(r), 0, Mathf.Cos(r));
            var right = new Vector3(d.z, 0, -d.x);
            var dk = Put(p, desk, x, y, face + 180f, fitH: 0.76f);
            if (!dk) return;
            float top = Measure(dk, p).max.y;
            Put(p, VNB + "Office/office_chair.prefab", x - d.x * 0.85f, y - d.z * 0.85f, face, fitH: 1.0f);
            for (int i = 0; i < monitors; i++)
            {
                float o = (i - (monitors - 1) / 2f) * 0.58f;
                Put(p, NGE + "monitor.prefab", x + d.x * 0.18f + right.x * o, y + d.z * 0.18f + right.z * o, face + 180f, elev: top, fitH: 0.45f);
            }
            Put(p, NGE + "keyboard.prefab", x - d.x * 0.15f, y - d.z * 0.15f, face + 180f, elev: top, fitLen: 0.45f);
            Put(p, NGE + "mouse.prefab", x - d.x * 0.15f + right.x * 0.35f, y - d.z * 0.15f + right.z * 0.35f, face + 180f, elev: top, fitLen: 0.11f);
        }

        static Material Mat(string name, Color c, float smooth, bool glass = false, bool emissive = false)
        {
            System.IO.Directory.CreateDirectory(GEN);
            string path = GEN + "M_" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            if (glass)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 3f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
