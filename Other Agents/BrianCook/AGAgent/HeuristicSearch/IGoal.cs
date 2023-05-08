namespace GeometryFriendsAgents
{
    public interface IGoal<TState>
    {
        bool IsGoal(ref TState state);
    }
}
