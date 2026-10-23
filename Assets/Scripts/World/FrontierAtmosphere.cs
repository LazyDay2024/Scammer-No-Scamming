using UnityEngine;
using UnityEngine.Rendering;

namespace ScammerNoScamming
{
    // Ambient light for the golden-hour town. The scene has no baked lighting data, so without this the
    // build took its ambient from the red dusk sky: shadows went black and every interior turned red.
    [ExecuteAlways]
    public class FrontierAtmosphere : MonoBehaviour
    {
        public Color sky = new Color(.46f, .5f, .54f);
        public Color horizon = new Color(.56f, .46f, .35f);
        public Color ground = new Color(.27f, .21f, .15f);
        [Range(0f, 1f)] public float reflections = .3f;

        void OnEnable() => Apply();

        public void Apply()
        {
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(horizon);
            probe.AddDirectionalLight(Vector3.up, sky, .55f);
            probe.AddDirectionalLight(Vector3.down, ground, .35f);
            RenderSettings.ambientMode = AmbientMode.Custom;
            RenderSettings.ambientProbe = probe;
            RenderSettings.reflectionIntensity = reflections;
        }
    }
}
