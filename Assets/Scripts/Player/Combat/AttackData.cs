using UnityEngine;
using UnityEngine.Serialization;

namespace Player.Combat
{
    [CreateAssetMenu(menuName = "Combat/Attack Data", fileName = "new Attack Data")]
    public class AttackData : ScriptableObject
    {
        [Header("Animation")]
        public string animationName;
        public float crossFadeDuration = 0.1f;
        [Header("Telegraph")]
        [Min(0f)] public float telegraphDuration = 0.25f;
        [Tooltip("Leave empty when this attack has no telegraph animation.")]
        public string telegraphAnimation;
        
        [Header("Defense & Attack Indicator")]
        [Tooltip("Basic: block/parry. ParryOnly: bypasses held guard. DodgeOnly: bypasses guard and parry.")]
        public global::Combat.AttackIndicators.AttackDefense defense;

        [Header("Damage & Impact")]
        public float damage = 25f;
        public float poiseDamage = 10f;
        public float hitStopDuration = 0.05f;
        [Min(0f)] public float knockbackForce = 6f;

        [Header("Impact")]
        [FormerlySerializedAs("hitReactionMultiplier")]
        [Tooltip("Scales hit recovery, enemy recoil, and hit audio/VFX intensity.")]
        [Min(0f)] public float impactMultiplier = 1f;

        [Header("Movement Lunge")]
        public bool hasLunge = false;
        public float forwardImpulse = 6f;
        public float impulseDecay = 25f;
        
        [Header("Combo Chaining")]
        public AttackData nextLightAttack;
        public AttackData nextHeavyAttack;
        
        [Header("Juice & Effects")]
        public GameObject hitVfxPrefab;
        public AudioClip swingSfx;
    }
}
