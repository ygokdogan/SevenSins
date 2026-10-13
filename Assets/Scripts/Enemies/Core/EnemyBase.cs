using Combat;
using Combat.AttackIndicators;
using Core;
using Enemies.Animation;
using Enemies.AttackStyles;
using Enemies.Combat;
using Enemies.Feedback;
using Enemies.HUD;
using UnityEngine;

namespace Enemies.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMovement), typeof(EnemyLocomotionAnimation), typeof(EnemyAttackIndicator))]
    public abstract partial class EnemyBase : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyConfig config;
        [SerializeField] private Transform player;
        [Tooltip("Optional. Bind the Animator on your manually authored hand/weapon rig.")]
        [SerializeField] private EnemyAnimationManager animationManager;
        [SerializeField] private bool showPatrolGizmos = true;

        public Transform lockPoint;
        
        private StateMachine<EnemyBase> stateMachine;
        private Collider bodyCollider;
        private bool isDead;

        public EnemyConfig Config => config;
        public EnemyMovement Movement { get; private set; }
        public EnemyLocomotionAnimation Locomotion { get; private set; }
        public EnemyAttackStyle AttackStyle { get; private set; }
        public EnemyAnimationManager AnimationManager => animationManager;
        public EnemyAttackIndicator AttackIndicator { get; private set; }
        public EnemyPerception Perception { get; private set; }
        public EnemyFeedback Feedback { get; private set; }
        public EnemyHealthBar HealthBar { get; private set; }
        public EnemyCombatStates States { get; private set; }
        public IState<EnemyBase> CurrentState => stateMachine?.CurrentState;
        public bool IsAlive => stateMachine != null && !isDead;
        public float CurrentHealth { get; private set; }
        public HitInfo CurrentHit { get; private set; }
        public Vector3[] PatrolPoints { get; private set; }
        public float EngagementRange => AttackStyle.EngagementRange(this);
        public float HorizontalDistance => Perception?.Target == null ? float.PositiveInfinity :
            Vector3.ProjectOnPlane(Perception.Target.position - transform.position, Vector3.up).magnitude;
        public bool IsAttackWindowOpen { get; set; }
        public bool IsAnimationFinished { get; set; }
        public bool IsGrappleLocked => States != null && CurrentState == States.Grapple;
        public bool CanBeGrapplePulled => IsAlive && config.canBeGrapplePulled;
        public Vector3 GrappleCenter => bodyCollider != null ? bodyCollider.bounds.center : transform.position;
        
        protected virtual void ConfigureStates(EnemyCombatStates states) { }
        protected virtual bool TryBlockHit(HitInfo hit) => false;
        protected virtual void OnParried() { }
        public virtual void OnGrapplePulled() { }
        protected virtual void OnGrappleReleased() { }

        protected virtual void Awake()
        {
            Movement = GetComponent<EnemyMovement>();
            Locomotion = GetComponent<EnemyLocomotionAnimation>();
            AttackIndicator = GetComponent<EnemyAttackIndicator>();
            bodyCollider = GetComponent<Collider>();
            AttackStyle = GetComponent<EnemyAttackStyle>();
            animationManager = GetComponentInChildren<EnemyAnimationManager>(true);
            
            HealthBar = GetComponentInChildren<EnemyHealthBar>();
            
            Perception = new EnemyPerception(transform, player, config);
            Feedback = new EnemyFeedback(gameObject);
            
            CurrentHealth = config.maxHealth;
            HealthBar.SetValue(CurrentHealth);
            
            PatrolPoints = new Vector3[config.patrolOffsets?.Length ?? 0];
            for (int i = 0; i < PatrolPoints.Length; i++)
                PatrolPoints[i] = transform.TransformPoint(config.patrolOffsets[i]);
            
            States = new EnemyCombatStates(AttackStyle);
            ConfigureStates(States);
            stateMachine = new StateMachine<Core.EnemyBase>(this);
            
            if (animationManager != null)
            {
                animationManager.OnAttackWindowOpened += OpenAttackWindow;
                animationManager.OnAttackWindowClosed += CloseAttackWindow;
                animationManager.OnAnimationFinished += FinishAnimation;
            }
            stateMachine.ChangeState(States.Idle);
        }

        protected virtual void Update()
        {
            if (!IsAlive) return;
            Perception.Refresh();
            stateMachine.Tick(Time.deltaTime);
        }

        public void RequestTransition(IState<Core.EnemyBase> state)
        {
            if (!IsAlive || IsGrappleLocked || state == null) return;
            stateMachine.ChangeState(state);
        }

        public void TransitionToCombat() => RequestTransition(
            HorizontalDistance > EngagementRange ? States.Approach : States.Engage);
        public void ClearLastSeenPosition() => Perception.ClearLastSeenPosition();

        public bool BeginGrappleLock(GameObject source = null)
        {
            if (!IsAlive || IsGrappleLocked) return false;

            Perception.AlertFrom(source);
            stateMachine.ChangeState(States.Grapple);
            return true;
        }

        public void EndGrappleLock()
        {
            if (!IsAlive || !IsGrappleLocked) return;
            OnGrappleReleased();
            stateMachine.ChangeState(States.Idle);
        }

        public void TakeHit(HitInfo hit)
        {
            if (!IsAlive) return;
            Perception.AlertFrom(hit.attacker);
            if (TryBlockHit(hit)) return;
            if (CurrentState == States.Stun)
                hit.damage *= Mathf.Max(1f, config.stunnedDamageMultiplier);
            
            CurrentHit = hit;
            CurrentHealth -= hit.damage;
            HealthBar.SetValue(CurrentHealth);
            
            Feedback.PlayHit(hit);
            if (CurrentHealth <= 0f)
            {
                isDead = true;
                stateMachine.ChangeState(null);
                Feedback.PlayDeath(hit);
                Destroy(gameObject);
                return;
            }
            if (IsGrappleLocked) return;
            stateMachine.ForceChangeState(States.Hit);
            Movement.ApplyKnockback(hit.hitDirection, hit.knockbackForce,
                hit.attacker != null ? hit.attacker.transform : null);
        }

        public void Parried()
        {
            if (!IsAlive || IsGrappleLocked) return;
            OnParried();
            Perception.Refresh();
            stateMachine.ForceChangeState(States.Stun);
            
            Vector3 away = Perception.Target != null ? transform.position - Perception.Target.position : -transform.forward;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            Feedback.PlayReaction(new HitInfo { damage = 10f, hitDirection = away.normalized, impactMultiplier = 1f });
            Movement.ApplyKnockback(away, config.parryKnockbackForce, Perception.Target);
        }

        private void OpenAttackWindow() => IsAttackWindowOpen = IsAlive && !IsGrappleLocked && CurrentState == States.Attack;
        private void CloseAttackWindow() => IsAttackWindowOpen = false;
        private void FinishAnimation() => IsAnimationFinished = IsAlive && !IsGrappleLocked && CurrentState == States.Attack;

        protected virtual void OnDestroy()
        {
            if (animationManager == null) return;
            animationManager.OnAttackWindowOpened -= OpenAttackWindow;
            animationManager.OnAttackWindowClosed -= CloseAttackWindow;
            animationManager.OnAnimationFinished -= FinishAnimation;
        }
    }
}
