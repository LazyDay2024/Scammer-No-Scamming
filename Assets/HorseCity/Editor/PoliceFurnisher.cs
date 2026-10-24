using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HorseCity.EditorTools
{
    /// <summary>
    /// Furnishes the police station (separated QL_Police export) with Sci-Fi Office Lite props:
    /// lobby front desk, chief's office, records room, two interrogation rooms, a control room
    /// with a video wall, an evidence/server room and a guard post at the cells.
    /// Re-running replaces the previous furnishing. Undoable.
    /// Each prop is scaled so its largest horizontal size matches 'fit' and dropped onto the floor
    /// (or onto the desk it sits on). 'yaw' points the prop's +Z (front) the way it should face;
    /// if a model faces the other way, change FrontYawOffset below and re-run.
    /// </summary>
    public static class PoliceFurnisher
    {
        const string GroupName = "Police_Furnishing";
        const float FloorY = 0.17f;              // police floor height inside the QL_Police export
        public static float FrontYawOffset = 0f; // flip to 180 if the asset pack faces -Z

        static readonly Dictionary<string, float> YawFix = new Dictionary<string, float>
        {
            // per-prefab corrections (degrees), filled after checking in the scene
        };

        struct P
        {
            public string room, prefab; public float x, z, yaw, fit, lift; public int stackOn;
            public P(string room, string prefab, float x, float z, float yaw, float fit, int stackOn, float lift)
            { this.room = room; this.prefab = prefab; this.x = x; this.z = z; this.yaw = yaw; this.fit = fit; this.stackOn = stackOn; this.lift = lift; }
        }

        static readonly P[] Items =
        {
            new P("Lobby", "Assets/ScifiOfficeLite/Prefabs/Tables/White Wood/Drawer Table Long White Wood Variant.prefab", 163.155f, 110.049f, -124.0f, 2.4f, -1, 0.0f),
            new P("Lobby", "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/PC 2.prefab", 163.600f, 109.746f, -124.0f, 0.6f, 0, 0.0f),
            new P("Lobby", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair.prefab", 164.149f, 110.720f, -124.0f, 0.7f, -1, 0.0f),
            new P("Lobby", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair Variant.prefab", 157.598f, 111.850f, 146.0f, 0.65f, -1, 0.0f),
            new P("Lobby", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair Variant.prefab", 158.157f, 111.021f, 146.0f, 0.65f, -1, 0.0f),
            new P("Lobby", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair Variant.prefab", 162.183f, 105.052f, -34.0f, 0.65f, -1, 0.0f),
            new P("Lobby", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair Variant.prefab", 162.742f, 104.222f, -34.0f, 0.65f, -1, 0.0f),
            new P("Lobby", "Assets/ScifiOfficeLite/Prefabs/Drawers/Vase.prefab", 162.290f, 115.980f, -124.0f, 0.5f, -1, 0.0f),
            new P("Lobby", "Assets/ScifiOfficeLite/Prefabs/Drawers/Vase.prefab", 168.330f, 107.026f, -124.0f, 0.5f, -1, 0.0f),
            new P("ChiefOffice", "Assets/ScifiOfficeLite/Prefabs/Tables/Dark Oak/Table Dark Oak.prefab", 155.284f, 119.215f, 56.0f, 1.8f, -1, 0.0f),
            new P("ChiefOffice", "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/PC 2.prefab", 154.894f, 119.435f, 56.0f, 0.6f, 9, 0.0f),
            new P("ChiefOffice", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair.prefab", 154.206f, 118.488f, 56.0f, 0.7f, -1, 0.0f),
            new P("ChiefOffice", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair Variant.prefab", 156.192f, 120.551f, -124.0f, 0.65f, -1, 0.0f),
            new P("ChiefOffice", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair Variant.prefab", 156.863f, 119.556f, -124.0f, 0.65f, -1, 0.0f),
            new P("ChiefOffice", "Assets/ScifiOfficeLite/Prefabs/Drawers/Drawer 2 Oak Variant.prefab", 155.193f, 125.788f, 146.0f, 0.7f, -1, 0.0f),
            new P("ChiefOffice", "Assets/ScifiOfficeLite/Prefabs/Shelf with Crates.prefab", 151.876f, 123.551f, 146.0f, 1.8f, -1, 0.0f),
            new P("Records", "Assets/ScifiOfficeLite/Prefabs/Tables/Grey Wood/Drawer Table Long Grey Wood Variant.prefab", 165.120f, 100.519f, 56.0f, 2.2f, -1, 0.0f),
            new P("Records", "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/PC 2.prefab", 165.120f, 100.519f, 56.0f, 0.6f, 16, 0.0f),
            new P("Records", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair.prefab", 166.115f, 101.190f, -124.0f, 0.7f, -1, 0.0f),
            new P("Records", "Assets/ScifiOfficeLite/Prefabs/Tables/Grey Wood/Drawer Table Long Grey Wood Variant.prefab", 168.475f, 95.545f, 56.0f, 2.2f, -1, 0.0f),
            new P("Records", "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/PC 2.prefab", 168.475f, 95.545f, 56.0f, 0.6f, 19, 0.0f),
            new P("Records", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair.prefab", 169.470f, 96.216f, -124.0f, 0.7f, -1, 0.0f),
            new P("Records", "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/Server Rack.prefab", 174.543f, 96.742f, -34.0f, 0.8f, -1, 0.0f),
            new P("Records", "Assets/ScifiOfficeLite/Prefabs/Shelf without Crates.prefab", 167.426f, 104.969f, 146.0f, 1.8f, -1, 0.0f),
            new P("Interrogation1", "Assets/ScifiOfficeLite/Prefabs/Tables/Metal/Table Metal Variant.prefab", 167.368f, 130.984f, 56.0f, 1.4f, -1, 0.0f),
            new P("Interrogation1", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair.prefab", 166.290f, 130.258f, 56.0f, 0.7f, -1, 0.0f),
            new P("Interrogation1", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Stool.prefab", 168.446f, 131.711f, -124.0f, 0.45f, -1, 0.0f),
            new P("Interrogation2", "Assets/ScifiOfficeLite/Prefabs/Tables/Metal/Table Metal Variant.prefab", 170.723f, 126.010f, 56.0f, 1.4f, -1, 0.0f),
            new P("Interrogation2", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair.prefab", 169.645f, 125.283f, 56.0f, 0.7f, -1, 0.0f),
            new P("Interrogation2", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Stool.prefab", 171.801f, 126.737f, -124.0f, 0.45f, -1, 0.0f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/TVs/Tv 32 Inch.prefab", 179.721f, 126.530f, -124.0f, 2.0f, -1, 1.15f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/TVs/Tv 32 Inch.prefab", 181.063f, 124.541f, -124.0f, 2.0f, -1, 1.15f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/TVs/Tv 32 Inch.prefab", 182.405f, 122.551f, -124.0f, 2.0f, -1, 1.15f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/TVs/Tv 32 Inch.prefab", 179.721f, 126.530f, -124.0f, 2.0f, -1, 2.35f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/TVs/Tv 32 Inch.prefab", 181.063f, 124.541f, -124.0f, 2.0f, -1, 2.35f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/TVs/Tv 32 Inch.prefab", 182.405f, 122.551f, -124.0f, 2.0f, -1, 2.35f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/Tables/White Wood/Drawer Table Long White Wood Variant.prefab", 177.622f, 122.220f, 56.0f, 3.4f, -1, 0.0f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/PC 2.prefab", 177.175f, 122.883f, 56.0f, 0.6f, 36, 0.0f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/PC 2.prefab", 178.069f, 121.557f, 56.0f, 0.6f, 36, 0.0f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair.prefab", 176.263f, 122.268f, 56.0f, 0.7f, -1, 0.0f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair.prefab", 177.158f, 120.942f, 56.0f, 0.7f, -1, 0.0f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/Tables/White Wood/Drawer Table Long White Wood Variant.prefab", 174.306f, 119.983f, 56.0f, 3.4f, -1, 0.0f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/PC 2.prefab", 173.859f, 120.647f, 56.0f, 0.6f, 41, 0.0f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/PC 2.prefab", 174.753f, 119.320f, 56.0f, 0.6f, 41, 0.0f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair.prefab", 172.947f, 120.031f, 56.0f, 0.7f, -1, 0.0f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair.prefab", 173.841f, 118.705f, 56.0f, 0.7f, -1, 0.0f),
            new P("ControlRoom", "Assets/ScifiOfficeLite/Prefabs/Drawers/Drawer 2.prefab", 169.658f, 112.747f, -34.0f, 0.7f, -1, 0.0f),
            new P("Evidence", "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/Server Rack.prefab", 183.391f, 117.065f, -34.0f, 0.8f, -1, 0.0f),
            new P("Evidence", "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/Server Rack.prefab", 182.562f, 116.506f, -34.0f, 0.8f, -1, 0.0f),
            new P("Evidence", "Assets/ScifiOfficeLite/Prefabs/Shelf with Crates.prefab", 176.325f, 115.917f, 146.0f, 1.8f, -1, 0.0f),
            new P("Evidence", "Assets/ScifiOfficeLite/Prefabs/Shelf with Crates.prefab", 173.838f, 114.240f, 146.0f, 1.8f, -1, 0.0f),
            new P("Evidence", "Assets/ScifiOfficeLite/Prefabs/Drawers/Drawer 2 Oak Variant.prefab", 175.101f, 111.473f, -34.0f, 0.7f, -1, 0.0f),
            new P("CellBlock", "Assets/ScifiOfficeLite/Prefabs/Tables/Grey Wood/Table Grey Wood Variant.prefab", 179.150f, 106.364f, 56.0f, 1.6f, -1, 0.0f),
            new P("CellBlock", "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/PC 2.prefab", 179.150f, 106.364f, 56.0f, 0.6f, 52, 0.0f),
            new P("CellBlock", "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair.prefab", 178.155f, 105.693f, 56.0f, 0.7f, -1, 0.0f),
            new P("CellBlock", "Assets/ScifiOfficeLite/Prefabs/Shelf without Crates.prefab", 179.739f, 100.126f, -34.0f, 1.8f, -1, 0.0f),
        };

        [MenuItem("HORSE/Furnish Police Station")]
        static void Furnish()
        {
            Transform police = FindPoliceRoot();
            if (!police) { EditorUtility.DisplayDialog("Furnish Police Station", "Couldn't find QL_Police. Run HORSE > Swap In Separated Quest Locations first.", "OK"); return; }

            var old = police.Find(GroupName);
            if (old) Undo.DestroyObjectImmediate(old.gameObject);

            var group = new GameObject(GroupName);
            Undo.RegisterCreatedObjectUndo(group, "Furnish police station");
            group.transform.SetParent(police, false);

            var rooms = new Dictionary<string, Transform>();
            var placed = new List<GameObject>();
            int ok = 0;
            foreach (var it in Items)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(it.prefab);
                if (!asset) { Debug.LogWarning("PoliceFurnisher: missing " + it.prefab); placed.Add(null); continue; }

                if (!rooms.TryGetValue(it.room, out var room))
                {
                    room = new GameObject(it.room).transform;
                    room.SetParent(group.transform, false);
                    rooms[it.room] = room;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, room);
                float yaw = it.yaw + FrontYawOffset + (YawFix.TryGetValue(asset.name, out var f) ? f : 0f);
                go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                go.transform.localPosition = new Vector3(it.x, FloorY, it.z);

                // scale to the target footprint
                var b = WorldBounds(go);
                float size = Mathf.Max(b.size.x, b.size.z);
                if (it.fit > 0f && size > 0.001f)
                    go.transform.localScale *= it.fit / size;

                // drop onto the floor or onto the prop it stands on, centred on the target spot
                b = WorldBounds(go);
                float baseY = police.TransformPoint(new Vector3(0, FloorY, 0)).y;
                if (it.stackOn >= 0 && it.stackOn < placed.Count && placed[it.stackOn])
                    baseY = WorldBounds(placed[it.stackOn]).max.y;
                baseY += it.lift;
                Vector3 target = police.TransformPoint(new Vector3(it.x, FloorY, it.z));
                go.transform.position += new Vector3(target.x - b.center.x, baseY - b.min.y, target.z - b.center.z);

                placed.Add(go);
                ok++;
            }

            EditorSceneManager.MarkSceneDirty(group.scene);
            Selection.activeGameObject = group;
            Debug.Log($"PoliceFurnisher: placed {ok} props in {rooms.Count} rooms.");
        }

        static Transform FindPoliceRoot()
        {
            var sep = GameObject.Find("Quest_Locations_Separated");
            if (sep)
                foreach (Transform t in sep.transform)
                    if (t.name.StartsWith("QL_Police") && !t.name.Contains("Stable")) return t;
            return null;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
    }
}
