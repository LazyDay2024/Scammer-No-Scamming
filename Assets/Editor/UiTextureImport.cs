using UnityEditor;

// The map and minimap art are sampled by UV at their edges (minimap window, picture over the map): clamp, no mipmaps.
public class UiTextureImport : AssetPostprocessor
{
    static readonly string[] Paths = { "Assets/Art/UI/CityMap.png", "Assets/Resources/UI/minimap_frame.png", "Assets/Resources/UI/player_marker.png" };

    void OnPreprocessTexture()
    {
        if (assetPath == "Assets/Art/Sky/sky_dusk_4k.png")
        {
            // Equirectangular sky: wraps around horizontally, never vertically; the web build gets the 2k size.
            var sky = (TextureImporter)assetImporter;
            sky.textureType = TextureImporterType.Default;
            sky.wrapModeU = UnityEngine.TextureWrapMode.Repeat;
            sky.wrapModeV = UnityEngine.TextureWrapMode.Clamp;
            sky.mipmapEnabled = false;
            sky.filterMode = UnityEngine.FilterMode.Bilinear;
            sky.maxTextureSize = 4096;
            sky.textureCompression = TextureImporterCompression.CompressedHQ;
            var web = sky.GetPlatformTextureSettings("WebGL");
            web.overridden = true;
            web.maxTextureSize = 2048;
            web.format = TextureImporterFormat.Automatic;
            web.textureCompression = TextureImporterCompression.CompressedHQ;
            sky.SetPlatformTextureSettings(web);
            return;
        }
        if (System.Array.IndexOf(Paths, assetPath) < 0) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
    }
}
