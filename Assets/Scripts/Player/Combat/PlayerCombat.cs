using System.Collections;
using Combat;
using Core;
using Enemies.Combat;
using Input;
using Player.Combat.States;
using Player.Combat.VIsual_Feedback;
using Player.Movement;
using UnityEngine;

namespace Player.Combat
{
    [RequireComponent(typeof(PlayerManager))]
    public class PlayerCombat : MonoBehaviour, IDamageable
    {
        [Header("Starting Attacks")]
        [SerializeField] private AttackData defaultLightAttack;
        [SerializeField] private AttackData sheathedAttack;

        [Header("Hit Detection")]
        [SerializeField] private BoxCollider hitCollider;
        [SerializeField] private LayerMask hittableLayers;

        [Header("Hit Reaction")]
        [Tooltip("Base duration, multiplied by the incoming attack's Impact Multiplier.")]
        [SerializeField, Min(0f)] private float hitRecoveryDuration = 0.25f;

        [Header("Parry")]
        [SerializeField, Min(0f)] private float parryCooldown = 0.9f;
        
        private StateMachine<CombatContext> stateMachine;
        private CombatContext ctx;
        private InputHandler input;
        private PlayerAnimationManager animManager;
        private CameraManager cameraManager;
        private GuardImpactVisual guardImpactVisual;
        private PlayerHitFeedback hitFeedback;
        private Coroutine hitStopCoroutine;
        
        public bool IsSheathed => ctx.IsSheathed;
        public bool IsBlocking => ctx.IsBlocking;
        public IState<CombatContext> CurrentState => stateMachine?.CurrentState;

        private void Awake()
        {
            var player = GetComponent<PlayerManager>();
            player.Initialize();
            input = player.Input;
            animManager = player.Animation;
            cameraManager = player.Camera;
            guardImpactVisual = GetComponentInChildren<GuardImpactVisual>();
            hitFeedback = GetComponentInChildren<PlayerHitFeedback>();

            var states = new CombatStates
            {
                Idle = new CombatIdleState(),
                Attack = new CombatAttackState(),
                Sheathe = new CombatSheatheState(),
                Block = new CombatBlockState(),
                Hit = new CombatHitState()
            };

            ctx = new CombatContext
            {
                Transform = transform,
                AnimationManager = animManager,
                Controller = player.Controller,
                Input = input,
                HitCollider = hitCollider,
                HittableLayers = hittableLayers,
                CameraManager = cameraManager,
                HitRecoveryDuration = hitRecoveryDuration,
                ParryCooldown = parryCooldown,
                TriggerHitStop = TriggerHitStop,
                Player = player,
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
            animManager.OnAttackWindowOpened += OnAttackWindowOpened;
            animManager.OnAttackWindowClosed += OnAttackWindowClosed;
            
            stateMachine.ChangeState(states.Idle);
        }

        private void OnDestroy()
        {
            animManager.OnComboWindowOpened -= OnComboWindowOpened;
            animManager.OnComboWindowClosed -= OnComboWindowClosed;
            animManager.OnAnimationFinished -= OnAnimationFinished;
            animManager.OnLungeTriggered -= OnLungeTriggered;
            animManager.OnAttackWindowOpened -= OnAttackWindowOpened;
            animManager.OnAttackWindowClosed -= OnAttackWindowClosed;
        }

        private void OnDisable()
        {
            if (hitStopCoroutine != null)
            {
                Time.timeScale = 1f;
                hitStopCoroutine = null;
            }
        }

        public void TriggerHitStop(float duration)
        {
            if (duration <= 0f) return;
            if (hitStopCoroutine != null)
                StopCoroutine(hitStopCoroutine);
            hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            hitStopCoroutine = null;
        }
        
        public void TakeHit(HitInfo hitInfo)
        {
            if (hitInfo.attacker != null && CanBlock(hitInfo.attacker.transform))
            {
                bool parried = ctx.IsParrying;
                Vector3 point = hitInfo.hitPoint != Vector3.zero ? hitInfo.hitPoint : transform.position + Vector3.up;
                guardImpactVisual.Play(point, hitInfo.hitNormal, parried, hitInfo.impactMultiplier);
                cameraManager?.Shake(parried ? 1.4f : 0.9f, parried ? 0.22f : 0.15f);
                
                if (parried)
                {
                    var enemy = hitInfo.attacker.GetComponentInParent<EnemyCombat>();
                    if (enemy != null) enemy.Parried();
                    return;
                }
                
                Vector3 knockbackDirection = hitInfo.hitDirection;
                if (knockbackDirection.sqrMagnitude < 0.001f && hitInfo.attacker != null)
                    knockbackDirection = transform.position - hitInfo.attacker.transform.position;
                
                ctx.Player.ApplyKnockback(knockbackDirection, Mathf.Max(2f, hitInfo.knockbackForce * 0.3f));
                
            }
            else
            {
                Vector3 knockbackDirection = hitInfo.hitDirection;
                if (knockbackDirection.sqrMagnitude < 0.001f && hitInfo.attacker != null)
                    knockbackDirection = transform.position - hitInfo.attacker.transform.position;
                
                ctx.Player.ApplyKnockback(knockbackDirection, Mathf.Max(2f, hitInfo.knockbackForce));
                ctx.CurrentHit = hitInfo;
                stateMachine.ForceChangeState(ctx.States.Hit);
                cameraManager?.Shake(0.9f, 0.2f);
                hitFeedback.Play();
            }
        }

        public bool TryReflectProjectile(HitInfo hitInfo)
        {
            if (!ctx.IsParrying || hitInfo.attacker == null ||
                !CanBlock(hitInfo.attacker.transform)) return false;

            Vector3 point = hitInfo.hitPoint != Vector3.zero
                ? hitInfo.hitPoint : transform.position + Vector3.up;
            guardImpactVisual?.Play(point, hitInfo.hitNormal, true, hitInfo.impactMultiplier);
            cameraManager?.Shake(1.4f, 0.22f);
            return true;
        }

        private bool CanBlock(Transform attacker)
        {
            if (!ctx.IsBlocking) return false;
            
            Vector3 directionToAttacker = (attacker.position - transform.position).normalized;
            directionToAttacker.y = 0f;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            
            float dotProduct = Vector3.Dot(forward.normalized, directionToAttacker.normalized);
            return dotProduct >= 0.5f;
        }

        private void Update() => stateMachine.Tick(Time.deltaTime);

        private void OnComboWindowOpened()
        {
            if (stateMachine.CurrentState == ctx.States.Attack) ctx.IsComboWindowOpen = true;
        }
        private void OnComboWindowClosed() => ctx.IsComboWindowOpen = false;
        private void OnAnimationFinished() => ctx.IsAnimationFinished = true;
        private void OnLungeTriggered() => ctx.OnLungeTriggered?.Invoke();
        private void OnAttackWindowOpened()
        {
            if (stateMachine.CurrentState == ctx.States.Attack) ctx.IsAttackWindowOpen = true;
        }
        private void OnAttackWindowClosed() => ctx.IsAttackWindowOpen = false;
    }
}
