using Core;
using Enemies.Combat;
using Enemies.Combat.States;
using Enemies.States;

namespace Enemies.Core
{
    public sealed class EnemyCombatStates
    {
        public IState<EnemyBase> Idle { get; } = new EnemyIdleState();
        public IState<EnemyBase> Patrol { get; } = new EnemyPatrolState();
        public IState<EnemyBase> Investigate { get; } = new EnemyInvestigateState();
        public IState<EnemyBase> Hit { get; } = new EnemyHitState();
        public IState<EnemyBase> Stun { get; } = new EnemyStunState();
        public IState<EnemyBase> Grapple { get; } = new GrappleLockState();
        public IState<EnemyBase> Retreat { get; } = new EnemyRetreatState();
        public IState<EnemyBase> Approach { get; set; }
        public IState<EnemyBase> Engage { get; set; }
        public IState<EnemyBase> Attack { get; }
        public IState<EnemyBase> Escape { get; }

        public EnemyCombatStates(EnemyAttackStyle style)
        {
            Approach = new EnemyApproachState(attackDirectly: true);
            Engage = Approach;
            Attack = style.CreateState();
            Escape = style.CreateEscapeState() ?? Retreat;
        }
    }
}
