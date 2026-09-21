using UnityEngine;
using UnityEngine.UI;

namespace Player.Combat
{
    public class PlayerHitFeedback : MonoBehaviour
    {
        [SerializeField] private Color hitColor = new Color(0.8f, 0.05f, 0.04f, 1f);
        [SerializeField, Range(0f, 1f)] private float peakOpacity = 0.75f;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.55f;

        private Canvas canvas;
        private Image vignette;
        private Texture2D texture;
        private Sprite sprite;
        private float remaining;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var imageObject = new GameObject("Red Vignette", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvas.gameObject.transform, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                float edge = Mathf.Max(Mathf.Abs((x + 0.5f) / 128f * 2f - 1f),
                                       Mathf.Abs((y + 0.5f) / 128f * 2f - 1f));
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 1f, edge));
                pixels[y * 128 + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            vignette = imageObject.GetComponent<Image>();
            vignette.sprite = sprite;
            vignette.raycastTarget = false;
            vignette.color = new Color(hitColor.r, hitColor.g, hitColor.b, 0f);
        }

        public void Play()
        {
            remaining = fadeDuration;
        }

        private void Update()
        {
            if (remaining <= 0f) return;
            remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
            float opacity = peakOpacity * Mathf.Pow(remaining / fadeDuration, 1.5f);
            vignette.color = new Color(hitColor.r, hitColor.g, hitColor.b, opacity);
        }

        private void OnDestroy()
        {
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
        }
    }
}
