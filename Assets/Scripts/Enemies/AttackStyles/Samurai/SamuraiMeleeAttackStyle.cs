using Core;
using Enemies.Combat.States;
using UnityEngine;

namespace Enemies.Combat
{
    public class SamuraiMeleeAttackStyle : EnemyAttackStyle
    {
        [Header("Melee Range")]
        [SerializeField] private BoxCollider hitCollider;
        [SerializeField] private LayerMask hittableLayers;
        [SerializeField, Min(0.1f)] private float attackRange = 2f;
        [SerializeField, Min(0.1f)] private float combatDistance = 2.6f;

        [Header("Attack Movement")]
        [SerializeField, Min(0f)] private float telegraphAdvance = 0.35f;
        [SerializeField, Min(0f)] private float lungeDistance = 0.8f;
        [SerializeField, Min(0f)] private float lungeSpeed = 6f;

        [Header("Without Authored Animation")]
        [SerializeField, Min(0f)] private float fallbackHitStart = 0.1f;
        [SerializeField, Min(0f)] private float fallbackHitEnd = 0.3f;
        [SerializeField, Min(0.1f)] private float fallbackDuration = 0.6f;
        [SerializeField, Min(0.1f)] private float animationTimeout = 3f;
        public float FallbackHitStart => fallbackHitStart;
        public float FallbackHitEnd => Mathf.Max(fallbackHitStart, fallbackHitEnd);
        public float FallbackDuration => Mathf.Max(FallbackHitEnd, fallbackDuration);
        public float AnimationTimeout => Mathf.Max(FallbackDuration, animationTimeout);

        public BoxCollider HitCollider => hitCollider;
        public LayerMask HittableLayers => hittableLayers;
        public float AttackRange => attackRange;
        public float TelegraphAdvance => telegraphAdvance;
        public float LungeDistance => lungeDistance;
        public float LungeSpeed => lungeSpeed;

        public override IState<Core.EnemyBase> CreateState() => new SamuraiAttackState(this);

        public bool IsTargetInAttackBox(Core.EnemyBase ctx)
        {
            return hitCollider != null && ctx.Perception.TargetCollider != null &&
                Physics.ComputePenetration(hitCollider, hitCollider.transform.position,
                    hitCollider.transform.rotation, ctx.Perception.TargetCollider,
                    ctx.Perception.TargetCollider.transform.position, ctx.Perception.TargetCollider.transform.rotation,
                    out _, out _);
        }

        public float AttackBoxGap(Core.EnemyBase ctx)
        {
            if (hitCollider == null || ctx.Perception.TargetCollider == null) return float.PositiveInfinity;
            Vector3 targetPoint = ctx.Perception.TargetCollider.ClosestPoint(hitCollider.bounds.center);
            Vector3 boxPoint = hitCollider.ClosestPoint(targetPoint);
            return Vector3.Distance(boxPoint, ctx.Perception.TargetCollider.ClosestPoint(boxPoint));
        }

        protected override bool CanStartAttackInternal(Core.EnemyBase ctx)
        {
            if (attackData == null || ctx.Perception.Target == null || hitCollider == null ||
                ctx.Perception.TargetCollider == null) return false;
            Vector3 toward = ctx.Perception.Target.position - ctx.transform.position;
            toward.y = 0f;
            if (Vector3.Dot(ctx.transform.forward, toward) <= 0f) return false;
            return IsTargetInAttackBox(ctx) || AttackBoxGap(ctx) <= lungeDistance;
        }

        public override float DesiredDistance(Core.EnemyBase ctx)
        {
            if (hitCollider == null || ctx.Perception.TargetCollider == null) return combatDistance;
            Transform boxTransform = hitCollider.transform;
            Vector3 center = boxTransform.TransformPoint(hitCollider.center);
            Vector3 half = Vector3.Scale(hitCollider.size, boxTransform.lossyScale) * 0.5f;
            Vector3 forward = ctx.transform.forward;
            float front = Vector3.Dot(center - ctx.transform.position, forward) +
                Mathf.Abs(Vector3.Dot(boxTransform.right, forward)) * half.x +
                Mathf.Abs(Vector3.Dot(boxTransform.up, forward)) * half.y +
                Mathf.Abs(Vector3.Dot(boxTransform.forward, forward)) * half.z;
            float targetRadius = Mathf.Max(ctx.Perception.TargetCollider.bounds.extents.x,
                ctx.Perception.TargetCollider.bounds.extents.z);
            return Mathf.Min(combatDistance, Mathf.Max(0.5f, front + targetRadius - 0.2f));
        }

        public override bool ShouldAttackImmediately(Core.EnemyBase ctx) =>
            ctx.HorizontalDistance < attackRange * 0.8f;

        protected override bool IsCrowded(Core.EnemyBase ctx) =>
            ctx.HorizontalDistance < attackRange * 0.8f;

        private void OnValidate() => combatDistance = Mathf.Max(combatDistance, attackRange);
    }
}
