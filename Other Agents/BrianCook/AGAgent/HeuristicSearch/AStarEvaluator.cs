namespace GeometryFriendsAgents
{
    public class AStarEvaluator<TState> : ScalarEvaluator<TState>
    {
        ScalarEvaluator<TState> hEvaluator;

        public AStarEvaluator(ScalarEvaluator<TState> hEvaluator)
        {
            this.hEvaluator = hEvaluator;
        }

        public override EvaluationResult Compute(EvaluationContext<TState> context)
        {
            var result = context.Get(hEvaluator);
            result.Add(context.g);
            return result;
        }
    }
}
