using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using ScammerNoScamming;

public static class CitySceneBuilder
{
    const string ScenePath = "Assets/Scenes/CitySandbox.unity";
    const string MaterialFolder = "Assets/Materials/City";

    [MenuItem("Tools/Scammer No Scamming/Build City Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var mats = new CityMaterials
        {
            TowerA = Load("Mat_facade_tower_a"),
            TowerB = Load("Mat_facade_tower_b"),
            Mid = Load("Mat_facade_mid"),
            Roof = Load("Mat_roof_house"),
            Ground = Load("Mat_road_dirt"),
            Flat = FlatMaterial,
        };

        // Dirt texture covers 8 m per repeat across the whole ground plane.
        if (mats.Ground != null)
            mats.Ground.SetTextureScale("_BaseMap", Vector2.one * (CityBuilder.GroundSize / 8f));

        var city = CityBuilder.Build(mats);

        SetUpLighting();

        var player = PlayerSandbox.CreatePlayer();
        player.transform.position = new Vector3(0f, 0.05f, -62f);
        var camera = player.GetComponentInChildren<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.farClipPlane = 600f;
        var horse = PlayerSandbox.CreateHorse(FlatMaterial);
        horse.transform.position = new Vector3(4f, 0.05f, -60f);

        MarkStatic(city.Root);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"City scene saved to {ScenePath}: {city.Towers.Count} towers, {city.MidRise.Count} mid-rise, " +
                  $"{city.Houses.Count} houses, {city.Cars.Count} cars");
    }

    static void SetUpLighting()
    {
        var lightObject = new GameObject("Sun");
        lightObject.transform.rotation = Quaternion.Euler(22f, -140f, 0f);
        var sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.72f, 0.5f);
        sun.intensity = 1.3f;
        RenderSettings.sun = sun;

        RenderSettings.skybox = Load("Mat_sky_dusk");
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.42f, 0.4f);

        // Dust haze hides the edge of the city and keeps far geometry cheap to read.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.85f, 0.55f, 0.35f);
        RenderSettings.fogStartDistance = 140f;
        RenderSettings.fogEndDistance = 460f;
    }

    static void MarkStatic(GameObject root)
    {
        const StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;
        foreach (var t in root.GetComponentsInChildren<Transform>())
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
    }

    static Material Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
    }

    static Material FlatMaterial(Color color)
    {
        string path = $"{MaterialFolder}/Flat_{ColorUtility.ToHtmlStringRGB(color)}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.1f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
