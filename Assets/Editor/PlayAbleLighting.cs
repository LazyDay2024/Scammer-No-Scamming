using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Lighting pass for the open PlayAble scene (the city), matched to the skybox the scene uses: the sun light points
// where the sky's sun is, the ambient bands and the fog take the sky's own colours, and a cool fill comes from the
// side away from the sun so shadowed faces are not black. Every value is set absolutely, so running the menu again
// changes nothing more. The hideout interiors are lit by HideoutInteriors.
//
// To add a skybox: measure its sun (longitude u, elevation) and its horizon / zenith / nadir colours, then add a Look.
// Do not trust a formula for where the sun is: look up at the sky from above the roofs (EditorBridge: cam x 900 z pitch yaw fov, shot) and read
// the yaw where the sun shows; the light's yaw is that minus 180.
public static class PlayAbleLighting
{
    struct Look
    {
        public float sunPitch, sunYaw, sunIntensity;
        public Color sun;
        public float fillPitch, fillYaw, fillIntensity;
        public Color fill, ambientSky, ambientEquator, ambientGround, fog;
        public float ambientIntensity, fogDensity, exposure;
    }

    // "Sky_SiamDusk": Codex's teal-to-orange panorama; sun disc at lon 248, 6 degrees up.
    static readonly Look SiamDusk = new Look
    {
        sunPitch = 9f, sunYaw = 338f, sunIntensity = 1.3f, sun = new Color(1f, .6f, .36f),
        fillPitch = 38f, fillYaw = 158f, fillIntensity = .95f, fill = new Color(.4f, .75f, .8f),
        ambientSky = new Color(.3f, .56f, .6f), ambientEquator = new Color(.55f, .34f, .24f), ambientGround = new Color(.24f, .2f, .2f),
        ambientIntensity = 1.3f, fog = new Color(.62f, .4f, .3f), fogDensity = .0016f, exposure = .45f,
    };

    // "sky-2" (8K Skybox Pack Free, a Skybox/Cubemap): a red-orange sunset behind purple cloud banks. Its sun burst is at
    // u = .282 in the source image; measured in the editor (looking up from above the city) it shows at yaw 284, about
    // 23 degrees up, so the light travels toward yaw 104. Zenith clouds are rose-brown, the horizon glows orange-red and
    // the ground half is dark maroon. (The Cubemap shader does not orient the image like Skybox/Panoramic.)
    static readonly Look Sky2 = new Look
    {
        sunPitch = 23f, sunYaw = 104f, sunIntensity = 1.6f, sun = new Color(1f, .62f, .34f),
        fillPitch = 35f, fillYaw = 284f, fillIntensity = .55f, fill = new Color(.78f, .5f, .62f),
        ambientSky = new Color(.4f, .26f, .28f), ambientEquator = new Color(.62f, .34f, .26f), ambientGround = new Color(.28f, .14f, .11f),
        ambientIntensity = 1.2f, fog = new Color(.58f, .32f, .26f), fogDensity = .0014f, exposure = .5f,
    };

    [MenuItem("Tools/Scammer No Scamming/Light PlayAble City")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "PlayAble") { Debug.LogError("Open Assets/Scenes/PlayAble.unity first."); return; }

        // The scene's own skybox decides the look; an empty scene gets the Codex panorama.
        if (RenderSettings.skybox == null) RenderSettings.skybox = SiamDuskMaterial();
        string sky = RenderSettings.skybox.name;
        Look look;
        if (sky == "sky-2") look = Sky2;
        else if (sky == "Sky_SiamDusk") look = SiamDusk;
        else { Debug.LogError("No lighting look for skybox '" + sky + "'; add one to PlayAbleLighting."); return; }

        var sun = RenderSettings.sun;
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(look.sunPitch, look.sunYaw, 0f);
            sun.color = look.sun;
            sun.intensity = look.sunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .85f;
        }

        // Fill from the opposite side, no shadows; created if the scene has none.
        var directionals = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type == LightType.Directional && l != sun).ToList();
        var fill = directionals.FirstOrDefault(l => l.name == "SkyFill_Dusk") ?? directionals.FirstOrDefault();
        if (fill == null) fill = new GameObject("SkyFill_Dusk").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.name = "SkyFill_Dusk";
        fill.transform.rotation = Quaternion.Euler(look.fillPitch, look.fillYaw, 0f);
        fill.color = look.fill;
        fill.intensity = look.fillIntensity;
        fill.shadows = LightShadows.None;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = look.ambientSky;
        RenderSettings.ambientEquatorColor = look.ambientEquator;
        RenderSettings.ambientGroundColor = look.ambientGround;
        RenderSettings.ambientIntensity = look.ambientIntensity;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = look.fog; // the horizon haze of the panorama
        RenderSettings.fogDensity = look.fogDensity;

        // Street lamps: warmer and reaching further, so the pools of light overlap on the road.
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type == LightType.Point))
        {
            string n = l.name;
            if (n.StartsWith("StreetLamp_") || n.Contains("_Lamp_"))
            {
                l.color = new Color(1f, .8f, .52f);
                l.intensity = 3f;
                l.range = 20f;
            }
            else if (l.transform.parent != null && l.transform.parent.name == "InteriorLights")
            {
                // Police station: office fluorescents, the lobby was unreadable.
                l.color = new Color(1f, .94f, .82f);
                l.intensity = 3.2f;
                l.range = 13f;
            }
        }

        // Grade: a little more exposure so the dusk reads instead of crushing to black.
        var volume = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).FirstOrDefault(v => v.isGlobal);
        if (volume != null && volume.sharedProfile != null && volume.sharedProfile.TryGet(out ColorAdjustments color))
        {
            color.postExposure.Override(look.exposure);
            EditorUtility.SetDirty(volume.sharedProfile);
        }

        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (camera.CompareTag("MainCamera")) camera.clearFlags = CameraClearFlags.Skybox;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    static Material SiamDuskMaterial()
    {
        const string path = "Assets/Art/Sky/Sky_SiamDusk.mat";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sky/sky_dusk_4k.png");
        if (texture == null) throw new System.InvalidOperationException("Missing Assets/Art/Sky/sky_dusk_4k.png");
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Skybox/Panoramic")); AssetDatabase.CreateAsset(material, path); }
        material.shader = Shader.Find("Skybox/Panoramic");
        material.SetTexture("_MainTex", texture);
        material.SetFloat("_Mapping", 1f);     // Latitude Longitude Layout
        material.SetFloat("_ImageType", 0f);   // 360 degrees
        material.SetFloat("_MirrorOnBack", 0f);
        material.SetFloat("_Layout", 0f);
        material.SetFloat("_Rotation", 0f);
        material.SetFloat("_Exposure", 1f);
        material.SetColor("_Tint", new Color(.5f, .5f, .5f, .5f));
        EditorUtility.SetDirty(material);
        return material;
    }
}
