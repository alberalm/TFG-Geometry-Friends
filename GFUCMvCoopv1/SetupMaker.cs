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
                for (int x = p.leftEdge; x <= p.rightEdge; x++)
                {
                    debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, (p.yTop + 1) * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), color));
                }
                //debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH), p.id.ToString(), GeometryFriends.XNAStub.Color.Black));
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
                    new Size(20, 20), GeometryFriends.XNAStub.Color.Black));
                debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2 + 1, p.yTop * GameInfo.PIXEL_LENGTH + 9),
                    new Size(18, 18), GeometryFriends.XNAStub.Color.Green));
                debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH + 6),
                    p.id.ToString(), GeometryFriends.XNAStub.Color.Black));
            }
        }

        private void DrawCircleSimplifiedPlatforms(ref List<DebugInformation> debugInformation)
        {
            foreach (Platform p in levelMapCircle.simplified_platforms)
            {
                for (int x = p.leftEdge; x <= p.rightEdge; x++)
                {
                    if (p.real)
                    {
                        debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, p.yTop * GameInfo.PIXEL_LENGTH),
                            new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Chocolate));
                    }
                    else
                    {
                        foreach (Platform small in levelMapCircle.simplified_to_small[p])
                        {
                            for (int x1 = small.leftEdge; x1 <= small.rightEdge; x1++)
                            {
                                debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x1 * GameInfo.PIXEL_LENGTH, small.yTop * GameInfo.PIXEL_LENGTH),
                                    new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.LightPink));
                            }
                        }
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

        private void DrawCollectibles(ref List<DebugInformation> debugInformation)
        {
            int count = 0;
            foreach (CollectibleRepresentation c in levelMapRectangle.initialCollectiblesInfo)
            {
                debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(c.X - 5, c.Y - 5), count.ToString(), GeometryFriends.XNAStub.Color.Black));
                count++;
            }
        }
       
        public void DrawLevelMap(ref List<DebugInformation> debugInformation)
        {
            //Common obstacles
            //DrawObstacles(ref debugInformation);
            //DrawLegend(ref debugInformation);

            //Rectangle info
            //DrawRectangleSmallPlatforms(ref debugInformation);
            //DrawRectangleSimplifiedPlatformsNumbers(ref debugInformation);
            //DrawRectangleSmallPlatformsNumbers(ref debugInformation);

            //Circle info
            DrawCircleSimplifiedPlatforms(ref debugInformation);
            DrawCircleSimplifiedPlatformsNumbers(ref debugInformation);
            //DrawCircleSmallPlatformsNumbers(ref List < DebugInformation > debugInformation);

            //Collectibles
            //DrawCollectibles(ref debugInformation);
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
                if (actionSelectorCircle.move != null && actionSelectorRectangle.move != null)
                {
                    if (actionSelectorCircle.move.x == actionSelectorRectangle.move.x)
                    {
                        actionSelectorCircle.move.x++;
                    }
                    changing = Math.Sign(actionSelectorCircle.move.x - actionSelectorRectangle.move.x) != Math.Sign(circleInfo.X - rectangleInfo.X);
                }
            }
            else
            {
                changing = false;
            }
        }

        public void PlanDebug(ref List<DebugInformation> newDebugInfo)
        {
            int x = 900;
            int y = 50;
            int step = 1;
            newDebugInfo.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x, y), new Size(350, 25*(2* fullPlanCircle.Count+2)), GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x,y), "Plan del círculo", GeometryFriends.XNAStub.Color.Yellow));
            foreach (MoveInformation m in fullPlanCircle)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x, y+25*step),
                    step.ToString()+".- "+m.moveType.ToString()+" de P"+m.departurePlatform.id.ToString()+" a P" + m.landingPlatform.id.ToString(),
                    GeometryFriends.XNAStub.Color.Yellow));
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
            y = 50 + 25 * step;
            step = 1;
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x, y), "Plan del rectángulo", GeometryFriends.XNAStub.Color.Green));
            foreach (MoveInformation m in fullPlanRectangle)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(x, y + 25 * step),
                    step.ToString() + ".- " + m.moveType.ToString() + " de P" + m.departurePlatform.id.ToString() + " a P" + m.landingPlatform.id.ToString(),
                    GeometryFriends.XNAStub.Color.Green));
                foreach (Tuple<float, float> tup in m.path)
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 2, GeometryFriends.XNAStub.Color.Green));
                }
                if (m.path.Count > 0)
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(m.path[m.path.Count / 2].Item1 + 3, m.path[m.path.Count / 2].Item2 + 5), 16, GeometryFriends.XNAStub.Color.Black));
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(m.path[m.path.Count / 2].Item1 + 4, m.path[m.path.Count / 2].Item2 + 6), 14, GeometryFriends.XNAStub.Color.Green));
                    newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(m.path[m.path.Count / 2].Item1, m.path[m.path.Count / 2].Item2), step.ToString(), GeometryFriends.XNAStub.Color.Black));
                }
                step++;
            }
        }
    }
}
