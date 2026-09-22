using UnityEngine;

namespace Player.Movement
{
    public sealed class PlayerMovementRuntimeData
    {
        public Vector3 Velocity;
        public float VerticalVelocity;
        public bool CanMove = true;
        public bool CanSprint = true;
        public float SpeedMultiplier = 1f;

        public float CoyoteTimer;
        public bool AirDashAvailable = true;

        public float WallRunCooldownTimer;
        public Vector3 WallNormal;
        public Vector3 WallRunDirection;
        public bool HasConsumedWallRunSurface;
        public Vector3 LastWallRunNormal;
        public Vector3 LastWallRunPoint;
        public bool IsGrappleWallRunning;

        public Vector3 GrappleAnchor;
        public Vector3 GrappleNormal;
        public bool GrappleFlyThrough;
        public bool GrappleWallRunEntry;
        public bool GrappleHeld;
        public bool PreserveAirMomentum;
    }
}
