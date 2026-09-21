using Enemies.Combat.States;
using Core;

namespace Enemies.Combat
{
    public class EnemyCombatStates
    {
        public EnemyIdleState Idle;
        public EnemyPatrolState Patrol;
        public EnemyInvestigateState Investigate;
        public EnemyApproachState Approach;
        public EnemyStalkState Stalk;
        public IState<EnemyCombatContext> Attack;
        public IState<EnemyCombatContext> Escape;
        public EnemyRetreatState Retreat;
        public EnemyHitState Hit;
        public EnemyStunState Stun;
    }
}
