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
            anim.Play("Sheathe");
        }

        public void PlayUnsheatheAnimation()
        {
            IsPlayingAnimation = true;
            HandIKManager.SetWeight(1f);
            anim.Play("Unsheathe");
        }
        
        public void ComboWindowOpened() => OnComboWindowOpened?.Invoke();
        public void ComboWindowClosed() => OnComboWindowClosed?.Invoke();
        public void LungeTriggered() => OnLungeTriggered?.Invoke();
        
        public void AnimationFinished()
        {
            IsPlayingAnimation = false;
            OnAnimationFinished?.Invoke();
        }
    }
}