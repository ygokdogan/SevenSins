using Core;
using Enemies.Combat.States;

namespace Enemies.Combat
{
    public class EnemyArcher : EnemyBase
    {
        private EnemyCombatStates states;
        protected override IState<EnemyBase> HitState => states.Hit;
        protected override IState<EnemyBase> StunState => states.Stun;

        protected override IState<EnemyBase> CreateInitialState()
        {
            states = new EnemyCombatStates
            {
                Idle = new EnemyIdleState(),
                Patrol = new EnemyPatrolState(),
                Investigate = new EnemyInvestigateState(),
                Approach = new EnemyApproachState(),
                Engage = new EnemyStalkState(),
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
