using GeometryFriends;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Drawing;
using System.Text.RegularExpressions;
using GeometryFriends.AI;

namespace GeometryFriendsAgents
{
    [Serializable]
    public abstract class SegmentTransitionAttempt
    {
        public CircleOperator op;
        public int timeSteps;
        public float probability;
        public CircleState fromState;
        public CircleState toState;

        public Point[] trajectory;
        public int[] collectibles = SegmentModel.NoCollectibles; 

        public Moves[] actions;
        public float nearestCorner;

        public Policy<GameState> Policy;

        protected SegmentTransitionAttempt()
        {
        }

        protected SegmentTransitionAttempt(CircleState fromState, CircleOperator op, int timeSteps, float probability,
            CircleState toState, Point[] trajectory, IEnumerable<int> collectibles)
        {
            this.op = op;
            this.timeSteps = timeSteps;
            this.probability = probability;
            this.fromState = fromState;
            this.toState = toState;
            this.trajectory = trajectory;
            if (collectibles.Count() > 0)
                this.collectibles = collectibles.ToArray();
            else
                this.collectibles = SegmentModel.NoCollectibles;

            if (toState.IsNull)
                throw new ApplicationException("Bad transition to null state!");
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = toState.GetHashCode();
                result = (result * 397) ^ fromState.GetHashCode();
                result = (result * 397) ^ (int)op.Move;
                result = (result * 397) ^ timeSteps;
                result = (result * 397) ^ (int)(probability * 100);
                return result;
            }
        }
    }
    [Serializable]
    public class SegmentTransition : SegmentTransitionAttempt
    {
        public SegmentTransition()
        {
        }

        public SegmentTransition(CircleState fromState, CircleOperator op, int timeSteps, float probability,
            CircleState toState, Point[] trajectory, IEnumerable<int> collectibles)
            : base(fromState, op, timeSteps, probability, toState, trajectory, collectibles)
        {
        }

        public override string ToString()
        {
            return string.Format("(Action:{0}x{1} p={2} {3}->{4})", op, timeSteps, probability, fromState, toState);
        }
    }
    [Serializable]
    public class SegmentFailure : SegmentTransitionAttempt
    {
        public SegmentFailure()
        {
        }

        public SegmentFailure(CircleState fromState, CircleState toState, CircleOperator op, int timeSteps, float probability,
            Point[] trajectory, IEnumerable<int> collectibles)
            : base(fromState, op, timeSteps, probability, toState, trajectory, collectibles)
        {
        }

        public override string ToString()
        {
            return string.Format("(Action:{0}x{1} p={2} {3}->FAILURE)", op, timeSteps, probability, fromState);
        }
    }

    [Serializable]
    public abstract class SegmentPotentialJumpAction
    {
        public float x;
        public float xVelocity;
        public int failedAttempts = 0;
        public int succeededAttempts = 0;

        public SegmentPotentialJumpAction(float x, float xVelocity)
        {
            this.x = x;
            this.xVelocity = xVelocity;
        }
    }

    [Serializable]
    public class SegmentPotentialJumpTransition : SegmentPotentialJumpAction
    {
        public Segment toSegment;

        public PointF CollisionPoint { get; private set; }
        public float CollisionDistance { get; private set; }

        public SegmentPotentialJumpTransition(Segment toSegment, float x, float xVelocity)
            : base(x, xVelocity)
        {
            this.toSegment = toSegment;
        }

        public void SetCollisionPoint(PointF point)
        {
            CollisionPoint = point;
            CollisionDistance = Helpers.SquaredDistance(point, toSegment.midpoint);
        }

        public override string ToString()
        {
            return $"JUMP TRANSITION: TO: {toSegment} X: {x} XVEL: {xVelocity}";
        }
    }

    [Serializable]
    public class SegmentPotentialRollTransition
    {
        public Segment toSegment;

        public float xVel;
        public int failedAttempts = 0;
        public int succeededAttempts = 0;
        public bool IsUpward = false;

        public SegmentPotentialRollTransition(Segment toSegment, float xVel)
        {
            this.toSegment = toSegment;
            this.xVel = xVel;
        }

        public override string ToString()
        {
            if (IsUpward)
                return $"ROLL UP TRANSITION: TO: {toSegment} XVEL: {xVel}";
            else
                return $"ROLL TRANSITION: TO: {toSegment} XVEL: {xVel}";
        }
    }

    [Serializable]
    public class SegmentPotentialJumpCollect : SegmentPotentialJumpAction
    {
        public int collectible;

        public SegmentPotentialJumpCollect(int collectible, float x, float xVelocity)
            : base(x, xVelocity)
        {
            this.collectible = collectible;
        }

        public override string ToString()
        {
            return $"JUMP COLLECT: collectible {collectible} X: {x} XVEL: {xVelocity}";
        }
    }

    [Serializable]
    public class SegmentPotentialRollCollect
    {
        public int collectible;

        public float xVel;
        public int failedAttempts = 0;
        public int succeededAttempts = 0;

        public SegmentPotentialRollCollect(int collectible, float xVel)
        {
            this.collectible = collectible;
            this.xVel = xVel;
        }

        public override string ToString()
        {
            return $"ROLLOFF COLLECT: collectible {collectible} XVEL: {xVel}";
        }
    }

    [Serializable]
    public class SegmentExploringJump : SegmentPotentialJumpAction
    {
        public SegmentExploringJump(float x, float xVelocity)
            : base(x, xVelocity)
        {
        }

        public override string ToString()
        {
            return $"EXPLORING JUMP: X: {x} XVEL: {xVelocity}";
        }
    }
}
