using System;
using Core;
using Enemies.Combat.States;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Enemies.Combat
{
    public class ArcherRangedAttackStyle : EnemyAttackStyle
    {
        [Header("Ranged Attack")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private RangedAttackData[] attackOptions;
        [SerializeField] private LayerMask sightBlockingLayers = ~0;
        private readonly RaycastHit[] sightHits = new RaycastHit[16];

        [Header("Spacing")]
        [SerializeField, Min(0f)] private float minimumRange = 3f;
        [SerializeField, Min(0.1f)] private float preferredRange = 8f;
        [SerializeField, Min(0.1f)] private float maximumRange = 12f;
        [SerializeField, Min(0.1f)] private float rangeTolerance = 0.5f;

        [Header("Close-Range Escape")]
        [SerializeField, Min(0f)] private float escapeTriggerRange = 4f;
        [SerializeField, Range(0f, 1f)] private float escapeChance = 0.35f;
        [SerializeField, Min(0f)] private float escapeCooldown = 9f;
        [SerializeField, Min(1.5f)] private float escapeDistance = 4f;
        [SerializeField, Min(0.1f)] private float escapeSpeed = 11f;

        private float nextEscapeRollTime;
        private float nextEscapeAllowedTime;
        private ArcherEscapeState escapeState;

        public float EscapeSpeed => escapeSpeed;

        public override IState<Core.EnemyBase> CreateState() => new ArcherAttackState(this);
        public override IState<Core.EnemyBase> CreateEscapeState() =>
            escapeState = new ArcherEscapeState(this);
        public override float DesiredDistance(Core.EnemyBase ctx) => preferredRange;
        public override float EngagementRange(Core.EnemyBase ctx) =>
            Mathf.Max(ctx.Config.stalkRange, preferredRange + rangeTolerance);

        public override bool MoveWhileStalking(Core.EnemyBase ctx, float dt)
        {
            MaintainPreferredRange(ctx);
            return true;
        }

        public override bool TryStartEscape(Core.EnemyBase ctx)
        {
            if (escapeState == null || ctx.Perception.Target == null ||
                ctx.HorizontalDistance > escapeTriggerRange ||
                Time.time < nextEscapeRollTime || Time.time < nextEscapeAllowedTime)
                return false;

            nextEscapeRollTime = Time.time + 0.8f;
            if (!ctx.Movement.TryFindSafeDashDestination(ctx.Perception.Target, escapeDistance,
                    out Vector3 destination) || Random.value >= escapeChance) return false;

            nextEscapeAllowedTime = Time.time + escapeCooldown;
            escapeState.SetDestination(destination);
            ctx.RequestTransition(ctx.States.Escape);
            return true;
        }

        public void MaintainPreferredRange(Core.EnemyBase ctx)
        {
            if (ctx.Perception.Target == null) return;
            Vector3 away = ctx.transform.position - ctx.Perception.Target.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -ctx.transform.forward;
            float distance = away.magnitude;
            if (Mathf.Abs(distance - preferredRange) <= rangeTolerance)
            {
                ctx.Movement.Stop();
                ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
                return;
            }
            Vector3 destination = ctx.Perception.Target.position + away.normalized * preferredRange;
            ctx.Movement.MoveToNearest(destination, 1f, ctx.Config.walkSpeed, rangeTolerance * 0.5f);
            ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.Walk);
        }

        public RangedAttackData SelectAttack()
        {
            if (attackOptions != null && attackOptions.Length > 0)
            {
                int start = Random.Range(0, attackOptions.Length);
                for (int i = 0; i < attackOptions.Length; i++)
                {
                    RangedAttackData option = attackOptions[(start + i) % attackOptions.Length];
                    if (option != null && option.projectilePrefab != null) return option;
                }
            }
            return attackData as RangedAttackData;
        }

        protected override bool CanStartAttackInternal(Core.EnemyBase ctx)
        {
            if (SelectAttack() == null || muzzle == null ||
                ctx.Perception.TargetCollider == null) return false;
            if (ctx.HorizontalDistance < minimumRange || ctx.HorizontalDistance > maximumRange)
                return false;

            Vector3 targetPoint = GetTargetPoint(ctx);
            Vector3 toTarget = targetPoint - muzzle.position;
            if (Vector3.Dot(ctx.transform.forward, toTarget.normalized) < 0.5f) return false;
            int count = Physics.RaycastNonAlloc(new Ray(muzzle.position, toTarget.normalized),
                sightHits, toTarget.magnitude, sightBlockingLayers,
                QueryTriggerInteraction.Ignore);
            float closestDistance = float.PositiveInfinity;
            Collider closest = null;
            for (int i = 0; i < count; i++)
            {
                Collider candidate = sightHits[i].collider;
                if (candidate.transform.IsChildOf(ctx.transform)) continue;
                if (sightHits[i].distance >= closestDistance) continue;
                closestDistance = sightHits[i].distance;
                closest = candidate;
            }
            return closest == null || closest.transform.IsChildOf(ctx.Perception.Target);
        }

        public override void FinishAttack(Core.EnemyBase ctx)
        {
            ScheduleNextAttack();
            ctx.RequestTransition(ctx.States.Engage);
        }

        protected override bool IsCrowded(Core.EnemyBase ctx) =>
            ctx.HorizontalDistance < minimumRange;

        public void Fire(Core.EnemyBase ctx, RangedAttackData attack)
        {
            if (muzzle == null || attack == null || attack.projectilePrefab == null || ctx.Perception.TargetCollider == null) return;
            Vector3 targetPoint = GetTargetPoint(ctx);
            Vector3 direction = targetPoint - muzzle.position;
            if (direction.sqrMagnitude < 0.001f) direction = muzzle.forward;
            EnemyProjectile projectile = Instantiate(attack.projectilePrefab, muzzle.position,
                Quaternion.LookRotation(direction.normalized));
            projectile.Initialize(ctx.transform, attack, targetPoint);
        }

        private static Camera GetTargetCamera(Core.EnemyBase ctx)
        {
            if (ctx.Perception.Target == null) return null;
            var player = ctx.Perception.Target.GetComponentInParent<Player.PlayerManager>();
            Camera camera = player != null && player.GrappleController != null
                ? player.GrappleController.AimCamera : null;
            if (camera == null) camera = ctx.Perception.Target.GetComponentInChildren<Camera>();
            return camera;
        }

        private static Vector3 GetTargetPoint(Core.EnemyBase ctx)
        {
            Camera camera = GetTargetCamera(ctx);
            if (camera != null) return camera.transform.position;
            // Non-player targets still aim at the upper body rather than the feet.
            Bounds bounds = ctx.Perception.TargetCollider.bounds;
            return bounds.center + Vector3.up * bounds.extents.y * 0.65f;
        }

        private void OnEnable()
        {
            maximumRange = Mathf.Max(maximumRange, minimumRange);
            preferredRange = Mathf.Clamp(preferredRange, minimumRange, maximumRange);
        }
    }
}
