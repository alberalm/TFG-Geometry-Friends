using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI;

namespace GeometryFriendsAgents
{
    public class SegmentAnalyzer
    {
        const float MaxSimulationTime = 15f;
        Func<Simulation, bool> isFinished = s => s.ElapsedTime >= MaxSimulationTime;

        Random random = new Random();
        SegmentModel segmentModel;
        CircleStateMapper stateMapper;
        Dictionary<int, float> velocityByDistance = new Dictionary<int, float>();
        float deltaX;
        float deltaXvel;
        public Segment ExploringSegment { get; private set; }
        List<Segment> allSegments;
        public DebugInformation[] DebugInfo { get; private set; }
        public int Explorations { get; private set; }
        Moves[] airMoves = new Moves[] { Moves.NO_ACTION };

        public const float stoppedXvel = 10f;
        static public bool IsDebug = false;

        public SegmentAnalyzer(SegmentModel segmentModel, CircleStateMapper stateMapper, float deltaX, float deltaXvel)
        {
            this.segmentModel = segmentModel;
            this.deltaX = deltaX;
            this.deltaXvel = deltaXvel;
            this.stateMapper = stateMapper;
            allSegments = segmentModel.GetAllSegments();
            segmentModel.InitializePotentialTransitions(5, 10);
        }

        List<Segment> _tempExplored = new List<Segment>();
        int[] remainingCollectibles;

        public void Explore(Segment fromSegment, int[] remainingCollectibles)
        {
            if (IsDebug) Logger.Write($"START EXPLORING");
            Explorations += 1;
            _tempExplored.Clear();
            this.remainingCollectibles = remainingCollectibles;
            Explore(fromSegment, _tempExplored);
            if (IsDebug) Logger.Write($"STOP EXPLORING");
        }

        int GetJumpCollectAttempts(int numExplorations)
        {
            if (numExplorations < 3)
                return 10;
            return 4;
        }

        int GetRollCollectAttempts(int numExplorations)
        {
            if (numExplorations < 3)
                return 8;
            return 4;
        }

        int GetJumpTransitionAttempts(int numExplorations)
        {
            if (numExplorations < 3)
                return 25;

            return 10;
        }

        bool SortByJumpTransitionDistance(int numExplorations)
        {
            return numExplorations % 4 == 0;
        }

        int GetRollTransitionAttempts(int numExplorations)
        {
            if (numExplorations < 3)
                return 8;
            return 4;
        }

        int GetExploringJumpAttempts(int numExplorations)
        {
            if (numExplorations < 3)
                return 2;

            return numExplorations * 2;
        }

        void Explore(Segment fromSegment, List<Segment> explored)
        {
            if (explored.Contains(fromSegment))
                return;

            ExploringSegment = fromSegment;
            var segmentInfo = segmentModel.modelInfo[fromSegment];
            if (segmentInfo.StoppedState == null)
                return;

            explored.Add(fromSegment);
            segmentInfo.ExplorationAttempts += 1;

            if (IsDebug) Logger.Write($"****************** EXPLORING FROM {fromSegment}");
            if (!segmentInfo.SearchedSegmentCollectibles)
            {
                for (float x = fromSegment.from.X; x <= fromSegment.to.X; x += deltaX)
                {
                    stateMapper.GetCollectibles(new PointF(x, fromSegment.from.Y), tempCollectibles);
                    foreach (int collectible in tempCollectibles)
                    {
                        if (!segmentInfo.segmentCollectibles.Contains(collectible))
                        {
                            if (IsDebug) Logger.Write($"GOT COLLECTIBLE {collectible} by rolling on current segment!");
                            segmentInfo.segmentCollectibles.Add(collectible);
                        }
                        if (!segmentInfo.collectibles.Contains(collectible))
                            segmentInfo.collectibles.Add(collectible);
                    }
                }
                segmentInfo.SearchedSegmentCollectibles = true;
            }
            foreach (var entry in segmentInfo.potentialJumpCollects)
            {
                var collectible = entry.Key;
                if (!remainingCollectibles.Contains(collectible))
                    continue;
                var goal = segmentModel.Collectibles[collectible];
                var potentialJumpCollects = entry.Value.Where(c => c.failedAttempts + c.succeededAttempts == 0).Shuffle();
                int count = Math.Min(potentialJumpCollects.Count(), GetJumpCollectAttempts(segmentInfo.ExplorationAttempts));
                int attempts = 0;
                foreach (var potentialJumpCollect in potentialJumpCollects)
                {
                    foreach (var airMove in airMoves)
                    {
                        var actionSimulator = segmentInfo.StoppedState.CreateUpdatedSimulator();
                        try
                        {
                            var simulation = new Simulation();
                            var rollToPolicy = new RollToPolicy(potentialJumpCollect.x, potentialJumpCollect.xVelocity, fromSegment.y);
                            var jumpPolicy = new JumpAndStopPolicy(airMove);
                            var policy = new CompositePolicy(rollToPolicy, jumpPolicy);

                            if (IsDebug) Logger.Write($"Trying from {fromSegment} to collectible {goal} jumping from {rollToPolicy.targetX} velocity {rollToPolicy.targetXvelocity}  (out of {potentialJumpCollects.Count()} choices)");

                            var gameState = segmentInfo.StoppedGameState.Copy();
                            simulation.Simulate(gameState, actionSimulator, policy, isFinished);

                            bool succeeded = false;
                            if (policy.IsFinished(gameState) && !policy.Failed)
                            {
                                if (IsDebug) Logger.Write($"Policy finished at {gameState} Elapsed={simulation.ElapsedTime} Steps={policy.Steps}");
                                var circle = gameState.Circle;
                                var endSegment = segmentModel.GetSegmentBeneath(circle.X, circle.Y);
                                var startCircle = segmentInfo.StoppedGameState.Circle;
                                var fromState = stateMapper.CreateState(rollToPolicy.targetX, startCircle.Y, rollToPolicy.targetXvelocity, 0);
                                var toState = stateMapper.CreateState(circle);
                                if (toState.isGrounded && endSegment != null)
                                {
                                    var probability = 1f;
                                    GetCollectibles(simulation.Trajectory, trajectoryCollectibles);
                                    var transition = new SegmentTransition(fromState, CircleOperator.Jump, policy.Steps, probability, toState, simulation.Trajectory.ToArray(), trajectoryCollectibles);
                                    transition.Policy = policy;
                                    if (simulation.Actions != null)
                                        transition.actions = simulation.Actions.ToArray();
                                    transition.nearestCorner = simulation.NearestCorner;

                                    if (trajectoryCollectibles.Count > 0 || endSegment != fromSegment)
                                    {
                                        addTransition(segmentInfo, fromSegment, endSegment, transition);
                                        var endInfo = segmentModel.GetInfo(endSegment);
                                        if (endInfo.StoppedState == null)
                                        {
                                            endInfo.StoppedState = actionSimulator;
                                            endInfo.StoppedGameState = gameState;
                                        }
                                    }

                                    if (trajectoryCollectibles.Count > 0)
                                    {
                                        if (trajectoryCollectibles.Contains(collectible))
                                            succeeded = true;
                                        if (IsDebug) Logger.Write($"SUCCEEDED: Collected {trajectoryCollectibles.ToOutputString()}, ended on segment {endSegment}");
                                    }
                                    else
                                    {
                                        if (IsDebug) Logger.Write($"FAILED: No collectibles. Ended on segment {endSegment}");
                                    }
                                }
                                else
                                {
                                    if (IsDebug) Logger.Write($"FAILED: Not grounded");
                                }
                            }
                            else
                            {
                                if (IsDebug) Logger.Write($"Policy FAILED Elapsed = {simulation.ElapsedTime} Steps = {policy.Steps}");
                            }

                            if (succeeded)
                                potentialJumpCollect.succeededAttempts++;
                            else
                                potentialJumpCollect.failedAttempts++;
                        }
                        catch (Exception ex)
                        {
                            if (IsDebug) Logger.Write($"Simulation Exception: {ex.Message}");
                            if (IsDebug) Logger.Write($"{ex.StackTrace}");
                            throw ex;
                        }
                    }

                    attempts++;
                    if (attempts >= count)
                        break;
                }
            }
            foreach (var entry in segmentInfo.potentialRollCollects)
            {
                var collectible = entry.Key;
                if (!remainingCollectibles.Contains(collectible))
                    continue;
                var goal = segmentModel.Collectibles[collectible];
                var potentialRollCollects = entry.Value.Where(c => c.failedAttempts + c.succeededAttempts == 0).Shuffle();
                int count = Math.Min(potentialRollCollects.Count(), GetRollCollectAttempts(segmentInfo.ExplorationAttempts));
                int attempts = 0;
                foreach (var potentialRollCollect in potentialRollCollects)
                {
                    foreach (var airMove in airMoves)
                    {
                        var actionSimulator = segmentInfo.StoppedState.CreateUpdatedSimulator();
                        try
                        {
                            var simulation = new Simulation();

                            RollToPolicy rollToPolicy;
                            if (potentialRollCollect.xVel > 0)
                            {
                                var x = fromSegment.to.X;
                                rollToPolicy = new RollToPolicy(x, potentialRollCollect.xVel, fromSegment.y) { IsRollOff = true };
                            }
                            else
                            {
                                var x = fromSegment.from.X;
                                rollToPolicy = new RollToPolicy(x, potentialRollCollect.xVel, fromSegment.y) { IsRollOff = true };
                            }
                            var fallPolicy = new FallAndStopPolicy(airMove);
                            var policy = new CompositePolicy(rollToPolicy, fallPolicy);

                            if (IsDebug) Logger.Write($"Trying from {fromSegment} to collectible {goal} rolling from {rollToPolicy.targetX} velocity {rollToPolicy.targetXvelocity}");

                            var gameState = segmentInfo.StoppedGameState.Copy();
                            simulation.Simulate(gameState, actionSimulator, policy, isFinished);

                            bool succeeded = false;
                            if (policy.IsFinished(gameState) && !policy.Failed)
                            {
                                if (IsDebug) Logger.Write($"Policy finished at {gameState} Elapsed={simulation.ElapsedTime} Steps={policy.Steps}");
                                var circle = gameState.Circle;
                                var endSegment = segmentModel.GetSegmentBeneath(circle.X, circle.Y);
                                var startCircle = segmentInfo.StoppedGameState.Circle;
                                var fromState = stateMapper.CreateState(rollToPolicy.targetX, startCircle.Y, rollToPolicy.targetXvelocity, 0);
                                var toState = stateMapper.CreateState(circle);
                                if (toState.isGrounded && endSegment != null)
                                {
                                    var probability = 1f;
                                    GetCollectibles(simulation.Trajectory, trajectoryCollectibles);
                                    var transition = new SegmentTransition(fromState, CircleOperator.None, policy.Steps, probability, toState, simulation.Trajectory.ToArray(), trajectoryCollectibles);
                                    transition.Policy = policy;
                                    if (simulation.Actions != null)
                                        transition.actions = simulation.Actions.ToArray();
                                    transition.nearestCorner = simulation.NearestCorner;

                                    if (trajectoryCollectibles.Count > 0 || endSegment != fromSegment)
                                    {
                                        addTransition(segmentInfo, fromSegment, endSegment, transition);
                                        var endInfo = segmentModel.GetInfo(endSegment);
                                        if (endInfo.StoppedState == null)
                                        {
                                            endInfo.StoppedState = actionSimulator;
                                            endInfo.StoppedGameState = gameState;
                                        }
                                    }

                                    if (trajectoryCollectibles.Count > 0)
                                    {
                                        if (trajectoryCollectibles.Contains(collectible))
                                            succeeded = true;
                                        if (IsDebug) Logger.Write($"SUCCEEDED: Collected {trajectoryCollectibles.ToOutputString()}, ended on segment {endSegment}");
                                    }
                                    else
                                    {
                                        if (IsDebug) Logger.Write($"FAILED: No collectibles. Ended on segment {endSegment}");
                                    }
                                }
                                else
                                    if (IsDebug) Logger.Write($"FAILED: Not grounded");
                            }
                            else
                                if (IsDebug) Logger.Write($"Policy FAILED Elapsed = {simulation.ElapsedTime} Steps = {policy.Steps}");

                            if (succeeded)
                                potentialRollCollect.succeededAttempts += 1;
                            else
                                potentialRollCollect.failedAttempts += 1;
                            if (simulation.NearestCorner > SegmentPlanningProblem.cornerThreshold1)
                                attempts++;
                        }
                        catch (Exception ex)
                        {
                            if (IsDebug) Logger.Write($"Simulation Exception: {ex.Message}");
                            if (IsDebug) Logger.Write($"{ex.StackTrace}");
                            throw ex;
                        }
                    }

                    if (attempts >= count)
                        break;
                }
            }
            foreach (var toSegment in allSegments)
            {
                var isFocus = false;

                if (toSegment == fromSegment)
                    continue;
                var potentialJumpTransitions = segmentInfo.potentialJumpTransitions[toSegment].Where(t => t.failedAttempts + t.succeededAttempts == 0).Shuffle();
                if (SortByJumpTransitionDistance(segmentInfo.ExplorationAttempts))
                    potentialJumpTransitions.OrderBy(t => t.CollisionDistance);
                int count = Math.Min(potentialJumpTransitions.Count(), GetJumpTransitionAttempts(segmentInfo.ExplorationAttempts));
                int attempts = 0;
                if (isFocus) Logger.Write($"EVALUATING {fromSegment}: totalCount {potentialJumpTransitions.Count()}  count {count}");
                foreach (var potentialTransition in potentialJumpTransitions)
                {
                    foreach (var airMove in airMoves)
                    {
                        var actionSimulator = segmentInfo.StoppedState.CreateUpdatedSimulator();
                        try
                        {
                            var simulation = new Simulation();

                            var rollToPolicy = new RollToPolicy(potentialTransition.x, potentialTransition.xVelocity, fromSegment.y);
                            var jumpPolicy = new JumpAndStopPolicy(airMove);
                            var policy = new CompositePolicy(rollToPolicy, jumpPolicy);

                            if (IsDebug) Logger.Write($"Trying from {fromSegment} to {toSegment} jumping from {rollToPolicy.targetX} velocity {rollToPolicy.targetXvelocity}");

                            var gameState = segmentInfo.StoppedGameState.Copy();
                            simulation.Simulate(gameState, actionSimulator, policy, isFinished);

                            bool succeeded = false;
                            if (policy.IsFinished(gameState) && !policy.Failed)
                            {
                                if (IsDebug) Logger.Write($"Policy finished at {gameState} Elapsed={simulation.ElapsedTime} Steps={policy.Steps}");
                                var circle = gameState.Circle;
                                var endSegment = segmentModel.GetSegmentBeneath(circle.X, circle.Y);
                                var startCircle = segmentInfo.StoppedGameState.Circle;
                                var fromState = stateMapper.CreateState(rollToPolicy.targetX, startCircle.Y, rollToPolicy.targetXvelocity, 0);
                                var toState = stateMapper.CreateState(circle);
                                if (toState.isGrounded && endSegment != null)
                                {
                                    var probability = 1f;
                                    GetCollectibles(simulation.Trajectory, trajectoryCollectibles);
                                    var transition = new SegmentTransition(fromState, CircleOperator.Jump, policy.Steps, probability, toState, simulation.Trajectory.ToArray(), trajectoryCollectibles);
                                    transition.Policy = policy;
                                    if (simulation.Actions != null)
                                        transition.actions = simulation.Actions.ToArray();
                                    transition.nearestCorner = simulation.NearestCorner;

                                    addTransition(segmentInfo, fromSegment, endSegment, transition);
                                    var endInfo = segmentModel.GetInfo(endSegment);
                                    if (endInfo.StoppedState == null)
                                    {
                                        endInfo.StoppedState = actionSimulator;
                                        endInfo.StoppedGameState = gameState;
                                    }

                                    if (endSegment == toSegment)
                                    {
                                        succeeded = true;
                                        if (IsDebug) Logger.Write($"SUCCEEDED: Ended on segment {endSegment}");
                                    }
                                    else
                                        if (IsDebug) Logger.Write($"FAILED: Ended on segment {endSegment}");
                                }
                                else
                                    if (IsDebug) Logger.Write($"FAILED: Not grounded");
                            }
                            else
                                if (IsDebug) Logger.Write($"Policy FAILED Elapsed = {simulation.ElapsedTime} Steps = {policy.Steps}");

                            if (succeeded)
                                potentialTransition.succeededAttempts++;
                            else
                                potentialTransition.failedAttempts++;
                            if (simulation.NearestCorner > SegmentPlanningProblem.cornerThreshold1)
                                attempts++;

                            var debugInfo = simulation.GetTrajectoryDebug(simulation.Trajectory);
                            DebugInfo = debugInfo.Concat(GetTransitionDebug(potentialTransition, segmentInfo.StoppedGameState.Circle.Y)).ToArray();
                        }
                        catch (Exception ex)
                        {
                            if (IsDebug) Logger.Write($"Simulation Exception: {ex.Message}");
                            if (IsDebug) Logger.Write($"{ex.StackTrace}");
                            throw ex;
                        }
                    }

                    if (attempts >= count)
                        break;
                }
                var potentialRollTransitions = segmentInfo.potentialRollTransitions[toSegment].Where(t => t.failedAttempts + t.succeededAttempts == 0).Shuffle();
                count = Math.Min(potentialRollTransitions.Count(), GetRollTransitionAttempts(segmentInfo.ExplorationAttempts));
                attempts = 0;
                foreach (var potentialTransition in potentialRollTransitions)
                {
                    foreach (var airMove in airMoves)
                    {
                        var actionSimulator = segmentInfo.StoppedState.CreateUpdatedSimulator();
                        try
                        {
                            var simulation = new Simulation();
                            Policy<GameState> policy;
                            float fromX, fromXvelocity;

                            if (toSegment.y < fromSegment.y)
                            {
                                var xVel = potentialTransition.xVel;
                                RollUpPolicy rollUpPolicy;
                                if (xVel > 0)
                                {
                                    rollUpPolicy = new RollUpPolicy(toSegment.from.X, toSegment.y, xVel);
                                    fromX = fromSegment.to.X;
                                    fromXvelocity = xVel;
                                }
                                else
                                {
                                    rollUpPolicy = new RollUpPolicy(toSegment.to.X, toSegment.y, xVel);
                                    fromX = fromSegment.from.X;
                                    fromXvelocity = xVel;
                                }
                                policy = rollUpPolicy;
                                if (IsDebug) Logger.Write($"Trying from {fromSegment} to {toSegment} rolling up at {rollUpPolicy.x},{rollUpPolicy.y} velocity {rollUpPolicy.xVelocity}");
                            }
                            else
                            {
                                RollToPolicy rollToPolicy;
                                var xVel = potentialTransition.xVel;
                                if (xVel > 0)
                                {
                                    var x = fromSegment.to.X;
                                    rollToPolicy = new RollToPolicy(x, xVel, fromSegment.y) { IsRollOff = true };
                                }
                                else
                                {
                                    var x = fromSegment.from.X;
                                    rollToPolicy = new RollToPolicy(x, xVel, fromSegment.y) { IsRollOff = true };
                                }
                                var fallPolicy = new FallAndStopPolicy(airMove);
                                policy = new CompositePolicy(rollToPolicy, fallPolicy);
                                fromX = rollToPolicy.targetX;
                                fromXvelocity = rollToPolicy.targetXvelocity;

                                if (IsDebug) Logger.Write($"Trying from {fromSegment} to {toSegment} rolling from {rollToPolicy.targetX} velocity {rollToPolicy.targetXvelocity}");
                            }

                            var gameState = segmentInfo.StoppedGameState.Copy();
                            simulation.Simulate(gameState, actionSimulator, policy, isFinished);

                            bool succeeded = false;
                            if (policy.IsFinished(gameState) && !policy.Failed)
                            {
                                if (IsDebug) Logger.Write($"Policy finished at {gameState} Elapsed={simulation.ElapsedTime} Steps={policy.Steps}");
                                var circle = gameState.Circle;
                                var endSegment = segmentModel.GetSegmentBeneath(circle.X, circle.Y);
                                var startCircle = segmentInfo.StoppedGameState.Circle;
                                var fromState = stateMapper.CreateState(fromX, startCircle.Y, fromXvelocity, 0);
                                var toState = stateMapper.CreateState(circle);
                                if (toState.isGrounded && endSegment != null)
                                {
                                    var probability = 1f;
                                    GetCollectibles(simulation.Trajectory, trajectoryCollectibles);
                                    var transition = new SegmentTransition(fromState, CircleOperator.None, policy.Steps, probability, toState, simulation.Trajectory.ToArray(), trajectoryCollectibles);
                                    transition.Policy = policy;
                                    if (simulation.Actions != null)
                                        transition.actions = simulation.Actions.ToArray();
                                    transition.nearestCorner = simulation.NearestCorner;

                                    addTransition(segmentInfo, fromSegment, endSegment, transition);
                                    var endInfo = segmentModel.GetInfo(endSegment);
                                    if (endInfo.StoppedState == null)
                                    {
                                        endInfo.StoppedState = actionSimulator;
                                        endInfo.StoppedGameState = gameState;
                                    }

                                    if (endSegment == toSegment)
                                    {
                                        succeeded = true;
                                        if (IsDebug) Logger.Write($"SUCCEEDED: Ended on segment {endSegment}");
                                    }
                                    else
                                        if (IsDebug) Logger.Write($"FAILED: Ended on segment {endSegment}");
                                }
                                else
                                    if (IsDebug) Logger.Write($"FAILED: Not grounded");
                            }
                            else
                                if (IsDebug) Logger.Write($"Policy FAILED Elapsed = {simulation.ElapsedTime} Steps = {policy.Steps}");

                            if (succeeded)
                                potentialTransition.succeededAttempts++;
                            else
                                potentialTransition.failedAttempts++;
                            if (simulation.NearestCorner > SegmentPlanningProblem.cornerThreshold1)
                                attempts++;

                            var debugInfo = simulation.GetTrajectoryDebug(simulation.Trajectory);
                            DebugInfo = debugInfo.ToArray();
                        }
                        catch (Exception ex)
                        {
                            if (IsDebug) Logger.Write($"Simulation Exception: {ex.Message}");
                            if (IsDebug) Logger.Write($"{ex.StackTrace}");
                            throw ex;
                        }
                    }

                    if (attempts >= count)
                        break;
                }
                Explore(toSegment, explored);
            }
            {
                var exploringJumps = segmentInfo.exploringJumps.Where(c => c.failedAttempts + c.succeededAttempts == 0).Shuffle();
                int count = Math.Min(exploringJumps.Count(), GetExploringJumpAttempts(segmentInfo.ExplorationAttempts));
                int attempts = 0;
                foreach (var exploringJump in exploringJumps)
                {
                    foreach (var airMove in airMoves)
                    {
                        var actionSimulator = segmentInfo.StoppedState.CreateUpdatedSimulator();
                        try
                        {
                            var simulation = new Simulation();
                            var rollToPolicy = new RollToPolicy(exploringJump.x, exploringJump.xVelocity, fromSegment.y);
                            var jumpPolicy = new JumpAndStopPolicy(airMove);
                            var policy = new CompositePolicy(rollToPolicy, jumpPolicy);

                            if (IsDebug) Logger.Write($"Trying from {fromSegment} exploring jump from {rollToPolicy.targetX} velocity {rollToPolicy.targetXvelocity}  (out of {exploringJumps.Count()} choices)");

                            var gameState = segmentInfo.StoppedGameState.Copy();
                            simulation.Simulate(gameState, actionSimulator, policy, isFinished);

                            bool succeeded = false;
                            if (policy.IsFinished(gameState) && !policy.Failed)
                            {
                                if (IsDebug) Logger.Write($"Policy finished at {gameState} Elapsed={simulation.ElapsedTime} Steps={policy.Steps}");
                                var circle = gameState.Circle;
                                var endSegment = segmentModel.GetSegmentBeneath(circle.X, circle.Y);
                                var startCircle = segmentInfo.StoppedGameState.Circle;
                                var fromState = stateMapper.CreateState(rollToPolicy.targetX, startCircle.Y, rollToPolicy.targetXvelocity, 0);
                                var toState = stateMapper.CreateState(circle);
                                if (toState.isGrounded && endSegment != null)
                                {
                                    var probability = 1f;
                                    GetCollectibles(simulation.Trajectory, trajectoryCollectibles);
                                    var transition = new SegmentTransition(fromState, CircleOperator.Jump, policy.Steps, probability, toState, simulation.Trajectory.ToArray(), trajectoryCollectibles);
                                    transition.Policy = policy;
                                    if (simulation.Actions != null)
                                        transition.actions = simulation.Actions.ToArray();
                                    transition.nearestCorner = simulation.NearestCorner;

                                    if (trajectoryCollectibles.Count > 0 || endSegment != fromSegment)
                                    {
                                        addTransition(segmentInfo, fromSegment, endSegment, transition);
                                        var endInfo = segmentModel.GetInfo(endSegment);
                                        if (endInfo.StoppedState == null)
                                        {
                                            endInfo.StoppedState = actionSimulator;
                                            endInfo.StoppedGameState = gameState;
                                        }

                                        succeeded = true;
                                    }

                                    if (succeeded)
                                    {
                                        if (IsDebug) Logger.Write($"SUCCEEDED: Collected {trajectoryCollectibles.ToOutputString()}, ended on segment {endSegment}");
                                    }
                                    else
                                    {
                                        if (IsDebug) Logger.Write($"FAILED: No collectibles. Ended on segment {endSegment}");
                                    }
                                }
                                else
                                {
                                    if (IsDebug) Logger.Write($"FAILED: Not grounded");
                                }
                            }
                            else
                            {
                                if (IsDebug) Logger.Write($"Policy FAILED Elapsed = {simulation.ElapsedTime} Steps = {policy.Steps}");
                            }

                            if (succeeded)
                                exploringJump.succeededAttempts++;
                            else
                                exploringJump.failedAttempts++;
                        }
                        catch (Exception ex)
                        {
                            if (IsDebug) Logger.Write($"Simulation Exception: {ex.Message}");
                            if (IsDebug) Logger.Write($"{ex.StackTrace}");
                            throw ex;
                        }
                    }

                    attempts++;
                    if (attempts >= count)
                        break;
                }
            }
        }

        void addTransition(SegmentInfo segmentInfo, Segment fromSegment, Segment toSegment, SegmentTransition transition)
        {
            if (fromSegment == toSegment)
            {
                if (transition.collectibles.Length > 0)
                {
                    var failure = new SegmentFailure(transition.fromState, transition.toState, transition.op, transition.timeSteps, transition.probability, transition.trajectory, transition.collectibles);
                    failure.Policy = transition.Policy;
                    failure.actions = transition.actions;
                    failure.nearestCorner = transition.nearestCorner;
                    segmentInfo.failures.Add(failure);
                }
            }
            else
                segmentInfo.connections[toSegment].Add(transition);
        }

        List<DebugInformation> GetTransitionDebug(SegmentPotentialJumpTransition transition, float y)
        {
            var debugInfo = new List<DebugInformation>();
            debugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(transition.x - 2, y), new PointF(transition.x + 2, y), GeometryFriends.XNAStub.Color.Blue));
            return debugInfo;
        }

        public event EventHandler<EventArgs> Step;

        void OnStep()
        {
            Step?.Invoke(this, EventArgs.Empty);
        }

        List<int> trajectoryCollectibles = new List<int>();

        List<int> tempCollectibles = new List<int>();
        void GetCollectibles(List<Point> trajectory, List<int> collectibles)
        {
            collectibles.Clear();
            foreach (var point in trajectory)
            {
                stateMapper.GetCollectibles(new PointF(point.X, point.Y), tempCollectibles);
                foreach (int collectible in tempCollectibles)
                    if (!collectibles.Contains(collectible))
                        collectibles.Add(collectible);
            }
        }
    }
}
