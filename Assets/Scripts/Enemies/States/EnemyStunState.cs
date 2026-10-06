using Core;

namespace Enemies.States
{
    public class EnemyStunState : IState<Core.EnemyBase>
    {
        private float remaining;

        public void Enter(Core.EnemyBase enemy)
        {
            enemy.Movement.Stop();
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            remaining = enemy.Config.parryStunDuration;
            enemy.IsAttackWindowOpen = false;
            enemy.IsAnimationFinished = false;
            enemy.AnimationManager?.PlayAnimation("Idle", 0.08f);
            enemy.Feedback.ShowStun();
        }

        public void Tick(Core.EnemyBase enemy, float dt)
        {
            remaining -= dt;
            if (remaining <= 0f)
                enemy.RequestTransition(enemy.States.Idle);
        }

        public void Exit(Core.EnemyBase enemy)
        {
            enemy.Feedback.HideStun();
        }
    }
}
