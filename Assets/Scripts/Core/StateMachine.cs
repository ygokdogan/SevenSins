namespace Core
{
    public class StateMachine<TContext>
    {
        private IState<TContext> currentState;
        private readonly TContext ctx;

        public IState<TContext> CurrentState => currentState;

        public StateMachine(TContext context) => ctx = context;

        public void ChangeState(IState<TContext> newState)
        {
            if(currentState == newState) return;
            TransitionTo(newState);
        }

        public void ForceChangeState(IState<TContext> newState)
        {
            TransitionTo(newState);
        }

        private void TransitionTo(IState<TContext> newState)
        {
            currentState?.Exit(ctx);
            currentState = newState;
            currentState?.Enter(ctx);
        }

        public void Tick(float dt) => currentState?.Tick(ctx, dt);
    }
}
