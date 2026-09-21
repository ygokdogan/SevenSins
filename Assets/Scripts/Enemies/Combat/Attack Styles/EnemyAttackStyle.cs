using Core;
using Player.Combat;
using UnityEngine;

namespace Enemies.Combat
{
    public abstract class EnemyAttackStyle : MonoBehaviour
    {
        [SerializeField] protected AttackData attackData;
        [SerializeField, Min(0f)] private float minAttackCooldown = 0.65f;
        [SerializeField, Min(0f)] private float maxAttackCooldown = 1.1f;
        [SerializeField, Range(0f, 1f)] private float retreatChance = 0.35f;

        public AttackData AttackData => attackData;

        public abstract IState<EnemyCombatContext> CreateState();
        public virtual IState<EnemyCombatContext> CreateEscapeState() => null;
        public abstract float DesiredDistance(EnemyCombatContext ctx);
        public abstract bool CanStartAttack(EnemyCombatContext ctx);
        public virtual float EngagementRange(EnemyCombatContext ctx) => ctx.StalkRange;
        public virtual bool MoveWhileStalking(EnemyCombatContext ctx, float dt) => false;
        public virtual bool TryStartEscape(EnemyCombatContext ctx) => false;
        public virtual bool ShouldAttackImmediately(EnemyCombatContext ctx) => false;

        protected virtual bool IsCrowded(EnemyCombatContext ctx) => false;

        public virtual void FinishAttack(EnemyCombatContext ctx)
        {
            ScheduleNextAttack(ctx);
            bool retreat = IsCrowded(ctx) || Random.value < retreatChance;
            ctx.RequestTransition(retreat ? ctx.States.Retreat : ctx.States.Stalk);
        }

        protected void ScheduleNextAttack(EnemyCombatContext ctx) =>
            ctx.NextAttackTime = Time.time + Random.Range(minAttackCooldown,
                Mathf.Max(minAttackCooldown, maxAttackCooldown));
    }
}
