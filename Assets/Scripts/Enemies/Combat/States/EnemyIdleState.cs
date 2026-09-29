using Core;

namespace Enemies.Combat.States
{
    public class EnemyIdleState : IState<EnemyBase>
    {
        public void Enter(EnemyBase enemy)
        {
            enemy.Movement.Stop();
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            enemy.IsAnimationFinished = false;
            enemy.AnimationManager?.PlayAnimation("Idle");
        }

        public void Tick(EnemyBase enemy, float dt)
        {
            if (!enemy.Movement.IsOnNavMesh) return;
            if (enemy.Target == null)
            {
                if (enemy.HasLastSeenPosition)
                {
                    enemy.RequestTransition(enemy.States.Investigate);
                    return;
                }
                if (enemy.PatrolPoints != null && enemy.PatrolPoints.Length > 0)
                    enemy.RequestTransition(enemy.States.Patrol);
                return;
            }
            enemy.TransitionToCombat();
        }

        public void Exit(EnemyBase enemy)
        {
        }
    }
}
