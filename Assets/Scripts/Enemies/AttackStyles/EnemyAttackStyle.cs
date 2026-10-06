using Core;
using Player.Combat;
using UnityEngine;

namespace Enemies.Combat
{
    [DisallowMultipleComponent]
    public abstract class EnemyAttackStyle : MonoBehaviour
    {
        [SerializeField] protected AttackData attackData;
        [SerializeField, Min(0f)] private float minAttackCooldown = 0.65f;
        [SerializeField, Min(0f)] private float maxAttackCooldown = 1.1f;
        [SerializeField, Range(0f, 1f)] private float retreatChance = 0.35f;

        private float nextAttackTime;

        public AttackData AttackData => attackData;

        public virtual bool Supports(Core.EnemyBase enemy) => true;
        public abstract IState<Core.EnemyBase> CreateState();
        public virtual IState<Core.EnemyBase> CreateEscapeState() => null;
        public abstract float DesiredDistance(Core.EnemyBase ctx);
        public virtual float EngagementRange(Core.EnemyBase ctx) => ctx.Config.stalkRange;
        public virtual bool MoveWhileStalking(Core.EnemyBase ctx, float dt) => false;
        public virtual bool TryStartEscape(Core.EnemyBase ctx) => false;
        public virtual bool ShouldAttackImmediately(Core.EnemyBase ctx) => false;

        public bool CanStartAttack(Core.EnemyBase ctx) =>
            Time.time >= nextAttackTime && CanStartAttackInternal(ctx);

        protected abstract bool CanStartAttackInternal(Core.EnemyBase ctx);
        protected virtual bool IsCrowded(Core.EnemyBase ctx) => false;

        public virtual void FinishAttack(Core.EnemyBase ctx)
        {
            ScheduleNextAttack();
            bool retreat = IsCrowded(ctx) || Random.value < retreatChance;
            ctx.RequestTransition(retreat ? ctx.States.Retreat : ctx.States.Engage);
        }

        protected void ScheduleNextAttack() =>
            nextAttackTime = Time.time + Random.Range(minAttackCooldown,
                Mathf.Max(minAttackCooldown, maxAttackCooldown));
    }
}
