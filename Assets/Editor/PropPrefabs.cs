using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// The downloaded prop models (case folder, old map, ammo boxes) turned into ready-to-place prefabs: URP materials from
// their textures, scaled to a real-world size, pivot at the bottom centre. Rerunnable; builders call BuildAll first.
// Credits: see Assets/Models/Props/CREDITS.txt.
public static class PropPrefabs
{
    const string Root = "Assets/Models/Props";

    [MenuItem("Tools/Scammer No Scamming/Build Prop Prefabs")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(Root + "/Prefabs");
        Directory.CreateDirectory(Root + "/Materials");
        // name, model file, longest side in metres
        Make("AmmoBox", "AmmoBox/AmmoBox.fbx", .6f, (r, i, n) => Lit("AmmoBox", "AmmoBox/AmmoBox_D.png", "AmmoBox/AmmoBox_N.png", .5f, .45f));
        Make("AmmoBoxLarge", "AmmoBoxLarge/AmmoBoxLarge.fbx", 1f, (r, i, n) => Lit("AmmoBoxLarge", "AmmoBoxLarge/AmmoBoxLarge_D.png", "AmmoBoxLarge/AmmoBoxLarge_N.png", .5f, .45f));
        Make("CaseFolder", "CaseFolder/CaseFolder.obj", .62f, // the OBJ lists the four sheets first and the folder cover last; it may arrive as one renderer with five slots
            (r, i, n) => r.name.Contains("Folder_1") || (n > 1 && i == n - 1) ? Lit("CaseFolder_Cover", "CaseFolder/CaseFolder_tex3.jpg", null, 0f, .3f)
            : Lit("CaseFolder_Paper", "CaseFolder/CaseFolder_tex0.jpg", null, 0f, .15f));
        Make("OldMap", "OldMap/OldMap.obj", .85f, (r, i, n) => Lit("OldMap", "OldMap/OldMap_tex0.jpg", null, 0f, .1f));
        AssetDatabase.SaveAssets();
    }

    public static GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/{name}.prefab");

    static Material Lit(string name, string baseMap, string normalMap, float metallic, float smoothness)
    {
        string path = $"{Root}/Materials/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find("Universal Render Pipeline/Lit");
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/{baseMap}"));
        m.SetColor("_BaseColor", Color.white);
        if (normalMap != null)
        {
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/{normalMap}"));
            m.EnableKeyword("_NORMALMAP");
        }
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void Make(string name, string modelFile, float longestSide, System.Func<Renderer, int, int, Material> material)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/{modelFile}");
        if (model == null) throw new System.InvalidOperationException("Missing model " + modelFile);
        var root = new GameObject(name);
        var inner = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
        PrefabUtility.UnpackPrefabInstance(inner, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var r in inner.GetComponentsInChildren<Renderer>())
        {
            var slots = r.sharedMaterials;
            for (int i = 0; i < slots.Length; i++) slots[i] = material(r, i, slots.Length);
            r.sharedMaterials = slots;
        }
        var b = Bounds(root);
        float longest = Mathf.Max(b.size.x, b.size.z, b.size.y * 0.01f);
        float scale = longestSide / Mathf.Max(.0001f, Mathf.Max(b.size.x, b.size.z));
        inner.transform.localScale *= scale;
        b = Bounds(root);
        inner.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z); // bottom centre on the origin
        b = Bounds(root);
        var box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, b.size.y / 2f, 0f);
        box.size = b.size;
        PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/{name}.prefab");
        Debug.Log($"PropPrefabs {name}: size {b.size}");
        Object.DestroyImmediate(root);
    }

    static Bounds Bounds(GameObject root)
    {
        var rs = root.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}

// Props import without their own materials (PropPrefabs assigns URP ones) and with real normal maps.
public class PropImport : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/Models/Props/")) return;
        var importer = (ModelImporter)assetImporter;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.isReadable = false;
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Models/Props/") || !assetPath.EndsWith("_N.png")) return;
        ((TextureImporter)assetImporter).textureType = TextureImporterType.NormalMap;
    }
}
