using System;
using Player.Movement;

namespace Player
{
    [Serializable]
    public class DefaultCameraProfile : StateCameraProfile
    {
        public override void Handle(PlayerMovement movement, float deltaTime)
        {
            base.Handle(movement, deltaTime);

            CameraManager camera = movement.Player.Camera;
            camera.Look(movement.Player.Input.LookInput);
            camera.TiltCamera(-movement.Player.Input.MoveInput.x * tiltAmount, cameraTiltSpeed);
        }
    }
}
