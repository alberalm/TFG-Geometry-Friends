using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    public class SetupMaker
    {
        // Agents
        public RectangleAgent rectangleAgent;
        public CircleAgent circleAgent;

        //Sensors Information and level state
        public CountInformation numbersInfo;
        public RectangleRepresentation rectangleInfo;
        public CircleRepresentation circleInfo;
        public ObstacleRepresentation[] obstaclesInfo;
        public ObstacleRepresentation[] greenObstaclesInfo;
        public ObstacleRepresentation[] yellowObstaclesInfo;
        public CollectibleRepresentation[] collectiblesInfo;
        public int nCollectiblesLeft;

        public Dictionary<CollectibleRepresentation, int> collectibleId;

        // LevelMaps
        public LevelMapCircle levelMapCircle;
        public LevelMapRectangle levelMapRectangle;
        public LevelMapCooperative levelMapCooperative;

        // Planning
        public Graph graphCircle;
        public Graph graphRectangle;
        public List<MoveInformation> planCircle;
        public List<MoveInformation> planRectangle;

        public List<MoveInformation> fullPlanCircle;
        public List<MoveInformation> fullPlanRectangle;

        // Execution
        public ActionSelectorCircle actionSelectorCircle;
        public ActionSelectorRectangle actionSelectorRectangle;
        public Platform currentPlatformCircle;
        public Platform currentPlatformRectangle;

        // Learning
        public LearningCircle lCircle;
        public LearningRectangle lRectangle;

        public SetupMaker(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI)
        {
            numbersInfo = nI;
            nCollectiblesLeft = nI.CollectiblesCount;
            rectangleInfo = rI;
            circleInfo = cI;
            obstaclesInfo = oI;
            greenObstaclesInfo = rPI;
            yellowObstaclesInfo = cPI;
            collectiblesInfo = colI;

            levelMapCircle = new LevelMapCircle();
            levelMapRectangle = new LevelMapRectangle();

            lCircle = new LearningCircle();
            lRectangle = new LearningRectangle();

            collectibleId = new Dictionary<CollectibleRepresentation, int>();
            for (int i = 0; i < colI.Length; i++)
            {
                collectibleId[colI[i]] = i;
            }
        }
        
        public void SetUp(){

            levelMapCircle.CreateLevelMap(collectiblesInfo, obstaclesInfo, greenObstaclesInfo);
            levelMapRectangle.CreateLevelMap(collectiblesInfo, obstaclesInfo, yellowObstaclesInfo);

            levelMapCooperative = new LevelMapCooperative(levelMapCircle, levelMapRectangle);

            levelMapCooperative.CreateLevelMap();

            // Circle

            graphCircle = new Graph(levelMapCircle.GetPlatforms(), collectiblesInfo);

            planCircle = graphCircle.SearchAlgorithm(levelMapCircle.PlatformBelowCircle(circleInfo).id, collectiblesInfo, null);
            fullPlanCircle = new List<MoveInformation>(planCircle);

            actionSelectorCircle = new ActionSelectorCircle(collectibleId, lCircle, levelMapCircle, graphCircle);

            // Rectangle
            
            graphRectangle = new Graph(levelMapRectangle.simplified_platforms, collectiblesInfo);

            Platform initialPlatformRectangle = levelMapRectangle.PlatformBelowRectangle(rectangleInfo);

            planRectangle = graphRectangle.SearchAlgorithm(initialPlatformRectangle.id, collectiblesInfo, null);

            if (!graphRectangle.planIsComplete)
            {
                MoveInformation m_left = new MoveInformation(new Platform(-1), new Platform(-1), (int)rectangleInfo.X / GameInfo.PIXEL_LENGTH, (int)rectangleInfo.X / GameInfo.PIXEL_LENGTH, 0, MoveType.FALL, new List<int>(), new List<Tuple<float, float>>(), 10);
                m_left.moveDuringFlight = Moves.MOVE_LEFT;
                levelMapRectangle.moveGenerator.trajectoryAdder.rectangleSimulator.SimulateMove(ref levelMapRectangle.platformList, rectangleInfo.X, rectangleInfo.Y, rectangleInfo.VelocityX, rectangleInfo.VelocityY, ref m_left, RectangleShape.GetShape(rectangleInfo));

                MoveInformation m_right = new MoveInformation(new Platform(-1), new Platform(-1), (int)rectangleInfo.X / GameInfo.PIXEL_LENGTH, (int)rectangleInfo.X / GameInfo.PIXEL_LENGTH, 0, MoveType.FALL, new List<int>(), new List<Tuple<float, float>>(), 10);
                m_right.moveDuringFlight = Moves.MOVE_RIGHT;
                levelMapRectangle.moveGenerator.trajectoryAdder.rectangleSimulator.SimulateMove(ref levelMapRectangle.platformList, rectangleInfo.X, rectangleInfo.Y, rectangleInfo.VelocityX, rectangleInfo.VelocityY, ref m_right, RectangleShape.GetShape(rectangleInfo));

                if (m_left.landingPlatform.id >= 0 && levelMapRectangle.small_to_simplified[m_left.landingPlatform].id != initialPlatformRectangle.id)
                {
                    List<MoveInformation> plan_left = graphRectangle.SearchAlgorithm(levelMapRectangle.small_to_simplified[m_left.landingPlatform].id, collectiblesInfo, null);
                    if (graphRectangle.planIsComplete)
                    {
                        planRectangle = plan_left;
                        rectangleAgent.currentAction = Moves.MOVE_LEFT;
                    }
                }
                if (m_right.landingPlatform.id >= 0 && levelMapRectangle.small_to_simplified[m_right.landingPlatform].id != initialPlatformRectangle.id)
                {
                    List<MoveInformation> plan_right = graphRectangle.SearchAlgorithm(levelMapRectangle.small_to_simplified[m_right.landingPlatform].id, collectiblesInfo, null);
                    if (graphRectangle.planIsComplete)
                    {
                        if (rectangleAgent.currentAction == Moves.NO_ACTION || plan_right.Count < planRectangle.Count)
                        {
                            planRectangle = plan_right;
                            rectangleAgent.currentAction = Moves.MOVE_RIGHT;
                        }
                    }
                }

            }

            fullPlanRectangle = new List<MoveInformation>(planRectangle);

            actionSelectorRectangle = new ActionSelectorRectangle(collectibleId, lRectangle, levelMapRectangle, graphRectangle);
        }
    }
}
