using Combat;
using Core;
using Enemies.Combat.Utilities;
using Enemies.Combat.Visual_Feedback;
using UnityEngine;

namespace Enemies.Combat
{
    [RequireComponent(typeof(EnemyMovement), typeof(EnemyLocomotionAnimation))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyConfig config;

        [Header("Target")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform eyePoint;
        [SerializeField] private bool showPatrolGizmos = true;

        private StateMachine<EnemyBase> stateMachine;
        private Collider playerCollider;
        private bool isDead;
        private readonly EnemyVisionSensor visionSensor = new EnemyVisionSensor();

        // References
        public Transform Transform => transform;
        public EnemyAnimationManager AnimationManager { get; private set; }
        public EnemyMovement Movement { get; private set; }
        public EnemyLocomotionAnimation Locomotion { get; private set; }
        public EnemyAttackStyle AttackStyle { get; private set; }
        public EnemyConfig Config => config;
        public Transform Target { get; set; }
        public Collider TargetCollider { get; set; }
        public Vector3[] PatrolPoints { get; private set; }
        public Vector3 LastSeenPosition { get; set; }
        public bool HasLastSeenPosition { get; set; }

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

        
        public HitFlash HitFlash { get; private set; }
        public HitJuice HitJuice { get; private set; }
        public HitFx HitFx { get; private set; }
        public HitInfo CurrentHit { get; set; }
        public EnemyStunVisual StunVisual { get; private set; }
        public float StunDuration => Config.parryStunDuration;

        public EnemyCombatStates States { get; protected set; }
        public virtual float EngagementRange => AttackStyle.EngagementRange(this);
        protected virtual bool ShouldApproachTarget => HorizontalDistance > EngagementRange;

        public void RequestTransition(IState<EnemyBase> state) => stateMachine.ChangeState(state);

        public void TransitionToCombat() =>
            RequestTransition(ShouldApproachTarget ? States.Approach : States.Engage);

        // Attack Configuration
        public float NextAttackTime { get; set; }

        // Health
        public float CurrentHealth { get; set; }

        // Runtime Flags
        public bool IsAttackWindowOpen { get; set; }
        public bool IsAnimationFinished { get; set; }

        protected virtual void Awake()
        {
            if (config == null)
            {
                Debug.LogError("EnemyBase needs an EnemyConfig asset.", this);
                enabled = false;
                return;
            }
            var patrolPoints = SetPlayerAndPatrolPoints();

            AnimationManager = GetComponentInChildren<EnemyAnimationManager>();
            EnemyMovement movement = GetComponent<EnemyMovement>();
            EnemyLocomotionAnimation locomotion = GetComponent<EnemyLocomotionAnimation>();
            EnemyAttackStyle[] attackStyles = GetComponents<EnemyAttackStyle>();
            if (attackStyles.Length != 1)
            {
                Debug.LogError("EnemyBase needs exactly one EnemyAttackStyle component.", this);
                enabled = false;
                return;
            }
            EnemyAttackStyle attackStyle = attackStyles[0];
            Movement = movement;
            Locomotion = locomotion;
            AttackStyle = attackStyle;
            PatrolPoints = patrolPoints;
            HitJuice = GetComponentInChildren<HitJuice>();
            HitFlash = GetComponentInChildren<HitFlash>();
            HitFx = GetComponentInChildren<HitFx>();
            StunVisual = GetComponentInChildren<EnemyStunVisual>();
            CurrentHealth = config.maxHealth;
            stateMachine = new StateMachine<EnemyBase>(this);

            if (AnimationManager != null)
            {
                AnimationManager.OnAttackWindowOpened += OnAttackWindowOpened;
                AnimationManager.OnAttackWindowClosed += OnAttackWindowClosed;
                AnimationManager.OnAnimationFinished += OnAnimationFinished;
            }

            stateMachine.ChangeState(CreateInitialState());
        }

        protected abstract IState<EnemyBase> CreateInitialState();
        protected abstract IState<EnemyBase> HitState { get; }
        protected abstract IState<EnemyBase> StunState { get; }

        private Vector3[] SetPlayerAndPatrolPoints()
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null) player = playerObject.transform;
            }
            playerCollider = player == null ? null :
                (player.GetComponent<Collider>() ?? player.GetComponentInChildren<Collider>());
            Vector3[] patrolPoints = new Vector3[config.patrolOffsets?.Length ?? 0];
            for (int i = 0; i < patrolPoints.Length; i++)
                patrolPoints[i] = transform.TransformPoint(config.patrolOffsets[i]);
            
            return patrolPoints;
        }

        protected virtual void OnDestroy()
        {
            if (AnimationManager == null) return;
            AnimationManager.OnAttackWindowOpened -= OnAttackWindowOpened;
            AnimationManager.OnAttackWindowClosed -= OnAttackWindowClosed;
            AnimationManager.OnAnimationFinished -= OnAnimationFinished;
        }

        protected virtual void Update()
        {
            if (stateMachine == null) return;
            RefreshTarget();
            stateMachine.Tick(Time.deltaTime);
        }

        private void RefreshTarget()
        {
            if (visionSensor.CanSee(transform, eyePoint, player, playerCollider, config))
            {
                Target = player;
                TargetCollider = playerCollider;
                LastSeenPosition = player.position;
                HasLastSeenPosition = true;
            }
            else if (Target != null)
            {
                Target = null;
                TargetCollider = null;
            }
        }

        public void TakeHit(HitInfo hitInfo)
        {
            if (isDead || stateMachine == null) return;
            AlertFromAttacker(hitInfo.attacker);
            CurrentHit = hitInfo;
            stateMachine.ForceChangeState(HitState);
            Movement.ApplyKnockback(hitInfo.hitDirection, hitInfo.knockbackForce,
                hitInfo.attacker != null ? hitInfo.attacker.transform : null);

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        public void Parried()
        {
            if (isDead || stateMachine == null) return;
            if (player != null)
            {
                Target = player;
                TargetCollider = playerCollider;
                LastSeenPosition = player.position;
                HasLastSeenPosition = true;
            }
            stateMachine.ForceChangeState(StunState);
            Vector3 away = Target != null
                ? transform.position - Target.position : -transform.forward;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            HitJuice?.CalculateHitReaction(new HitInfo
            {
                damage = 10f,
                hitDirection = away.normalized,
                impactMultiplier = 1f
            });
            Movement.ApplyKnockback(away, config.parryKnockbackForce, Target);
        }

        private void AlertFromAttacker(GameObject attacker)
        {
            if (player == null || attacker == null || !attacker.transform.IsChildOf(player)) return;
            Target = player;
            TargetCollider = playerCollider;
            LastSeenPosition = player.position;
            HasLastSeenPosition = true;
        }

        private void Die()
        {
            if (isDead || stateMachine == null) return;
            isDead = true;
            HitFx?.SpawnDeathVfx(CurrentHit);
            Destroy(gameObject);
        }

        private void OnAttackWindowOpened() => IsAttackWindowOpen = true;
        private void OnAttackWindowClosed() => IsAttackWindowOpen = false;
        private void OnAnimationFinished() => IsAnimationFinished = true;
        protected virtual void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Vector3 origin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.5f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, config.sightRange);
            Gizmos.DrawRay(origin, Quaternion.Euler(0f, -config.fieldOfView * 0.5f, 0f) *
                transform.forward * config.sightRange);
            Gizmos.DrawRay(origin, Quaternion.Euler(0f, config.fieldOfView * 0.5f, 0f) *
                transform.forward * config.sightRange);
        }

        protected virtual void OnDrawGizmos()
        {
            if (!showPatrolGizmos || config == null) return;
            int count = Application.isPlaying && PatrolPoints != null
                ? PatrolPoints.Length : config.patrolOffsets?.Length ?? 0;
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                Vector3 point = PatrolGizmoPoint(i);
                Gizmos.color = i == 0 ? Color.green : Color.cyan;
                Gizmos.DrawSphere(point, 0.18f);
                Gizmos.DrawWireSphere(point, 0.35f);
#if UNITY_EDITOR
                UnityEditor.Handles.color = Gizmos.color;
                UnityEditor.Handles.Label(point + Vector3.up * 0.45f, $"Patrol {i + 1}");
#endif
                if (count < 2) continue;
                Vector3 next = PatrolGizmoPoint((i + 1) % count);
                Vector3 travel = next - point;
                if (travel.sqrMagnitude < 0.01f) continue;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(point, next);
                Vector3 direction = travel.normalized;
                Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
                Vector3 arrow = Vector3.Lerp(point, next, 0.7f);
                Gizmos.DrawLine(arrow, arrow - direction * 0.3f + side * 0.18f);
                Gizmos.DrawLine(arrow, arrow - direction * 0.3f - side * 0.18f);
            }
        }

        private Vector3 PatrolGizmoPoint(int index) =>
            Application.isPlaying && PatrolPoints != null
                ? PatrolPoints[index] : transform.TransformPoint(config.patrolOffsets[index]);
    }
}
