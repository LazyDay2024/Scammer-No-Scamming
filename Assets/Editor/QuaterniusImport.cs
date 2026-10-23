using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Import rules for the CC0 Quaternius models in Assets/ThirdParty/Quaternius:
// characters and the horse use the Generic rig so their clips play on an Animator/Playable graph,
// and looping clips (idle, walk, run...) are marked to loop.
public class QuaterniusImport : AssetPostprocessor
{
    const string Root = "Assets/ThirdParty/Quaternius/";
    static readonly string[] Looping = { "idle", "walk", "run", "gallop", "eating", "sit", "wave", "dance" };

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Root)) return;
        var importer = (ModelImporter)assetImporter;
        bool animated = assetPath.Contains("/Characters/") || assetPath.Contains("/Animals/");
        importer.animationType = animated ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
        importer.importAnimation = animated;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
    }

    void OnPreprocessAnimation()
    {
        if (!assetPath.StartsWith(Root)) return;
        var importer = (ModelImporter)assetImporter;
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            string n = clip.name.ToLowerInvariant();
            clip.loopTime = Looping.Any(n.Contains) && !n.Contains("death");
        }
        importer.clipAnimations = clips;
    }

    [MenuItem("Tools/Scammer No Scamming/Dump Quaternius Clips")]
    public static void Dump()
    {
        foreach (var path in Directory.GetFiles(Root, "*.fbx", SearchOption.AllDirectories))
        {
            var p = path.Replace('\\', '/');
            if (!p.Contains("/Characters/") && !p.Contains("/Animals/")) continue;
            var clips = AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).Select(c => $"{c.name}({c.length:0.0}s{(c.isLooping ? ",loop" : "")})");
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            var renderer = go != null ? go.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            var bounds = renderer != null ? renderer.bounds.size : Vector3.zero;
            Debug.Log($"CLIPS {Path.GetFileNameWithoutExtension(p)} size={bounds} scale={go?.transform.localScale}: {string.Join(", ", clips)}");
        }
    }
}
