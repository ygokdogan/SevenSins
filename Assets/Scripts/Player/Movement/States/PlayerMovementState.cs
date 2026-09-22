using Core;
using UnityEngine;

namespace Player.Movement
{
    public abstract class PlayerMovementState : MonoBehaviour, IState<PlayerMovement>
    {
        public abstract void Enter(PlayerMovement player);
        public abstract void Tick(PlayerMovement player, float deltaTime);
        public abstract void Exit(PlayerMovement player);
    }
}
