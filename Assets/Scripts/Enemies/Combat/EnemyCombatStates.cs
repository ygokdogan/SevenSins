using Core;

namespace Enemies.Combat
{
    public class EnemyCombatStates
    {
        public IState<EnemyBase> Idle;
        public IState<EnemyBase> Patrol;
        public IState<EnemyBase> Investigate;
        public IState<EnemyBase> Approach;
        public IState<EnemyBase> Engage;
        public IState<EnemyBase> Attack;
        public IState<EnemyBase> Escape;
        public IState<EnemyBase> Retreat;
        public IState<EnemyBase> Hit;
        public IState<EnemyBase> Stun;
    }
}
