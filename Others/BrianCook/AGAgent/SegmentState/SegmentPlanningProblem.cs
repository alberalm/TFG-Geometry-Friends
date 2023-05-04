using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GeometryFriendsAgents
{
    public class SegmentPlanningProblem : PlanningProblem<SegmentState, SegmentOperator>
    {
        public GameArea gameArea;
        public SegmentState initialState;
        public CircleStateMapper stateMapper;
        public SegmentModel segmentModel;

        public override SegmentState InitialState { get { return initialState; } }

        public override IGoal<SegmentState> Goal { get { return goal; } }
        IGoal<SegmentState> goal = new SegmentGoal();

        public SegmentPlanningProblem(GameArea gameArea, CircleStateMapper stateMapper, SegmentModel segmentModel)
        {
            this.gameArea = gameArea;
            this.stateMapper = stateMapper;
            this.segmentModel = segmentModel;

            Logger.Write($"Game area {gameArea}");
            Logger.Write($"State mapper {stateMapper}");
            Logger.Write($"Segment model {segmentModel}");
            var initialSegment = segmentModel.GetSegmentBeneath(gameArea.StartingPosition.X, gameArea.StartingPosition.Y);
            if (initialSegment != null)
            {
                var remainingCollectibles = new List<int>();
                for (int i = 0; i < gameArea.Collectibles.Count; i++)
                    remainingCollectibles.Add(i);
                initialState = new SegmentState(initialSegment, gameArea.StartingPosition.X, 0, remainingCollectibles.ToArray());
            }
            else
                Logger.Write($"WARNING: No initial segment state");
        }

        public void SetInitialState(SegmentState state)
        {
            initialState = state;
        }

        public override bool IsGoal(ref SegmentState state)
        {
            return goal.IsGoal(ref state);
        }

        public override void GetApplicableOperators(List<SegmentOperator> operators, ref SegmentState state)
        {
            operators.Clear();

            if (!segmentModel.modelInfo.ContainsKey(state.segment))
                throw new ApplicationException("State segment not in model: " + state);

            var info = segmentModel.modelInfo[state.segment];
            foreach (var collectible in info.segmentCollectibles)
                if (state.remainingCollectibles.Contains(collectible))
                {
                    var targetLocation = stateMapper.GetCollectible(collectible);

                    var nextState = state.Copy();
                    var deltaY = Math.Abs(targetLocation.Y - state.y);
                    var deltaXsquared = (GamePhysics.CIRCLE_RADIUS + GamePhysics.GOAL_RADIUS) * (GamePhysics.CIRCLE_RADIUS + GamePhysics.GOAL_RADIUS) - deltaY * deltaY;
                    var deltaX = deltaXsquared > 0 ? (float)Math.Sqrt(deltaXsquared) : 0;
                    if (targetLocation.X > state.x)
                        targetLocation.X = Math.Min(Math.Max(state.x, targetLocation.X - deltaX + RollToPolicy.XTolerance), targetLocation.X);
                    else if (targetLocation.X < state.x)
                        targetLocation.X = Math.Max(Math.Min(state.x, targetLocation.X + deltaX - RollToPolicy.XTolerance), targetLocation.X);

                    nextState.x = targetLocation.X; 
                    nextState.xVelocity = 0;
                    nextState.RemoveCollectible(collectible);
                    var trajectory = new Point[] { new Point((int)state.x, (int)state.y), new Point((int)nextState.x, (int)nextState.y) };
                    var policy = new RollToPolicy(nextState.x, nextState.xVelocity, nextState.y);
                    int cost = EstimateRollingTimeSteps(state.x, nextState.x, state.xVelocity, 0);
                    var op = new PolicyOperator(state, nextState, trajectory, policy, cost);
                    operators.Add(op);
                }
            var relevantFailures = new List<Tuple<SegmentFailure, float>>();
            foreach (var failure in info.failures)
                foreach (var collectible in failure.collectibles)
                    if (state.remainingCollectibles.Contains(collectible))
                    {
                        var point = stateMapper.GetCollectible(collectible);
                        var xDistance = Math.Abs(point.X - failure.fromState.x);
                        xDistance += Math.Abs(point.X - failure.toState.x);
                        relevantFailures.Add(new Tuple<SegmentFailure, float>(failure, xDistance));
                    }
            relevantFailures.Sort((a, b) => { return a.Item2.CompareTo(b.Item2); });
            if (relevantFailures.Count > 0)
            {
                var failure = relevantFailures[0].Item1;
                var op = GetTransitionOperator(state, failure);
                operators.Add(op);
            }
            foreach (var entry in info.connections)
            {
                var toSegment = entry.Key;
                var transitions = entry.Value;
                foreach (var transition in transitions)
                {
                    if (transition.Policy != null && transition.Policy.Failures > 0)
                        continue;

                    var op = GetTransitionOperator(state, transition);
                    operators.Add(op);
                }
            }
        }

        PolicyOperator GetTransitionOperator(SegmentState state, SegmentTransitionAttempt transition)
        {
            if (transition.Policy is CompositePolicy && transition.collectibles.Length == 0)
            {
                var composite = transition.Policy as CompositePolicy;
                var rollPolicy = composite.GetChildPolicy<RollToPolicy>();
                var jumpPolicy = composite.GetChildPolicy<JumpAndStopPolicy>();
                var fallPolicy = composite.GetChildPolicy<FallAndStopPolicy>();
                if (rollPolicy != null && jumpPolicy != null)
                {
                    int cost = EstimateRollingTimeSteps(state.x, rollPolicy.targetX, state.xVelocity, rollPolicy.targetXvelocity);
                    CircleRepresentation groundedState;
                    int groundedSteps;
                    if (jumpPolicy.GetGroundedPoint(out groundedState, out groundedSteps))
                    {
                        cost += groundedSteps;
                        jumpPolicy.TruncateWhenGrounded();

                        var endState = stateMapper.CreateState(groundedState);
                        var nextState = segmentModel.GetSegmentState(endState, state.remainingCollectibles);
                        nextState.RemoveCollectibles(transition.collectibles);
                        return new PolicyOperator(state, nextState, transition.trajectory, transition.Policy, cost + GetCornerCostPenalty(transition.Policy.NearestCorner));
                    }
                }
                else if (rollPolicy != null && fallPolicy != null)
                {
                    int cost = EstimateRollingTimeSteps(state.x, rollPolicy.targetX, state.xVelocity, rollPolicy.targetXvelocity);
                    CircleRepresentation groundedState;
                    int groundedSteps;
                    if (fallPolicy.GetGroundedPoint(out groundedState, out groundedSteps))
                    {
                        cost += groundedSteps;
                        fallPolicy.TruncateWhenGrounded();

                        var endState = stateMapper.CreateState(groundedState);
                        var nextState = segmentModel.GetSegmentState(endState, state.remainingCollectibles);
                        nextState.RemoveCollectibles(transition.collectibles);
                        return new PolicyOperator(state, nextState, transition.trajectory, transition.Policy, cost + GetCornerCostPenalty(transition.Policy.NearestCorner));
                    }
                }
            }

            if (transition.Policy is CompositePolicy)
            {
                var composite = transition.Policy as CompositePolicy;
                var rollPolicy = composite.GetChildPolicy<RollToPolicy>();
                var jumpPolicy = composite.GetChildPolicy<JumpAndStopPolicy>();
                var fallPolicy = composite.GetChildPolicy<FallAndStopPolicy>();
                if (rollPolicy != null && jumpPolicy != null)
                {
                    int cost = EstimateRollingTimeSteps(state.x, rollPolicy.targetX, state.xVelocity, rollPolicy.targetXvelocity);
                    cost += jumpPolicy.Steps;

                    var nextState = segmentModel.GetSegmentState(transition.toState, state.remainingCollectibles);
                    nextState.RemoveCollectibles(transition.collectibles);
                    return new PolicyOperator(state, nextState, transition.trajectory, transition.Policy, cost + GetCornerCostPenalty(transition.Policy.NearestCorner));
                }
                else if (rollPolicy != null && fallPolicy != null)
                {
                    int cost = EstimateRollingTimeSteps(state.x, rollPolicy.targetX, state.xVelocity, rollPolicy.targetXvelocity);
                    cost += fallPolicy.Steps;

                    var nextState = segmentModel.GetSegmentState(transition.toState, state.remainingCollectibles);
                    nextState.RemoveCollectibles(transition.collectibles);
                    return new PolicyOperator(state, nextState, transition.trajectory, transition.Policy, cost + GetCornerCostPenalty(transition.Policy.NearestCorner));
                }
            }
            else if (transition.Policy is RollToPolicy)
            {
                var rollPolicy = transition.Policy as RollToPolicy;
                int cost = EstimateRollingTimeSteps(state.x, rollPolicy.targetX, state.xVelocity, rollPolicy.targetXvelocity);

                var nextState = segmentModel.GetSegmentState(transition.toState, state.remainingCollectibles);
                nextState.RemoveCollectibles(transition.collectibles);
                return new PolicyOperator(state, nextState, transition.trajectory, transition.Policy, cost + GetCornerCostPenalty(transition.Policy.NearestCorner));
            }

            {
                var nextState = segmentModel.GetSegmentState(transition.toState, state.remainingCollectibles);
                nextState.RemoveCollectibles(transition.collectibles);
                Logger.Write($"IGNORING COST PENALTY! {transition.Policy}");
                return new PolicyOperator(state, nextState, transition.trajectory, transition.Policy, transition.timeSteps);
            }
        }

        public const float cornerThreshold1 = GamePhysics.CIRCLE_RADIUS / 4;
        const float cornerCost1 = 0.2f;
        const float totalCornerCost1 = (cornerThreshold1 - cornerThreshold2) * cornerCost1;
        const float cornerThreshold2 = GamePhysics.CIRCLE_RADIUS / 8;
        const float cornerCost2 = 5f;
        const float totalCornerCost2 = (cornerThreshold2 - cornerThreshold3) * cornerCost2 + totalCornerCost1;
        const float cornerThreshold3 = 3; 
        const float cornerCost3 = 10f;

        int GetCornerCostPenalty(float nearestCorner)
        {
            nearestCorner -= GamePhysics.CIRCLE_RADIUS;

            if (nearestCorner > cornerThreshold1)
            {
                return 0;
            }

            float costSeconds = 0;
            if (nearestCorner > cornerThreshold2)
                costSeconds = (cornerThreshold1 - nearestCorner) * cornerCost1;
            else if (nearestCorner > cornerThreshold3)
                costSeconds = totalCornerCost1 + (cornerThreshold2 - nearestCorner) * cornerCost2;
            else if (nearestCorner < 0.1f)
                costSeconds = 50;
            else
                costSeconds = 25;

            var penalty = (int)(costSeconds / Simulation.TimeStep);
            return penalty;
        }

        public override bool GetSuccessorState(ref SegmentState state, SegmentOperator op, out SegmentState successor)
        { 
            var policyOp = op as PolicyOperator;
            successor = policyOp.ToState;
            return true;
        }

        public static int EstimateRollingTimeSteps(float x, float targetX, float xVelocity, float targetVelocity)
        {
            var rollingState = new RollingState() { X = x, XVelocity = xVelocity, targetX = targetX, targetXVelocity = targetVelocity };
            var seconds = Math.Abs(RollToPolicy.VTable.GetValue(rollingState)) / 100;
            return (int)(seconds / Simulation.TimeStep);
        }

        public override float GetCost(SegmentOperator op)
        {
            if (op.Cost > 0)
                return op.Cost;
            return 1;
        }

        public override string GetString(ref SegmentState state)
        {
            return state.ToString();
        }
    }
}
