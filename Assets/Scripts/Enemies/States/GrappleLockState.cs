using Core;

namespace Enemies.States
{
    // Owns the enemy's movement lock until explicit release or death.
    public sealed class GrappleLockState : IState<Core.EnemyBase>
    {
        public void Enter(Core.EnemyBase enemy)
        {
            enemy.Movement.EndDash();
            enemy.Movement.ClearKnockback();
            enemy.Movement.Stop();
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            enemy.IsAttackWindowOpen = false;
            enemy.IsAnimationFinished = false;
            enemy.AttackIndicator.Hide();
            // TODO(Animation): Play the grapple hold / pull animation.
            enemy.AnimationManager?.PlayAnimation("Idle", 0.08f);
        }

        // The grapple controller drives pull motion. This state intentionally makes no AI decisions.
        public void Tick(Core.EnemyBase enemy, float dt) { }

        public void Exit(Core.EnemyBase enemy)
        {
            enemy.Movement.EndGrapplePull();
            enemy.Movement.Stop();
            enemy.IsAttackWindowOpen = false;
            enemy.IsAnimationFinished = false;
        }
    }
}
