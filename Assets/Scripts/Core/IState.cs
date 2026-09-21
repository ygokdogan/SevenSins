namespace Core
{
    public interface IState<TContext>
    {
        void Enter(TContext ctx);
        void Tick(TContext ctx, float dt);
        void Exit(TContext ctx);
    }
}