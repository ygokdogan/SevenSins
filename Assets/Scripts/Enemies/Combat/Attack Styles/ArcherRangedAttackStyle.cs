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
        private EnemyDashRetreatState escapeState;

        public float EscapeSpeed => escapeSpeed;

        public override IState<EnemyBase> CreateState() => new EnemyRangedAttackState(this);
        public override IState<EnemyBase> CreateEscapeState() =>
            escapeState = new EnemyDashRetreatState(this);
        public override float DesiredDistance(EnemyBase ctx) => preferredRange;
        public override float EngagementRange(EnemyBase ctx) =>
            Mathf.Max(ctx.StalkRange, preferredRange + rangeTolerance);

        public override bool MoveWhileStalking(EnemyBase ctx, float dt)
        {
            MaintainPreferredRange(ctx);
            return true;
        }

        public override bool TryStartEscape(EnemyBase ctx)
        {
            if (escapeState == null || ctx.Target == null ||
                ctx.HorizontalDistance > escapeTriggerRange ||
                Time.time < nextEscapeRollTime || Time.time < nextEscapeAllowedTime)
                return false;

            nextEscapeRollTime = Time.time + 0.8f;
            if (!ctx.Movement.TryFindSafeDashDestination(ctx.Target, escapeDistance,
                    out Vector3 destination) || Random.value >= escapeChance) return false;

            nextEscapeAllowedTime = Time.time + escapeCooldown;
            escapeState.SetDestination(destination);
            ctx.RequestTransition(ctx.States.Escape);
            return true;
        }

        public void MaintainPreferredRange(EnemyBase ctx)
        {
            if (ctx.Target == null) return;
            Vector3 away = ctx.Transform.position - ctx.Target.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -ctx.Transform.forward;
            float distance = away.magnitude;
            if (Mathf.Abs(distance - preferredRange) <= rangeTolerance)
            {
                ctx.Movement.Stop();
                ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
                return;
            }
            Vector3 destination = ctx.Target.position + away.normalized * preferredRange;
            ctx.Movement.MoveToNearest(destination, 1f, ctx.WalkSpeed, rangeTolerance * 0.5f);
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

        public override bool CanStartAttack(EnemyBase ctx)
        {
            if (SelectAttack() == null || muzzle == null ||
                ctx.TargetCollider == null) return false;
            if (ctx.HorizontalDistance < minimumRange || ctx.HorizontalDistance > maximumRange)
                return false;

            Vector3 targetPoint = ctx.TargetCollider.bounds.center;
            Vector3 toTarget = targetPoint - muzzle.position;
            if (Vector3.Dot(ctx.Transform.forward, toTarget.normalized) < 0.5f) return false;
            int count = Physics.RaycastNonAlloc(new Ray(muzzle.position, toTarget.normalized),
                sightHits, toTarget.magnitude, sightBlockingLayers,
                QueryTriggerInteraction.Ignore);
            float closestDistance = float.PositiveInfinity;
            Collider closest = null;
            for (int i = 0; i < count; i++)
            {
                Collider candidate = sightHits[i].collider;
                if (candidate.transform.IsChildOf(ctx.Transform)) continue;
                if (sightHits[i].distance >= closestDistance) continue;
                closestDistance = sightHits[i].distance;
                closest = candidate;
            }
            return closest == null || closest.transform.IsChildOf(ctx.Target);
        }

        public override void FinishAttack(EnemyBase ctx)
        {
            ScheduleNextAttack(ctx);
            ctx.RequestTransition(ctx.States.Engage);
        }

        protected override bool IsCrowded(EnemyBase ctx) =>
            ctx.HorizontalDistance < minimumRange;

        public void Fire(EnemyBase ctx, RangedAttackData attack)
        {
            if (muzzle == null || attack == null || attack.projectilePrefab == null || ctx.TargetCollider == null) return;
            Vector3 targetPoint = ctx.TargetCollider.bounds.center;
            Vector3 direction = targetPoint - muzzle.position;
            if (direction.sqrMagnitude < 0.001f) direction = muzzle.forward;
            EnemyProjectile projectile = Instantiate(attack.projectilePrefab, muzzle.position,
                Quaternion.LookRotation(direction.normalized));
            projectile.Initialize(ctx.Transform, attack, targetPoint);
        }

        private void OnEnable()
        {
            maximumRange = Mathf.Max(maximumRange, minimumRange);
            preferredRange = Mathf.Clamp(preferredRange, minimumRange, maximumRange);
        }
    }
}
