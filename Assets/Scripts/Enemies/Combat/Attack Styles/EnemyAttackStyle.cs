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

        public abstract IState<EnemyBase> CreateState();
        public virtual IState<EnemyBase> CreateEscapeState() => null;
        public abstract float DesiredDistance(EnemyBase ctx);
        public abstract bool CanStartAttack(EnemyBase ctx);
        public virtual float EngagementRange(EnemyBase ctx) => ctx.StalkRange;
        public virtual bool MoveWhileStalking(EnemyBase ctx, float dt) => false;
        public virtual bool TryStartEscape(EnemyBase ctx) => false;
        public virtual bool ShouldAttackImmediately(EnemyBase ctx) => false;

        protected virtual bool IsCrowded(EnemyBase ctx) => false;

        public virtual void FinishAttack(EnemyBase ctx)
        {
            ScheduleNextAttack(ctx);
            bool retreat = IsCrowded(ctx) || Random.value < retreatChance;
            ctx.RequestTransition(retreat ? ctx.States.Retreat : ctx.States.Engage);
        }

        protected void ScheduleNextAttack(EnemyBase ctx) =>
            ctx.NextAttackTime = Time.time + Random.Range(minAttackCooldown,
                Mathf.Max(minAttackCooldown, maxAttackCooldown));
    }
}
