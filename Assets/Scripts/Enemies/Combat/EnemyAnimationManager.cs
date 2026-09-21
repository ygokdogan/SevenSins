using System;
using UnityEngine;

namespace Enemies.Combat
{
    public class EnemyAnimationManager : MonoBehaviour
    {
        private Animator anim;

        public event Action OnAttackWindowOpened;
        public event Action OnAttackWindowClosed;
        public event Action OnAnimationFinished;

        private void Awake()
        {
            anim = GetComponent<Animator>();
        }

        public void PlayAnimation(string animName, float crossFade = 0.1f)
        {
            anim.CrossFadeInFixedTime(animName, crossFade);
        }
        
        public void AttackWindowOpened() => OnAttackWindowOpened?.Invoke();
        public void AttackWindowClosed() => OnAttackWindowClosed?.Invoke();
        public void AnimationFinished() => OnAnimationFinished?.Invoke();
    }
}
