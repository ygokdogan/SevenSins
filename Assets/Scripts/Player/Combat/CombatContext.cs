using System;
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
        public PlayerMovement Movement;
        
        public Action<IState<CombatContext>> RequestTransition;
        public CombatStates States;

        public AttackData CurrentAttack;
        public AttackData DefaultLightAttack;
        public AttackData SheathedAttack;
        
        public Action OnLungeTriggered;
        public bool IsSheathed = true;
        public bool IsComboWindowOpen = false;
        public bool IsAnimationFinished = false;
    }

    public class CombatStates
    {
        public CombatIdleState Idle;
        public CombatAttackState Attack;
        public CombatSheatheState Sheathe;
    } 
}