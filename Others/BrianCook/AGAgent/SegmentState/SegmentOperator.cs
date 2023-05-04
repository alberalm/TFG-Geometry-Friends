using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GeometryFriendsAgents
{
    public abstract class SegmentOperator : Operator
    {
        public SegmentState FromState;
        public SegmentState ToState;
        public Point[] Trajectory;
        public int Cost;

        protected SegmentOperator(SegmentState fromState, SegmentState toState, Point[] trajectory)
        {
            FromState = fromState;
            ToState = toState;
            Trajectory = trajectory;
        }
    }

    public enum OperatorResult
    {
        InProgress,
        Complete,
        Failed
    }

    public class PolicyOperator : SegmentOperator
    {
        public Policy<GameState> Policy;

        public PolicyOperator(SegmentState fromState, SegmentState toState, Point[] trajectory, Policy<GameState> policy, int cost)
            : base(fromState, toState, trajectory)
        {
            Policy = policy;
            Cost = cost;
        }

        public override string ToString()
        {
            return $"POLICY {Policy} from:{FromState} to:{ToState}";
        }
    }
}
