using System;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

using GeometryFriends.AI;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Communication;
using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    /// <summary>
    /// A circle agent implementation for the GeometryFriends game that demonstrates prediction and history keeping capabilities.
    /// </summary>
    public class CircleAgent : AbstractCircleAgent
    {
        //Sensors Information
        private LevelRepresentation levelInfo;
        private CircleRepresentation circleInfo;
        private RectangleRepresentation rectangleInfo;

        // Graph
        private GraphCircle graph;

        // Search Algorithm
        private SubgoalAStar subgoalAStar;

        // Reinforcement Learning
        private ActionSelector actionSelector;

        // Messages
        private GameInfo.CooperationStatus cooperation;
        private List<AgentMessage> messages;

        // Auxiliary Variables
        private Moves currentAction;
        private Graph.Move? nextMove;
        private DateTime lastMoveTime;
        private int targetPointX_InAir;
        private bool[] previousCollectibles;
        private Graph.Platform? previousPlatform, currentPlatform;

        public CircleAgent()
        {
            nextMove = null;
            currentPlatform = null;
            previousPlatform = null;
            lastMoveTime = DateTime.Now;
            currentAction = Moves.NO_ACTION;
            cooperation = GameInfo.CooperationStatus.SINGLE_MODE;

            graph = new GraphCircle();
            subgoalAStar = new SubgoalAStar();
            actionSelector = new ActionSelector();
            levelInfo = new LevelRepresentation();

            messages = new List<AgentMessage>();
        }

        //implements abstract circle interface: used to setup the initial information so that the agent has basic knowledge about the level
        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
        
            messages.Add(new AgentMessage(GameInfo.IST_CIRCLE_PLAYING));

            // Create Level Array
            levelInfo.CreateLevelArray(colI, oI, rPI, cPI);

            // Create Graph
            graph.initialRectangleInfo = rI;
            graph.SetupGraph(levelInfo.GetLevelArray(), colI.Length);
            graph.SetPossibleCollectibles(cI);

            if (rI.X < 0 || rI.Y < 0)
            {
                graph.DeleteCooperationPlatforms();
                cooperation = GameInfo.CooperationStatus.NOT_COOPERATING;
            }

            // Initial Information
            circleInfo = cI;
            rectangleInfo = rI;
            targetPointX_InAir = (int)circleInfo.X;
            previousCollectibles = levelInfo.GetObtainedCollectibles();
        }

        //implements abstract circle interface: registers updates from the agent's sensors that it is up to date with the latest environment information
        /*WARNING: this method is called independently from the agent update - Update(TimeSpan elapsedGameTime) - so care should be taken when using complex 
         * structures that are modified in both (e.g. see operation on the "remaining" collection)      
         */
        public override void SensorsUpdated(int nC, RectangleRepresentation rI, CircleRepresentation cI, CollectibleRepresentation[] colI)
        {
            circleInfo = cI;
            rectangleInfo = rI;
            levelInfo.collectibles = colI;
        }

        //implements abstract circle interface: signals if the agent is actually implemented or not
        public override bool ImplementedAgent()
        {
            return true;
        }

        //implements abstract circle interface: provides the name of the agent to the agents manager in GeometryFriends
        public override string AgentName()
        {
            return "IST Circle";
        }

        //implements abstract circle interface: GeometryFriends agents manager gets the current action intended to be actuated in the enviroment for this agent
        public override Moves GetAction()
        {
            return currentAction;
        }

        //implements abstract circle interface: updates the agent state logic and predictions
        public override void Update(TimeSpan elapsedGameTime)
        {

            //if (cooperation == GameInfo.CooperationStatus.SINGLE_MODE)
            //{
            //    graph.DeleteCooperationPlatforms();
            //    cooperation = GameInfo.CooperationStatus.NOT_COOPERATING;
            //}

            if ((DateTime.Now - lastMoveTime).TotalMilliseconds >= 20)
            {

                currentPlatform = graph.GetPlatform(new LevelRepresentation.Point((int)circleInfo.X, (int)circleInfo.Y), GameInfo.MAX_CIRCLE_HEIGHT, (int)circleInfo.VelocityY);

                if (!currentPlatform.HasValue)
                {

                    if ((cooperation == GameInfo.CooperationStatus.AWAITING_MORPH || cooperation == GameInfo.CooperationStatus.MORPH_READY) && IsRidingRectangle())
                    {
                        currentPlatform = previousPlatform;  
                    }
                }

                if (currentPlatform.HasValue &&
                    cooperation == GameInfo.CooperationStatus.AWAITING_MORPH &&
                    !IsRidingRectangle())
                {
                    cooperation = GameInfo.CooperationStatus.NOT_COOPERATING;
                }


                if (currentPlatform.HasValue)
                {
                    if (IsDifferentPlatform() || IsGetCollectible())
                    {
                        targetPointX_InAir = (currentPlatform.Value.leftEdge + currentPlatform.Value.rightEdge) / 2;
                        Task.Factory.StartNew(SetNextEdge);
                    }

                    if (nextMove.HasValue)
                    {

                        if (nextMove.Value.reachablePlatform.type == Graph.platformType.COOPERATION &&
                            cooperation == GameInfo.CooperationStatus.NOT_COOPERATING)
                        {
                            currentAction = Cooperate((Graph.Move)nextMove);
                        }

                        else if (currentPlatform.Value.type != Graph.platformType.COOPERATION &&
                                nextMove.Value.reachablePlatform.type != Graph.platformType.COOPERATION &&
                                cooperation == GameInfo.CooperationStatus.MORPH_READY)
                        {
                            cooperation = GameInfo.CooperationStatus.NOT_COOPERATING;
                            messages.Add(new AgentMessage(GameInfo.COOPERATION_FINISHED));
                        }

                        else if (cooperation == GameInfo.CooperationStatus.RIDING)
                        {
                            currentAction = actionSelector.GetCurrentAction(circleInfo, (int)rectangleInfo.X, 0, true);
                        }

                        else if ((cooperation == GameInfo.CooperationStatus.RIDE_READY || cooperation == GameInfo.CooperationStatus.RIDE_READY) &&
                            currentPlatform.Value.type == Graph.platformType.COOPERATION)
                        {
                            currentAction = actionSelector.GetCurrentAction(circleInfo, (int)rectangleInfo.X, 0, true);
                        }

                        else if (cooperation == GameInfo.CooperationStatus.AWAITING_RIDE)
                        {
                            currentAction = Moves.NO_ACTION;
                        }

                        else if (cooperation == GameInfo.CooperationStatus.AWAITING_MORPH)
                        {
                            currentAction = actionSelector.GetCurrentAction(circleInfo, (int)rectangleInfo.X, 0, true);
                        }

                        else if (-GameInfo.MAX_VELOCITYY <= circleInfo.VelocityY && circleInfo.VelocityY <= GameInfo.MAX_VELOCITYY)
                        {
                            if (nextMove.Value.type == Graph.movementType.STAIR_GAP)
                            {
                                currentAction = nextMove.Value.rightMove ? Moves.ROLL_RIGHT : Moves.ROLL_LEFT;
                            }
                            else
                            {
                                currentAction = actionSelector.GetCurrentAction(circleInfo, nextMove.Value.movePoint.x, nextMove.Value.velocityX, nextMove.Value.rightMove);
                            }
                        }
                        else
                        {
                            currentAction = actionSelector.GetCurrentAction(circleInfo, targetPointX_InAir, 0, true);
                        }

                    }

                    else if (cooperation != GameInfo.CooperationStatus.NOT_COOPERATING && currentPlatform.Value.type != Graph.platformType.COOPERATION)
                    {
                        cooperation = GameInfo.CooperationStatus.NOT_COOPERATING;
                        messages.Add(new AgentMessage(GameInfo.COOPERATION_FINISHED));
                    }
                }

                else
                {
                    if (nextMove.HasValue)
                    {
                        if (nextMove.Value.reachablePlatform.type == Graph.platformType.COOPERATION &&
                            cooperation == GameInfo.CooperationStatus.NOT_COOPERATING)
                        {
                            currentAction = Cooperate((Graph.Move)nextMove);
                        }
                        else if (cooperation == GameInfo.CooperationStatus.RIDE_READY || cooperation == GameInfo.CooperationStatus.RIDE_HELP)
                        {
                            currentAction = actionSelector.GetCurrentAction(circleInfo, (int)rectangleInfo.X, 0, true);
                        }
                        else if (nextMove.Value.type == Graph.movementType.JUMP)
                        {
                            currentAction = Moves.NO_ACTION;
                        }
                        else if (nextMove.Value.type == Graph.movementType.STAIR_GAP)
                        {
                            currentAction = nextMove.Value.rightMove ? Moves.ROLL_RIGHT : Moves.ROLL_LEFT;
                        }
                        else if (cooperation == GameInfo.CooperationStatus.AWAITING_MORPH)
                        {
                            currentAction = actionSelector.GetCurrentAction(circleInfo, (int)rectangleInfo.X, 0, true);
                        }
                        else
                        {
                            if (nextMove.Value.collideCeiling && circleInfo.VelocityY < 0)
                            {
                                currentAction = Moves.NO_ACTION;
                            }
                            else
                            {
                                currentAction = actionSelector.GetCurrentAction(circleInfo, targetPointX_InAir, 0, true);
                            }
                        }
                    }
                }

                if (!nextMove.HasValue)
                {
                    currentAction = actionSelector.GetCurrentAction(circleInfo, (int)circleInfo.X, 0, false);
                }

                lastMoveTime = DateTime.Now;
            }

            if (nextMove.HasValue)
            {

                if ((cooperation == GameInfo.CooperationStatus.RIDE_READY && actionSelector.IsGoal(circleInfo, (int) rectangleInfo.X, 0, true)) ||
                    (cooperation == GameInfo.CooperationStatus.RIDE_HELP && currentPlatform.HasValue && currentPlatform.Value.type == Graph.platformType.COOPERATION && IsRidingRectangle()))
                {
                    cooperation = GameInfo.CooperationStatus.RIDING;
                    messages.Add(new AgentMessage(GameInfo.RIDING, nextMove));
                }

                if (!actionSelector.IsGoal(circleInfo, nextMove.Value.movePoint.x, nextMove.Value.velocityX, nextMove.Value.rightMove))
                {
                    return;
                }

                if (-GameInfo.MAX_VELOCITYY <= circleInfo.VelocityY && circleInfo.VelocityY <= GameInfo.MAX_VELOCITYY)
                {
                    targetPointX_InAir = (nextMove.Value.reachablePlatform.leftEdge + nextMove.Value.reachablePlatform.rightEdge) / 2;

                    if (nextMove.Value.type == Graph.movementType.JUMP && cooperation == GameInfo.CooperationStatus.IN_POSITION)
                    {
                        currentAction = Cooperate((Graph.Move)nextMove);
                    }

                    if (nextMove.Value.type == Graph.movementType.JUMP && cooperation != GameInfo.CooperationStatus.AWAITING_MORPH)
                    {
                        currentAction = Moves.JUMP;
                        if (cooperation == GameInfo.CooperationStatus.MORPH_READY)
                        {
                            messages.Add(new AgentMessage(GameInfo.JUMPED));
                        }

                        if (cooperation == GameInfo.CooperationStatus.RIDE_READY && nextMove.Value.collideCeiling)
                        {
                            cooperation = GameInfo.CooperationStatus.RIDE_HELP;
                            messages.Add(new AgentMessage(GameInfo.RIDE_HELP));
                        }

                    }

                }
            }

        }

        private bool IsGetCollectible()
        {

            bool[] currentCollectibles = levelInfo.GetObtainedCollectibles();

            if (previousCollectibles.SequenceEqual(currentCollectibles))
            {
                return false;
            }


            for (int previousC = 0; previousC < previousCollectibles.Length; previousC++)
            {

                if (!previousCollectibles[previousC])
                {

                    for (int currentC = 0; currentC < currentCollectibles.Length; currentC++)
                    {

                        if (currentCollectibles[currentC])
                        {

                            foreach (Graph.Platform p in graph.platforms)
                            {

                                foreach (Graph.Move m in p.moves)
                                {

                                    m.collectibles_onPath[currentC] = false;

                                }

                            }

                        }
                    }
                }

            }

            previousCollectibles = currentCollectibles;

            return true;
        }

        private bool IsDifferentPlatform()
        {

            if (currentPlatform.HasValue)
            {
                if (!previousPlatform.HasValue)
                {
                    previousPlatform = currentPlatform;
                    return true;
                }
                else if (currentPlatform.Value.id != previousPlatform.Value.id)
                {
                    previousPlatform = currentPlatform;
                    return true;
                }
            }

            previousPlatform = currentPlatform;
            return false;
        }

        private void SetNextEdge()
        {

            Graph.Move? previousMove = nextMove;

            nextMove = null;
            nextMove = subgoalAStar.CalculateShortestPath(currentPlatform.Value, new LevelRepresentation.Point((int)circleInfo.X, (int)circleInfo.Y),
                Enumerable.Repeat<bool>(true, levelInfo.initialCollectibles.Length).ToArray(),
                levelInfo.GetObtainedCollectibles(), levelInfo.initialCollectibles);

            if (previousMove.HasValue && nextMove.HasValue &&
                (previousMove.Value.type != nextMove.Value.type ||
                previousMove.Value.movePoint.x != nextMove.Value.movePoint.x ||
                previousMove.Value.landPoint.x != nextMove.Value.landPoint.x) &&
                cooperation == GameInfo.CooperationStatus.AWAITING_RIDE)
            {
                cooperation = GameInfo.CooperationStatus.NOT_COOPERATING;
                messages.Add(new AgentMessage(GameInfo.CLEAN_RIDES));
            }


            if (nextMove.HasValue &&
                currentPlatform.HasValue &&
                currentPlatform.Value.type != Graph.platformType.COOPERATION &&
                nextMove.Value.reachablePlatform.type != Graph.platformType.COOPERATION)
            {
                cooperation = GameInfo.CooperationStatus.NOT_COOPERATING;
                messages.Add(new AgentMessage(GameInfo.COOPERATION_FINISHED));
            }

            if (nextMove.HasValue &&
                nextMove.Value.reachablePlatform.type != Graph.platformType.COOPERATION &&
                            cooperation == GameInfo.CooperationStatus.NOT_COOPERATING)
            {
                messages.Add(new AgentMessage(GameInfo.INDIVIDUAL_MOVE, nextMove));
            }

            if (nextMove.HasValue &&
                cooperation == GameInfo.CooperationStatus.RIDING)
            {
                messages.Add(new AgentMessage(GameInfo.RIDING, nextMove));
            }

        }

        private Moves Cooperate(Graph.Move move)
        {
            Graph.Move manipulatedMove = Graph.CopyMove(move);
            manipulatedMove.collectibles_onPath = graph.GetCollectiblesFromCooperation(move);

            Graph.Platform fromPlatform;

            if (!currentPlatform.HasValue)
            {
                fromPlatform = (Graph.Platform) previousPlatform;

            }
            else
            {
                fromPlatform = (Graph.Platform) currentPlatform;
            }

            if (fromPlatform.type == Graph.platformType.COOPERATION)
            {
                manipulatedMove.height = 50 + (fromPlatform.height - (move.movePoint.y + GameInfo.CIRCLE_RADIUS));
                manipulatedMove.type = Graph.movementType.MORPH_UP;
                messages.Add(new AgentMessage(GameInfo.AWAITING_MORPH, manipulatedMove));
                cooperation = GameInfo.CooperationStatus.AWAITING_MORPH;
            }
            else
            {

                bool rectangleIsRight = (rectangleInfo.X - circleInfo.X > 0);

                if (rectangleIsRight != move.rightMove)
                {
                    foreach (Graph.Move m in fromPlatform.moves)
                    {
                        if (m.reachablePlatform.id == move.reachablePlatform.id &&
                            m.type == Graph.movementType.JUMP &&
                            m.rightMove != move.rightMove)
                        {
                            nextMove = m;
                            manipulatedMove = Graph.CopyMove(m);
                            manipulatedMove.collectibles_onPath = graph.GetCollectiblesFromCooperation(m);
                            break;
                        }
                    }
                }

                manipulatedMove.height = 50 + (move.reachablePlatform.height - (move.landPoint.y + GameInfo.CIRCLE_RADIUS));
                manipulatedMove.type = Graph.movementType.RIDE;
                messages.Add(new AgentMessage(GameInfo.AWAITING_RIDE, manipulatedMove));
                cooperation = GameInfo.CooperationStatus.AWAITING_RIDE;
            }

            return Moves.NO_ACTION;

        }

        //implements abstract circle interface: signals the agent the end of the current level
        public override void EndGame(int collectiblesCaught, int timeElapsed)
        {
            Console.WriteLine("CIRCLE - Collectibles caught = {0}, Time elapsed - {1}", collectiblesCaught, timeElapsed);
        }

        public override List<GeometryFriends.AI.Communication.AgentMessage> GetAgentMessages()
        {
            List<AgentMessage> toSent = new List<AgentMessage>(messages);
            messages.Clear();
            return toSent;
        }

        //implememts abstract agent interface: receives messages from the circle agent
        public override void HandleAgentMessages(List<GeometryFriends.AI.Communication.AgentMessage> newMessages)
        {
            foreach (AgentMessage item in newMessages)
            {

                if (item.Message.Equals(GameInfo.IST_RECTANGLE_PLAYING))
                {
                    cooperation = GameInfo.CooperationStatus.NOT_COOPERATING;
                }

                if (item.Message.Equals(GameInfo.RIDE_READY))
                {
                    cooperation = GameInfo.CooperationStatus.RIDE_READY;
                }

                if (item.Message.Equals(GameInfo.MORPH_READY))
                {
                    cooperation = GameInfo.CooperationStatus.MORPH_READY;
                }

                if (item.Message.Equals(GameInfo.IN_POSITION))
                {
                    cooperation = GameInfo.CooperationStatus.IN_POSITION;
                }

            }

            return;
        }

        public static bool IsObstacle_onPixels(int[,] levelArray, List<LevelRepresentation.ArrayPoint> checkPixels)
        {
            if (checkPixels.Count == 0)
            {
                return true;
            }

            foreach (LevelRepresentation.ArrayPoint i in checkPixels)
            {
                if (levelArray[i.yArray, i.xArray] == LevelRepresentation.BLACK || levelArray[i.yArray, i.xArray] == LevelRepresentation.GREEN)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsRidingRectangle()
        {
            int rectangleWidth = GameInfo.RECTANGLE_AREA / (int)rectangleInfo.Height;

            if ((circleInfo.X >= rectangleInfo.X - (rectangleWidth / 2)) &&
                (circleInfo.X <= rectangleInfo.X + (rectangleWidth / 2)) &&
                 Math.Abs(rectangleInfo.Y - (rectangleInfo.Height / 2) - GameInfo.CIRCLE_RADIUS - circleInfo.Y) <= 8)
            {
                return true;
            }

            return false;
        }

    }
}

