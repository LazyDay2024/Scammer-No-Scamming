using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Lays every imported CC0 model out in rows (with its size label) in a throwaway scene, so we can check
// scale, facing and materials before placing them in the level. Saves a top-down and a side shot.
public static class AssetLineup
{
    [MenuItem("Tools/Scammer No Scamming/Asset Lineup")]
    public static void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var folders = new[] { "Assets/ThirdParty/OldWest", "Assets/ThirdParty/WesternObjects", "Assets/ThirdParty/Quaternius/Nature" };
        float z = 0f;
        foreach (var folder in folders)
        {
            float x = 0f, rowDepth = 0f;
            var paths = Directory.GetFiles(folder).Where(p => p.EndsWith(".obj") || p.EndsWith(".fbx")).Select(p => p.Replace('\\', '/')).OrderBy(p => p);
            foreach (var path in paths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.position = Vector3.zero;
                var renderers = go.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                go.transform.position = new Vector3(x - b.min.x, -b.min.y, z - b.min.z);
                var label = new GameObject("Label").AddComponent<TextMesh>();
                label.text = Path.GetFileNameWithoutExtension(path) + $"\n{b.size.x:0.0}x{b.size.y:0.0}x{b.size.z:0.0}";
                label.characterSize = .12f; label.fontSize = 32; label.anchor = TextAnchor.UpperLeft;
                label.transform.position = new Vector3(x, .02f, z - .3f);
                label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                x += b.size.x + 1.5f;
                rowDepth = Mathf.Max(rowDepth, b.size.z);
                Debug.Log($"LINEUP {path} size={b.size}");
            }
            z += rowDepth + 4f;
        }
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = new Vector3(30f, 1f, 30f);
        ground.transform.position = new Vector3(100f, -.01f, 50f);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/AssetLineup.unity");
    }
}
