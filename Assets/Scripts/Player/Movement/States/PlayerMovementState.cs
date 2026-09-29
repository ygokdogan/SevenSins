using Core;
using UnityEngine;

namespace Player.Movement.States
{
    public abstract class PlayerMovementState : MonoBehaviour, IState<PlayerMovement>
    {
        [Header("Camera")]
        [SerializeField] private DefaultCameraProfile cameraProfile = new();

        public virtual StateCameraProfile CameraProfile => cameraProfile;

        public abstract void Initialize(PlayerMovement movement);

        public abstract void Enter(PlayerMovement movement);
        public abstract void Tick(PlayerMovement movement, float deltaTime);
        public abstract void Exit(PlayerMovement movement);

    }
}
