using Combat.AttackIndicators;
using Core;
using Enemies.Combat;
using Enemies.Combat.States;
using Enemies.Core;
using Enemies.Types.Shield;
using Player.Combat;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Utilities;

namespace Enemies.AttackStyles.Shield
{
    [MovedFrom(true, "Enemies.Combat", "Assembly-CSharp", "ShieldBearerAttackStyle")]
    public sealed class ShieldAttackStyle : EnemyAttackStyle
    {
        public AttackData bashAttack;
        public AttackData swordAttack;

        public float bashSpeed = 8f;
        public float bashDuration = 0.28f;
        public float bashDashDistance = 2.1f;
        [Range(0.5f, 1f)] public float bashStandRatio = 0.7f;
        
        public float swordLungeDistance = 3f;
        public float lungeMaxTime = 0.6f;
        public float lungeSpeed = 12f;

        public BoxCollider bashHitCollider;
        public BoxCollider hitCollider;
        public LayerMask hittableLayers;
        
        [Range(0.5f, 1f)] public float standRatio = 0.95f;

        public float animationTimeout = 1.5f;
        
        private bool Broken(EnemyBase ctx) => ctx is EnemyShield s && s.ShieldBroken;
        private BoxCollider ActiveBox(EnemyBase ctx) => Broken(ctx) ? hitCollider : bashHitCollider;
        
        private float Reach(EnemyBase ctx)
        {
            float reach = MeleeHitUtility.ForwardReach(ActiveBox(ctx), ctx.transform);
            var tc = ctx.Perception.TargetCollider;
            if (tc != null) reach += Mathf.Max(tc.bounds.extents.x, tc.bounds.extents.z);
            return Broken(ctx) ? reach : reach * bashStandRatio + bashDashDistance;
        }
        
        public override float DesiredDistance(EnemyBase ctx) =>
            Broken(ctx) ? AttackRange(ctx) * standRatio : Reach(ctx) * standRatio;
        public override float EngagementRange(EnemyBase ctx) => Reach(ctx) * standRatio;

        protected override bool CanStartAttackInternal(EnemyBase ctx)
        {
            var target = ctx.Perception.TargetCollider;
            if (ctx.Perception.Target == null || target == null) return false;
            
            Vector3 delta = ctx.Perception.Target.position - ctx.transform.position;
            delta.y = 0f;
            if (Vector3.Dot(ctx.transform.forward, delta.normalized) < 0.8f) return false;
            
            if (Broken(ctx)) return hitCollider != null &&
                (MeleeHitUtility.IsOverlapping(hitCollider, target) ||
                 MeleeHitUtility.BoxGap(hitCollider, target) * standRatio <= swordLungeDistance);
            return ctx.HorizontalDistance <= Reach(ctx);
        }
        
        public override IState<EnemyBase> CreateState() => new ShieldAttackState(this);
        public override void FinishAttack(EnemyBase ctx)
        { 
            ScheduleNextAttack();
            ctx.RequestTransition(ctx.States.Engage);
        }
        
        public float AttackRange(EnemyBase ctx)
        {
            float reach = MeleeHitUtility.ForwardReach(hitCollider, ctx.transform);
            var target = ctx.Perception.TargetCollider;

            float targetRadius = target != null ? Mathf.Max(target.bounds.extents.x, target.bounds.extents.z) : 0f;
            return reach + targetRadius;
        }

    }
}
