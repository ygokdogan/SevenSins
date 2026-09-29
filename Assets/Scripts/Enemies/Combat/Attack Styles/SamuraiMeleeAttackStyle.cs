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

        public BoxCollider HitCollider => hitCollider;
        public LayerMask HittableLayers => hittableLayers;
        public float AttackRange => attackRange;
        public float TelegraphAdvance => telegraphAdvance;
        public float LungeDistance => lungeDistance;
        public float LungeSpeed => lungeSpeed;

        public override IState<EnemyBase> CreateState() => new EnemyAttackState(this);

        public bool IsTargetInAttackBox(EnemyBase ctx)
        {
            return hitCollider != null && ctx.TargetCollider != null &&
                Physics.ComputePenetration(hitCollider, hitCollider.transform.position,
                    hitCollider.transform.rotation, ctx.TargetCollider,
                    ctx.TargetCollider.transform.position, ctx.TargetCollider.transform.rotation,
                    out _, out _);
        }

        public float AttackBoxGap(EnemyBase ctx)
        {
            if (hitCollider == null || ctx.TargetCollider == null) return float.PositiveInfinity;
            Vector3 targetPoint = ctx.TargetCollider.ClosestPoint(hitCollider.bounds.center);
            Vector3 boxPoint = hitCollider.ClosestPoint(targetPoint);
            return Vector3.Distance(boxPoint, ctx.TargetCollider.ClosestPoint(boxPoint));
        }

        public override bool CanStartAttack(EnemyBase ctx)
        {
            if (attackData == null || ctx.Target == null || hitCollider == null ||
                ctx.TargetCollider == null) return false;
            Vector3 toward = ctx.Target.position - ctx.Transform.position;
            toward.y = 0f;
            if (Vector3.Dot(ctx.Transform.forward, toward) <= 0f) return false;
            return IsTargetInAttackBox(ctx) || AttackBoxGap(ctx) <= lungeDistance;
        }

        public override float DesiredDistance(EnemyBase ctx)
        {
            if (hitCollider == null || ctx.TargetCollider == null) return combatDistance;
            Transform boxTransform = hitCollider.transform;
            Vector3 center = boxTransform.TransformPoint(hitCollider.center);
            Vector3 half = Vector3.Scale(hitCollider.size, boxTransform.lossyScale) * 0.5f;
            Vector3 forward = ctx.Transform.forward;
            float front = Vector3.Dot(center - ctx.Transform.position, forward) +
                Mathf.Abs(Vector3.Dot(boxTransform.right, forward)) * half.x +
                Mathf.Abs(Vector3.Dot(boxTransform.up, forward)) * half.y +
                Mathf.Abs(Vector3.Dot(boxTransform.forward, forward)) * half.z;
            float targetRadius = Mathf.Max(ctx.TargetCollider.bounds.extents.x,
                ctx.TargetCollider.bounds.extents.z);
            return Mathf.Min(combatDistance, Mathf.Max(0.5f, front + targetRadius - 0.2f));
        }

        public override bool ShouldAttackImmediately(EnemyBase ctx) =>
            ctx.HorizontalDistance < attackRange * 0.8f;

        protected override bool IsCrowded(EnemyBase ctx) =>
            ctx.HorizontalDistance < attackRange * 0.8f;

        private void OnValidate() => combatDistance = Mathf.Max(combatDistance, attackRange);
    }
}
