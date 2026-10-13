using Player.Movement;
using UnityEngine;

namespace Player.Combat.Visual_Feedback
{
    // Lightweight, non-interactive overlay; all animation uses unscaled time for hit-stop.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class PlayerCombatHud : MonoBehaviour
    {
        [SerializeField] private Color grappleColor = new Color(1f, 0.82f, 0.12f, 1f);
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform root, grappleRoot, hitRoot;
        [SerializeField] private CanvasGroup grappleGroup, hitGroup;
        private GrappleController grapple;
        private float grappleBlend;
        private Transform hintedTarget;
        private float targetLostTime;
        [SerializeField] private UnityEngine.UI.Image crosshairImage, grappleImage, hitImage;
        private Collider enemyBoundsCollider;
        private Transform boundsTarget;

        private void Awake()
        {
            grapple = GetComponent<GrappleController>();
            if (canvas == null)
            {
                Debug.LogError("Combat HUD references are missing. Run Tools/Western Samurai/Bake Persistent Objects.", this);
                enabled = false;
                return;
            }
            grappleGroup.alpha = hitGroup.alpha = 0;
        }

#if UNITY_EDITOR
        public void BakePersistentObjects()
        {
            if (canvas != null) return;
            var go = new GameObject("Combat HUD", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;
            root = go.GetComponent<RectTransform>();
            var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            grappleRoot = Group("Grapple Reticle", out grappleGroup);
            hitRoot = Group("Hitmarker", out hitGroup);
            grappleGroup.alpha = hitGroup.alpha = 0;
        }

        public void ConfigureReticleSprites(Sprite crosshair, Sprite grappleSprite, Sprite hit)
        {
            // Reuse an authored crosshair container, including its user-adjusted scale.
            foreach (Transform child in root)
                if (child.name == "Crosshair" && child.childCount > 0)
                {
                    if (crosshairImage != null && crosshairImage.transform != child)
                        UnityEditor.Undo.DestroyObjectImmediate(crosshairImage.gameObject);
                    crosshairImage = child.GetComponent<UnityEngine.UI.Image>() ??
                        child.gameObject.AddComponent<UnityEngine.UI.Image>();
                    crosshairImage.rectTransform.sizeDelta = Vector2.one * 16;
                    break;
                }
            if (crosshairImage == null)
                crosshairImage = Line(root, "Crosshair", Vector2.zero, Vector2.one * 16, 0, Color.white)
                    .GetComponent<UnityEngine.UI.Image>();
            grappleImage = grappleRoot.GetComponent<UnityEngine.UI.Image>() ??
                grappleRoot.gameObject.AddComponent<UnityEngine.UI.Image>();
            hitImage = hitRoot.GetComponent<UnityEngine.UI.Image>() ??
                hitRoot.gameObject.AddComponent<UnityEngine.UI.Image>();
            crosshairImage.sprite = crosshair;
            grappleImage.sprite = grappleSprite;
            grappleImage.type = UnityEngine.UI.Image.Type.Sliced;
            grappleImage.fillCenter = false;
            hitImage.sprite = hit;
            hitRoot.sizeDelta = Vector2.one * 72;
            foreach (var image in new[] { crosshairImage, grappleImage, hitImage })
            {
                image.color = Color.white;
                image.raycastTarget = false;
            }
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child != null && child.name.StartsWith("Dash Wind "))
                    UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
            Transform oldDashRoot = root.Find("Dash Effect");
            if (oldDashRoot != null && oldDashRoot.childCount == 0)
                UnityEditor.Undo.DestroyObjectImmediate(oldDashRoot.gameObject);
            // Remove only the old generated strokes; retain the animated roots and their references.
            foreach (Transform parent in new Transform[] { root, grappleRoot, hitRoot, crosshairImage.transform })
                for (int i = parent.childCount - 1; i >= 0; i--)
                {
                    GameObject child = parent.GetChild(i).gameObject;
                    if (child.name == "Crosshair Shadow H" || child.name == "Crosshair Shadow V" ||
                        child.name == "Crosshair H" || child.name == "Crosshair V" ||
                        child.name == "Corner Outline H" || child.name == "Corner Outline V" ||
                        child.name == "Corner H" || child.name == "Corner V" ||
                        child.name == "Hit Shadow" || child.name == "Hit Stroke")
                        UnityEditor.Undo.DestroyObjectImmediate(child);
                }
        }

#endif

        private void OnEnable()
        {
            // Keep previously baked hitmarker objects hidden after removal of hit confirmations.
            if (hitGroup != null) hitGroup.alpha = 0;
            if (canvas != null) canvas.enabled = true;
        }

        private void OnDisable()
        {
            grappleBlend = 0;
            hintedTarget = null;
            targetLostTime = 0;
            if (canvas != null) canvas.enabled = false;
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            Camera camera = grapple != null ? grapple.AimCamera : Camera.main;
            bool visible = grapple != null && grapple.isActiveAndEnabled && grapple.HasPrediction && !grapple.IsGrappling && camera != null;
            if (visible)
            {
                visible = UpdateGrappleFrame(camera);
            }
            if (visible)
            {
                targetLostTime = 0;
                if (hintedTarget != grapple.PredictionTarget)
                {
                    hintedTarget = grapple.PredictionTarget;
                    grappleBlend = 0;
                }
            }
            else
            {
                targetLostTime += dt;
                // Brief raycast flicker should not keep replaying the acquisition animation.
                if (targetLostTime > 0.3f) hintedTarget = null;
            }
            grappleBlend = Mathf.MoveTowards(grappleBlend, visible ? 1 : 0, dt * 4.5f);
            // Overshoot gives target acquisition a distinct snap, then a subtle breathing motion.
            float t = grappleBlend - 1;
            float eased = 1 + 2.70158f * t * t * t + 1.70158f * t * t;
            grappleGroup.alpha = grappleBlend;
            float pulse = 1 + 0.035f * Mathf.Sin(Time.unscaledTime * 5) * grappleBlend;
            grappleRoot.localScale = Vector3.one * Mathf.LerpUnclamped(2.1f, 1, eased) * pulse;
            grappleRoot.localRotation = Quaternion.Euler(0, 0, Mathf.LerpUnclamped(-20, 45, eased));


        }

        private bool UpdateGrappleFrame(Camera camera)
        {
            bool enemyTarget = grapple.PredictionEnemy != null;
            Vector2 center;
            Vector2 size = new Vector2(44, 44);
            if (enemyTarget)
            {
                if (boundsTarget != grapple.PredictionTarget)
                {
                    boundsTarget = grapple.PredictionTarget;
                    enemyBoundsCollider = boundsTarget.GetComponent<Collider>();
                    if (enemyBoundsCollider == null)
                        foreach (Collider collider in boundsTarget.GetComponentsInChildren<Collider>())
                            if (!collider.isTrigger && collider.enabled) { enemyBoundsCollider = collider; break; }
                }
                if (enemyBoundsCollider == null || !enemyBoundsCollider.enabled) return false;
                Bounds bounds = enemyBoundsCollider.bounds;
                Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 screen = camera.WorldToScreenPoint(corner);
                    if (screen.z <= camera.nearClipPlane) return false;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 point);
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
                if (!root.rect.Overlaps(Rect.MinMaxRect(min.x, min.y, max.x, max.y))) return false;
                center = (min + max) * 0.5f;
                // A 45-degree square contains this screen-space box when its side
                // is (width + height) / sqrt(2). Leave extra space for the settling animation.
                Vector2 extent = max - min;
                float side = Mathf.Max(44, (extent.x + extent.y) / Mathf.Sqrt(2) + 24);
                size = Vector2.one * side;
            }
            else
            {
                boundsTarget = null;
                Vector3 screen = camera.WorldToScreenPoint(grapple.PredictionPosition);
                if (screen.z <= 0 || !camera.pixelRect.Contains(new Vector2(screen.x, screen.y))) return false;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out center);
            }
            grappleRoot.anchoredPosition = center;
            grappleRoot.sizeDelta = size;
            return true;
        }

#if UNITY_EDITOR
        private RectTransform Group(string name, out CanvasGroup group)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.sizeDelta = new Vector2(40, 40);
            group = go.GetComponent<CanvasGroup>();
            group.blocksRaycasts = group.interactable = false;
            return rect;
        }

        private static RectTransform Line(Transform parent, string name, Vector2 position,
            Vector2 size, float angle, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.raycastTarget = false;
            image.color = color;
            return rect;
        }

#endif
    }
}
