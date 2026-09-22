using Input;
using UnityEngine;
using UnityEngine.Rendering;

namespace Player.Movement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement), typeof(InputHandler))]
    public sealed class GrappleController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform ropeOrigin;
        [SerializeField] private Transform predictionPoint;

        [Header("Grapple Target")]
        [SerializeField, Min(1f)] private float maxDistance = 25f;
        [SerializeField] private LayerMask grappleLayers = ~0;
        [SerializeField] private bool requireGrappleAnchor;
        [SerializeField, Min(0f)] private float predictionSphereCastRadius = 0.6f;

        [Header("Rope")]
        [SerializeField, Min(0.001f)] private float ropeWidth = 0.035f;
        [SerializeField] private Color ropeColor = new Color(0.35f, 0.19f, 0.07f, 1f);
        [SerializeField, Min(0.1f)] private float ropeDrawSpeed = 8f;
        [SerializeField] private Vector3 fallbackOriginOffset = new Vector3(0.32f, -0.28f, 0.45f);

        private PlayerMovement movement;
        private InputHandler input;
        private LineRenderer rope;
        private Material ropeMaterial;
        private Transform anchorTransform;
        private Vector3 localAnchor;
        private Vector3 currentRopeEnd;
        private bool ownsPredictionPoint;

        public bool IsGrappling => movement != null && movement.IsGrappling;
        public int AnchorLayerMask => anchorTransform != null ? 1 << anchorTransform.gameObject.layer : 0;
        public Vector3 AnchorPoint => TryGetAnchor(out Vector3 point) ? point : Vector3.zero;
        public bool HasPrediction { get; private set; }
        public Vector3 PredictionPosition { get; private set; }

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            input = GetComponent<InputHandler>();
            if (aimCamera == null) aimCamera = Camera.main;

            var ropeObject = new GameObject("Swing Rope");
            ropeObject.transform.SetParent(transform, false);
            rope = ropeObject.AddComponent<LineRenderer>();
            rope.useWorldSpace = true;
            rope.positionCount = 2;
            rope.widthMultiplier = ropeWidth;
            rope.numCapVertices = 4;
            rope.shadowCastingMode = ShadowCastingMode.Off;
            rope.receiveShadows = false;
            ropeMaterial = new Material(Shader.Find("Sprites/Default"));
            rope.sharedMaterial = ropeMaterial;
            rope.startColor = rope.endColor = ropeColor;
            rope.enabled = false;

            if (predictionPoint == null)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Swing Target Preview";
                marker.layer = 2; // Ignore Raycast
                Collider markerCollider = marker.GetComponent<Collider>();
                markerCollider.enabled = false;
                Destroy(markerCollider);
                marker.transform.SetParent(transform, false);
                marker.transform.localScale = Vector3.one * 0.12f;
                marker.GetComponent<Renderer>().sharedMaterial = ropeMaterial;
                predictionPoint = marker.transform;
                ownsPredictionPoint = true;
            }
            predictionPoint.gameObject.SetActive(false);
        }

        // PlayerMovement invokes this before ticking its state, independent of Update order.
        public void ProcessInput()
        {
            if (!input.isActiveAndEnabled)
            {
                ReleaseGrapple();
                return;
            }
            if (!input.GrappleInput) return;
            input.UseGrappleInput();
            if (IsGrappling) ReleaseGrapple();
            else TryStartGrapple();
        }

        public bool TryStartGrapple()
        {
            if (!isActiveAndEnabled || IsGrappling ||
                !TryFindTarget(out RaycastHit hit, out Vector3 point)) return false;
            anchorTransform = hit.collider.transform;
            localAnchor = anchorTransform.InverseTransformPoint(point);
            GrappleAnchor marker = hit.collider.GetComponentInParent<GrappleAnchor>();
            bool flyThrough = marker != null && marker.Arrival == GrappleArrival.FlyThrough;
            if (!movement.TryStartGrapple(point, hit.normal, flyThrough))
            {
                anchorTransform = null;
                return false;
            }

            currentRopeEnd = GetRopeOrigin();
            rope.enabled = true;
            rope.SetPosition(0, currentRopeEnd);
            rope.SetPosition(1, currentRopeEnd);
            HasPrediction = false;
            predictionPoint.gameObject.SetActive(false);
            return true;
        }

        private void LateUpdate()
        {
            if (IsGrappling)
            {
                if (!TryGetAnchor(out Vector3 point))
                {
                    ReleaseGrapple();
                    return;
                }
                rope.enabled = true;
                currentRopeEnd = Vector3.Lerp(currentRopeEnd, point,
                    1f - Mathf.Exp(-ropeDrawSpeed * Time.deltaTime));
                rope.SetPosition(0, GetRopeOrigin());
                rope.SetPosition(1, currentRopeEnd);
                return;
            }

            rope.enabled = false;
            HasPrediction = TryFindTarget(out _, out Vector3 predicted);
            PredictionPosition = predicted;
            predictionPoint.gameObject.SetActive(HasPrediction);
            if (HasPrediction) predictionPoint.position = predicted;
        }

        private bool TryFindTarget(out RaycastHit hit, out Vector3 point)
        {
            hit = default;
            point = default;
            if (aimCamera == null) return false;
            Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
            // Prefer a direct hit; a sphere cast is only an aim-assistance fallback.
            if (SelectTarget(Physics.RaycastAll(ray, maxDistance, grappleLayers,
                    QueryTriggerInteraction.Collide), out hit, out point)) return true;
            return predictionSphereCastRadius > 0f &&
                SelectTarget(Physics.SphereCastAll(ray, predictionSphereCastRadius, maxDistance,
                    grappleLayers, QueryTriggerInteraction.Collide), out hit, out point);
        }

        private bool SelectTarget(RaycastHit[] hits, out RaycastHit selected, out Vector3 point)
        {
            selected = default;
            point = default;
            float nearest = float.PositiveInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.distance >= nearest) continue;
                GrappleAnchor marker = hit.collider.GetComponentInParent<GrappleAnchor>();
                if ((requireGrappleAnchor || hit.collider.isTrigger) && marker == null) continue;
                Vector3 candidate = marker != null ? marker.GetAttachmentPoint(hit.point) : hit.point;
                Vector3 delta = candidate - aimCamera.transform.position;
                float distance = delta.magnitude;
                if (distance < 0.01f || distance > maxDistance || !IsVisible(candidate, hit.collider)) continue;
                selected = hit;
                point = candidate;
                nearest = hit.distance;
            }
            return selected.collider != null;
        }

        private bool IsVisible(Vector3 point, Collider target)
        {
            Vector3 delta = point - aimCamera.transform.position;
            foreach (RaycastHit obstacle in Physics.RaycastAll(aimCamera.transform.position,
                delta.normalized, delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (obstacle.collider == target || obstacle.transform.IsChildOf(transform)) continue;
                if (obstacle.distance < delta.magnitude - 0.05f) return false;
            }
            return true;
        }

        public bool TryGetAnchor(out Vector3 point)
        {
            point = default;
            if (anchorTransform == null || !anchorTransform.gameObject.activeInHierarchy) return false;
            point = anchorTransform.TransformPoint(localAnchor);
            return true;
        }

        public void ReleaseGrapple()
        {
            if (rope != null) rope.enabled = false;
            if (movement != null) movement.StopGrapple();
            anchorTransform = null;
        }

        private Vector3 GetRopeOrigin()
        {
            if (ropeOrigin != null) return ropeOrigin.position;
            return (aimCamera != null ? aimCamera.transform : transform).TransformPoint(fallbackOriginOffset);
        }

        private void OnDisable()
        {
            ReleaseGrapple();
            HasPrediction = false;
            if (predictionPoint != null) predictionPoint.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (rope != null) Destroy(rope.gameObject);
            if (ropeMaterial != null) Destroy(ropeMaterial);
            if (ownsPredictionPoint && predictionPoint != null) Destroy(predictionPoint.gameObject);
        }
    }
}
