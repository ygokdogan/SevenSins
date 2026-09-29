using System.Collections;
using Combat;
using Core;
using Enemies.Combat;
using Player.Combat.States;
using Player.Combat.VIsual_Feedback;
using UnityEngine;

namespace Player.Combat
{
    [RequireComponent(typeof(PlayerManager))]
    public class PlayerCombat : MonoBehaviour, IDamageable
    {
        public PlayerCombatConfig config = new();
        public event System.Action HitConfirmed;
        public void NotifyHitConfirmed() => HitConfirmed?.Invoke();

        [Header("Hit Detection")]
        [SerializeField] private BoxCollider hitCollider;
        [SerializeField] private LayerMask hittableLayers;

        private StateMachine<PlayerCombat> stateMachine;
        private PlayerAnimationManager animationManager;
        private CameraManager cameraManager;
        private GuardImpactVisual guardImpactVisual;
        private PlayerHitFeedback hitFeedback;
        private Coroutine hitStopCoroutine;
        
        public PlayerManager Player { get; private set; }
        public CombatIdleState IdleState { get; private set; }
        public CombatAttackState AttackState { get; private set; }
        public CombatSheatheState SheatheState { get; private set; }
        public CombatBlockState BlockState { get; private set; }
        public CombatHitState HitState { get; private set; }

        public BoxCollider HitCollider => hitCollider;
        public LayerMask HittableLayers => hittableLayers;
        public bool IsSheathed { get; set; } = true;
        public bool IsBlocking => CurrentState != null && CurrentState == BlockState;
        public bool IsParrying => IsBlocking && BlockState.IsParrying;
        public PlayerCombatState CurrentState => stateMachine?.CurrentState as PlayerCombatState;

        public void ChangeState(PlayerCombatState nextState) => stateMachine.ChangeState(nextState);
        public void ForceChangeState(PlayerCombatState nextState) => stateMachine.ForceChangeState(nextState);

        private void Awake()
        {
            Player = GetComponent<PlayerManager>();
            Player.Initialize();
            animationManager = Player.AnimationManager;
            cameraManager = Player.Camera;
            guardImpactVisual = GetComponentInChildren<GuardImpactVisual>();
            hitFeedback = GetComponentInChildren<PlayerHitFeedback>();

            IdleState = Player.GetCombatState<CombatIdleState>();
            AttackState = Player.GetCombatState<CombatAttackState>();
            SheatheState = Player.GetCombatState<CombatSheatheState>();
            BlockState = Player.GetCombatState<CombatBlockState>();
            HitState = Player.GetCombatState<CombatHitState>();

            IdleState.Initialize(this);
            AttackState.Initialize(this);
            SheatheState.Initialize(this);
            BlockState.Initialize(this);
            HitState.Initialize(this);

            stateMachine = new StateMachine<PlayerCombat>(this);

            animationManager.OnComboWindowOpened += OnComboWindowOpened;
            animationManager.OnComboWindowClosed += OnComboWindowClosed;
            animationManager.OnAnimationFinished += OnAnimationFinished;
            animationManager.OnLungeTriggered += OnLungeTriggered;
            animationManager.OnAttackWindowOpened += OnAttackWindowOpened;
            animationManager.OnAttackWindowClosed += OnAttackWindowClosed;
            
            ChangeState(IdleState);
        }

        private void OnDestroy()
        {
            if (animationManager == null) return;
            animationManager.OnComboWindowOpened -= OnComboWindowOpened;
            animationManager.OnComboWindowClosed -= OnComboWindowClosed;
            animationManager.OnAnimationFinished -= OnAnimationFinished;
            animationManager.OnLungeTriggered -= OnLungeTriggered;
            animationManager.OnAttackWindowOpened -= OnAttackWindowOpened;
            animationManager.OnAttackWindowClosed -= OnAttackWindowClosed;
        }

        private void OnEnable()
        {
            if (stateMachine != null && CurrentState == null) ChangeState(IdleState);
        }

        private void OnDisable()
        {
            if (stateMachine != null) ChangeState(null);
            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
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
                bool parried = IsParrying;
                Vector3 point = hitInfo.hitPoint != Vector3.zero ? hitInfo.hitPoint : transform.position + Vector3.up;
                guardImpactVisual?.Play(point, hitInfo.hitNormal, parried, hitInfo.impactMultiplier);
                cameraManager?.Shake(parried ? 1.4f : 0.9f, parried ? 0.22f : 0.15f);
                
                if (parried)
                {
                    var enemy = hitInfo.attacker.GetComponentInParent<EnemyBase>();
                    if (enemy != null) enemy.Parried();
                    return;
                }
                
                Vector3 knockbackDirection = hitInfo.hitDirection;
                if (knockbackDirection.sqrMagnitude < 0.001f && hitInfo.attacker != null)
                    knockbackDirection = transform.position - hitInfo.attacker.transform.position;
                
                Player.Movement.ApplyKnockback(knockbackDirection, Mathf.Max(2f, hitInfo.knockbackForce * 0.3f));
                
            }
            else
            {
                Vector3 knockbackDirection = hitInfo.hitDirection;
                if (knockbackDirection.sqrMagnitude < 0.001f && hitInfo.attacker != null)
                    knockbackDirection = transform.position - hitInfo.attacker.transform.position;
                
                Player.Movement.ApplyKnockback(knockbackDirection, Mathf.Max(2f, hitInfo.knockbackForce));
                HitState.Begin(this, hitInfo);
                cameraManager?.Shake(0.9f, 0.2f);
                hitFeedback?.Play();
            }
        }

        public bool TryReflectProjectile(HitInfo hitInfo)
        {
            if (!IsParrying || hitInfo.attacker == null ||
                !CanBlock(hitInfo.attacker.transform)) return false;

            Vector3 point = hitInfo.hitPoint != Vector3.zero
                ? hitInfo.hitPoint : transform.position + Vector3.up;
            guardImpactVisual?.Play(point, hitInfo.hitNormal, true, hitInfo.impactMultiplier);
            cameraManager?.Shake(1.4f, 0.22f);
            return true;
        }

        private bool CanBlock(Transform attacker)
        {
            if (!IsBlocking) return false;
            
            Vector3 directionToAttacker = (attacker.position - transform.position).normalized;
            directionToAttacker.y = 0f;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            
            float dotProduct = Vector3.Dot(forward.normalized, directionToAttacker.normalized);
            return dotProduct >= 0.5f;
        }

        private void Update() => stateMachine.Tick(Time.deltaTime);

        private void OnComboWindowOpened() => CurrentState?.OnComboWindowOpened();
        private void OnComboWindowClosed() => CurrentState?.OnComboWindowClosed();
        private void OnAnimationFinished() => CurrentState?.OnAnimationFinished();
        private void OnLungeTriggered() => CurrentState?.OnLungeTriggered();
        private void OnAttackWindowOpened() => CurrentState?.OnAttackWindowOpened();
        private void OnAttackWindowClosed() => CurrentState?.OnAttackWindowClosed();
    }
}
