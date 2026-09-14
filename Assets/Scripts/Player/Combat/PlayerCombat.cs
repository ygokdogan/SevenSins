using Input;
using Player.Combat.States;
using UnityEngine;

namespace Player.Combat
{
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Starting Attacks")]
        [SerializeField] private AttackData defaultLightAttack;
        [SerializeField] private AttackData sheathedAttack;
        
        private StateMachine<CombatContext> stateMachine;
        private CombatContext ctx;
        private InputHandler input;
        private PlayerAnimationManager animManager;
        
        public bool IsSheathed => ctx.IsSheathed;
        public IState<CombatContext> CurrentState => stateMachine?.CurrentState;

        private void Awake()
        {
            input = GetComponent<InputHandler>();
            animManager = GetComponentInChildren<PlayerAnimationManager>();

            var states = new CombatStates
            {
                Idle = new CombatIdleState(),
                Attack = new CombatAttackState(),
                Sheathe = new CombatSheatheState()
            };

            ctx = new CombatContext
            {
                Transform = transform,
                AnimationManager = animManager,
                Controller = GetComponent<CharacterController>(),
                Input = input,
                Movement = GetComponent<PlayerMovement>(),
                States = states,
                DefaultLightAttack = defaultLightAttack,
                SheathedAttack = sheathedAttack,
                IsSheathed = true
            };
            
            stateMachine = new StateMachine<CombatContext>(ctx);
            ctx.RequestTransition = stateMachine.ChangeState;
            
            animManager.OnComboWindowOpened += OnComboWindowOpened;
            animManager.OnComboWindowClosed += OnComboWindowClosed;
            animManager.OnAnimationFinished += OnAnimationFinished;
            animManager.OnLungeTriggered += OnLungeTriggered;
            
            stateMachine.ChangeState(states.Idle);
        }

        private void OnDestroy()
        {
            animManager.OnComboWindowOpened -= OnComboWindowOpened;
            animManager.OnComboWindowClosed -= OnComboWindowClosed;
            animManager.OnAnimationFinished -= OnAnimationFinished;
            animManager.OnLungeTriggered -= OnLungeTriggered;
        }

        private void Update() => stateMachine.Tick(Time.deltaTime);

        private void OnComboWindowOpened() => ctx.IsComboWindowOpen = true;
        private void OnComboWindowClosed() => ctx.IsComboWindowOpen = false;
        private void OnAnimationFinished() => ctx.IsAnimationFinished = true;
        private void OnLungeTriggered() => ctx.OnLungeTriggered?.Invoke();
    }
}
