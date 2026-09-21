using UnityEngine;

namespace Enemies.Combat
{
    [CreateAssetMenu(menuName = "Enemies/Enemy Config", fileName = "New Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        [Header("Vision & Search")]
        [Min(0.1f)] public float sightRange = 12f;
        [Range(1f, 360f)] public float fieldOfView = 120f;
        [Tooltip("Within this distance, a clear-line-of-sight target is noticed from any angle. Set to 0 to disable.")]
        [Min(0f)] public float closeAwarenessRange;
        public LayerMask sightLayers = ~0;
        [Min(0f)] public float searchDuration = 1.5f;

        [Header("Patrol")]
        [Tooltip("Local offsets from the enemy's spawn position.")]
        public Vector3[] patrolOffsets;
        [Min(0f)] public float patrolWaitDuration = 0.7f;

        [Header("Movement & Combat")]
        [Min(0.1f)] public float stalkRange = 6f;
        [Min(0f)] public float sprintSpeed = 5f;
        [Min(0f)] public float walkSpeed = 2f;
        [Min(0f)] public float retreatDistance = 2f;
        [Min(0f)] public float retreatDuration = 0.45f;

        [Header("Health & Parry")]
        [Min(0f)] public float maxHealth = 100f;
        [Min(0.1f)] public float parryStunDuration = 2f;
        [Min(0f)] public float parryKnockbackForce = 7f;
    }
}
