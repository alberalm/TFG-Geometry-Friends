using GeometryFriends.AI;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static GeometryFriendsAgents.LevelMap;
using Size = System.Drawing.Size;

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
        public Platform currentPlatformCircle = new Platform(-1);
        public Platform currentPlatformRectangle = new Platform(-1);
        public bool circleAgentReadyForCircleTilt;
        public bool circleAgentReadyForCoop;
        public bool rectangleAgentReadyForCoop;
        public bool circleInAir = false;
        public bool changing = false;

        // Recovery mode
        public int timesStuckRectangle = 0;
        public int timesStuckCircle = 0;
        public CircleRepresentation lastCircleInfo;
        public RectangleRepresentation lastRectangleInfo;
        public int numStuck = 0;

        // Learning
        public LearningCircle lCircle;
        public LearningRectangle lRectangle;


        //Explainability
        private GeometryFriends.XNAStub.Color color_green = GeometryFriends.XNAStub.Color.LightGreen;
        private GeometryFriends.XNAStub.Color color_yellow = GeometryFriends.XNAStub.Color.LightGoldenrodYellow;
        public string circle_immediate_goal = "";
        public string rectangle_immediate_goal = "";
        public string circle_state = "";
        public string rectangle_state = "";

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

            circleAgentReadyForCoop = false;
            rectangleAgentReadyForCoop = false;
            circleAgentReadyForCircleTilt = false;
        }
        
        public void SetUp()
        {
            levelMapRectangle.CreateLevelMap(collectiblesInfo, obstaclesInfo, yellowObstaclesInfo);
            levelMapCircle.CreateLevelMap(collectiblesInfo, obstaclesInfo, greenObstaclesInfo, levelMapRectangle.moveGenerator);

            levelMapCooperative = new LevelMapCooperative(levelMapCircle, levelMapRectangle);
            levelMapCooperative.CreateLevelMap(ref circle_to_rectangle, levelMapRectangle.small_to_simplified);

            graph = new Graph(levelMapCircle.simplified_platforms, levelMapRectangle.simplified_platforms, circle_to_rectangle, collectiblesInfo);
            Platform p_rectangle = levelMapRectangle.PlatformBelowRectangle(rectangleInfo);
            Platform p_circle = levelMapCircle.PlatformBelowCircle(circleInfo);
            if (p_circle.id!=-1 && p_rectangle.id != -1)
            {
                graph.SearchAlgorithm(levelMapCircle.small_to_simplified[p_circle].id, p_rectangle.id, collectiblesInfo);
            }
            

            // Circle

            planCircle = graph.GetCirclePlan();
            fullPlanCircle = new List<MoveInformation>(planCircle);

            actionSelectorCircle = new ActionSelectorCircle(collectibleId, lCircle, levelMapCircle, graph, this);

            // Rectangle
            
            planRectangle = graph.GetRectanglePlan();
            fullPlanRectangle = new List<MoveInformation>(planRectangle);

            actionSelectorRectangle = new ActionSelectorRectangle(collectibleId, lRectangle, levelMapRectangle, graph, this);
        }

        public void Replanning()
        {
            currentPlatformCircle = levelMapCircle.CirclePlatform(circleInfo);
            currentPlatformRectangle = levelMapRectangle.PlatformBelowRectangle(rectangleInfo);
            
            if (currentPlatformCircle.id != -1 && currentPlatformRectangle.id != -1)
            {
                graph.SearchAlgorithm(levelMapCircle.small_to_simplified[currentPlatformCircle].id, currentPlatformRectangle.id, collectiblesInfo);
                planCircle = graph.GetCirclePlan();
                fullPlanCircle = new List<MoveInformation>(planCircle);
                planRectangle = graph.GetRectanglePlan();
                fullPlanRectangle = new List<MoveInformation>(planRectangle);
            }
        }

        

        public bool CircleAboveRectangle()
        {
            double width = GameInfo.RECTANGLE_AREA / rectangleInfo.Height;
            return circleInfo.X <= rectangleInfo.X + width / 2 &&
                circleInfo.X >= rectangleInfo.X - width / 2 &&
                Math.Abs(circleInfo.Y + GameInfo.CIRCLE_RADIUS + rectangleInfo.Height / 2 - rectangleInfo.Y) < 2 * GameInfo.PIXEL_LENGTH;
        }

        public void UpdateChanging()
        { 
            if (currentPlatformCircle.yTop == currentPlatformRectangle.yTop && currentPlatformCircle.real && currentPlatformRectangle.real &&
               ((currentPlatformCircle.leftEdge < rectangleInfo.X/GameInfo.PIXEL_LENGTH && currentPlatformCircle.rightEdge > rectangleInfo.X / GameInfo.PIXEL_LENGTH) 
               || (currentPlatformRectangle.leftEdge < circleInfo.X / GameInfo.PIXEL_LENGTH && currentPlatformRectangle.rightEdge > circleInfo.X / GameInfo.PIXEL_LENGTH))) // Same platform
            {                   
                if (actionSelectorCircle.move != null && actionSelectorRectangle.move != null &&
                    (actionSelectorRectangle.move.moveType != MoveType.CIRCLETILT || actionSelectorCircle.move.moveType != MoveType.COOPMOVE))
                {
                    if (actionSelectorCircle.move.x == actionSelectorRectangle.move.x)
                    {
                        changing = false;
                        return;
                    }
                    changing = Math.Sign(actionSelectorCircle.move.x - actionSelectorRectangle.move.x) != Math.Sign(circleInfo.X - rectangleInfo.X);
                    if (changing && circleAgent.currentPlatformCircle.id != -1 && rectangleAgent.currentPlatformRectangle.id != -1)
                    {
                        actionSelectorCircle.nextActionPhisics(ref planCircle, circleAgent.remaining, circleInfo, rectangleInfo, circleAgent.currentPlatformCircle);
                        actionSelectorRectangle.nextActionPhisics(ref planRectangle, rectangleAgent.remaining, circleInfo, rectangleInfo, rectangleAgent.currentPlatformRectangle);
                        changing = Math.Sign(actionSelectorCircle.move.x - actionSelectorRectangle.move.x) != Math.Sign(circleInfo.X - rectangleInfo.X);
                    }
                }
            }
            else
            {
                changing = false;
            }
        }

        private void DrawObstacles(ref List<DebugInformation> debugInformation)
        {
            for (int x = 0; x < GameInfo.LEVEL_MAP_WIDTH; x++)
            {
                for (int y = 0; y < GameInfo.LEVEL_MAP_HEIGHT; y++)
                {
                    if ((levelMapCircle.levelMap[x, y] == PixelType.OBSTACLE || levelMapCircle.levelMap[x, y] == PixelType.PLATFORM) &&
                        (levelMapRectangle.levelMap[x, y] == PixelType.OBSTACLE || levelMapRectangle.levelMap[x, y] == PixelType.PLATFORM))
                    {
                        if ((x + y) % 2 == 0)
                        {
                            debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Black));
                        }
                        else
                        {
                            debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.White));
                        }
                    }
                    else if (levelMapCircle.levelMap[x, y] == PixelType.OBSTACLE || levelMapCircle.levelMap[x, y] == PixelType.PLATFORM)
                    {
                        if ((x + y) % 2 == 0)
                        {
                            debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Green));
                        }
                        else
                        {
                            debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.White));
                        }
                    }
                    else if (levelMapRectangle.levelMap[x, y] == PixelType.OBSTACLE || levelMapRectangle.levelMap[x, y] == PixelType.PLATFORM)
                    {
                        if ((x + y) % 2 == 0)
                        {
                            debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Yellow));
                        }
                        else
                        {
                            debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Black));
                        }
                    }
                    else if (levelMapRectangle.levelMap[x, y] == PixelType.EMPTY)
                    {
                        //debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF position, Size size, XNAStub.Color color););
                    }
                    else if (levelMapRectangle.levelMap[x, y] == PixelType.DIAMOND)
                    {
                        debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Purple));

                    }
                    else if (levelMapRectangle.levelMap[x, y] == PixelType.PLATFORM)
                    {
                        //debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Chocolate));
                    }
                }
            }
        }

        private void DrawLegend(ref List<DebugInformation> debugInformation)
        {
            debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(0, 0), new Size(GameInfo.LEVEL_WIDTH, 5 * GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Black));

            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(50, 10), "None", GeometryFriends.XNAStub.Color.Chocolate));
            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(125, 10), "Square", GeometryFriends.XNAStub.Color.Brown));
            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(225, 10), "Horizontal", GeometryFriends.XNAStub.Color.Purple));
            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(350, 10), "Square+Horizontal", GeometryFriends.XNAStub.Color.Orange));
            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(550, 10), "Vertical", GeometryFriends.XNAStub.Color.Yellow));
            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(675, 10), "Square+Vertical", GeometryFriends.XNAStub.Color.Green));
            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(850, 10), "Vertical+Horizontal", GeometryFriends.XNAStub.Color.Red));
            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(1100, 10), "All", GeometryFriends.XNAStub.Color.Blue));
        }

        private void DrawRectangleSmallPlatforms(ref List<DebugInformation> debugInformation)
        {
            foreach (Platform p in levelMapRectangle.platformList)
            {
                GeometryFriends.XNAStub.Color color = GeometryFriends.XNAStub.Color.Black;
                int suma = 0;
                for (int i = 0; i < 3; i++)
                {
                    suma += p.shapes[i] ? (int)(Math.Pow(2, i)) : 0;
                }
                switch (suma)
                {
                    case 0:
                        color = GeometryFriends.XNAStub.Color.Chocolate;
                        break;
                    case 1:
                        color = GeometryFriends.XNAStub.Color.Brown;
                        break;
                    case 2:
                        color = GeometryFriends.XNAStub.Color.Purple;
                        break;
                    case 3:
                        color = GeometryFriends.XNAStub.Color.Orange;
                        break;
                    case 4:
                        color = GeometryFriends.XNAStub.Color.Yellow;
                        break;
                    case 5:
                        color = GeometryFriends.XNAStub.Color.Green;
                        break;
                    case 6:
                        color = GeometryFriends.XNAStub.Color.Red;
                        break;
                    case 7:
                        color = GeometryFriends.XNAStub.Color.Blue;
                        break;
                }
                debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(p.leftEdge * GameInfo.PIXEL_LENGTH, (p.yTop + 1) * GameInfo.PIXEL_LENGTH), new Size((p.rightEdge-p.leftEdge+1)*GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), color));
            }
        }

        private void DrawRectangleSmallPlatformsNumbers(ref List<DebugInformation> debugInformation)
        {
            foreach (Platform p in levelMapRectangle.platformList)
            {
                debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH + 10),
                    new Size(20, 20), GeometryFriends.XNAStub.Color.Black));
                debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2 + 1, p.yTop * GameInfo.PIXEL_LENGTH + 9),
                    new Size(18, 18), GeometryFriends.XNAStub.Color.Green));
                debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH + 6),
                    p.id.ToString(), GeometryFriends.XNAStub.Color.Black));
            }
        }

        private void DrawRectangleSimplifiedPlatformsNumbers(ref List<DebugInformation> debugInformation)
        {
            foreach (Platform p in levelMapRectangle.simplified_platforms)
            {
                debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH + 10),
                    new Size(35, 20), GeometryFriends.XNAStub.Color.Black));
                debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2 + 1, p.yTop * GameInfo.PIXEL_LENGTH + 9),
                    new Size(33, 18), GeometryFriends.XNAStub.Color.Green));
                debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH + 6),
                    "R" + p.id.ToString(), GeometryFriends.XNAStub.Color.Black));
            }
        }

        private void DrawCircleSimplifiedPlatforms(ref List<DebugInformation> debugInformation)
        {
            foreach (Platform p in levelMapCircle.simplified_platforms)
            {
                if (p.real)
                {
                    debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(p.leftEdge * GameInfo.PIXEL_LENGTH, p.yTop * GameInfo.PIXEL_LENGTH),
                            new Size((p.rightEdge - p.leftEdge + 1) * GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Chocolate));
                }
                else
                {
                    foreach (Platform small in levelMapCircle.simplified_to_small[p])
                    {
                        debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(small.leftEdge * GameInfo.PIXEL_LENGTH, small.yTop * GameInfo.PIXEL_LENGTH),
                                    new Size((small.rightEdge-small.leftEdge+1)*GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH/2), GeometryFriends.XNAStub.Color.LightPink));
                    }
                }                
            }
        }

        private void DrawCircleSmallPlatformsNumbers(ref List<DebugInformation> debugInformation)
        {
            foreach (Platform p in levelMapCircle.platformList)
            {
                int tam = 20 + (p.id / 10) * 15;
                if (p.real)
                {

                    debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH - 21),
                    new Size(tam, 20), GeometryFriends.XNAStub.Color.Black));
                    debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2 + 1, p.yTop * GameInfo.PIXEL_LENGTH - 20),
                        new Size(tam - 2, 18), GeometryFriends.XNAStub.Color.Yellow));
                    debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH - 24),
                        p.id.ToString(), GeometryFriends.XNAStub.Color.Black));
                }
                else
                {
                    debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH - 51),
                    new Size(tam, 20), GeometryFriends.XNAStub.Color.Black));
                    debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2 + 1, p.yTop * GameInfo.PIXEL_LENGTH - 50),
                        new Size(tam - 2, 18), GeometryFriends.XNAStub.Color.Yellow));
                    debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH - 54),
                        p.id.ToString(), GeometryFriends.XNAStub.Color.Black));
                }

            }
        }

        private void DrawCircleSimplifiedPlatformsNumbers(ref List<DebugInformation> debugInformation)
        {
            foreach (Platform p in levelMapCircle.simplified_platforms)
            {
                int tam = 35 + (p.id / 10) * 15;
                if (p.real)
                {

                    debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH - 21),
                    new Size(tam, 20), GeometryFriends.XNAStub.Color.Black));
                    debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2 + 1, p.yTop * GameInfo.PIXEL_LENGTH - 20),
                        new Size(tam - 2, 18), GeometryFriends.XNAStub.Color.Yellow));
                    debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH - 24),
                        "C"+p.id.ToString(), GeometryFriends.XNAStub.Color.Black));
                }
                else
                {
                    debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH - 51),
                    new Size(tam, 20), GeometryFriends.XNAStub.Color.Black));
                    debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2 + 1, p.yTop * GameInfo.PIXEL_LENGTH - 50),
                        new Size(tam - 2, 18), GeometryFriends.XNAStub.Color.Yellow));
                    debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH - 54),
                        "C" + p.id.ToString(), GeometryFriends.XNAStub.Color.Black));
                }

            }
        }

        private void DrawCollectibles(ref List<DebugInformation> debugInformation)
        {
            int count = 0;
            foreach (CollectibleRepresentation c in levelMapRectangle.initialCollectiblesInfo)
            {
                if (collectiblesInfo.Contains(c)) {
                    debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(c.X - 20, c.Y - 13), "D" + count.ToString(), GeometryFriends.XNAStub.Color.Black));
                }
                count++;
            }
        }

        public void DrawLevelMap(ref List<DebugInformation> debugInformation)
        {
            //Common obstacles
            //DrawObstacles(ref debugInformation);
            //DrawLegend(ref debugInformation);

            //Rectangle obstacles info
            DrawRectangleSmallPlatforms(ref debugInformation);
            DrawRectangleSimplifiedPlatformsNumbers(ref debugInformation);
            //DrawRectangleSmallPlatformsNumbers(ref debugInformation);

            //Circle obstacles info
            DrawCircleSimplifiedPlatforms(ref debugInformation);
            DrawCircleSimplifiedPlatformsNumbers(ref debugInformation);
            //DrawCircleSmallPlatformsNumbers(ref List < DebugInformation > debugInformation);

            
        }

        public void PlanDebug(ref List<DebugInformation> newDebugInfo)
        {
            //Circle plan
            newDebugInfo.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(0, 715), new Size(640, 85), color_yellow));
            int x = 10;
            int y = 720;
            int step = 1;    
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x,y), "Plan del círculo", GeometryFriends.XNAStub.Color.Black));
            foreach (MoveInformation m in fullPlanCircle)
            {
                if (step == 3)
                {
                    y = 645;
                }
                if (step <= 5)
                {
                    if (!m.departurePlatform.real)
                    {
                        newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x + (step<=2? 0:320), y + 25 * step),
                        step.ToString() + ".- " + m.moveType.ToString() + " de Rect (C" + m.departurePlatform.id.ToString() + ") a C" + m.landingPlatform.id.ToString(),
                        GeometryFriends.XNAStub.Color.Black));
                    }
                    else if (!m.landingPlatform.real)
                    {
                        newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x + (step <= 2 ? 0 : 320), y + 25 * step),
                        step.ToString() + ".- " + m.moveType.ToString() + " de C" + m.departurePlatform.id.ToString() + " a Rect (C" + m.landingPlatform.id.ToString() + ")",
                        GeometryFriends.XNAStub.Color.Black));
                    }
                    else if (m.moveType==MoveType.COOPMOVE)
                    {
                        newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x + (step <= 2 ? 0 : 320), y + 25 * step),
                        step.ToString() + ".- WAIT en C" + m.departurePlatform.id.ToString(),
                        GeometryFriends.XNAStub.Color.Black));
                    }
                    else 
                    {
                        newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x + (step <= 2 ? 0 : 320), y + 25 * step),
                        step.ToString() + ".- " + m.moveType.ToString() + " de C" + m.departurePlatform.id.ToString() + " a C" + m.landingPlatform.id.ToString(),
                        GeometryFriends.XNAStub.Color.Black));
                    }
                }
                
                foreach (Tuple<float, float> tup in m.path)
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 2, GeometryFriends.XNAStub.Color.Yellow));
                }
                if (m.moveType!=MoveType.COOPMOVE)
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(m.path[m.path.Count / 2].Item1 + 3, m.path[m.path.Count / 2].Item2 + 5), 16, GeometryFriends.XNAStub.Color.Black));
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(m.path[m.path.Count / 2].Item1+4, m.path[m.path.Count / 2].Item2+6), 14, GeometryFriends.XNAStub.Color.Yellow));
                    newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(m.path[m.path.Count / 2].Item1, m.path[m.path.Count / 2].Item2), step.ToString(), GeometryFriends.XNAStub.Color.Black));
                }

                step++;
            }

            //Rectangle plan
            newDebugInfo.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(640, 715), new Size(640, 85), color_green));
            x = 650;
            y = 720;
            step = 1;
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x, y), "Plan del rectángulo", GeometryFriends.XNAStub.Color.Black));
            foreach (MoveInformation m in fullPlanRectangle)
            {
                if (step == 3)
                {
                    y = 645;
                }
                if (step <= 5)
                {
                    if (m.moveType == MoveType.COOPMOVE && (!fullPlanCircle[step - 1].departurePlatform.real || !fullPlanCircle[step - 1].landingPlatform.real))
                    {
                        newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x + (step <= 2 ? 0 : 320), y + 25 * step),
                        step.ToString() + ".- HELP CIRCLE en R" + m.landingPlatform.id.ToString(),
                        GeometryFriends.XNAStub.Color.Black));
                    }
                    else if (m.moveType == MoveType.COOPMOVE)
                    {
                        newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x + (step <= 2 ? 0 : 320), y + 25 * step),
                        step.ToString() + ".- WAIT en R" + m.landingPlatform.id.ToString(),
                        GeometryFriends.XNAStub.Color.Black));
                    }
                    else if (m.moveType == MoveType.MONOSIDEDROP)
                    {
                        newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x + (step <= 2 ? 0 : 320), y + 25 * step),
                        step.ToString() + ".- MNSDDROP de R" + m.departurePlatform.id.ToString() + " a R" + m.landingPlatform.id.ToString(),
                        GeometryFriends.XNAStub.Color.Black));
                    }
                    else if (m.moveType == MoveType.BIGHOLEADJ)
                    {
                        newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x + (step <= 2 ? 0 : 320), y + 25 * step),
                        step.ToString() + ".- BIGHADJ de R" + m.departurePlatform.id.ToString() + " a R" + m.landingPlatform.id.ToString(),
                        GeometryFriends.XNAStub.Color.Black));
                    }
                    else if (m.moveType == MoveType.BIGHOLEDROP)
                    {
                        newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x + (step <= 2 ? 0 : 320), y + 25 * step),
                        step.ToString() + ".- BIGHDROP de R" + m.departurePlatform.id.ToString() + " a R" + m.landingPlatform.id.ToString(),
                        GeometryFriends.XNAStub.Color.Black));
                    }
                    else
                    {
                        newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x + (step <= 2 ? 0 : 320), y + 25 * step),
                        step.ToString() + ".- " + m.moveType.ToString() + " de R" + m.departurePlatform.id.ToString() + " a R" + m.landingPlatform.id.ToString(),
                        GeometryFriends.XNAStub.Color.Black));
                    }
                    
                }
                foreach (Tuple<float, float> tup in m.path)
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 2, GeometryFriends.XNAStub.Color.Green));
                }
                if (m.path.Count > 0 && m.moveType!=MoveType.COOPMOVE)
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(m.path[m.path.Count / 2].Item1 + 3, m.path[m.path.Count / 2].Item2 + 5), 16, GeometryFriends.XNAStub.Color.Black));
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(m.path[m.path.Count / 2].Item1 + 4, m.path[m.path.Count / 2].Item2 + 6), 14, GeometryFriends.XNAStub.Color.Green));
                    newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(m.path[m.path.Count / 2].Item1, m.path[m.path.Count / 2].Item2), step.ToString(), GeometryFriends.XNAStub.Color.Black));
                }
                step++;
            }
        }
        private void DrawCircle(ref List<DebugInformation> newDebugInfo)
        {
            /*
            //Circle Silhouette
            int[] CIRCLE_SIZE = new int[] { 3, 4, 5, 5, 5, 5, 5, 5, 4, 3 };//Divided by 2
            for (int i = -GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i < GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i++)
            {
                for (int j = -CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j < CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j++)
                {
                    int x = (int)(circleInfo.X / GameInfo.PIXEL_LENGTH);
                    int y = (int)(circleInfo.Y / GameInfo.PIXEL_LENGTH);
                    DebugInformation di = DebugInformationFactory.CreateRectangleDebugInfo(new PointF((x + i) * GameInfo.PIXEL_LENGTH, (y + j) * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.YellowGreen);
                    newDebugInfo.Add(di);
                }
            }*/



            newDebugInfo.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(40, 316), new Size(118, 200), color_yellow));

            //Circle velocity

            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(circleInfo.X, circleInfo.Y), new PointF(circleInfo.X + circleInfo.VelocityX, circleInfo.Y), GeometryFriends.XNAStub.Color.Red));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(circleInfo.X, circleInfo.Y), new PointF(circleInfo.X, circleInfo.Y + circleInfo.VelocityY), GeometryFriends.XNAStub.Color.Blue));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X + 20, circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X - 20, circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X + 40, circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X - 20, circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(5, 400), "Vx: " + (int)circleInfo.VelocityX, GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(5, 430), "Target vx: " + actionSelectorCircle.target_velocity, GeometryFriends.XNAStub.Color.Black));

            //Circle target position
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(actionSelectorCircle.target_position * GameInfo.PIXEL_LENGTH, circleInfo.Y), 10, GeometryFriends.XNAStub.Color.Yellow));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(5, 460), "Dist. x: "+ (int)Math.Abs(circleInfo.X / GameInfo.PIXEL_LENGTH - actionSelectorCircle.target_position), GeometryFriends.XNAStub.Color.Black));

            //Acceleration point
            if (actionSelectorCircle.target_velocity > 0)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(actionSelectorCircle.target_position * GameInfo.PIXEL_LENGTH- actionSelectorCircle.acceleration_distance, circleInfo.Y), 5, GeometryFriends.XNAStub.Color.Pink));
            }
            else
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(actionSelectorCircle.target_position * GameInfo.PIXEL_LENGTH + actionSelectorCircle.acceleration_distance, circleInfo.Y), 5, GeometryFriends.XNAStub.Color.Pink));
            }
            //Breaking point
            if (circleInfo.VelocityX > 0)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X + actionSelectorCircle.brake_distance, circleInfo.Y), 5, GeometryFriends.XNAStub.Color.Gray));
            }
            else {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X - actionSelectorCircle.brake_distance, circleInfo.Y), 5, GeometryFriends.XNAStub.Color.Gray));
            }

            //Current platform circle
            currentPlatformCircle = levelMapCircle.CirclePlatform(circleInfo);            
            if (levelMapCircle.small_to_simplified.ContainsKey(currentPlatformCircle))
            {
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(circleInfo.X, circleInfo.Y), "C"+levelMapCircle.small_to_simplified[currentPlatformCircle].id.ToString(), GeometryFriends.XNAStub.Color.Black));
            }
            else
            {
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(circleInfo.X, circleInfo.Y), "-1", GeometryFriends.XNAStub.Color.Black));

            }
          

            //Current Action            
            if (circleAgent.currentAction == Moves.NO_ACTION)
            {
                 
            }
            else if (circleAgent.currentAction == Moves.ROLL_LEFT)
            {
                LeftArrow(50, 375, ref newDebugInfo);
            }
            else if (circleAgent.currentAction == Moves.ROLL_RIGHT)
            {
                RightArrow(110, 375, ref newDebugInfo);
            }
            else if (circleAgent.currentAction == Moves.JUMP)
            {
                UpArrow(80, 345, ref newDebugInfo);
            }

            
        }
        private void DrawRectangle(ref List<DebugInformation> newDebugInfo)
        {
            newDebugInfo.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(1122, 306), new Size(118, 220), color_green));
            //Rectangle velocity
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(rectangleInfo.X, rectangleInfo.Y), new PointF(rectangleInfo.X + rectangleInfo.VelocityX, rectangleInfo.Y), GeometryFriends.XNAStub.Color.Red));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(rectangleInfo.X, rectangleInfo.Y), new PointF(rectangleInfo.X, rectangleInfo.Y + rectangleInfo.VelocityY), GeometryFriends.XNAStub.Color.Blue));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(rectangleInfo.X + 20, rectangleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(rectangleInfo.X - 20, rectangleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(rectangleInfo.X + 40, rectangleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(rectangleInfo.X - 20, rectangleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(1122, 440), "Vx: " + (int)rectangleInfo.VelocityX, GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(1122, 470), "Target vx: " + actionSelectorRectangle.target_velocity, GeometryFriends.XNAStub.Color.Black));

            //Rectangle target position
            if (actionSelectorRectangle.move != null)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(actionSelectorRectangle.move.x * GameInfo.PIXEL_LENGTH, rectangleInfo.Y), 10, GeometryFriends.XNAStub.Color.Green));
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(1122, 500), "Dist. x: "+ (int)Math.Abs(rectangleInfo.X / GameInfo.PIXEL_LENGTH - actionSelectorRectangle.move.x), GeometryFriends.XNAStub.Color.Black));

                if (actionSelectorRectangle.target_velocity > 0)
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(actionSelectorRectangle.move.x * GameInfo.PIXEL_LENGTH - actionSelectorRectangle.acceleration_distance, rectangleInfo.Y), 5, GeometryFriends.XNAStub.Color.Pink));
                }
                else
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(actionSelectorRectangle.move.x * GameInfo.PIXEL_LENGTH + actionSelectorRectangle.acceleration_distance, rectangleInfo.Y), 5, GeometryFriends.XNAStub.Color.Pink));
                }
                if (rectangleInfo.VelocityX > 0)
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(rectangleInfo.X + actionSelectorRectangle.brake_distance, rectangleInfo.Y), 5, GeometryFriends.XNAStub.Color.Gray));
                }
                else
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(rectangleInfo.X - actionSelectorRectangle.brake_distance, rectangleInfo.Y), 5, GeometryFriends.XNAStub.Color.Gray));
                }

            }


            //Platform
            currentPlatformRectangle = levelMapRectangle.PlatformBelowRectangle(rectangleInfo);

            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(rectangleInfo.X, rectangleInfo.Y), "R" + currentPlatformRectangle.id.ToString(), GeometryFriends.XNAStub.Color.Black));
           

            //Current Action
             
            if (rectangleAgent.currentAction == Moves.NO_ACTION)
            {
                
            }
            else if (rectangleAgent.currentAction == Moves.MOVE_LEFT)
            {
                LeftArrow(1170, 365, ref newDebugInfo);
            }
            else if (rectangleAgent.currentAction == Moves.MOVE_RIGHT)
            {
                RightArrow(1230, 365, ref newDebugInfo);
            }
            else if (rectangleAgent.currentAction == Moves.MORPH_DOWN)
            {
                DownArrow(1200, 395, ref newDebugInfo);
            }
            else if (rectangleAgent.currentAction == Moves.MORPH_UP)
            {
                UpArrow(1200, 335, ref newDebugInfo);
            }
        }
        private void DrawPanels(ref List<DebugInformation> newDebugInfo)
        {           
            //Top panels
            newDebugInfo.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(0, 0), new Size(640, 130), color_yellow));
            newDebugInfo.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(640, 0), new Size(640, 130), color_green));
           //Lateral panels
            newDebugInfo.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(0, 80), new Size(40, 635), color_yellow));
            newDebugInfo.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(1240, 80), new Size(40, 635), color_green));
        }

        private void DrawCircleExplanation(ref List<DebugInformation> newDebugInfo)
        {
            string message1 = "Objetivo inmediato: " + circle_immediate_goal;
            int maxLength = 55;
            int y = 0;
            if (message1.Length > maxLength)
            {
                string m1 = "";
                string m2 = "";
                foreach (string word in message1.Split(' '))
                {
                    if (m2.Length == 0)
                    {
                        if (m1.Length + 1 + word.Length > maxLength)
                        {
                            m2+= word+ " ";
                        }
                        else
                        {
                            m1 += word + " ";
                        }
                    }
                    else
                    {
                        m2 += word + " ";
                    }
                    
                };
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(10, 10), m1, GeometryFriends.XNAStub.Color.Black));
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(10, 40), m2, GeometryFriends.XNAStub.Color.Black));
                y = 30;
            }
            else
            {
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(10, 10), message1, GeometryFriends.XNAStub.Color.Black));
            }
            string message2 = "Estado: " + circle_state;
            if (message2.Length > maxLength)
            {
                string m1 = "";
                string m2 = "";
                foreach (string word in message2.Split(' '))
                {
                    if (m2.Length == 0)
                    {
                        if (m1.Length + 1 + word.Length > maxLength)
                        {
                            m2 += word + " ";
                        }
                        else
                        {
                            m1 += word + " ";
                        }
                    }
                    else
                    {
                        m2 += word + " ";
                    }

                };
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(10, y + 40), m1, GeometryFriends.XNAStub.Color.Black));
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(10, y + 70), m2, GeometryFriends.XNAStub.Color.Black));
                y = 30;
            }
            else
            {
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(10, y + 40), message2, GeometryFriends.XNAStub.Color.Black));
            }
        }

        private void DrawRectangleExplanation(ref List<DebugInformation> newDebugInfo)
        {
            string message1 = "Objetivo inmediato: " + rectangle_immediate_goal;
            int maxLength = 55;
            int y = 0;
            if (message1.Length > maxLength)
            {
                string m1 = "";
                string m2 = "";
                foreach (string word in message1.Split(' '))
                {
                    if (m2.Length == 0)
                    {
                        if (m1.Length + 1 + word.Length > maxLength)
                        {
                            m2 += word + " ";
                        }
                        else
                        {
                            m1 += word + " ";
                        }
                    }
                    else
                    {
                        m2 += word + " ";
                    }

                };
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(650, 10), m1, GeometryFriends.XNAStub.Color.Black));
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(650, 40), m2, GeometryFriends.XNAStub.Color.Black));
                y = 30;
            }
            else
            {
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(650, 10), message1, GeometryFriends.XNAStub.Color.Black));
            }
            string message2 = "Estado: " + rectangle_state;
            if (message2.Length > maxLength)
            {
                string m1 = "";
                string m2 = "";
                foreach (string word in message2.Split(' '))
                {
                    if (m2.Length == 0)
                    {
                        if (m1.Length + 1 + word.Length > maxLength)
                        {
                            m2 += word + " ";
                        }
                        else
                        {
                            m1 += word + " ";
                        }
                    }
                    else
                    {
                        m2 += word + " ";
                    }

                };
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(650, y + 40), m1, GeometryFriends.XNAStub.Color.Black));
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(650, y + 70), m2, GeometryFriends.XNAStub.Color.Black));
                y = 30;
            }
            else
            {
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(650, y + 40), message2, GeometryFriends.XNAStub.Color.Black));
            }
        }

        public void ExplainabilitySystem(ref List<DebugInformation> newDebugInfo)
        {
            
            /*levelMapCircle.DrawConnections(ref newDebugInfo);
            levelMapRectangle.DrawConnections(ref newDebugInfo);*/
            //DrawLevelMap(ref newDebugInfo);
            DrawCollectibles(ref newDebugInfo);
            PlanDebug(ref newDebugInfo);
            DrawPanels(ref newDebugInfo);
            DrawCircle(ref newDebugInfo);
            DrawRectangle(ref newDebugInfo);
            DrawCircleExplanation(ref newDebugInfo);
            DrawRectangleExplanation(ref newDebugInfo);
        }

        private void UpArrow(float centerx, float centery, ref List<DebugInformation> newDebugInfo)
        {
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx - 5, centery + 20), new PointF(centerx - 5, centery), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx + 5, centery + 20), new PointF(centerx + 5, centery), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx + 5, centery + 20), new PointF(centerx - 5, centery+20), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx - 5, centery     ), new PointF(centerx - 15, centery), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx + 5, centery     ), new PointF(centerx + 15, centery), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx - 15, centery     ), new PointF(centerx, centery - 25), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx    , centery - 25), new PointF(centerx + 15, centery), GeometryFriends.XNAStub.Color.Black));
        }
        private void DownArrow(float centerx, float centery, ref List<DebugInformation> newDebugInfo)
        {
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx - 5, centery - 20), new PointF(centerx - 5, centery), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx + 5, centery - 20), new PointF(centerx + 5, centery), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx + 5, centery - 20), new PointF(centerx - 5, centery - 20), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx - 5, centery), new PointF(centerx - 15, centery), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx + 5, centery), new PointF(centerx + 15, centery), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx - 15, centery), new PointF(centerx, centery + 25), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx, centery + 25), new PointF(centerx + 15, centery), GeometryFriends.XNAStub.Color.Black));
        }
        private void RightArrow(float centerx, float centery, ref List<DebugInformation> newDebugInfo)
        {
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx - 20, centery + 5), new PointF(centerx , centery + 5), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx - 20, centery - 5), new PointF(centerx , centery - 5), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx - 20, centery - 5), new PointF(centerx - 20, centery + 5), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx, centery + 5), new PointF(centerx , centery + 15), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx , centery - 5), new PointF(centerx , centery - 15), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx , centery + 15), new PointF(centerx + 25, centery), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx + 25, centery ), new PointF(centerx , centery - 15), GeometryFriends.XNAStub.Color.Black));
        }
        private void LeftArrow(float centerx, float centery, ref List<DebugInformation> newDebugInfo)
        {
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx + 20, centery + 5), new PointF(centerx, centery + 5), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx + 20, centery - 5), new PointF(centerx, centery - 5), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx + 20, centery - 5), new PointF(centerx + 20, centery + 5), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx, centery + 5), new PointF(centerx, centery + 15), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx, centery - 5), new PointF(centerx, centery - 15), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx, centery + 15), new PointF(centerx - 25, centery), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(centerx - 25, centery), new PointF(centerx, centery - 15), GeometryFriends.XNAStub.Color.Black));
        }
    }
    
}
