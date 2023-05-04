namespace GeometryFriendsAgents
{
    public class SegmentGoal : IGoal<SegmentState>
    {
        public bool IsGoal(ref SegmentState state)
        {
            return state.remainingCollectibles == null || state.remainingCollectibles.Length == 0;
        }
    }
}
