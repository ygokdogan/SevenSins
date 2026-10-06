using Core;

namespace Enemies.States
{
    public class EnemyIdleState : IState<Core.EnemyBase>
    {
        public void Enter(Core.EnemyBase enemy)
        {
            enemy.Movement.Stop();
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            enemy.IsAnimationFinished = false;
            enemy.AnimationManager?.PlayAnimation("Idle");
        }

        public void Tick(Core.EnemyBase enemy, float dt)
        {
            if (!enemy.Movement.IsOnNavMesh) return;
            if (enemy.Perception.Target == null)
            {
                if (enemy.Perception.HasLastSeenPosition)
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

        public void Exit(Core.EnemyBase enemy)
        {
        }
    }
}
