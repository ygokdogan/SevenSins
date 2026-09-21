using System;
using Input;
using Player.IK;
using UnityEngine;

namespace Player
{
    public class PlayerAnimationManager : MonoBehaviour
    {
        private Animator anim;
        public bool CanPlayAttackAnimation { get; private set; } = true;
        public bool IsPlayingAnimation { get; private set; } = false;

        public event Action OnComboWindowOpened;
        public event Action OnComboWindowClosed;
        public event Action OnAnimationFinished;
        public event Action OnLungeTriggered;
        public event Action OnAttackWindowOpened;
        public event Action OnAttackWindowClosed;

        private void Awake()
        {
            anim = GetComponent<Animator>();
        }

        public void PlayAttackAnimation(string animName, float crossFade = 0.1f)
        {
            if (!CanPlayAttackAnimation) return;
            
            IsPlayingAnimation = true;
            HandIKManager.SetWeight(1f);
            anim.CrossFadeInFixedTime(animName, crossFade);
        }

        public void PlaySheatheAnimation()
        {
            IsPlayingAnimation = true;
            HandIKManager.SetWeight(0f);
            anim.CrossFadeInFixedTime("Sheathe", 0.1f);
        }

        public void PlayUnsheatheAnimation()
        {
            IsPlayingAnimation = true;
            HandIKManager.SetWeight(1f);
            anim.CrossFadeInFixedTime("Unsheathe", 0.1f);
        }

        public void PlayBlockAnimation(float crossFade = 0.1f)
        {
            IsPlayingAnimation = true;
            HandIKManager.SetWeight(1f);
            anim.CrossFadeInFixedTime("block", crossFade);
        }

        public void StopBlockAnimation(float crossFade = 0.15f)
        {
            IsPlayingAnimation = false;
            anim.CrossFadeInFixedTime("Idle_Unsheathed", crossFade);
        }

        public void PlayHitReaction(bool sheathed)
        {
            IsPlayingAnimation = false;
            HandIKManager.SetWeight(sheathed ? 0f : 1f);
            anim.CrossFadeInFixedTime(sheathed ? "Idle_Sheathed" : "Idle_Unsheathed", 0.05f);
        }
        
        public void ComboWindowOpened() => OnComboWindowOpened?.Invoke();
        public void ComboWindowClosed() => OnComboWindowClosed?.Invoke();
        public void LungeTriggered() => OnLungeTriggered?.Invoke();
        public void AttackWindowOpened() => OnAttackWindowOpened?.Invoke();
        public void AttackWindowClosed() => OnAttackWindowClosed?.Invoke();
        
        public void AnimationFinished()
        {
            IsPlayingAnimation = false;
            OnAnimationFinished?.Invoke();
        }
    }
}
