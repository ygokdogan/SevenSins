using UnityEngine;

namespace Player.Movement.States
{
    public sealed class WallRunState : PlayerMovementState
    {
        private PlayerMovementConfig.WallRunSettings config;
        private PlayerMovementConfig.JumpSettings jumpConfig;

        public override void Initialize(PlayerMovement movement)
        {
            config = movement.config.WallRun;
            jumpConfig = movement.config.Jump;
        }

        [SerializeField] private WallRunCameraProfile wallRunCameraProfile = new();
        public override StateCameraProfile CameraProfile => wallRunCameraProfile;

        private float timeRemaining;
        private float gravityRampTimer;
        private Vector3 runDirection;
        private int wallLayers;
        private float cooldownTimer;
        private bool hasConsumedSurface;
        private Vector3 lastWallNormal;
        private Vector3 lastWallPoint;

        public Vector3 WallNormal { get; private set; }
        public Vector3 RunDirection => runDirection;

        public void ResetOnLanding()
        {
            cooldownTimer = 0f;
            hasConsumedSurface = false;
            lastWallNormal = Vector3.zero;
            lastWallPoint = Vector3.zero;
            WallNormal = Vector3.zero;
        }

        public void TickCooldown(float dt) => cooldownTimer = Mathf.Max(0f, cooldownTimer - dt);
        public void StartCooldown(float duration) => cooldownTimer = duration;

        public void Begin(PlayerMovement movement, Vector3 surfaceNormal, int layers)
        {
            WallNormal = Vector3.ProjectOnPlane(surfaceNormal, Vector3.up).normalized;
            wallLayers = layers;
            movement.ChangeState(this);
        }

        public bool TryStart(PlayerMovement player)
        {
            if (!player.CanMove || player.Player.Controller.isGrounded || cooldownTimer > 0f)
                return false;
            if (player.Player.Input.MoveInput.y < config.MinInput)
                return false;

            Vector3 planarVelocity = Vector3.ProjectOnPlane(player.Velocity, Vector3.up);
            Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up);
            if (planarVelocity.magnitude < config.MinSpeed || Vector3.Dot(forward, planarVelocity) <= 0f)
                return false;

            if (!TryFindWall(player, out Vector3 normal, out RaycastHit hit) ||
                IsConsumedSurface(normal, hit.point)) return false;

            Begin(player, normal, config.Layers);
            return player.IsWallRunning;
        }

        public override void Enter(PlayerMovement movement)
        {
            cooldownTimer = 0f;

            movement.AirborneState.SetMomentumPreservation(false);
            timeRemaining = config.Duration;
            gravityRampTimer = 0f;
            if (!FindCurrentWall(movement, out Vector3 entryNormal, out RaycastHit hit))
            {
                cooldownTimer = config.Cooldown;
                movement.ChangeState(movement.AirborneState);
                return;
            }

            WallNormal = entryNormal;
            hasConsumedSurface = true;
            lastWallNormal = WallNormal;
            lastWallPoint = hit.point;
            runDirection = GetRunDirection(movement, WallNormal);

            movement.Velocity = runDirection * config.Speed;
            movement.VerticalVelocity = 0f;

        }

        public override void Tick(PlayerMovement movement, float dt)
        {
            timeRemaining -= dt;
            bool stopInput = movement.Player.Input.MoveInput.y < config.MinInput;
            if (!movement.CanMove || stopInput || !FindCurrentWall(movement, out Vector3 wallNormal, out _))
            {
                LeaveWall(movement);
                return;
            }

            WallNormal = wallNormal;
            Vector3 desiredDirection = GetRunDirection(movement, wallNormal);
            if (Vector3.Dot(desiredDirection, runDirection) < 0f) desiredDirection = runDirection;
            runDirection = Vector3.Slerp(runDirection, desiredDirection, 12f * dt).normalized;

            if (movement.Player.Input.JumpInput)
            {
                movement.Player.Input.UseJumpInput();
                movement.Player.Input.ConsumeJumpRelease();
                movement.Velocity = wallNormal * config.JumpHorizontalSpeed + runDirection * config.JumpForwardCarry;
                movement.VerticalVelocity = Mathf.Sqrt(config.JumpHeight * 2f * jumpConfig.Gravity);
                cooldownTimer = config.Cooldown;
                movement.ChangeState(movement.AirborneState);
                return;
            }

            Vector3 planarVelocity = runDirection * config.Speed;
            movement.Velocity = planarVelocity;

            if (timeRemaining > 0f)
                movement.VerticalVelocity = 0f;
            else
            {
                gravityRampTimer += dt;
                float gravityBlend = Mathf.Clamp01(gravityRampTimer / config.GravityRampDuration);
                movement.VerticalVelocity = Mathf.Max(
                    movement.VerticalVelocity - jumpConfig.Gravity * gravityBlend * dt,
                    -jumpConfig.MaxFallSpeed);
                if (gravityBlend >= 1f)
                {
                    LeaveWall(movement);
                    return;
                }
            }

            Vector3 motion = planarVelocity + Vector3.up * movement.VerticalVelocity - wallNormal * config.StickForce;
            movement.Player.Controller.Move(motion * dt);
            if (movement.Player.Controller.isGrounded)
                movement.ChangeState(movement.GroundedState);
        }

        public override void Exit(PlayerMovement movement) { }

        private bool FindCurrentWall(PlayerMovement player, out Vector3 normal, out RaycastHit hit)
        {
            float radius = Mathf.Max(0.05f, player.Player.Controller.radius * 0.65f);
            if (Physics.SphereCast(player.Player.Controller.bounds.center, radius, -WallNormal,
                out hit, config.CheckDistance, wallLayers, QueryTriggerInteraction.Ignore)
                && IsRunnable(hit.normal) && Vector3.Dot(hit.normal, WallNormal) > 0.5f)
            {
                normal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
                return true;
            }

            normal = Vector3.zero;
            hit = default;
            return false;
        }

        private void LeaveWall(PlayerMovement movement)
        {
            cooldownTimer = config.Cooldown;
            movement.ChangeState(movement.AirborneState);
        }

        private bool IsConsumedSurface(Vector3 normal, Vector3 point)
        {
            if (!hasConsumedSurface) return false;
            bool sameFacing = Vector3.Dot(normal, lastWallNormal) > 0.97f;
            float planeDistance = Mathf.Abs(Vector3.Dot(point - lastWallPoint, lastWallNormal));
            return sameFacing && planeDistance < 0.4f;
        }

        private static Vector3 GetRunDirection(PlayerMovement player, Vector3 wallNormal)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, wallNormal).normalized;
            Vector3 look = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up);
            float facing = Vector3.Dot(look, tangent);
            if (Mathf.Abs(facing) < 0.1f) facing = Vector3.Dot(tangent, player.Velocity);
            return facing >= 0f ? tangent : -tangent;
        }

        private bool TryFindWall(PlayerMovement player, out Vector3 wallNormal, out RaycastHit wallHit)
        {
            Vector3 center = player.Player.Controller.bounds.center;
            float radius = Mathf.Max(0.05f, player.Player.Controller.radius * 0.65f);
            bool leftHit = Physics.SphereCast(center, radius, -player.transform.right, out RaycastHit left,
                config.CheckDistance, config.Layers, QueryTriggerInteraction.Ignore) && IsRunnable(left.normal);
            bool rightHit = Physics.SphereCast(center, radius, player.transform.right, out RaycastHit right,
                config.CheckDistance, config.Layers, QueryTriggerInteraction.Ignore) && IsRunnable(right.normal);

            if (!leftHit && !rightHit)
            {
                wallNormal = Vector3.zero;
                wallHit = default;
                return false;
            }

            wallHit = leftHit && rightHit ? (left.distance <= right.distance ? left : right) : (leftHit ? left : right);
            wallNormal = Vector3.ProjectOnPlane(wallHit.normal, Vector3.up).normalized;
            return true;
        }

        private static bool IsRunnable(Vector3 normal) => Mathf.Abs(normal.y) < 0.2f;
    }
}
