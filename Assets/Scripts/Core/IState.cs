using UnityEngine;

namespace Core
{
    public interface IState<T> 
    {
        void Enter(T ctx);
        void Tick(T ctx, float dt);
        void Exit(T ctx);
    }
}