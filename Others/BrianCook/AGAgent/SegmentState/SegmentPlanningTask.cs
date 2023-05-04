using System;
using System.Collections.Generic;

namespace GeometryFriendsAgents
{
    public class SegmentPlanningTask
    {
        GameArea gameArea;
        public SearchNode<SegmentState, SegmentOperator> Solution { get; private set; }
        public SearchNode<SegmentState, SegmentOperator> BestNode { get; private set; }
        public List<SegmentOperator> Plan { get; private set; }
        public SearchEngine<SegmentState, SegmentOperator> SearchEngine { get; private set; }
        public SearchNode<SegmentState, SegmentOperator> CurrentNode { get { return SearchEngine.CurrentNode; } }
        public bool IsStopped { get { return (SearchEngine == null) || SearchEngine.IsStopped; } }
        public SegmentModel SegmentModel;

        SegmentPlanningProblem problem;

        public SegmentPlanningTask(GameArea area, SegmentPlanningProblem problem)
        {
            this.problem = problem;
            SegmentModel = problem.segmentModel;
            gameArea = area;
        }

        public SearchStatus Run(double timeLimitSeconds)
        {
            int goalCountMultiplier = 0;
            var goalCountEvaluator = new SegmentGoalCountHeuristic(goalCountMultiplier);
            int gWeight = 1;
            int hWeight = 1;
            var evaluator = new WeightedAStarEvaluator<SegmentState>(goalCountEvaluator, gWeight, hWeight);
            var openList = new SimpleOpenList<SegmentState, SearchNode<SegmentState, SegmentOperator>>(evaluator);
            var search = new EagerSearch<SegmentState, SegmentOperator>(problem, openList);

            SearchEngine = search;
            BestNode = null;
            search.SearchStep += (object sender, EventArgs e) =>
            {
                OnStep();
            };
            search.NodeExpanded += (object sender, NodeEventArgs<SegmentState, SegmentOperator> e) =>
            {
                OnNodeExpanded(sender, e);
            };
            search.NodeEvaluated += (object sender, NodeEvaluatedEventArgs<SegmentState, SegmentOperator> e) =>
            {
                if (BestNode == null)
                    BestNode = search.InitialNode;

                if (e.Node.State.remainingCollectibles.Length < BestNode.State.remainingCollectibles.Length)
                    BestNode = e.Node;
                else if (e.Node.State.remainingCollectibles.Length == BestNode.State.remainingCollectibles.Length && e.Node.g < BestNode.g)
                    BestNode = e.Node;

                OnNodeEvaluated(sender, e);
            };

            var status = search.Search(timeLimitSeconds);
            Solution = search.SolutionNode;
            if (status == SearchStatus.Solved)
                Plan = CreatePlan(Solution);
            else
                Plan = CreatePlan(BestNode);

            return status;
        }

        List<SegmentOperator> CreatePlan(SearchNode<SegmentState, SegmentOperator> solutionNode)
        {
            var plan = new List<SegmentOperator>();
            var node = solutionNode;
            while (node != null && node.CreatingOperator != null)
            {
                plan.Add(node.CreatingOperator as SegmentOperator);
                node = node.Parent;
            }
            plan.Reverse();
            if (plan.Count == 0)
                return null;

            return plan;
        }

        public void Stop()
        {
            SearchEngine.Stop();
        }

        void OnStep()
        {
            if (Step != null)
                Step(this, EventArgs.Empty);
        }

        public event EventHandler<EventArgs> Step;

        void OnNodeExpanded(object sender, NodeEventArgs<SegmentState, SegmentOperator> e)
        {
            if (NodeExpanded != null)
                NodeExpanded(sender, e);
        }

        public EventHandler<NodeEventArgs<SegmentState, SegmentOperator>> NodeExpanded;

        void OnNodeEvaluated(object sender, NodeEventArgs<SegmentState, SegmentOperator> e)
        {
            if (NodeEvaluated != null)
                NodeEvaluated(sender, e);
        }

        public EventHandler<NodeEventArgs<SegmentState, SegmentOperator>> NodeEvaluated;
    }
}
