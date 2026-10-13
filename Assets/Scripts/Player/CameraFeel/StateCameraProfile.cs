using System;
using Player.Movement;
using UnityEngine;

namespace Player.CameraFeel
{
    [Serializable]
    public abstract class StateCameraProfile
    {
        [Range(1f, 179f)] public float cameraFov = 60f;
        [Min(0f)] public float cameraFovSpeed = 8f;
        [Min(0f)] public float tiltAmount = 2f;
        [Min(0f)] public float cameraTiltSpeed = 12f;

        public virtual void Enter(PlayerMovement movement)
        {
            movement.Player.Camera?.SetFov(cameraFov, cameraFovSpeed);
        }

        public virtual void Handle(PlayerMovement movement, float deltaTime)
        {
            movement.Player.Camera.SetFov(cameraFov, cameraFovSpeed);
        }
    }
}
