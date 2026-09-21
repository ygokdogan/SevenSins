using System;
using Combat;
using Core;
using Enemies.Combat.Utilities;
using Enemies.Combat.Visual_Feedback;
using UnityEngine;

namespace Enemies.Combat
{
    public class EnemyCombatContext
    {
        // References
        public Transform Transform;
        public EnemyAnimationManager AnimationManager;
        public EnemyMovement Movement;
        public EnemyLocomotionAnimation Locomotion;
        public EnemyAttackStyle AttackStyle;
        public EnemyConfig Config;
        public Transform Target;
        public Collider TargetCollider;
        public Vector3[] PatrolPoints;
        public Vector3 LastSeenPosition;
        public bool HasLastSeenPosition;

        public float SprintSpeed => Config.sprintSpeed;
        public float WalkSpeed => Config.walkSpeed;
        public float StalkRange => Config.stalkRange;
        public float RetreatDistance => Config.retreatDistance;
        public float RetreatDuration => Config.retreatDuration;
        public float PatrolWaitDuration => Config.patrolWaitDuration;

        public float HorizontalDistance
        {
            get
            {
                if (Target == null) return float.PositiveInfinity;
                Vector3 delta = Target.position - Transform.position;
                delta.y = 0f;
                return delta.magnitude;
            }
        }

        
        public HitFlash HitFlash;
        public HitJuice HitJuice;
        public HitFx HitFx;
        public HitInfo CurrentHit;
        public EnemyStunVisual StunVisual;
        public float StunDuration => Config.parryStunDuration;

        // State Machine
        public Action<IState<EnemyCombatContext>> RequestTransition;
        public EnemyCombatStates States;

        // Attack Configuration
        public float NextAttackTime;

        // Health
        public float CurrentHealth;

        // Runtime Flags
        public bool IsAttackWindowOpen;
        public bool IsAnimationFinished;
    }
}
