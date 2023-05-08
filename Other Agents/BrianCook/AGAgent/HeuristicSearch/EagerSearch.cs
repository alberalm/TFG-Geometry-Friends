using System;
using System.Collections.Generic;

namespace GeometryFriendsAgents
{
    public class EagerSearch<TState, TAction> : SearchEngine<TState, TAction>
    {
        public bool ReopenClosedNodes = true;
        protected PlanningProblem<TState, TAction> problem;
        protected IGoal<TState> goal;
        protected OpenList<TState, SearchNode<TState, TAction>> openList;
        protected Dictionary<TState, SearchNode<TState, TAction>> registeredStates = new Dictionary<TState, SearchNode<TState, TAction>>();
        protected bool isLocalSearch;

        public EagerSearch(PlanningProblem<TState, TAction> problem, OpenList<TState, SearchNode<TState, TAction>> openList)
        {
            this.problem = problem;
            this.openList = openList;
        }

        public override SearchNode<TState, TAction> Initialize()
        {
            openList.Clear();
            registeredStates.Clear();
            SolutionNode = null;
            goal = problem.Goal;
            isLocalSearch = false;
            var initialState = problem.InitialState;
            var node = GetNode(initialState);
            node.OpenInitial();
            var context = new EvaluationContext<TState>(initialState, goal, 0);
            openList.Insert(context, node);
            return node;
        }

        protected bool IsGoal(ref TState state)
        {
            return problem.IsGoal(ref state);
        }

        public override SearchStatus Step()
        {
            return Step(null);
        }

        SearchStatus Step(Action<EvaluationContext<TState>, SearchNode<TState, TAction>> onEvaluated)
        {
            SearchNode<TState, TAction> node;
            do
            {
                if (openList.IsEmpty)
                    return SearchStatus.Failed;
                node = openList.RemoveNext();
            } while (node.IsClosed);
            CurrentNode = node;
            node.Close();
            if (IsGoal(ref node.State))
            {
                SolutionNode = node;
                return SearchStatus.Solved;
            }

            bool addedSuccessor = false;

            var operators = new List<TAction>();
            problem.GetApplicableOperators(operators, ref node.State);
            foreach (var op in operators)
            {

                TState successor;
                if (problem.GetSuccessorState(ref node.State, op, out successor))
                {
                    if (ProcessSuccessor(node, ref successor, op, onEvaluated) != null)
                        addedSuccessor = true;
                }
            }

            if (!addedSuccessor)
            {
                node.MarkAsDeadEnd();
                DebugOutput("  (DEAD END, NO NEW SUCCESSORS ADDED)");
            }

            OnNodeExpanded(node);
            return SearchStatus.InProgress;
        }

        protected SearchNode<TState, TAction> ProcessSuccessor(SearchNode<TState, TAction> node, ref TState successorState, 
            TAction op, Action<EvaluationContext<TState>, SearchNode<TState, TAction>> onEvaluated)
        {
            var successorNode = GetNode(successorState);
            var cost = problem.GetCost(op);
            var newG = node.g + cost;
            if (successorNode.IsDeadEnd)
                return null;

            if (successorNode.IsNew)
            {
                var context = new EvaluationContext<TState>(successorState, goal, newG);
                if (openList.IsDeadEnd(context))
                {
                    successorNode.MarkAsDeadEnd();
                    OnNodeEvaluated(successorNode, context.Evaluations);
                    return null;
                }

                successorNode.Open(node, op, cost);
                openList.Insert(context, successorNode);
                if (onEvaluated != null)
                    onEvaluated(context, successorNode);
                OnNodeEvaluated(successorNode, context.Evaluations);
                return successorNode;
            }
            else if (successorNode.g > newG)
            {
                if (ReopenClosedNodes)
                {
                    successorNode.Reopen(node, op, cost);
                    var context = new EvaluationContext<TState>(successorState, goal, newG);
                    openList.Insert(context, successorNode);
                    if (onEvaluated != null)
                        onEvaluated(context, successorNode);
                    OnNodeEvaluated(successorNode, context.Evaluations);
                    return successorNode;
                }
                else
                    successorNode.UpdateParent(node, op, cost);
            }
            return null;
        }

        protected SearchNode<TState, TAction> GetNode(TState state)
        {
            SearchNode<TState, TAction> node;
            if (registeredStates.TryGetValue(state, out node))
            {
                return node;
            }
            node = new SearchNode<TState, TAction>(state);
            registeredStates[state] = node;
            return node;
        }

        protected override void OnNodeExpanded(SearchNode<TState, TAction> node)
        {
            openList.OnExpanded(node);

            base.OnNodeExpanded(node);
        }
    }
}
