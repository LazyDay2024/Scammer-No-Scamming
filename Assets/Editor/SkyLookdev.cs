using UnityEditor;
using UnityEngine;
using ScammerNoScamming;

// Photographic skies (Poly Haven CC0 HDRIs) with the sun light aligned to the sun in the image, so shadows
// fall the way the sky says they should. Use the menu to switch and compare in the open scene.
public static class SkyLookdev
{
    // name, sun azimuth (deg from image centre), sun elevation, ambient sky, horizon, fog colour
    struct Sky { public string name; public float az, el; public Color sky, horizon, fog; public float exposure; }
    static readonly Sky[] Skies = {
        new Sky { name = "evening_road_01_puresky", az = 37.1f, el = 16.6f, sky = new Color(.5f, .58f, .68f), horizon = new Color(.66f, .6f, .52f), fog = new Color(.7f, .64f, .56f), exposure = .75f },
        new Sky { name = "belfast_sunset_puresky", az = 37.8f, el = 7f, sky = new Color(.55f, .5f, .62f), horizon = new Color(.8f, .58f, .4f), fog = new Color(.78f, .6f, .48f), exposure = .9f },
        new Sky { name = "industrial_sunset_02_puresky", az = 35.9f, el = 6f, sky = new Color(.38f, .5f, .68f), horizon = new Color(.72f, .55f, .45f), fog = new Color(.66f, .6f, .58f), exposure = 1f },
    };

    [MenuItem("Tools/Scammer No Scamming/Sky/Evening Road (default)")] static void A() => Apply(0);
    [MenuItem("Tools/Scammer No Scamming/Sky/Belfast Sunset")] static void B() => Apply(1);
    [MenuItem("Tools/Scammer No Scamming/Sky/Industrial Sunset")] static void C() => Apply(2);

    public static void Apply(int index)
    {
        var sky = Skies[index];
        string texPath = $"Assets/Art/Sky/{sky.name}.hdr";
        var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
        if (importer != null && (importer.mipmapEnabled || importer.maxTextureSize < 4096 || importer.wrapModeU != TextureWrapMode.Repeat))
        {
            importer.mipmapEnabled = false;               // mips on a lat-long map show a seam at the back
            importer.maxTextureSize = 4096;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        WorldSceneArt.EnsureFolder("Assets/Art/Sky");
        string matPath = $"Assets/Art/Sky/Sky_{sky.name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material == null) { material = new Material(Shader.Find("Skybox/Panoramic")); AssetDatabase.CreateAsset(material, matPath); }
        material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        material.SetFloat("_Mapping", 1f);      // latitude-longitude
        material.SetFloat("_ImageType", 0f);    // 360
        material.SetFloat("_Exposure", sky.exposure);
        material.SetFloat("_Rotation", 0f);
        EditorUtility.SetDirty(material);
        RenderSettings.skybox = material;

        // Panoramic maps a direction d to u = 0.5 - atan2(d.z, d.x) / 2pi, so the image's sun sits at atan2(z, x) = -az.
        float a = -sky.az * Mathf.Deg2Rad, e = sky.el * Mathf.Deg2Rad;
        var toSun = new Vector3(Mathf.Cos(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Sin(a) * Mathf.Cos(e));
        if (RenderSettings.sun != null)
        {
            RenderSettings.sun.transform.rotation = Quaternion.LookRotation(-toSun);
            RenderSettings.sun.color = Color.Lerp(new Color(1f, .78f, .55f), new Color(1f, .93f, .82f), Mathf.InverseLerp(4f, 25f, sky.el));
            RenderSettings.sun.intensity = Mathf.Lerp(1.4f, 2.2f, Mathf.InverseLerp(4f, 25f, sky.el));
        }
        var atmosphere = Object.FindAnyObjectByType<FrontierAtmosphere>();
        if (atmosphere != null)
        {
            atmosphere.sky = sky.sky; atmosphere.horizon = sky.horizon; atmosphere.ground = new Color(.32f, .25f, .18f);
            atmosphere.Apply();
            EditorUtility.SetDirty(atmosphere);
        }
        RenderSettings.fogColor = sky.fog;
        // HDR sky: no extra post exposure, a little more contrast so the light reads as late afternoon.
        var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/Settings/FrontierLook.asset");
        if (profile != null && profile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments color))
        {
            color.postExposure.Override(-.15f);
            color.contrast.Override(18f);
            color.saturation.Override(-4f);
            EditorUtility.SetDirty(profile);
        }
        Debug.Log($"Sky {sky.name}: sun pitch {sky.el} toward {toSun}");
    }
}
