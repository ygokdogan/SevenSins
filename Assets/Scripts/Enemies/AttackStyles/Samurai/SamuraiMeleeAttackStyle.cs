using Core;
using Enemies.Combat;
using Enemies.Core;
using UnityEngine;
using Utilities;

namespace Enemies.AttackStyles.Samurai
{
    public class SamuraiMeleeAttackStyle : EnemyAttackStyle
    {
        [Header("Melee Range")]
        [SerializeField] private BoxCollider hitCollider;
        [SerializeField] private LayerMask hittableLayers;
        [SerializeField, Min(0.1f)] private float combatDistance = 2.6f;
        [SerializeField, Range(0.5f, 1f)] private float standRatio = 0.95f;

        [Header("Attack Movement")]
        [SerializeField, Min(0f)] private float telegraphAdvance = 0.35f;
        [SerializeField, Min(0f)] private float lungeDistance = 0.8f;
        [SerializeField, Min(0f)] private float lungeSpeed = 6f;

        public BoxCollider HitCollider => hitCollider;
        public LayerMask HittableLayers => hittableLayers;
        public float TelegraphAdvance => telegraphAdvance;
        public float LungeDistance => lungeDistance;
        public float LungeSpeed => lungeSpeed;

        public override IState<EnemyBase> CreateState() => new SamuraiAttackState(this);

        public float AttackRange(EnemyBase ctx)
        {
            if (hitCollider == null) return combatDistance;
            
            float reach = MeleeHitUtility.ForwardReach(hitCollider, ctx.transform);
            var target = ctx.Perception.TargetCollider;

            float targetRadius = target != null ? Mathf.Max(target.bounds.extents.x, target.bounds.extents.z) : 0f;
            return reach + targetRadius;
        }

        protected override bool CanStartAttackInternal(EnemyBase ctx)
        {
            if (attackData == null || ctx.Perception.Target == null || hitCollider == null ||
                ctx.Perception.TargetCollider == null) return false;
            Vector3 toward = ctx.Perception.Target.position - ctx.transform.position;
            toward.y = 0f;
            if (Vector3.Dot(ctx.transform.forward, toward) <= 0f) return false;
            return MeleeHitUtility.IsOverlapping(hitCollider, ctx.Perception.TargetCollider) || MeleeHitUtility.BoxGap(hitCollider, ctx.Perception.TargetCollider) <= lungeDistance;
        }

        public override float DesiredDistance(Core.EnemyBase ctx)
        {
            if (hitCollider == null || ctx.Perception.TargetCollider == null) return combatDistance;
            return AttackRange(ctx) * standRatio;
        }

        public override bool ShouldAttackImmediately(EnemyBase ctx) =>
            ctx.HorizontalDistance < AttackRange(ctx) * 0.9f;

        protected override bool IsCrowded(EnemyBase ctx) =>
            ctx.HorizontalDistance < AttackRange(ctx) * 0.8f;
        
    }
}
