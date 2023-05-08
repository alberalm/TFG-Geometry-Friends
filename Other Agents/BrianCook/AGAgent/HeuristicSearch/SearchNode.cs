using System;
using System.Diagnostics;

namespace GeometryFriendsAgents
{
    public class SearchNode<TState, TAction>
    {
        public NodeStatus Status;
        public TState State;
        public SearchNode<TState, TAction> Parent;
        public TAction CreatingOperator;
        public float g;

        public bool IsNew { get { return Status == NodeStatus.New; } }
        public bool IsOpen { get { return Status == NodeStatus.Open; } }
        public bool IsClosed { get { return Status == NodeStatus.Closed || Status == NodeStatus.DeadEnd; } }
        public bool IsDeadEnd { get { return Status == NodeStatus.DeadEnd; } }

        public SearchNode(TState state)
        {
            State = state;
            Status = NodeStatus.New;
        }

        public override string ToString()
        {
            return string.Format("[{0}:{1}->G={2}:{3}]", Status, CreatingOperator, g, State);
        }
        public void OpenInitial()
        {
            Debug.Assert(IsNew);
            Status = NodeStatus.Open;
            g = 0;
            Parent = null;
            CreatingOperator = default(TAction);
        }

        public void Open(SearchNode<TState, TAction> parent, TAction parentOperator, float cost)
        {
            Debug.Assert(IsNew);
            Status = NodeStatus.Open;
            g = parent.g + cost;
            Parent = parent;
            CreatingOperator = parentOperator;
        }

        public void Reopen()
        {
            Debug.Assert(IsOpen || IsClosed);
            Status = NodeStatus.Open;
        }

        public void Reopen(SearchNode<TState, TAction> parent, TAction parentOperator, float cost)
        {
            Debug.Assert(IsOpen || IsClosed);
            Status = NodeStatus.Open;
            g = parent.g + cost;
            Parent = parent;
            CreatingOperator = parentOperator;
        }

        public void UpdateParent(SearchNode<TState, TAction> parent, TAction parentOperator, float cost)
        {
            Debug.Assert(IsOpen || IsClosed);
            g = parent.g + cost;
            Parent = parent;
            CreatingOperator = parentOperator;
        }

        public void Close()
        {
            Debug.Assert(IsOpen);
            Status = NodeStatus.Closed;
        }

        public void MarkAsDeadEnd()
        {
            Status = NodeStatus.DeadEnd;
        }
    }

    public class StateEventArgs<TState> : EventArgs
    {
        public TState State;
        public StateEventArgs(TState state)
        {
            State = state;
        }
    }

    public class StateEventArgs<TState1, TState2> : EventArgs
    {
        public TState1 State1;
        public TState2 State2;
        public StateEventArgs(TState1 state1, TState2 state2)
        {
            State1 = state1;
            State2 = state2;
        }
    }

    public class NodeEventArgs<TState, TAction> : EventArgs
    {
        public SearchNode<TState, TAction> Node;
    }

    public class NodeEvaluatedEventArgs<TState, TAction> : NodeEventArgs<TState, TAction>
    {
        public int Evaluations;
    }
}
