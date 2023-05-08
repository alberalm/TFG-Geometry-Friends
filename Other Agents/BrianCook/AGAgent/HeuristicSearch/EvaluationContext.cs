using System.Collections.Generic;

namespace GeometryFriendsAgents
{
    public class EvaluationContext<TState>
    {
        public float g;
        Dictionary<ScalarEvaluator<TState>, EvaluationResult> cache = new Dictionary<ScalarEvaluator<TState>, EvaluationResult>();
        public TState State;
        public IGoal<TState> Goal;
        public int Evaluations { get { return cache.Count; } }

        public EvaluationContext()
        {
        }

        public EvaluationContext(TState state, IGoal<TState> goal, float g)
        {
            State = state;
            Goal = goal;
            this.g = g;
        }

        public EvaluationResult Get(ScalarEvaluator<TState> evaluator)
        {
            EvaluationResult result;
            if (cache.TryGetValue(evaluator, out result))
                return result;
            result = evaluator.Compute(this);
            cache[evaluator] = result;
            return result;
        }
    }
}
