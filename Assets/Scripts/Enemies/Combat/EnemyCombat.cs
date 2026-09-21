using Combat;
using Core;
using Enemies.Combat.States;
using Enemies.Combat.Utilities;
using Enemies.Combat.Visual_Feedback;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies.Combat
{
    [RequireComponent(typeof(EnemyMovement), typeof(EnemyLocomotionAnimation))]
    public class EnemyCombat : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyConfig config;

        [Header("Target")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform eyePoint;
        [SerializeField] private bool showPatrolGizmos = true;

        private StateMachine<EnemyCombatContext> stateMachine;
        private EnemyCombatContext ctx;
        private EnemyAnimationManager animManager;
        private Collider playerCollider;
        private bool isDead;
        private readonly EnemyVisionSensor visionSensor = new EnemyVisionSensor();

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("EnemyCombat needs an EnemyConfig asset.", this);
                enabled = false;
                return;
            }
            var patrolPoints = SetPlayerAndPatrolPoints();

            animManager = GetComponentInChildren<EnemyAnimationManager>();
            EnemyMovement movement = GetComponent<EnemyMovement>();
            EnemyLocomotionAnimation locomotion = GetComponent<EnemyLocomotionAnimation>();
            EnemyAttackStyle[] attackStyles = GetComponents<EnemyAttackStyle>();
            if (attackStyles.Length != 1)
            {
                Debug.LogError("EnemyCombat needs exactly one EnemyAttackStyle component.", this);
                enabled = false;
                return;
            }
            EnemyAttackStyle attackStyle = attackStyles[0];

            var states = new EnemyCombatStates
            {
                Idle = new EnemyIdleState(),
                Patrol = new EnemyPatrolState(),
                Investigate = new EnemyInvestigateState(),
                Approach = new EnemyApproachState(),
                Stalk = new EnemyStalkState(),
                Attack = attackStyle.CreateState(),
                Escape = attackStyle.CreateEscapeState(),
                Retreat = new EnemyRetreatState(),
                Hit = new EnemyHitState(),
                Stun = new EnemyStunState()
            };

            ctx = new EnemyCombatContext
            {
                Transform = transform,
                Target = null,
                TargetCollider = null,
                Movement = movement,
                Locomotion = locomotion,
                AttackStyle = attackStyle,
                Config = config,
                PatrolPoints = patrolPoints,
                AnimationManager = animManager,
                HitJuice = GetComponentInChildren<HitJuice>(),
                HitFlash = GetComponentInChildren<HitFlash>(),
                HitFx = GetComponentInChildren<HitFx>(),
                StunVisual = GetComponentInChildren<EnemyStunVisual>(),
                States = states,
                CurrentHealth = config.maxHealth
            };

            stateMachine = new StateMachine<EnemyCombatContext>(ctx);
            ctx.RequestTransition = stateMachine.ChangeState;

            if (animManager != null)
            {
                animManager.OnAttackWindowOpened += OnAttackWindowOpened;
                animManager.OnAttackWindowClosed += OnAttackWindowClosed;
                animManager.OnAnimationFinished += OnAnimationFinished;
            }

            stateMachine.ChangeState(states.Idle);
        }

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

        private void OnDestroy()
        {
            if (animManager == null) return;
            animManager.OnAttackWindowOpened -= OnAttackWindowOpened;
            animManager.OnAttackWindowClosed -= OnAttackWindowClosed;
            animManager.OnAnimationFinished -= OnAnimationFinished;
        }

        private void Update()
        {
            if (stateMachine == null) return;
            RefreshTarget();
            stateMachine.Tick(Time.deltaTime);
        }

        private void RefreshTarget()
        {
            if (visionSensor.CanSee(transform, eyePoint, player, playerCollider, config))
            {
                ctx.Target = player;
                ctx.TargetCollider = playerCollider;
                ctx.LastSeenPosition = player.position;
                ctx.HasLastSeenPosition = true;
            }
            else if (ctx.Target != null)
            {
                ctx.Target = null;
                ctx.TargetCollider = null;
            }
        }

        public void TakeHit(HitInfo hitInfo)
        {
            if (isDead) return;
            AlertFromAttacker(hitInfo.attacker);
            ctx.CurrentHit = hitInfo;
            stateMachine.ForceChangeState(ctx.States.Hit);
            ctx.Movement.ApplyKnockback(hitInfo.hitDirection, hitInfo.knockbackForce,
                hitInfo.attacker != null ? hitInfo.attacker.transform : null);

            if (ctx.CurrentHealth <= 0f)
            {
                Die();
            }
        }

        public void Parried()
        {
            if (isDead) return;
            if (player != null)
            {
                ctx.Target = player;
                ctx.TargetCollider = playerCollider;
                ctx.LastSeenPosition = player.position;
                ctx.HasLastSeenPosition = true;
            }
            stateMachine.ForceChangeState(ctx.States.Stun);
            Vector3 away = ctx.Target != null
                ? transform.position - ctx.Target.position : -transform.forward;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            ctx.HitJuice?.CalculateHitReaction(new HitInfo
            {
                damage = 10f,
                hitDirection = away.normalized,
                impactMultiplier = 1f
            });
            ctx.Movement.ApplyKnockback(away, config.parryKnockbackForce, ctx.Target);
        }

        private void AlertFromAttacker(GameObject attacker)
        {
            if (player == null || attacker == null || !attacker.transform.IsChildOf(player)) return;
            ctx.Target = player;
            ctx.TargetCollider = playerCollider;
            ctx.LastSeenPosition = player.position;
            ctx.HasLastSeenPosition = true;
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            ctx.HitFx?.SpawnDeathVfx(ctx.CurrentHit);
            Destroy(gameObject);
        }

        private void OnAttackWindowOpened() => ctx.IsAttackWindowOpen = true;
        private void OnAttackWindowClosed() => ctx.IsAttackWindowOpen = false;
        private void OnAnimationFinished() => ctx.IsAnimationFinished = true;
        private void OnDrawGizmosSelected()
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

        private void OnDrawGizmos()
        {
            if (!showPatrolGizmos || config == null) return;
            int count = Application.isPlaying && ctx?.PatrolPoints != null
                ? ctx.PatrolPoints.Length : config.patrolOffsets?.Length ?? 0;
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
            Application.isPlaying && ctx?.PatrolPoints != null
                ? ctx.PatrolPoints[index] : transform.TransformPoint(config.patrolOffsets[index]);
    }
}
