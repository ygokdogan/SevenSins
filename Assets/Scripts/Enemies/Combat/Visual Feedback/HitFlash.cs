using UnityEngine;

namespace Core
{
    public class HitFlash : MonoBehaviour
    {
        private static readonly Color SamuraiBase = new Color(1f, 0f, 0.1963191f, 1f);
        private static readonly Color SamuraiFlash = new Color(1f, 0.4f, 0.4f, 1f);
        [SerializeField] private float flashDuration = 0.09f;
        
        private Renderer bodyRenderer;
        private MaterialPropertyBlock propBlock;
        private Color baseColor = Color.white;
        private Color baseEmission = Color.black;
        private Color calculatedFlashColor;
        private string colorProperty = "_Color";
        private float flashTimer;
        private bool isFlashing;

        private void Awake()
        {
            bodyRenderer = GetComponent<Renderer>();
            propBlock = new MaterialPropertyBlock();
            
            if (bodyRenderer != null && bodyRenderer.sharedMaterial != null)
            {
                if (bodyRenderer.sharedMaterial.HasProperty("_BaseColor"))
                    colorProperty = "_BaseColor";
                if (bodyRenderer.sharedMaterial.HasProperty(colorProperty))
                    baseColor = bodyRenderer.sharedMaterial.GetColor(colorProperty);
                if (bodyRenderer.sharedMaterial.HasProperty("_EmissionColor"))
                    baseEmission = bodyRenderer.sharedMaterial.GetColor("_EmissionColor");
            }
            calculatedFlashColor = new Color(
                Lift(baseColor.r, SamuraiBase.r, SamuraiFlash.r),
                Lift(baseColor.g, SamuraiBase.g, SamuraiFlash.g),
                Lift(baseColor.b, SamuraiBase.b, SamuraiFlash.b),
                baseColor.a);
        }

        private void Update()
        {
            UpdateFlash(Time.deltaTime);
        }

        public void TriggerFlash()
        {
            if (bodyRenderer == null) return;
            flashTimer = flashDuration;
            isFlashing = true;

            bodyRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(colorProperty, calculatedFlashColor);
            propBlock.SetColor("_EmissionColor", new Color(
                calculatedFlashColor.r * 2f,
                calculatedFlashColor.g * 2f,
                calculatedFlashColor.b * 2f, 1f));
            bodyRenderer.SetPropertyBlock(propBlock);
        }

        private static float Lift(float baseChannel, float referenceBase, float referenceFlash)
        {
            float available = 1f - referenceBase;
            if (available <= 0.0001f) return baseChannel;
            float ratio = Mathf.Clamp01((referenceFlash - referenceBase) / available);
            return Mathf.Lerp(baseChannel, 1f, ratio);
        }
        
        private void UpdateFlash(float dt)
        {
            if (!isFlashing) return;

            flashTimer -= dt;
            if (flashTimer <= 0f)
            {
                isFlashing = false;
                propBlock.SetColor(colorProperty, baseColor);
                propBlock.SetColor("_EmissionColor", baseEmission);
                bodyRenderer.SetPropertyBlock(propBlock);
            }
        }
    }
}
