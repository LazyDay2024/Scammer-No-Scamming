using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ScammerNoScamming;

public static class PlayerSandboxBuilder
{
    const string ScenePath = "Assets/Scenes/PlayerSandbox.unity";
    const string MaterialFolder = "Assets/Materials";

    [MenuItem("Tools/Scammer No Scamming/Build Player Sandbox Scene")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets", "Materials");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var world = PlayerSandbox.Create(MaterialFor);
        world.Player.GetComponent<PlayerWeaponController>().tracerMaterial = GetMaterial("Tracer", new Color(1f, 0.9f, 0.4f), "Universal Render Pipeline/Unlit");

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Player sandbox scene saved to " + ScenePath);
    }

    static Material MaterialFor(Color color)
    {
        return GetMaterial("Sandbox_" + ColorUtility.ToHtmlStringRGB(color), color, "Universal Render Pipeline/Lit");
    }

    static Material GetMaterial(string name, Color color, string shaderName)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        material = new Material(Shader.Find(shaderName));
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
