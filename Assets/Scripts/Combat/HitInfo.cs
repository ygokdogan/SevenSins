using UnityEngine;

namespace Combat
{
    public struct HitInfo
    {
        public Combat.AttackIndicators.AttackDefense defense;
        public float damage;
        public float poiseDamage;
        public float impactMultiplier;
        public float knockbackForce;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public Vector3 hitDirection;
        public GameObject attacker;
    }
}
