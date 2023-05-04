using System.Collections.Generic;

namespace GeometryFriendsAgents
{
    public abstract class PlanningProblem<TState, TAction>
    {
        public abstract TState InitialState { get; }
        public abstract bool IsGoal(ref TState state);
        public abstract void GetApplicableOperators(List<TAction> operators, ref TState state);
        public abstract bool GetSuccessorState(ref TState state, TAction op, out TState successor);
        public abstract string GetString(ref TState state);
        public abstract float GetCost(TAction op);
        public abstract IGoal<TState> Goal { get; }
    }
}

