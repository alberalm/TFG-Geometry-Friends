using System;
using System.Diagnostics;

namespace GeometryFriendsAgents
{
    public abstract class SearchEngine<TState, TAction>
    {
        public int Bound = int.MaxValue; 
        public int Expansions { get; private set; }
        public int Evaluations { get; private set; }
        public bool EnableDebugOutput;
        public bool IsStopped { get; private set; }
        public SearchNode<TState, TAction> InitialNode { get; protected set; }
        public SearchNode<TState, TAction> CurrentNode { get; protected set; }
        public SearchNode<TState, TAction> BestNode { get; protected set; }
        public SearchNode<TState, TAction> SolutionNode { get; protected set; }
        public double ElapsedTime { get; protected set; }

        public SearchEngine()
        {
            IsStopped = true;
        }

        public abstract SearchNode<TState, TAction> Initialize();

        public abstract SearchStatus Step();

        public SearchStatus Search(double timeLimit)
        {
            IsStopped = false;
            ElapsedTime = 0;

            var stopwatch = new Stopwatch();
            stopwatch.Restart();
            Expansions = Evaluations = 0;

            InitialNode = Initialize();

            while ((double)stopwatch.ElapsedMilliseconds / 1000.0 < timeLimit && !IsStopped)
            {
                ElapsedTime = (double)stopwatch.ElapsedMilliseconds / 1000.0;
                var status = Step();
                if (status != SearchStatus.InProgress)
                {
                    IsStopped = true;
                    return status;
                }
                OnStep();
            }
            IsStopped = true;
            return SearchStatus.Timeout;
        }

        protected void OnStep()
        {
            if (SearchStep != null)
                SearchStep(this, EventArgs.Empty);
        }

        protected void DebugOutput(string text)
        {
            if (EnableDebugOutput)
                Debug.WriteLine(text);
        }

        public void Stop()
        {
            IsStopped = true;
        }

        public event EventHandler<EventArgs> SearchStep;
        NodeEventArgs<TState, TAction> nodeEventArgs = new NodeEventArgs<TState, TAction>();
        NodeEvaluatedEventArgs<TState, TAction> nodeEvaluationEventArgs = new NodeEvaluatedEventArgs<TState, TAction>();

        protected virtual void OnNodeAdded(SearchNode<TState, TAction> node)
        {
            if (NodeAdded != null)
            {
                nodeEventArgs.Node = node;
                NodeAdded(this, nodeEventArgs);
            }
        }

        protected virtual void OnNodeAdded(NodeEventArgs<TState, TAction> args)
        {
            if (NodeAdded != null)
                NodeAdded(this, args);
        }

        public EventHandler<NodeEventArgs<TState, TAction>> NodeAdded;

        protected virtual void OnNodeSelected(SearchNode<TState, TAction> node)
        {
            if (NodeSelected != null)
            {
                nodeEventArgs.Node = node;
                NodeSelected(this, nodeEventArgs);
            }
        }

        protected virtual void OnNodeSelected(NodeEventArgs<TState, TAction> args)
        {
            if (NodeSelected != null)
                NodeSelected(this, args);
        }

        public EventHandler<NodeEventArgs<TState, TAction>> NodeSelected;

        protected virtual void OnNodeExpanded(SearchNode<TState, TAction> node)
        {
            Expansions++;
            if (NodeExpanded != null)
            {
                nodeEventArgs.Node = node;
                NodeExpanded(this, nodeEventArgs);
            }
        }

        protected virtual void OnNodeExpanded(NodeEventArgs<TState, TAction> args)
        {
            Expansions++;
            if (NodeExpanded != null)
                NodeExpanded(this, args);
        }

        public EventHandler<NodeEventArgs<TState, TAction>> NodeExpanded;

        protected virtual void OnNodeEvaluated(SearchNode<TState, TAction> node, int evaluations)
        {
            Evaluations += evaluations;
            if (NodeEvaluated != null)
            {
                nodeEvaluationEventArgs.Node = node;
                nodeEvaluationEventArgs.Evaluations = evaluations;
                NodeEvaluated(this, nodeEvaluationEventArgs);
            }
        }

        protected virtual void OnNodeEvaluated(NodeEvaluatedEventArgs<TState, TAction> args)
        {
            Evaluations += args.Evaluations;
            if (NodeEvaluated != null)
                NodeEvaluated(this, args);
        }

        public EventHandler<NodeEvaluatedEventArgs<TState, TAction>> NodeEvaluated;

        protected virtual void OnSetSubgoal(StateEventArgs<TState, IGoal<TState>> args)
        {
            if (SetSubgoal != null)
                SetSubgoal(this, args);
        }

        public EventHandler<StateEventArgs<TState, IGoal<TState>>> SetSubgoal;
    }
}
