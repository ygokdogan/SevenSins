using System;
using Combat;
using UnityEngine;

namespace Player.Combat
{
    [Serializable]
    public class PlayerCombatConfig
    {
        public AttackData DefaultLightAttack;
        public AttackData SheathedAttack;
        [Min(0f)] public float HitRecoveryDuration = 0.25f;
        [Min(0f)] public float ParryCooldown = 0.9f;
        [Min(0f)] public float ParryWindowDuration = 0.2f;
        [Min(0f)] public float BlockMoveSpeedMultiplier = 0.7f;
    }
}
