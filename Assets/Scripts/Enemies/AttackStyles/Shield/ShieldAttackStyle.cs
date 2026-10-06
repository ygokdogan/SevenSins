using Combat.AttackIndicators;
using Enemies.Combat.States;
using Core;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Enemies.Combat
{
    [MovedFrom(true, "Enemies.Combat", "Assembly-CSharp", "ShieldBearerAttackStyle")]
    public sealed class ShieldAttackStyle : EnemyAttackStyle
    {
        [SerializeField, Min(0.1f)] private float bashRange = 2.8f;
        [SerializeField, Min(0f)] private float telegraphDuration = 0.5f;
        [SerializeField, Min(0f)] private float bashSpeed = 8f;
        [SerializeField, Min(0f)] private float bashDamage = 12f;
        [SerializeField, Min(0f)] private float bashPush = 12f;
        [SerializeField, Min(0f)] private float swordDamage = 8f;
        [SerializeField] private AttackDefense bashDefense = AttackDefense.ParryOnly;
        [SerializeField] private AttackDefense swordDefense = AttackDefense.Basic;
        public float TelegraphDuration => telegraphDuration;
        public float BashSpeed => bashSpeed;
        public float BashDamage => bashDamage;
        public float BashPush => bashPush;
        public float SwordDamage => swordDamage;
        public AttackDefense BashDefense => bashDefense;
        public AttackDefense SwordDefense => swordDefense;

        public override bool Supports(Core.EnemyBase enemy) => enemy is EnemyShield;

        public override float DesiredDistance(Core.EnemyBase ctx) => 1.5f;
        public override float EngagementRange(Core.EnemyBase ctx) => bashRange;
        protected override bool CanStartAttackInternal(Core.EnemyBase ctx)
        {
            if (ctx.Perception.Target == null || ctx.HorizontalDistance > bashRange) return false;
            Vector3 delta = ctx.Perception.Target.position - ctx.transform.position;
            delta.y = 0f;
            return Vector3.Dot(ctx.transform.forward, delta.normalized) > 0.8f;
        }
        public override IState<Core.EnemyBase> CreateState() => new ShieldAttackState(this);
        public override void FinishAttack(Core.EnemyBase ctx)
        { ScheduleNextAttack(); ctx.RequestTransition(ctx.States.Engage); }

    }
}
