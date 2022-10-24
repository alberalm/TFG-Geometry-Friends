using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GeometryFriends.AI.Interfaces;
using System.Drawing;
using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    /// <summary>
    /// A circle agent implementation for the GeometryFriends game that demonstrates prediction and history keeping capabilities.
    /// </summary>
    public class CircleAgent : AbstractCircleAgent
    {
        private LevelArray levelArray;
        private PlatformCircle platform;
        private SubgoalAStar subgoalAStar;
        private ActionSelector actionSelector;

        private Platform.PlatformInfo? previousPlatform;
        private Platform.PlatformInfo? currentPlatform;
        private bool getCollectibleFlag;
        private bool differentPlatformFlag;

        private int previousCollectibleNum;
        private int currentCollectibleNum;

        private Platform.MoveInfo? nextEdge;
        private int targetPointX_InAir;

        //agent implementation specificiation
        private bool implementedAgent;
        private string agentName = "IST Circle";

        //auxiliary variables for agent action
        private Moves currentAction;
        private List<Moves> possibleMoves;
        private DateTime lastMoveTime;
        private Random rnd;

        //Sensors Information and level state
        private CountInformation numbersInfo;
        private RectangleRepresentation rectangleInfo;
        private CircleRepresentation circleInfo;
        private ObstacleRepresentation[] obstaclesInfo;
        private ObstacleRepresentation[] rectanglePlatformsInfo;
        private ObstacleRepresentation[] circlePlatformsInfo;
        private CollectibleRepresentation[] collectiblesInfo;

        //Area of the game screen
        private Rectangle area;

        public CircleAgent()
        {
            //Change flag if agent is not to be used
            implementedAgent = true;

            //setup for action updates
            lastMoveTime = DateTime.Now;
            currentAction = Moves.NO_ACTION;
            rnd = new Random();

            levelArray = new LevelArray();
            platform = new PlatformCircle();
            subgoalAStar = new SubgoalAStar();
            actionSelector = new ActionSelector();

            previousPlatform = null;
            currentPlatform = null;
            getCollectibleFlag = false;
            differentPlatformFlag = false;

            //prepare the possible moves  
            possibleMoves = new List<Moves>();
            possibleMoves.Add(Moves.ROLL_LEFT);
            possibleMoves.Add(Moves.ROLL_RIGHT);
            possibleMoves.Add(Moves.JUMP);
        }

        //implements abstract circle interface: used to setup the initial information so that the agent has basic knowledge about the level
        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {

            numbersInfo = nI;
            currentCollectibleNum = nI.CollectiblesCount;
            rectangleInfo = rI;
            circleInfo = cI;
            obstaclesInfo = oI;
            rectanglePlatformsInfo = rPI;
            circlePlatformsInfo = cPI;
            collectiblesInfo = colI;
            this.area = area;

            nextEdge = null;
            targetPointX_InAir = (int)circleInfo.X;

            levelArray.CreateLevelArray(collectiblesInfo, obstaclesInfo, rectanglePlatformsInfo);
            platform.SetUp(levelArray.GetLevelArray(), levelArray.initialCollectiblesInfo.Length);

            //DebugSensorsInfo();
        }

        //implements abstract circle interface: registers updates from the agent's sensors that it is up to date with the latest environment information
        /*WARNING: this method is called independently from the agent update - Update(TimeSpan elapsedGameTime) - so care should be taken when using complex 
         * structures that are modified in both (e.g. see operation on the "remaining" collection)      
         */
        public override void SensorsUpdated(int nC, RectangleRepresentation rI, CircleRepresentation cI, CollectibleRepresentation[] colI)
        {
            currentCollectibleNum = nC;

            rectangleInfo = rI;
            circleInfo = cI;
            collectiblesInfo = colI;
        }

        //implements abstract circle interface: signals if the agent is actually implemented or not
        public override bool ImplementedAgent()
        {
            return implementedAgent;
        }

        //implements abstract circle interface: provides the name of the agent to the agents manager in GeometryFriends
        public override string AgentName()
        {
            return agentName;
        }

        //simple algorithm for choosing a random action for the circle agent
        private void RandomAction()
        {
            /*
             Circle Actions
             ROLL_LEFT = 1      
             ROLL_RIGHT = 2
             JUMP = 3
             GROW = 4
            */

            currentAction = possibleMoves[rnd.Next(possibleMoves.Count)];
        }

        //implements abstract circle interface: GeometryFriends agents manager gets the current action intended to be actuated in the enviroment for this agent
        public override Moves GetAction()
        {
            return currentAction;
        }

        //implements abstract circle interface: updates the agent state logic and predictions
        public override void Update(TimeSpan elapsedGameTime)
        {

            if ((DateTime.Now - lastMoveTime).TotalMilliseconds >= 20)
            { 
                // saber se a plataforma atual e diferente da plataforma anterior
                IsDifferentPlatform();
                // saber se um diamante foi colecionado
                IsGetCollectible();                

                // se o circulo se encontra numa plataforma
                if (currentPlatform.HasValue)
                {
                    if (differentPlatformFlag || getCollectibleFlag)
                    {
                        differentPlatformFlag = false;
                        getCollectibleFlag = false;

                        targetPointX_InAir = (currentPlatform.Value.leftEdge + currentPlatform.Value.rightEdge) / 2;

                        Task.Factory.StartNew(SetNextEdge);
                    }

                    // se o proximo objetivo estiver definido
                    if (nextEdge.HasValue)
                    {
                        if (-GameInfo.MAX_VELOCITYY <= circleInfo.VelocityY && circleInfo.VelocityY <= GameInfo.MAX_VELOCITYY)
                        {
                            if (nextEdge.Value.movementType == Platform.movementType.STAIR_GAP)
                            {
                                currentAction = nextEdge.Value.rightMove ? Moves.ROLL_RIGHT : Moves.ROLL_LEFT;
                            }
                            else
                            {
                                currentAction = actionSelector.GetCurrentAction(circleInfo, nextEdge.Value.movePoint.x, nextEdge.Value.velocityX, nextEdge.Value.rightMove);
                            }
                        }
                        else
                        {
                            currentAction = actionSelector.GetCurrentAction(circleInfo, targetPointX_InAir, 0, true);
                        }
                    }
                }

                // se o circulo nao se encontra numa plataforma
                else
                {
                    if (nextEdge.HasValue)
                    {
                        if (nextEdge.Value.movementType == Platform.movementType.STAIR_GAP)
                        {
                            currentAction = nextEdge.Value.rightMove ? Moves.ROLL_RIGHT : Moves.ROLL_LEFT;
                        }
                        else
                        {
                            if (nextEdge.Value.collideCeiling && circleInfo.VelocityY < 0)
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

                if (!nextEdge.HasValue)
                {
                    currentAction = actionSelector.GetCurrentAction(circleInfo, (int)circleInfo.X, 0, false);
                }

                lastMoveTime = DateTime.Now;
                DebugSensorsInfo();
            }

            if (nextEdge.HasValue)
            {
                if (!actionSelector.IsGoal(circleInfo, nextEdge.Value.movePoint.x, nextEdge.Value.velocityX, nextEdge.Value.rightMove))
                {
                    return;
                }

                if (-GameInfo.MAX_VELOCITYY <= circleInfo.VelocityY && circleInfo.VelocityY <= GameInfo.MAX_VELOCITYY)
                {
                    targetPointX_InAir = (nextEdge.Value.reachablePlatform.leftEdge + nextEdge.Value.reachablePlatform.rightEdge) / 2;

                    if (nextEdge.Value.movementType == Platform.movementType.JUMP)
                    {
                        currentAction = Moves.JUMP;
                    }
                }
            }
        }


        private void IsGetCollectible()
        {
            if (previousCollectibleNum != currentCollectibleNum)
            {
                getCollectibleFlag = true;
            }

            previousCollectibleNum = currentCollectibleNum;
        }

        private void IsDifferentPlatform()
        {
            currentPlatform = platform.GetPlatform_onCircle(new LevelArray.Point((int)circleInfo.X, (int)circleInfo.Y));

            if (currentPlatform.HasValue)
            {
                if (!previousPlatform.HasValue)
                {
                    differentPlatformFlag = true;
                }
                else if (currentPlatform.Value.id != previousPlatform.Value.id)
                {
                    differentPlatformFlag = true;
                }
            }

            previousPlatform = currentPlatform;
        }

        private void SetNextEdge()
        {
            nextEdge = null;
            nextEdge = subgoalAStar.CalculateShortestPath(currentPlatform.Value, new LevelArray.Point((int)circleInfo.X, (int)circleInfo.Y),
                Enumerable.Repeat<bool>(true, levelArray.initialCollectiblesInfo.Length).ToArray(),
                levelArray.GetObtainedCollectibles(collectiblesInfo), levelArray.initialCollectiblesInfo);
        }

        //typically used console debugging used in previous implementations of GeometryFriends
        protected void DebugSensorsInfo()
        {
            Console.WriteLine("Circle Agent - " + numbersInfo.ToString());

            Console.WriteLine("Circle Agent - " + rectangleInfo.ToString());

            Console.WriteLine("Circle Agent - " + circleInfo.ToString());

            foreach (ObstacleRepresentation i in obstaclesInfo)
            {
                Console.WriteLine("Circle Agent - " + i.ToString("Obstacle"));
            }

            foreach (ObstacleRepresentation i in rectanglePlatformsInfo)
            {
                Console.WriteLine("Circle Agent - " + i.ToString("Rectangle Platform"));
            }

            foreach (ObstacleRepresentation i in circlePlatformsInfo)
            {
                Console.WriteLine("Circle Agent - " + i.ToString("Circle Platform"));
            }

            foreach (CollectibleRepresentation i in collectiblesInfo)
            {
                Console.WriteLine("Circle Agent - " + i.ToString());
            }
        }

        //implements abstract circle interface: signals the agent the end of the current level
        public override void EndGame(int collectiblesCaught, int timeElapsed)
        {
            Console.WriteLine("CIRCLE - Collectibles caught = {0}, Time elapsed - {1}", collectiblesCaught, timeElapsed);
        }
    }
}

