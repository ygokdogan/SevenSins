using Core;

namespace Enemies.Combat.States
{
    public class EnemyStunState : IState<EnemyBase>
    {
        private float remaining;

        public void Enter(EnemyBase enemy)
        {
            enemy.Movement.Stop();
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            remaining = enemy.StunDuration;
            enemy.IsAttackWindowOpen = false;
            enemy.IsAnimationFinished = false;
            enemy.AnimationManager?.PlayAnimation("Idle", 0.08f);
            enemy.StunVisual?.Show();
        }

        public void Tick(EnemyBase enemy, float dt)
        {
            remaining -= dt;
            if (remaining <= 0f)
                enemy.RequestTransition(enemy.States.Idle);
        }

        public void Exit(EnemyBase enemy)
        {
            enemy.StunVisual?.Hide();
        }
    }
}
