namespace GeometryFriendsAgents
{
    public class SegmentGoalCountHeuristic : Heuristic<SegmentState>
    {
        int multiplier;

        public SegmentGoalCountHeuristic(int multiplier)
        {
            this.multiplier = multiplier;
        }

        public override EvaluationResult Compute(EvaluationContext<SegmentState> context)
        {
            var result = new EvaluationResult(multiplier * context.State.remainingCollectibles.Length);
            return result;
        }
    }
}
