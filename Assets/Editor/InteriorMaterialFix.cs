using UnityEditor;
using UnityEngine;

// One-shot: Standard / legacy materials in the imported Interiors packs render pink under URP; re-point them at URP/Lit.
public static class InteriorMaterialFix
{
    [MenuItem("Tools/Scammer No Scamming/Fix Interior Pack Materials")]
    public static void Run()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        int fixedCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Interiors" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || m.shader == null) continue;
            string n = m.shader.name;
            if (n.StartsWith("Universal Render Pipeline/")) continue;
            Color color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            Texture main = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            Texture bump = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
            Texture emissionMap = m.HasProperty("_EmissionMap") ? m.GetTexture("_EmissionMap") : null;
            Color emission = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
            float metallic = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;
            float smooth = m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : .3f;
            float mode = m.HasProperty("_Mode") ? m.GetFloat("_Mode") : 0f;
            bool hadEmission = m.IsKeywordEnabled("_EMISSION") && emission.maxColorComponent > 0f;
            m.shader = lit;
            m.SetColor("_BaseColor", color);
            m.SetTexture("_BaseMap", main);
            if (bump != null) { m.SetTexture("_BumpMap", bump); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smooth);
            if (hadEmission) { m.SetColor("_EmissionColor", emission); m.SetTexture("_EmissionMap", emissionMap); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
            if (mode == 1f) { m.SetFloat("_AlphaClip", 1f); m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450; }
            else if (mode >= 2f)
            {
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000;
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            EditorUtility.SetDirty(m);
            fixedCount++;
            Debug.Log("[InteriorMaterialFix] " + path + " (" + n + ")");
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[InteriorMaterialFix] converted " + fixedCount);
    }
}
