namespace GeometryFriendsAgents
{
    public abstract class StateActionFunction<TState, TAction>
    {
        public abstract void Reset();

        public abstract double GetValue(TState state, TAction action);

        public abstract void Update(TState state, TAction action, double value, double alpha);
    }
}
