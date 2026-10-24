using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HorseCity.EditorTools
{
    /// <summary>
    /// Replaces the merged quest buildings inside "_old_HORSE_City_merged" with the separated
    /// exports (QL_*.fbx), where every wall, floor and piece of furniture is its own object.
    /// The old merged pieces are only switched off, never deleted, and the whole thing is undoable.
    /// </summary>
    public static class QuestLocationSwap
    {
        const string ExportFolder = "Assets/Horse/Export/";
        const string RootName = "Quest_Locations_Separated";
        static readonly string[] Files = { "QL_Police", "QL_PoliceStable", "QL_Auction", "QL_Vet", "QL_WeaponShop" };

        [MenuItem("HORSE/Swap In Separated Quest Locations")]
        static void Swap()
        {
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            int disabled = 0;

            var old = GameObject.Find("_old_HORSE_City_merged");
            if (old)
            {
                pos = old.transform.position;
                rot = old.transform.rotation;
                foreach (var t in old.GetComponentsInChildren<Transform>(true))
                {
                    if (t == old.transform) continue;
                    string n = t.name;
                    bool quest = n.StartsWith("QL_") || n.StartsWith("WeaponShop_Neighbors");
                    if (!quest || !t.gameObject.activeSelf) continue;
                    Undo.RecordObject(t.gameObject, "Disable merged quest building");
                    t.gameObject.SetActive(false);
                    disabled++;
                }
            }

            var existing = GameObject.Find(RootName);
            if (existing) Undo.DestroyObjectImmediate(existing);

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Add separated quest locations");
            int added = 0;
            foreach (var f in Files)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ExportFolder + f + ".fbx");
                if (!asset) { Debug.LogWarning("QuestLocationSwap: missing " + ExportFolder + f + ".fbx"); continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                inst.transform.SetParent(root.transform, false);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;
                added++;
            }
            root.transform.SetPositionAndRotation(pos, rot);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log($"QuestLocationSwap: switched off {disabled} merged quest objects, added {added} separated quest files under '{RootName}'.");
        }
    }
}
