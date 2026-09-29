using Enemies.Combat.States;
using Core;

namespace Enemies.Combat
{
    public class EnemySamurai : EnemyBase
    {
        private EnemyCombatStates states;
        protected override IState<EnemyBase> HitState => states.Hit;
        protected override IState<EnemyBase> StunState => states.Stun;

        protected override IState<EnemyBase> CreateInitialState()
        {
            var approach = new EnemyApproachState(attackDirectly: true);
            states = new EnemyCombatStates
            {
                Idle = new EnemyIdleState(),
                Patrol = new EnemyPatrolState(),
                Investigate = new EnemyInvestigateState(),
                Approach = approach,
                Engage = approach,
                Attack = AttackStyle.CreateState(),
                Escape = AttackStyle.CreateEscapeState(),
                Retreat = new EnemyRetreatState(),
                Hit = new EnemyHitState(),
                Stun = new EnemyStunState()
            };
            States = states;
            return states.Idle;
        }

    }
}
