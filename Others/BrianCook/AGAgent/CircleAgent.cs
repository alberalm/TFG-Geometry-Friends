using GeometryFriends.AI;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Communication;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

//
// Geometry Friends competition entry for IEEE Conference on Games, London 2019.
//
// Brian Cook
// LearnLAB 
// University of Texas at Arlington, Dept. of Computer Science
// bcook99 @gmail.com
//

namespace GeometryFriendsAgents
{
    /// <summary>
    /// Abstraction-guided agent
    /// </summary>
    public class CircleAgent : AbstractCircleAgent
    {
        string agentName = "AGAgent";

        protected int currentThreadId { get { return Helpers.GetCurrentThreadId(); } }
        protected string ThreadId { get { return "[" + currentThreadId + "]"; } }
        protected Moves currentAction
        {
            get { return _currentAction; }
            set
            {
                if (_currentAction != value)
                {
                    _currentAction = value;
                }
            }
        }
        Moves _currentAction = Moves.NO_ACTION;
        DebugInformation[] debugInfo = null;

        DateTime gameStartTime;

        double totalElapsedGameTime = 0;
        double totalElapsedClockTime { get { return (HighResolutionDateTime.Now - gameStartTime).TotalSeconds; } }

        ActionSimulator predictor = null;

        List<AgentMessage> noMessages = new List<AgentMessage>();   

        protected ObstacleGrid obstacleGrid;
        protected CircleStateMapper stateMapper;
        protected CircleState senseState;

        GameState _gameState;
        GameState gameState
        {
            get
            {
                GameState state;
                lock (this)
                    state = _gameState;
                return state;
            }
        }
        Policy<GameState> policy;
        GameArea gameArea;

        public const float stoppedXvel = 10f;

        SegmentModel segmentModel;
        SegmentPlanningProblem segmentProblem;
        List<PolicyOperator> segmentPlan;
        SegmentAnalyzer analyzer;
        bool needToJump;
        bool needToStop;
        StopPolicy stopPolicy = new StopPolicy();
        const float timeStep = 0.01f;

        public CircleAgent()
        {
            currentAction = Moves.NO_ACTION;

            LogInfo("");
            LogInfo("__________________________________________________________________________");
            LogInfo($"Starting {agentName}...");
        }

        public override void Setup(CountInformation nI, RectangleRepresentation rectangle, CircleRepresentation circle,
            ObstacleRepresentation[] obstacles, ObstacleRepresentation[] rectanglePlatforms, ObstacleRepresentation[] circlePlatforms,
            CollectibleRepresentation[] collectibles, Rectangle area, double timeLimit)
        {
            try
            {
                LogInfo("**** BEGIN SETUP ****");

                if (gameArea == null)
                {
                    gameArea = new GameArea();
                    gameArea.Area = area;
                    gameArea.CircleRadius = circle.Radius;
                    gameArea.Collectibles = collectibles.ToList();
                    gameArea.CollectionName = "Unknown";
                    gameArea.GoalRadius = 20;
                    gameArea.LevelNumber = -1;
                    gameArea.Obstacles = obstacles.Concat(circlePlatforms).ToList();
                    gameArea.StartingPosition = new PointF(circle.X, circle.Y);
                    gameArea.WorldNumber = -1;
                }

                gameStartTime = HighResolutionDateTime.Now;
                totalElapsedGameTime = 0;

                var startTime = HighResolutionDateTime.Now;
                int border = area.X;
                obstacleGrid = new ObstacleGrid(area.Width + 2 * border, area.Height + 2 * border, border, obstacles.Concat(rectanglePlatforms).ToArray());
                stateMapper = CircleStateMapper.Create(gameArea, obstacleGrid);
                LogInfo("Creating segment model...");
                segmentModel = SegmentModel.Build(gameArea, obstacleGrid);

                LogInfo("Creating analyzer...");
                analyzer = new SegmentAnalyzer(segmentModel, stateMapper, 10f, 10f);
                LogInfo("Creating planning problem...");
                segmentProblem = new SegmentPlanningProblem(gameArea, stateMapper, segmentModel);

                LogInfo("Creating control policy...");
                policy = CreateAgentPolicy();

                UpdateGameState(circle, collectibles);
                policy.Initialize(gameState, timeStep);

                LogInfo("**** END SETUP ****");
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in Setup: " + ex.Message);
                Logger.Write(ex.StackTrace);
                throw new ApplicationException("Exception in Setup: " + ex.Message, ex);
            }
        }

        void UpdateGameState(CircleRepresentation circle, CollectibleRepresentation[] collectibles)
        {
            var remaining = new List<int>();
            foreach (var collectible in collectibles)
            {
                int index = gameArea.Collectibles.IndexOf(collectible);
                if (index < 0)
                    LogError("Bad collectible! " + collectible);
                else
                    remaining.Add(index);
            }

            var elapsedClockTime = HighResolutionDateTime.Now - gameStartTime;
            var newGameState = new GameState()
            {
                Circle = circle,
                GameArea = gameArea,
                ObstacleGrid = obstacleGrid,
                StateMapper = stateMapper,
                RemainingCollectibles = remaining.ToArray(),
                ElapsedGameTime = totalElapsedGameTime,
                ElapsedClockTime = elapsedClockTime.TotalSeconds,
            };

            var _senseState = stateMapper.CreateState(circle);

            lock (this)
            {
                _gameState = newGameState;
                senseState = _senseState;
            }
        }

        public override void SensorsUpdated(int numCollectibles, RectangleRepresentation rectangle, CircleRepresentation circle,
            CollectibleRepresentation[] collectibles)
        {
            UpdateGameState(circle, collectibles);
        }

        bool inUpdate = false;

        public override void Update(TimeSpan elapsedGameTime)
        {
            if (inUpdate)
            {
                LogError("Re-entrant call to Update");
                return;
            }

            try
            {
                inUpdate = true;
                totalElapsedGameTime += elapsedGameTime.TotalSeconds;
                if (needToJump)
                {
                    currentAction = Moves.JUMP;
                    return;
                }

                if (segmentPlan != null)
                {
                    needToStop = false;
                    var state = senseState;
                    if (ExecuteAbstractPlan(gameState, state, gameState.RemainingCollectibles, ref segmentPlan))
                        return;
                }
                ActionSimulator actionSimulator;
                lock (this)
                    actionSimulator = predictor;
                if (actionSimulator == null || !actionSimulator.CharactersReady())
                    return;

                var simState = stateMapper.CreateState(actionSimulator.CirclePositionX, actionSimulator.CirclePositionY, actionSimulator.CircleVelocityX, actionSimulator.CircleVelocityY);
                if (simState.isGrounded && Math.Abs(simState.xVelocity) < stoppedXvel)
                {
                    var simSegment = segmentModel.GetSegmentBeneath(simState.x, simState.y);
                    if (simSegment != null)
                    {
                        var simInfo = segmentModel.modelInfo[simSegment];
                        if (simInfo.StoppedState == null)
                        {
                            simInfo.StoppedState = actionSimulator;
                            simInfo.StoppedGameState = new GameState()
                            {
                                Circle = new CircleRepresentation(simState.x, simState.y, simState.xVelocity, simState.yVelocity, gameArea.CircleRadius),
                                GameArea = gameArea,
                                ObstacleGrid = obstacleGrid,
                                StateMapper = stateMapper
                            };
                        }
                    }
                }
                if (segmentPlan == null)
                {
                    needToStop = true;
                    var state = senseState;
                    if (!state.isGrounded || Math.Abs(state.xVelocity) >= stoppedXvel)
                        return;
                    var segment = segmentModel.GetSegmentBeneath(state.x, state.y);
                    if (segment == null)
                    {
                        LogInfo($"No current segment");
                        return;
                    }
                    var segmentInfo = segmentModel.modelInfo[segment];
                    if (segmentInfo.StoppedState == null)
                    {
                        LogInfo($"No simulator for segment {segment}");
                        return;
                    }
                    if (analyzer != null)
                    {
                        LogInfo("Searching for segment connectivity...");
                        var start = DateTime.Now;
                        analyzer.Explore(segment, gameState.RemainingCollectibles);
                        debugInfo = segmentModel.GetDebugInfo();
                        LogInfo($"Done searching for segment connectivity, took {(int)((DateTime.Now - start).TotalMilliseconds)}ms");
                    }

                    LogInfo("Starting planning...");
                    var startTime = HighResolutionDateTime.Now;
                    var segmentState = new SegmentState(segment, state.x, state.xVelocity, gameState.RemainingCollectibles);
                    segmentProblem.SetInitialState(segmentState);

                    var planningTask = new SegmentPlanningTask(gameArea, segmentProblem);
                    var timeLimitSeconds = 8;
                    var result = planningTask.Run(timeLimitSeconds);
                    segmentPlan = planningTask.Plan != null ? planningTask.Plan.Cast<PolicyOperator>().ToList() : null;

                    if (segmentPlan == null)
                        LogInfo($"Could not find plan, took {(DateTime.Now - startTime).TotalMilliseconds}ms");
                    else if (result == SearchStatus.Solved || result == SearchStatus.Timeout || totalElapsedClockTime > timeLimitSeconds)
                    {
                        if (result == SearchStatus.Solved)
                            LogInfo($"Found complete plan, took {(DateTime.Now - startTime).TotalMilliseconds}ms");
                        else
                            LogInfo($"Found partial plan, stopping further search effort (elapsed = {totalElapsedClockTime})");

                        int totalCost = 0;
                        foreach (var step in segmentPlan)
                        {
                            LogInfo($"   {step} (cost {step.Cost} = {step.Cost * Simulation.TimeStep} seconds) (nearest corner={step.Policy.NearestCorner})");
                            totalCost += step.Cost;
                        }
                        LogInfo($"Total cost: {totalCost} = {totalCost * Simulation.TimeStep} seconds");
                        debugInfo = GetDebugInfo(segmentPlan);
                        InitializeAbstractPlan(gameState, segmentPlan);
                    }
                    else
                    {
                        LogInfo($"Found partial plan, still trying... (elapsed = {totalElapsedClockTime})");
                        segmentPlan = null;
                    }
                }

                currentAction = policy.GetAction(gameState);
            }
            catch (Exception ex)
            {
                LogError($"Exception in Update: {ex.Message}  {ex.StackTrace}");
            }
            finally
            {
                inUpdate = false;
            }
        }

        public override Moves GetAction()
        {
            var action = currentAction;
            if (needToJump)
            {
                needToJump = false;
                currentAction = Moves.NO_ACTION;
                action = Moves.JUMP;
            }
            if (needToStop)
                return stopPolicy.GetAction(gameState);

            return action;
        }

        public override void ActionSimulatorUpdated(ActionSimulator updatedSimulator)
        {
            if (!updatedSimulator.CharactersReady())
                return;

            lock (this)
                predictor = updatedSimulator;
        }

        public override bool ImplementedAgent()
        {
            return true;
        }

        public override string AgentName()
        {
            return agentName;
        }

        public override DebugInformation[] GetDebugInformation()
        {
            return debugInfo;
        }

        public override List<AgentMessage> GetAgentMessages()
        {
            return noMessages;
        }

        public override void HandleAgentMessages(List<AgentMessage> newMessages)
        {
        }

        public override void EndGame(int collectiblesCaught, int timeElapsed)
        {
            LogInfo("END GAME: Circle collectibles caught = " + collectiblesCaught + ", time elapsed = " + timeElapsed);
        }

        Policy<GameState> CreateAgentPolicy()
        {
            return new DoNothingPolicy();
        }

        bool InitializeAbstractPlan(GameState gameState, List<PolicyOperator> plan)
        {
            if (plan != null && plan.Count > 0)
            {
                for (int i = plan.Count - 2; i >= 0; i--)
                {
                    var startX = plan[i].FromState.x;
                    var current = plan[i].Policy as RollToPolicy;
                    var next = plan[i + 1].Policy;
                    if (current != null)
                    {
                        var nextRoll = (next is CompositePolicy) ? (next as CompositePolicy).GetChildPolicy<RollToPolicy>() : next as RollToPolicy;
                        if (nextRoll != null)
                        {
                            if ((current.targetX > startX && nextRoll.targetX > current.targetX)
                                || (current.targetX < startX && nextRoll.targetX < current.targetX))
                            {
                                plan.RemoveAt(i);
                            }
                        }
                    }
                }

                var op = plan[0];
                op.Policy.Initialize(gameState, timeStep);
                return true;
            }
            return false;
        }

        bool ExecuteAbstractPlan(GameState gameState, CircleState state, int[] remainingCollectibles, ref List<PolicyOperator> plan)
        {
            if (plan == null)
                return false;

            while (plan.Count > 0)
            {
                var op = plan[0];
                Moves action;
                OperatorResult result;
                SegmentState segmentState = null;

                var currentSegment = segmentModel.GetSegment(state.x, state.y);
                var finishedOpGoal = currentSegment == op.ToState.segment && IsSubset(remainingCollectibles, op.ToState.remainingCollectibles);

                if (op.Policy.IsFinished(gameState) || finishedOpGoal)
                {
                    if (!op.Policy.IsFinished(gameState))
                        LogInfo($"Terminating policy early, reached target segment");

                    segmentState = segmentModel.GetSegmentState(state.x, state.y, state.xVelocity, remainingCollectibles);
                    if (op.ToState.segment == segmentState.segment && IsSubset(segmentState.remainingCollectibles, op.ToState.remainingCollectibles))
                        result = OperatorResult.Complete;
                    else
                    {
                        LogInfo($"OPERATOR FAILED: segmentState {segmentState} op {op}  (state {state})");
                        LogInfo($"   => segment {segmentState.segment} should be {op.ToState.segment} remaining {string.Join(",", segmentState.remainingCollectibles)} should be {string.Join(",", op.ToState.remainingCollectibles)}");
                        LogInfo($"   => remaining {string.Join(",", segmentState.remainingCollectibles)} should be {string.Join(",", op.ToState.remainingCollectibles)}");
                        result = OperatorResult.Failed;
                        op.Policy.OnFailed();
                    }
                    action = Moves.NO_ACTION;
                }
                else
                {
                    result = OperatorResult.InProgress;
                    action = op.Policy.GetAction(gameState);
                }

                if (result != OperatorResult.InProgress)
                {
                    if (segmentState != null)
                        LogInfo($"Update: state {segmentState} op {op} => action {action} result {result}");
                    else
                        LogInfo($"Update: state {state} (NULL SEGMENT) op {op} => action {action} result {result}");
                }

                if (result == OperatorResult.Failed)
                {
                    LogError("Current plan failed! Replanning...");
                    plan = null;
                    currentAction = Moves.NO_ACTION;
                    return false;
                }

                if (result == OperatorResult.InProgress)
                {
                    currentAction = action;
                    if (action == Moves.JUMP)
                        needToJump = true;
                    return true;
                }

                if (result == OperatorResult.Complete)
                {
                    LogInfo($"Operator completed: {op}, {plan.Count - 1} remaining");
                    plan.RemoveAt(0);
                    if (plan.Count > 0)
                        plan[0].Policy.Initialize(gameState, timeStep);

                    debugInfo = GetDebugInfo(plan);
                }
            }

            LogInfo("Current plan completed!");
            plan = null;
            return false;
        }

        public DebugInformation[] GetDebugInfo(List<PolicyOperator> plan)
        {
            var debugInfo = new List<DebugInformation>();
            debugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());

            foreach (var op in plan)
            {
                var trajectory = op.Trajectory;
                if (trajectory == null)
                    continue;
                var lastPoint = new Point();
                for (int i = 0; i < trajectory.Length; i++)
                    if (i == 0)
                        lastPoint = trajectory[i];
                    else
                    {
                        var point = trajectory[i];
                        if (Math.Abs(point.X - lastPoint.X) + Math.Abs(point.Y - lastPoint.Y) > 5)
                        {
                            debugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(lastPoint, point, GeometryFriends.XNAStub.Color.Blue));
                            lastPoint = point;
                        }
                    }
            }

            return debugInfo.ToArray();
        }

        bool IsSubset(int[] a, int[] b)
        {
            foreach (int n in a)
                if (!b.Contains(n))
                    return false;
            return true;
        }

        void LogInfo(string message)
        {
            Logger.Write(message);
        }

        void LogError(string message)
        {
            message = "ERROR: " + message;
            Logger.Write(message);
        }
    }
}

