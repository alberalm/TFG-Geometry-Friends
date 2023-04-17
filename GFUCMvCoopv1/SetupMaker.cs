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


        public Dictionary<int, int> circle_to_rectangle;
        // Planning
        public Graph graph;
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
            circle_to_rectangle = new Dictionary<int, int>();
            
            collectibleId = new Dictionary<CollectibleRepresentation, int>();
            for (int i = 0; i < colI.Length; i++)
            {
                collectibleId[colI[i]] = i;
            }
        }
        
        public void SetUp()
        {
            levelMapCircle.CreateLevelMap(collectiblesInfo, obstaclesInfo, greenObstaclesInfo);
            levelMapRectangle.CreateLevelMap(collectiblesInfo, obstaclesInfo, yellowObstaclesInfo);

            levelMapCooperative = new LevelMapCooperative(levelMapCircle, levelMapRectangle);

            levelMapCooperative.CreateLevelMap(ref circle_to_rectangle, levelMapRectangle.small_to_simplified);

            graph = new Graph(levelMapCircle.simplified_platforms, levelMapRectangle.simplified_platforms, circle_to_rectangle, collectiblesInfo);
            graph.SearchAlgorithm(levelMapCircle.small_to_simplified[levelMapCircle.PlatformBelowCircle(circleInfo)].id, levelMapRectangle.PlatformBelowRectangle(rectangleInfo).id, collectiblesInfo);

            // Circle

            planCircle = graph.GetCirclePlan();
            fullPlanCircle = new List<MoveInformation>(planCircle);

            actionSelectorCircle = new ActionSelectorCircle(collectibleId, lCircle, levelMapCircle, graph);

            // Rectangle
            
            planRectangle = graph.GetRectanglePlan();
            fullPlanRectangle = new List<MoveInformation>(planRectangle);

            actionSelectorRectangle = new ActionSelectorRectangle(collectibleId, lRectangle, levelMapRectangle, graph);
        }

        public void Replanning()
        {
            graph.SearchAlgorithm(levelMapCircle.small_to_simplified[levelMapCircle.PlatformBelowCircle(circleInfo)].id, levelMapRectangle.PlatformBelowRectangle(rectangleInfo).id, collectiblesInfo);
            planCircle = graph.GetCirclePlan();
            fullPlanCircle = new List<MoveInformation>(planCircle);
            planRectangle = graph.GetRectanglePlan();
            fullPlanRectangle = new List<MoveInformation>(planRectangle);
        }
    }
}
