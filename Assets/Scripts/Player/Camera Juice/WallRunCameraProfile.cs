using System;
using Player.Movement;
using UnityEngine;

namespace Player
{
    [Serializable]
    public class WallRunCameraProfile : StateCameraProfile
    {
        [Range(0f, 180f)] public float yawLimit = 55f;
        [Range(0f, 45f)] public float towardWallYawLimit = 10f;

        private float startYaw;
        private float wallSide;

        public override void Enter(PlayerMovement movement)
        {
            base.Enter(movement);
            startYaw = movement.transform.eulerAngles.y;
            Vector3 referenceRight = Quaternion.Euler(0f, startYaw, 0f) * Vector3.right;
            wallSide = Mathf.Sign(Vector3.Dot(-movement.WallRunState.WallNormal, referenceRight));
        }

        public override void Handle(PlayerMovement movement, float deltaTime)
        {
            base.Handle(movement, deltaTime);

            CameraManager camera = movement.Player.Camera;
            Vector2 look = movement.Player.Input.LookInput * camera.Sensitivity;
            camera.LookPitch(look.y);

            float currentOffset = Mathf.DeltaAngle(startYaw, movement.transform.eulerAngles.y);
            float minYaw = wallSide < 0f ? -towardWallYawLimit : -yawLimit;
            float maxYaw = wallSide > 0f ? towardWallYawLimit : yawLimit;
            float yawOffset = Mathf.Clamp(currentOffset + look.x, minYaw, maxYaw);
            camera.SetYaw(startYaw + yawOffset);
            camera.TiltCamera(
                Vector3.Dot(movement.WallRunState.WallNormal, movement.transform.right) * -tiltAmount,
                cameraTiltSpeed);
        }
    }
}
