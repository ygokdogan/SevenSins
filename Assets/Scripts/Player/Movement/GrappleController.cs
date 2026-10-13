using Input;
using Enemies.Combat;
using Enemies.Core;
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
        [SerializeField, Min(0f)] private float predictionSphereCastRadius = 0.6f;

        [Header("Enemy Grapple")]
        [SerializeField, Min(0.01f)] private float holdToFlyTime = 0.2f;
        [Tooltip("Enemy pull travel time, independent of distance. Starts when the grapple tap is released.")]
        [SerializeField, Min(0.05f)] private float enemyPullDuration = 0.25f;
        [SerializeField, Min(0.5f)] private float enemyStopDistance = 1.8f;

        [SerializeField, Min(0f)] private float enemyPullHopHeight = 0.8f;

        [Header("Rope")]
        [SerializeField, Min(0.001f)] private float ropeWidth = 0.035f;
        [SerializeField] private Color ropeColor = new Color(0.35f, 0.19f, 0.07f, 1f);
        [SerializeField, Min(0.1f)] private float ropeDrawSpeed = 8f;
        [SerializeField] private Vector3 fallbackOriginOffset = new Vector3(0.32f, -0.28f, 0.45f);

        private PlayerMovement movement;
        private InputHandler input;
        [SerializeField] private LineRenderer rope;
        private Transform anchorTransform;
        private Vector3 localAnchor;
        private Vector3 currentRopeEnd;

        private EnemyBase anchorEnemy;
        private bool pullingEnemy;
        private bool awaitingEnemyGesture;
        private float pullDuration;
        private float pullElapsed;
        private float pullDistance;
        private float pullStarted;
        private float pullBlockedTime;

        public bool IsFlying => movement != null && movement.IsGrappling;
        public bool IsPullingOrPending => pullingEnemy || awaitingEnemyGesture;

        // Called by both movement and combat before either consumes action inputs.
        public bool ProcessActionInput()
        {
            // Consume attacks before any cancellation or arrival logic; never queue them for release.
            if (IsGrappling) input.UsePrimaryAttackInput();
            if (IsFlying)
            {
                DiscardActionInputs();
                return false;
            }
            if (IsPullingOrPending && (input.DashInput || input.JumpInput ||
                input.BlockInput || input.IsBlockHeld || input.SheatheInput))
            {
                ReleaseGrapple();
                input.UseGrappleInput();
            }
            return true;
        }

        public void DiscardActionInputs()
        {
            input.UsePrimaryAttackInput();
            input.UseDashInput();
            input.UseJumpInput();
            input.ConsumeJumpRelease();
            input.UseBlockInput();
            input.UseSheatheInput();
        }

        public bool TryPerformAction() => !IsGrappling;

        public bool HasEnemyAnchor => anchorEnemy != null;
        public Camera AimCamera => aimCamera;

        public bool IsGrappling => awaitingEnemyGesture || pullingEnemy || (movement != null && movement.IsGrappling);
        public int AnchorLayerMask => anchorTransform != null ? 1 << anchorTransform.gameObject.layer : 0;
        public Vector3 AnchorPoint => TryGetAnchor(out Vector3 point) ? point : Vector3.zero;
        public bool HasPrediction { get; private set; }
        public Vector3 PredictionPosition { get; private set; }
        public Transform PredictionTarget { get; private set; }
        public EnemyBase PredictionEnemy { get; private set; }

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            input = GetComponent<InputHandler>();
            if (aimCamera == null) aimCamera = Camera.main;

            if (rope == null)
            {
                Debug.LogError("Grapple rope is not assigned. Run Tools/Western Samurai/Bake Persistent Objects.", this);
                enabled = false;
                return;
            }
            rope.enabled = false;

            if (predictionPoint != null) predictionPoint.gameObject.SetActive(false);
        }

        // PlayerMovement invokes this before ticking its state, independent of Update order.
        public void ProcessInput()
        {
            if (!input.isActiveAndEnabled)
            {
                ReleaseGrapple();
                return;
            }
            // Consume repeats before ticking: arrival this frame must not restart the lock.
            if (HasEnemyAnchor && IsGrappling) input.UseGrappleInput();
            ProcessActionInput();
            if (awaitingEnemyGesture || pullingEnemy) TickEnemyPull();
            else if (anchorEnemy != null && !movement.IsGrappling) ReleaseGrapple();
            if (!input.GrappleInput) return;
            input.UseGrappleInput();
            if (IsGrappling)
            {
                // Enemy grapples cannot be toggled off to repeatedly refresh their lock.
                if (!HasEnemyAnchor) ReleaseGrapple();
                return;
            }
            TryStartGrapple();
        }

        public bool TryStartGrapple()
        {
            if (!isActiveAndEnabled || !movement.CanMove || IsGrappling ||
                !TryFindTarget(out RaycastHit hit, out Vector3 point)) return false;
            anchorEnemy = hit.collider.GetComponentInParent<EnemyBase>();
            anchorTransform = anchorEnemy != null ? anchorEnemy.transform : hit.collider.transform;
            localAnchor = anchorTransform.InverseTransformPoint(point);
            GrappleAnchor marker = hit.collider.GetComponentInParent<GrappleAnchor>();
            bool flyThrough = anchorEnemy != null || (marker != null && marker.Arrival == GrappleArrival.FlyThrough);
            if (anchorEnemy != null && !anchorEnemy.BeginGrappleLock(gameObject))
            {
                anchorEnemy = null;
                anchorTransform = null;
                return false;
            }
            awaitingEnemyGesture = anchorEnemy != null && anchorEnemy.CanBeGrapplePulled &&
                anchorEnemy.Movement.IsOnNavMesh;
            pullStarted = Time.time;
            if (!awaitingEnemyGesture && !movement.TryStartGrapple(point, hit.normal, flyThrough))
            {
                ReleaseGrapple();
                return false;
            }

            input.UsePrimaryAttackInput();
            movement.Player.Combat?.CancelAttackForGrapple();
            currentRopeEnd = GetRopeOrigin();
            rope.enabled = true;
            rope.SetPosition(0, currentRopeEnd);
            rope.SetPosition(1, currentRopeEnd);
            HasPrediction = false;
            PredictionTarget = null;
            PredictionEnemy = null;
            if (predictionPoint != null) predictionPoint.gameObject.SetActive(false);
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

            if (anchorEnemy != null) ReleaseGrapple();
            rope.enabled = false;
            HasPrediction = TryFindTarget(out RaycastHit predictedHit, out Vector3 predicted);
            PredictionEnemy = HasPrediction ? predictedHit.collider.GetComponentInParent<EnemyBase>() : null;
            GrappleAnchor marker = HasPrediction ? predictedHit.collider.GetComponentInParent<GrappleAnchor>() : null;
            PredictionTarget = PredictionEnemy != null ? PredictionEnemy.transform :
                marker != null ? marker.transform : null;
            PredictionPosition = predicted;

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
                EnemyBase enemy = hit.collider.GetComponentInParent<EnemyBase>();
                if (enemy != null && (!enemy.isActiveAndEnabled || hit.collider.isTrigger)) continue;
                if (marker == null && enemy == null) continue;
                Vector3 candidate = enemy != null ? enemy.GrappleCenter : marker.GetAttachmentPoint(hit.point);
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
                EnemyBase enemy = target.GetComponentInParent<EnemyBase>();
                GrappleAnchor marker = target.GetComponentInParent<GrappleAnchor>();
                if (obstacle.collider == target || obstacle.transform.IsChildOf(transform) ||
                    (enemy != null && obstacle.transform.IsChildOf(enemy.transform)) ||
                    (marker != null && obstacle.transform.IsChildOf(marker.transform))) continue;
                if (obstacle.distance < delta.magnitude - 0.05f) return false;
            }
            return true;
        }

        public bool TryGetAnchor(out Vector3 point)
        {
            point = default;
            if (anchorTransform == null || !anchorTransform.gameObject.activeInHierarchy) return false;
            if (anchorEnemy != null && !anchorEnemy.isActiveAndEnabled) return false;
            point = anchorEnemy != null ? anchorEnemy.GrappleCenter : anchorTransform.TransformPoint(localAnchor);
            return true;
        }

        private void TickEnemyPull()
        {
            if (anchorEnemy == null || !anchorEnemy.isActiveAndEnabled || !movement.CanMove ||
                !anchorEnemy.Movement.IsOnNavMesh || Time.time - pullStarted >= movement.config.Grapple.Timeout)
            {
                ReleaseGrapple();
                return;
            }
            if (awaitingEnemyGesture)
            {
                // No enemy displacement until release distinguishes a tap from a hold.
                if (input.IsGrappleHeld)
                {
                    if (Time.time - pullStarted < holdToFlyTime) return;
                    awaitingEnemyGesture = false;
                    if (!movement.TryStartGrapple(anchorEnemy.GrappleCenter, Vector3.zero, true))
                        ReleaseGrapple();
                    return;
                }
                awaitingEnemyGesture = false;
                pullingEnemy = anchorEnemy.Movement.BeginGrapplePull();
                if (!pullingEnemy)
                {
                    ReleaseGrapple();
                    return;
                }
                Vector3 initialDelta = anchorEnemy.transform.position - transform.position;
                initialDelta.y = 0f;
                pullDistance = Mathf.Max(0f, initialDelta.magnitude - enemyStopDistance);
                pullDuration = Mathf.Max(0.05f, enemyPullDuration);
                pullElapsed = pullBlockedTime = 0f;
            }
            float previous = Mathf.Clamp01(pullElapsed / pullDuration);
            pullElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(pullElapsed / pullDuration);
            // Fast initial tug with a soft arrival. Recompute the remaining distance so
            // a moving player still receives the enemy within the same travel time.
            Vector3 remainingDelta = anchorEnemy.transform.position - transform.position;
            remainingDelta.y = 0f;
            float remainingDistance = Mathf.Max(0f, remainingDelta.magnitude - enemyStopDistance);
            float timeRatio = (1f - progress) / Mathf.Max(0.0001f, 1f - previous);
            float step = remainingDistance * (1f - timeRatio * timeRatio);
            float moved = anchorEnemy.Movement.GrapplePullTowards(transform.position,
                step, enemyStopDistance);
            float height = Mathf.Min(enemyPullHopHeight, pullDistance * 0.25f);
            anchorEnemy.Movement.SetGrappleHop(height * 4f * progress * (1f - progress));
            pullBlockedTime = step > 0.002f && moved < step * 0.1f
                ? pullBlockedTime + Time.deltaTime : 0f;
            if (progress >= 1f || pullBlockedTime > 0.3f) ReleaseGrapple();
        }

        public void ReleaseGrapple()
        {
            pullingEnemy = false;
            awaitingEnemyGesture = false;
            if (anchorEnemy != null) anchorEnemy.EndGrappleLock();
            anchorEnemy = null;
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
            PredictionTarget = null;
            PredictionEnemy = null;
            if (predictionPoint != null) predictionPoint.gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        public void BakePersistentObjects()
        {
            if (rope != null) return;
            var ropeObject = new GameObject("Swing Rope");
            ropeObject.transform.SetParent(transform, false);
            rope = ropeObject.AddComponent<LineRenderer>();
            rope.useWorldSpace = true;
            rope.positionCount = 2;
            rope.widthMultiplier = ropeWidth;
            rope.numCapVertices = 4;
            rope.shadowCastingMode = ShadowCastingMode.Off;
            rope.receiveShadows = false;
            string path = "Assets/Generated/RuntimeSetup/GrappleRope.mat";
            Material ropeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (ropeMaterial == null)
            {
                ropeMaterial = new Material(Shader.Find("Sprites/Default"));
                UnityEditor.AssetDatabase.CreateAsset(ropeMaterial, path);
            }
            rope.sharedMaterial = ropeMaterial;
            rope.startColor = rope.endColor = ropeColor;
            rope.enabled = false;

        }
#endif
    }
}
