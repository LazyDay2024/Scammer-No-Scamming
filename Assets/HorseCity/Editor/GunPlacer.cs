using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HorseCity.EditorTools
{
    /// <summary>
    /// Puts gun models from the "Low Poly Weapons VOL.1" pack (or any folder with that name) into
    /// a landmark's gun slots. Each gun is laid flat or stood up based on its own bounds, so it
    /// works without knowing how the pack's models are oriented.
    /// </summary>
    public static class GunPlacer
    {
        const string PackFolderName = "Low Poly Weapons VOL.1";
        static readonly string[] Skip = { "bullet", "shell", "casing", "muzzle", "flash", "fx", "particle", "demo", "scene", "mag", "ammo" };

        /// <summary>Gun assets found in the project, longest first. Empty if the pack is missing.</summary>
        public static List<GameObject> FindGuns()
        {
            string folder = FindPackFolder();
            if (folder == null) return new List<GameObject>();

            List<GameObject> Load(string filter) => AssetDatabase.FindAssets(filter, new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(go => go != null && !Skip.Any(k => go.name.ToLowerInvariant().Contains(k)))
                .Where(go => go.GetComponentsInChildren<Renderer>(true).Length > 0)
                .Distinct()
                .ToList();

            List<GameObject> guns = Load("t:Prefab");
            if (guns.Count == 0) guns = Load("t:Model");
            return guns.OrderByDescending(g => LocalBounds(g).size.magnitude).ToList();
        }

        static string FindPackFolder()
        {
            string direct = "Assets/" + PackFolderName;
            if (AssetDatabase.IsValidFolder(direct)) return direct;
            foreach (string guid in AssetDatabase.FindAssets("t:Folder"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileName(p) == PackFolderName) return p;
            }
            foreach (string guid in AssetDatabase.FindAssets("Low Poly Weapons"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(p)) return p;
            }
            return null;
        }

        /// <summary>
        /// Instantiates guns into the slots under parent (the landmark root). Rifles go in the tall
        /// upright slots first, the rest get spread over the flat slots.
        /// </summary>
        public static int Place(Transform parent, List<GunSlot> slots, List<GameObject> guns)
        {
            if (guns.Count == 0 || slots.Count == 0) return 0;
            Transform holder = new GameObject("Guns").transform;
            holder.SetParent(parent, false);

            int n = 0, flatIndex = guns.Count - 1, uprightIndex = 0;
            foreach (GunSlot slot in slots)
            {
                // long guns for the wall cases, short ones for the counters and table
                GameObject asset = slot.upright ? guns[uprightIndex++ % guns.Count] : guns[Mod(flatIndex--, guns.Count)];
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, holder);
                if (go == null) go = Object.Instantiate(asset, holder);
                go.name = asset.name;
                Fit(go.transform, slot);
                n++;
            }
            return n;
        }

        static int Mod(int a, int m) => ((a % m) + m) % m;

        static void Fit(Transform t, GunSlot slot)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            Vector3 baseScale = t.localScale;

            Bounds lb = LocalBounds(t.gameObject);
            Vector3 size = lb.size;
            // rank the gun's own axes by length: long (barrel), mid (grip height), thin (width)
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
            float[] len = { size.x, size.y, size.z };
            int[] order = { 0, 1, 2 };
            System.Array.Sort(order, (a, b) => len[b].CompareTo(len[a]));
            Vector3 aLong = axes[order[0]], aThin = axes[order[2]];
            float longest = Mathf.Max(0.0001f, len[order[0]]);

            // packs come in all sorts of units: bring anything silly back to real gun sizes
            float scale = 1f;
            if (longest > 2.5f || longest < 0.1f) scale = 0.8f / longest;
            if (longest * scale > slot.maxLength) scale = slot.maxLength / longest;
            t.localScale = baseScale * scale;

            // map long axis to the slot's x (flat) or y (upright), thin axis to up (flat) or out (upright)
            Quaternion basis = slot.upright
                ? Quaternion.LookRotation(aThin, aLong)
                : Quaternion.LookRotation(Vector3.Cross(aLong, aThin), aThin);
            Quaternion slotRot = Quaternion.Euler(0, slot.yaw, 0) * (slot.upright ? Quaternion.Euler(0, 180f, 0) : Quaternion.identity);
            t.localRotation = slotRot * Quaternion.Inverse(basis);

            // rest the bottom of the gun on the slot point
            Bounds wb = WorldBounds(t.gameObject);
            Vector3 target = t.parent.parent.TransformPoint(slot.pos);
            t.position += new Vector3(target.x - wb.center.x, target.y - wb.min.y + 0.005f, target.z - wb.center.z);
        }

        static Bounds LocalBounds(GameObject go)
        {
            // bounds in the root's own space, from mesh bounds so it works on unplaced prefab assets
            bool any = false;
            Bounds b = new Bounds();
            Matrix4x4 toRoot = go.transform.worldToLocalMatrix;
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Encapsulate(ref b, ref any, mf.sharedMesh.bounds, toRoot * mf.transform.localToWorldMatrix);
            }
            foreach (SkinnedMeshRenderer sr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (sr.sharedMesh == null) continue;
                Encapsulate(ref b, ref any, sr.sharedMesh.bounds, toRoot * sr.transform.localToWorldMatrix);
            }
            return any ? b : new Bounds(Vector3.zero, Vector3.one * 0.5f);
        }

        static Bounds WorldBounds(GameObject go)
        {
            bool any = false;
            Bounds b = new Bounds();
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null) Encapsulate(ref b, ref any, mf.sharedMesh.bounds, mf.transform.localToWorldMatrix);
            foreach (SkinnedMeshRenderer sr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (sr.sharedMesh != null) Encapsulate(ref b, ref any, sr.sharedMesh.bounds, sr.transform.localToWorldMatrix);
            return any ? b : new Bounds(go.transform.position, Vector3.one * 0.5f);
        }

        static void Encapsulate(ref Bounds b, ref bool any, Bounds mb, Matrix4x4 m)
        {
            Vector3 c = mb.center, e = mb.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = m.MultiplyPoint3x4(c + Vector3.Scale(e, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                else b.Encapsulate(p);
            }
        }
    }
}
