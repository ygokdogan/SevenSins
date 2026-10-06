using System;
using UnityEngine;

namespace Enemies.Combat
{
    public class EnemyAnimationManager : MonoBehaviour
    {
        private Animator anim;
        public bool HasController => anim != null && anim.runtimeAnimatorController != null;

        public event Action OnAttackWindowOpened;
        public event Action OnAttackWindowClosed;
        public event Action OnAnimationFinished;

        private void Awake()
        {
            anim = GetComponent<Animator>();
        }

        public bool PlayAnimation(string animName, float crossFade = 0.1f)
        {
            if (!HasController || string.IsNullOrWhiteSpace(animName)) return false;
            int hash = Animator.StringToHash(animName);
            if (!anim.HasState(0, hash)) return false;
            anim.CrossFadeInFixedTime(hash, crossFade);
            return true;
        }
        
        public void AttackWindowOpened() => OnAttackWindowOpened?.Invoke();
        public void AttackWindowClosed() => OnAttackWindowClosed?.Invoke();
        public void AnimationFinished() => OnAnimationFinished?.Invoke();
    }
}
