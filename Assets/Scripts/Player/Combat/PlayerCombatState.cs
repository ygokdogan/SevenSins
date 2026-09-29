using Core;
using UnityEngine;

namespace Player.Combat
{
    public abstract class PlayerCombatState : MonoBehaviour, IState<PlayerCombat>
    {
        protected PlayerCombatConfig config;
        protected PlayerManager player;

        public virtual void Initialize(PlayerCombat combat)
        {
            config = combat.config;
            player = combat.Player;
        }

        public abstract void Enter(PlayerCombat combat);
        public abstract void Tick(PlayerCombat combat, float deltaTime);
        public abstract void Exit(PlayerCombat combat);

        public virtual void OnComboWindowOpened() { }
        public virtual void OnComboWindowClosed() { }
        public virtual void OnAnimationFinished() { }
        public virtual void OnLungeTriggered() { }
        public virtual void OnAttackWindowOpened() { }
        public virtual void OnAttackWindowClosed() { }
    }
}
