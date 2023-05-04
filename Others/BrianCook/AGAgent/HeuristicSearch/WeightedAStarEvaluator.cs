namespace GeometryFriendsAgents
{
    public class WeightedAStarEvaluator<TState> : ScalarEvaluator<TState>
    {
        ScalarEvaluator<TState> hEvaluator;
        int gWeight, hWeight;

        public WeightedAStarEvaluator(ScalarEvaluator<TState> hEvaluator, int gWeight, int hWeight)
        {
            this.hEvaluator = hEvaluator;
            this.gWeight = gWeight;
            this.hWeight = hWeight;
        }

        public override EvaluationResult Compute(EvaluationContext<TState> context)
        {
            var result = context.Get(hEvaluator);
            result.Multiply(hWeight);
            result.Add(gWeight * context.g);
            return result;
        }
    }
}
