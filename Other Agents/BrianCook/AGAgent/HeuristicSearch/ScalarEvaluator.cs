namespace GeometryFriendsAgents
{
    public abstract class ScalarEvaluator<TState>
    {
        public abstract EvaluationResult Compute(EvaluationContext<TState> context);
    }
}
