using UnityEngine;

namespace Player.Combat
{
    [CreateAssetMenu(menuName = "Combat/Attack Data", fileName = "new Attack Data")]
    public class AttackData : ScriptableObject
    {
        [Header("Animation")]
        public string animationName;
        public float crossFadeDuration = 0.1f;
        
        [Header("Damage & Impact")]
        public float damage = 25f;
        public float poiseDamage = 10f;
        public float hitStopDuration = 0.05f;

        [Header("Movement Lunge")]
        public bool hasLunge = false;
        public float forwardImpulse = 6f;
        public float impulseDecay = 25f;
        
        [Header("Combo Chaining")]
        public AttackData nextLightAttack;
        public AttackData nextHeavyAttack;
        
        [Header("Juice & Effects")]
        //public CameraShakeProfile shakeProfile;
        public GameObject hitVfxPrefab;
        public AudioClip swingSfx;
    }
}
