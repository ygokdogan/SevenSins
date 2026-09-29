using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    [DisallowMultipleComponent]
    public class HitFlash : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float flashDuration = 0.09f;
        [Tooltip("Model root containing the body, clothing and skinned meshes. Defaults to this object.")]
        [SerializeField] private Transform modelRoot;
        [ColorUsage(false, true)]
        [SerializeField] private Color flashColor = new Color(2f, 1.2f, 1.2f, 1f);
        [Tooltip("Optional flash material for custom shaders. Original materials are restored afterwards.")]
        [SerializeField] private Material flashMaterial;
        [SerializeField, Range(0f, 1f)] private float flashBlend = 0.25f;
        [SerializeField, Range(1f, 2f)] private float flashBrightness = 1.25f;
        [SerializeField, Range(0f, 1f)] private float emissionStrength = 0.25f;

        private sealed class FlashTarget
        {
            public Renderer Renderer;
            public Material[] Originals;
            public Material[] Temporary;
            public MaterialPropertyBlock[] Blocks;
        }

        private readonly List<FlashTarget> targets = new List<FlashTarget>();
        private float remaining;

        public void TriggerFlash()
        {
            if (!isActiveAndEnabled) return;
            remaining = Mathf.Max(0.01f, flashDuration);
            if (targets.Count > 0) return; // Repeated hits extend the existing flash.

            Transform root = modelRoot != null ? modelRoot : transform;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                // Don't recolor particles, trails or UI under the model.
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                Material[] originals = renderer.sharedMaterials;
                if (originals.Length == 0) continue;
                var target = new FlashTarget
                {
                    Renderer = renderer,
                    Originals = originals,
                    Temporary = new Material[originals.Length],
                    Blocks = new MaterialPropertyBlock[originals.Length]
                };
                for (int i = 0; i < originals.Length; i++)
                {
                    target.Blocks[i] = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(target.Blocks[i], i);
                    Material temporary = CreateFlashMaterial(originals[i]);
                    target.Temporary[i] = temporary;
                    if (temporary == null) continue;

                    // Per-slot values also override any renderer-wide color/texture block.
                    var block = new MaterialPropertyBlock();
                    CopyFlashProperty(temporary, block, "_BaseColor", false);
                    CopyFlashProperty(temporary, block, "_Color", false);
                    CopyFlashProperty(temporary, block, "_EmissionColor", false);
                    CopyFlashProperty(temporary, block, "_BaseMap", true);
                    CopyFlashProperty(temporary, block, "_MainTex", true);
                    CopyFlashProperty(temporary, block, "_EmissionMap", true);
                    renderer.SetPropertyBlock(block, i);
                }
                renderer.sharedMaterials = target.Temporary;
                targets.Add(target);
            }
        }

        private Material CreateFlashMaterial(Material original)
        {
            Material source = flashMaterial != null ? flashMaterial : original;
            if (source == null) return null;

            // Private copies keep the imported model and other enemies untouched.
            Material temporary = new Material(source);
            if (flashMaterial == null)
            {
                // Keep the albedo texture; brighten its existing tint instead of replacing it.
                TintColor(temporary, "_BaseColor");
                TintColor(temporary, "_Color");
                if (temporary.HasProperty("_EmissionColor"))
                {
                    temporary.EnableKeyword("_EMISSION");
                    Color tint = temporary.HasProperty("_BaseColor")
                        ? temporary.GetColor("_BaseColor")
                        : temporary.HasProperty("_Color") ? temporary.GetColor("_Color") : Color.white;
                    temporary.SetColor("_EmissionColor", tint * emissionStrength);
                    Texture albedo = temporary.HasProperty("_BaseMap") ? temporary.GetTexture("_BaseMap")
                        : temporary.HasProperty("_MainTex") ? temporary.GetTexture("_MainTex") : null;
                    // Emission follows the model's texture colors, avoiding a white silhouette.
                    SetTexture(temporary, "_EmissionMap", albedo != null ? albedo : Texture2D.whiteTexture);
                    string map = temporary.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                    if (temporary.HasProperty(map) && temporary.HasProperty("_EmissionMap"))
                    {
                        temporary.SetTextureScale("_EmissionMap", temporary.GetTextureScale(map));
                        temporary.SetTextureOffset("_EmissionMap", temporary.GetTextureOffset(map));
                    }
                }
            }
            return temporary;
        }

        private void TintColor(Material material, string name)
        {
            if (!material.HasProperty(name)) return;
            Color original = material.GetColor(name);
            Color tint = new Color(Mathf.Clamp01(flashColor.r), Mathf.Clamp01(flashColor.g),
                Mathf.Clamp01(flashColor.b), original.a);
            Color result = Color.Lerp(original, tint, flashBlend) * flashBrightness;
            result.a = original.a;
            material.SetColor(name, result);
        }

        private static void SetTexture(Material material, string name, Texture texture)
        {
            if (material.HasProperty(name)) material.SetTexture(name, texture);
        }

        private static void CopyFlashProperty(Material material, MaterialPropertyBlock block,
            string name, bool texture)
        {
            if (!material.HasProperty(name)) return;
            if (texture) block.SetTexture(name, material.GetTexture(name) ?? Texture2D.whiteTexture);
            else block.SetColor(name, material.GetColor(name));
        }

        private void Update()
        {
            if (targets.Count == 0) return;
            remaining -= Time.deltaTime;
            if (remaining <= 0f) Restore();
        }

        private void Restore()
        {
            foreach (FlashTarget target in targets)
            {
                if (target.Renderer != null)
                {
                    target.Renderer.sharedMaterials = target.Originals;
                    for (int i = 0; i < target.Blocks.Length; i++)
                        target.Renderer.SetPropertyBlock(target.Blocks[i].isEmpty ? null : target.Blocks[i], i);
                }
                foreach (Material material in target.Temporary)
                    if (material != null) Destroy(material);
            }
            targets.Clear();
            remaining = 0f;
        }

        private void OnDisable() => Restore();
        private void OnDestroy() => Restore();
    }
}
