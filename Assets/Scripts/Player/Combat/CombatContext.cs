using System;
using Combat;
using Core;
using Input;
using Player.Combat.States;
using UnityEngine;

namespace Player.Combat
{
    public class CombatContext
    {
        public Transform Transform;
        public PlayerAnimationManager AnimationManager;
        public CharacterController Controller;
        public InputHandler Input;
        public PlayerManager Player;
        public BoxCollider HitCollider;
        public LayerMask HittableLayers;
        public CameraManager CameraManager;
        public float HitRecoveryDuration;
        public HitInfo CurrentHit;
        public float ParryCooldown;
        public float NextParryTime;
        public Action<float> TriggerHitStop;
        
        public Action<IState<CombatContext>> RequestTransition;
        public CombatStates States;

        public AttackData CurrentAttack;
        public AttackData DefaultLightAttack;
        public AttackData SheathedAttack;
        
        public Action OnLungeTriggered;
        
        public bool IsSheathed = true;
        public bool IsComboWindowOpen = false;
        public bool IsAnimationFinished = false;
        public bool IsAttackWindowOpen = false;
        public bool IsBlocking = false;
        public bool IsParrying = false;
    }

    public class CombatStates
    {
        public CombatIdleState Idle;
        public CombatAttackState Attack;
        public CombatSheatheState Sheathe;
        public CombatBlockState Block;
        public CombatHitState Hit;
    } 
}
