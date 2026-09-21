using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal static class URPMigration
{
    private const string PipelinePath = "Assets/Settings/WesternSamuraiURP.asset";
    private const string RendererPath = "Assets/Settings/WesternSamuraiRenderer.asset";

    [MenuItem("Tools/Western Samurai/Complete URP Migration")]
    private static void Migrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");

        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);
        }

        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline == null)
        {
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, PipelinePath);
        }

        GraphicsSettings.defaultRenderPipeline = pipeline;
        int originalQuality = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipeline;
        }
        QualitySettings.SetQualityLevel(originalQuality, false);

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (lit == null || unlit == null)
        {
            Debug.LogError("URP shaders were not found; material conversion was skipped.");
            return;
        }

        int converted = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == null) continue;
            string shaderName = material.shader.name;
            if (shaderName != "Standard" && shaderName != "Unlit/Color") continue;

            Color color = material.HasProperty("_Color")
                ? material.GetColor("_Color") : Color.white;
            Texture texture = material.HasProperty("_MainTex")
                ? material.GetTexture("_MainTex") : null;
            Vector2 scale = material.HasProperty("_MainTex")
                ? material.GetTextureScale("_MainTex") : Vector2.one;
            Vector2 offset = material.HasProperty("_MainTex")
                ? material.GetTextureOffset("_MainTex") : Vector2.zero;
            float metallic = material.HasProperty("_Metallic")
                ? material.GetFloat("_Metallic") : 0f;
            float smoothness = material.HasProperty("_Glossiness")
                ? material.GetFloat("_Glossiness") : 0.5f;

            material.shader = shaderName == "Standard" ? lit : unlit;
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", scale);
            material.SetTextureOffset("_BaseMap", offset);
            if (shaderName == "Standard")
            {
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Smoothness", smoothness);
            }
            EditorUtility.SetDirty(material);
            converted++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"URP migration complete. Converted {converted} built-in materials.");
    }
}
