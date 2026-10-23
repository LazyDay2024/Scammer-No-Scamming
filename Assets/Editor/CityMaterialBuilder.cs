using UnityEditor;
using UnityEngine;

// Sets import settings on the city textures and creates a material for each.
// Tiling stays at 1x1: the city builder sets it per building so windows keep their proportions.
public static class CityMaterialBuilder
{
    const string TextureFolder = "Assets/Art/Textures";
    const string MaterialFolder = "Assets/Materials/City";

    static readonly string[] TiledTextures =
    {
        "tex_facade_tower_a", "tex_facade_tower_b", "tex_facade_mid", "tex_roof_house", "tex_road_dirt"
    };

    [MenuItem("Tools/Scammer No Scamming/Create City Materials")]
    public static void Run()
    {
        EnsureFolder("Assets/Materials", "City");

        foreach (var name in TiledTextures)
        {
            ConfigureTexture(name, 1024, TextureWrapMode.Repeat, true);
            CreateLitMaterial(name);
        }

        ConfigureTexture("tex_sky_dusk", 2048, TextureWrapMode.Repeat, false);
        CreateSkyMaterial("tex_sky_dusk");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("City materials created in " + MaterialFolder);
    }

    static void ConfigureTexture(string name, int maxSize, TextureWrapMode wrap, bool mipmaps)
    {
        string path = $"{TextureFolder}/{name}.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null)
        {
            Debug.LogError("Missing texture " + path);
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.wrapMode = wrap;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = mipmaps;
        importer.anisoLevel = mipmaps ? 4 : 1;
        importer.maxTextureSize = maxSize;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.crunchedCompression = true; // smaller WebGL build
        importer.compressionQuality = 60;
        importer.SaveAndReimport();
    }

    static void CreateLitMaterial(string textureName)
    {
        string materialPath = $"{MaterialFolder}/{textureName.Replace("tex_", "Mat_")}.mat";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{textureName}.png");
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(material);
    }

    static void CreateSkyMaterial(string textureName)
    {
        string materialPath = $"{MaterialFolder}/Mat_sky_dusk.mat";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{textureName}.png");
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Skybox/Panoramic"));
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.SetTexture("_MainTex", texture);
        material.SetFloat("_Exposure", 1f);
        EditorUtility.SetDirty(material);
    }

    static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            AssetDatabase.CreateFolder(parent, child);
    }
}
