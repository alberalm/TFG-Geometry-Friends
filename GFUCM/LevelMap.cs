using GeometryFriends;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    class LevelMap
    {
        public LevelMap()
        {
            platformList = new List<Platform>();
        }
        public enum PixelType
        {
            EMPTY, OBSTACLE, DIAMOND, PLATFORM
        };

        public enum CircleCollisionType
        {
            Top, Right, Bottom, Left, Other, None
        };
        public class Platform
        {
            public int id;
            public int yTop;
            public int leftEdge;
            public int rightEdge;
            public List<MoveInformation> moveInfoList;

            public Platform()
            {
            }

            public Platform(int id, int yTop, int leftEdge, int rightEdge, List<MoveInformation> moveInfoList)
            {
                this.id = id;
                this.yTop = yTop;
                this.leftEdge = leftEdge;
                this.rightEdge = rightEdge;
                this.moveInfoList = moveInfoList;
            }

            public Platform(int id)
            {
                this.id = id;
                this.yTop = 0;
                this.leftEdge = 0;
                this.rightEdge = 0;
                this.moveInfoList = null;
            }
        }
       
        public struct MoveInformation
        {
            public Platform landingPlatform;
            public int x;
            public int xlandPoint;
            public int velocityX;
            public bool isJump;

            public MoveInformation(Platform landingPlatform, int x, int xlandPoint, int velocityX, bool isJump)
            {
                this.landingPlatform = landingPlatform;
                this.x = x;
                this.xlandPoint = xlandPoint;
                this.velocityX = velocityX;
                this.isJump = isJump;
            }
        }

        List<Platform> platformList;
        Graph graph;

        private readonly int[] COLLECTIBLE_SIZE = new int[] { 1, 2, 3, 3, 2, 1 };//Divided by 2
        private readonly int[] CIRCLE_SIZE = new int[] { 3, 4, 5, 5, 5, 5, 5, 5, 4, 3};//Divided by 2
        private readonly int NUM_VELOCITIES = 10;
        private readonly int VELOCITY_STEP = 20;

        public PixelType[,] levelMap = new PixelType[GameInfo.LEVEL_MAP_WIDTH , GameInfo.LEVEL_MAP_HEIGHT]; //x=i, y=j

        public CollectibleRepresentation[] initialCollectiblesInfo;

        List<CircleRepresentation> trajectory = new List<CircleRepresentation>();

        public struct MapPoint
        {
            public int xMap;
            public int yMap;

            public MapPoint(int xMap, int yMap)
            {
                this.xMap = xMap;
                this.yMap = yMap;
            }
        }

        public struct Point
        {
            public int x;
            public int y;

            public Point(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        public static MapPoint ConvertPointIntoArrayPoint(Point value)
        {
            return new MapPoint(ConvertValue_PointIntoArrayPoint(value.x), ConvertValue_PointIntoArrayPoint(value.y));
        }

        public static Point ConvertArrayPointIntoPoint(MapPoint value)
        {
            return new Point(ConvertValue_ArrayPointIntoPoint(value.xMap), ConvertValue_ArrayPointIntoPoint(value.yMap));
        }

        public static int ConvertValue_PointIntoArrayPoint(int pointValue)
        {
            return pointValue / GameInfo.PIXEL_LENGTH;
        }

        public static int ConvertValue_ArrayPointIntoPoint(int arrayValue)
        {
            return arrayValue * GameInfo.PIXEL_LENGTH;
        }

        public PixelType[,] GetlevelMap()
        {
            return levelMap;
        }

        public void CreateLevelMap(CollectibleRepresentation[] colI, ObstacleRepresentation[] oI, ObstacleRepresentation[] cPI)
        {
            SetCollectibles(colI);

            SetDefaultObstacles();

            SetObstacles(oI);

            SetObstacles(cPI);

            IdentifyPlatforms(oI);

            IdentifyPlatforms(cPI);

            IdentifyDefaultPlatform();

            PlatformUnion();

            GenerateMoveInformation();

            // TODO: FilterPlatforms();

            graph = new Graph(platformList);

            // DEBUG
            String s = "\n";
            s += "Number of platforms: " + platformList.Count.ToString() + "\n";
            for (int i = 0; i < graph.V; i++)
            {
                Platform p = platformList[i];
                s += "Platform id = " + p.id + "\n";
                s += "      Left edge = " + p.leftEdge + "\n";
                s += "      Right edge = " + p.rightEdge + "\n";
                s += "      Ytop = " + p.yTop + "\n";
                s += "      Connected to platforms: ";
                foreach(Graph.Edge e in graph.adj[i])
                {
                    s += e.to + ", ";
                }
                s += "\n";
                /*int i = 1;
                foreach(MoveInformation m in p.moveInfoList)
                {
                    s += "      Movement " + i.ToString() + "\n";
                    s += "          Jump Point = (" + m.x + "," + p.yTop + ")" + "\n";
                    s += "          X Velocity = " + m.velocityX + "\n";
                    s += "          Land Platf = " + m.landingPlatform.id + "\n";
                    i++;
                }*/
            }
            //Log.LogInformation(s, true);

            // TODO: graph.SearchAlgorithm();
        }
        
        private void SetCollectibles(CollectibleRepresentation[] colI)
        {
            foreach (CollectibleRepresentation d in colI)
            {
                int xMap = (int)(d.X / GameInfo.PIXEL_LENGTH);
                int yMap = (int)(d.Y / GameInfo.PIXEL_LENGTH);

                for (int x = xMap - 3; x < xMap + 3; x++)
                {
                    int i = x - xMap + 3;
                    for (int k = -COLLECTIBLE_SIZE[i]; k < COLLECTIBLE_SIZE[i]; k++)
                    {
                        levelMap[x, yMap + k] = PixelType.DIAMOND;

                    }
                }
            }
        }

        private void SetDefaultObstacles()
        {
            // Top obstacle
            for (int x = 0; x < GameInfo.LEVEL_MAP_WIDTH; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    levelMap[x, y] = PixelType.OBSTACLE;
                }
            }
            // Right obstacle
            for (int x = GameInfo.LEVEL_MAP_WIDTH - 5; x < GameInfo.LEVEL_MAP_WIDTH; x++)
            {
                for (int y = 0; y < GameInfo.LEVEL_MAP_HEIGHT; y++)
                {
                    levelMap[x, y] = PixelType.OBSTACLE;
                }
            }
            // Bottom obstacle
            for (int x = 0; x < GameInfo.LEVEL_MAP_WIDTH; x++)
            {
                for (int y = GameInfo.LEVEL_MAP_HEIGHT - 5; y < GameInfo.LEVEL_MAP_HEIGHT; y++)
                {
                    levelMap[x, y] = PixelType.OBSTACLE;
                }
            }
            // Left obstacle
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < GameInfo.LEVEL_MAP_HEIGHT; y++)
                {
                    levelMap[x, y] = PixelType.OBSTACLE;
                }
            }
        }

        private void SetObstacles(ObstacleRepresentation[] oI)
        {
            foreach (ObstacleRepresentation o in oI)
            {
                int xMap = (int)(o.X / GameInfo.PIXEL_LENGTH);
                int yMap = (int)(o.Y / GameInfo.PIXEL_LENGTH);
                int height = (int)(o.Height / GameInfo.PIXEL_LENGTH);
                int width = (int)(o.Width / GameInfo.PIXEL_LENGTH);

                for (int x = xMap - width / 2; x < (xMap + width / 2); x++)
                {
                    for (int y = yMap - height / 2; y < (yMap + height / 2); y++)
                    {
                        levelMap[x, y] = PixelType.OBSTACLE;

                    }
                }
            }
        }

        private void IdentifyPlatforms(ObstacleRepresentation[] oI)
        {
            bool prevPlatform = false;
            int xleft = 0;
            foreach (ObstacleRepresentation o in oI)
            {
                int xMap = (int)(o.X / GameInfo.PIXEL_LENGTH);
                int yMap = (int)(o.Y / GameInfo.PIXEL_LENGTH);
                int height = (int)(o.Height / GameInfo.PIXEL_LENGTH);
                int width = (int)(o.Width / GameInfo.PIXEL_LENGTH);
                int leftEdge= xMap - width / 2 + 1;
                int rightEdge = xMap + width / 2 - 1;
                int yTop = yMap - height / 2;
                prevPlatform = false;

                for (int x = leftEdge; x <= rightEdge; x++)
                {
                    bool flag = CircleIntersectsWithObstacle(x,yTop-GameInfo.CIRCLE_RADIUS/GameInfo.PIXEL_LENGTH)!=CircleCollisionType.None;
                    
                    if (!flag)
                    {
                        levelMap[x, yTop] = PixelType.PLATFORM;
                        if (!prevPlatform)
                        {
                            xleft = x;
                        }
                        prevPlatform = true;
                    }
                    else
                    {
                        if (prevPlatform)
                        {
                            platformList.Add(new Platform(platformList.Count, yTop, xleft, x-1, new List<MoveInformation>()));
                        }
                        prevPlatform = false;
                    }
                }
                if (prevPlatform)
                {
                    platformList.Add(new Platform(platformList.Count, yTop, xleft, rightEdge, new List<MoveInformation>()));
                }
            }
        }

        private void IdentifyDefaultPlatform()
        {
            //Bottom obstacle
            bool prevPlatform = false;
            int xleft = 0;
            for (int x = GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; x < GameInfo.LEVEL_MAP_WIDTH - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; x++)
            {
                int yTop = GameInfo.LEVEL_MAP_HEIGHT - 5;
                bool flag = CircleIntersectsWithObstacle(x, yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) != CircleCollisionType.None;
                if (!flag)
                {
                    levelMap[x, yTop] = PixelType.PLATFORM;
                    if (!prevPlatform)
                    {
                        xleft = x;
                    }

                    prevPlatform = true;
                }
                else
                {
                    if (prevPlatform)
                    {
                        platformList.Add(new Platform(platformList.Count, yTop, xleft, x - 1, new List<MoveInformation>()));
                    }
                    prevPlatform = false;
                }

            }
            if (prevPlatform)
            {
                platformList.Add(new Platform(platformList.Count, GameInfo.LEVEL_MAP_HEIGHT - 5, xleft, GameInfo.LEVEL_MAP_WIDTH - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH - 1, new List<MoveInformation>()));
            }
        }

        private void PlatformUnion()
        {
            for(int i = 0; i < platformList.Count(); i++)
            {
                Platform p1 = platformList[i];
                for(int j = i+1; j < platformList.Count(); j++)
                {
                    Platform p2 = platformList[j];
                    if (p1.yTop == p2.yTop)
                    {
                        if (p1.rightEdge + 2 == p2.leftEdge) //Union is needed
                        {
                            levelMap[p1.rightEdge + 1, p1.yTop] = PixelType.PLATFORM;
                            p1.rightEdge = p2.rightEdge;
                            platformList.Remove(p2);
                            i--;
                            break;
                        }
                        if (p2.rightEdge + 2 == p1.leftEdge) //Union is needed
                        {
                            levelMap[p2.rightEdge + 1, p1.yTop] = PixelType.PLATFORM;
                            p1.leftEdge = p2.leftEdge;
                            platformList.Remove(p2);
                            i--;
                            break;
                        }
                    }
                }
            }
            // Rename id
            for(int i = 0; i < platformList.Count(); i++)
            {
                platformList[i].id = i;
            }
        }

        //I've changed it. Now it returns a value of type CircleCollision={Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left, TopLeft, None}
        private CircleCollisionType CircleIntersectsWithObstacle(int x, int y)
        {
            
            for (int i =- GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i < GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i++)
            {
                for (int j =- CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j < CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j++)
                {
                    if ((x+i)>=0 && (y + j)>=0 && (x + i) < levelMap.GetLength(0) && (y + j) < levelMap.GetLength(1))//Check
                    {
                        if (levelMap[x + i, y + j] == PixelType.OBSTACLE || levelMap[x + i, y + j] == PixelType.PLATFORM)
                        {
                            if (i == -5)
                            {
                                return CircleCollisionType.Left;
                            }
                            else if (i == 4)
                            {
                                return CircleCollisionType.Right;
                            }
                            else if (j == -5)
                            {
                                return CircleCollisionType.Top;
                            }
                            else if (j == 4)
                            {
                                return CircleCollisionType.Bottom;
                            }
                            else
                            {
                                return CircleCollisionType.Other;
                            }
                        }
                    }
                }
            }


            return CircleCollisionType.None;
        }

        private void GenerateMoveInformation()
        {
            foreach (Platform p in platformList) {
                //Parabolic JUMPS
                
                Parallel.For(p.leftEdge, p.rightEdge + 1, x =>
                {
                    Parallel.For(0, NUM_VELOCITIES, i =>
                    {
                        int vx = i * VELOCITY_STEP;

                        if (EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, x, vx))
                        {
                            Platform landingPlatform = new Platform();
                            int xlandPoint=0;
                            SimulateMove(x*GameInfo.PIXEL_LENGTH, (p.yTop-GameInfo.CIRCLE_RADIUS/GameInfo.PIXEL_LENGTH) *GameInfo.PIXEL_LENGTH, vx, (int) GameInfo.JUMP_VELOCITYY, ref landingPlatform, ref xlandPoint);
                            MoveInformation m = new MoveInformation(landingPlatform,x,xlandPoint,vx,true);
                            //Check whether move should be added
                            lock (platformList)
                            {
                                p.moveInfoList.Add(m);
                            }
                        }

                        if (EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, x, -vx))
                        {
                            Platform landingPlatform = new Platform();
                            int xlandPoint = 0;
                            SimulateMove(x * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, -vx, (int)GameInfo.JUMP_VELOCITYY, ref landingPlatform, ref xlandPoint);
                            MoveInformation m = new MoveInformation(landingPlatform, x, xlandPoint, -vx, true);
                            //Check whether move should be added
                            lock (platformList)
                            {
                                p.moveInfoList.Add(m);
                            }
                        }
  
                    });

                });
                //Parabolic FALLS
                Parallel.For(0, NUM_VELOCITIES, i =>
                {
                    int vx = i * VELOCITY_STEP;

                    if (EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, p.rightEdge, vx))
                    {
                        Platform landingPlatform = new Platform();
                        int xlandPoint = 0;
                        SimulateMove(p.rightEdge * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, vx, 0, ref landingPlatform, ref xlandPoint);
                        MoveInformation m = new MoveInformation(landingPlatform, p.rightEdge, xlandPoint, vx, false);
                        lock (platformList)
                        {
                            p.moveInfoList.Add(m);
                        }
                    }

                    if (EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, p.leftEdge, -vx))
                    {
                        Platform landingPlatform = new Platform();
                        int xlandPoint = 0;
                        SimulateMove(p.leftEdge * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, -vx, 0, ref landingPlatform, ref xlandPoint);
                        MoveInformation m = new MoveInformation(landingPlatform, p.leftEdge, xlandPoint, -vx, false);
                        lock (platformList)
                        {
                            p.moveInfoList.Add(m);
                        }
                    }
                });
                //No move?
            }
        }

        private void SimulateMove(float x_0, float y_0, float vx_0, float vy_0, ref Platform landingPlatform, ref int xlandPoint)
        {
            float t = 0;
            int x_t = (int)(x_0/GameInfo.PIXEL_LENGTH);
            int y_t = (int)(y_0 / GameInfo.PIXEL_LENGTH);
            
            CircleCollisionType cct = CircleIntersectsWithObstacle(x_t, y_t);
            while (cct==CircleCollisionType.None)
            {
                t += 0.01f;
                x_t = (int)((x_0 + vx_0 * t)/GameInfo.PIXEL_LENGTH);
                y_t = (int)((y_0 - vy_0 * t + GameInfo.GRAVITY * Math.Pow(t, 2) / 2)/GameInfo.PIXEL_LENGTH);
                cct = CircleIntersectsWithObstacle(x_t, y_t);
            }

            if (cct == CircleCollisionType.Bottom)
            {
                xlandPoint = x_t;
                landingPlatform = GetPlatform(x_t, y_t);
                
            }
            else if (cct == CircleCollisionType.Other)
            {
                
            }
            else  //Left, Right or Top
            {
                Tuple<float, float> new_v = newVelocityAfterCollision(vx_0, (float)(vy_0 - GameInfo.GRAVITY * (t - 0.01f)), cct);

                SimulateMove(x_0 + vx_0 * (t - 0.01f), y_0 - vy_0 * (t - 0.01f) + (float)(GameInfo.GRAVITY * Math.Pow((t - 0.01f), 2) / 2), new_v.Item1, new_v.Item2, ref landingPlatform, ref xlandPoint);
                
            }
            
            
        }


        private Tuple<float, float> newVelocityAfterCollision(float vx, float vy, CircleCollisionType cct)//Don't call this function with cct=other, botton or none
        {
            if(cct==CircleCollisionType.Left|| cct == CircleCollisionType.Right)
            {
                return new Tuple<float, float>(-vx / 3, vy/3); //Adjust constants
            }
            else if (cct == CircleCollisionType.Top)
            {
                return new Tuple<float, float>(vx * GameInfo.VERTICAL_COLLISION_COEFFICIENT, -vy* GameInfo.VERTICAL_COLLISION_COEFFICIENT); //ToDO Vx coeff
            }
            else
            {
                return new Tuple<float, float>(0, 0);
            }
        }
        private Platform GetPlatform(int x, int y)
        {
            int xcollide = 0;
            int ycollide = 0;
            for (int i = -GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i < GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i++)
            {
                for (int j = -CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j < CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j++)
                {
                    if (levelMap[x + i, y + j] == PixelType.OBSTACLE || levelMap[x + i, y + j] == PixelType.PLATFORM)
                    {
                        xcollide = x + i;
                        ycollide = y + j;
                    }
                }
            }
            foreach (Platform p in platformList)
            {
                if(p.yTop == ycollide && p.leftEdge<= xcollide && xcollide <= p.rightEdge)
                {
                    return p;
                }
            }
            //Bugs when the collision isn't whith the top of an obstacle aka platform 
            return new Platform(-1);
        }

        private bool EnoughSpaceToAccelerate(int leftEdge, int rigthEdge, int x, int vx)
        {
            //TODO
            return true;
        }
 
        public void Debug(ref List<DebugInformation> debugInformation, CircleRepresentation circleInfo)
        {
            debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X, circleInfo.Y), 4, GeometryFriends.XNAStub.Color.Red));

            GeometryFriends.XNAStub.Color color = GeometryFriends.XNAStub.Color.Red;
            for (int x = 0; x < GameInfo.LEVEL_MAP_WIDTH; x++)
            {
                for (int y = 0; y < GameInfo.LEVEL_MAP_HEIGHT; y++)
                {

                    if (levelMap[x, y] == PixelType.OBSTACLE)
                    {
                        debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH-4, GameInfo.PIXEL_LENGTH-4), color));
                        Change(ref color);

                    }
                    else if (levelMap[x, y] == PixelType.EMPTY)
                    {
                        //debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF position, Size size, XNAStub.Color color););
                    }
                    else if (levelMap[x, y] == PixelType.DIAMOND)
                    {//DIAMOND
                        debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.ForestGreen));

                    } else if(levelMap[x, y] == PixelType.PLATFORM)
                    {
                        debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Chocolate));
                    }
                }

            }
            
            for (int i = -GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i < GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i++)
            {
                for (int j = -CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j < CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j++)
                {
                    int x = (int)(circleInfo.X/GameInfo.PIXEL_LENGTH);
                    int y = (int)(circleInfo.Y/GameInfo.PIXEL_LENGTH);
                    debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF((x+i) * GameInfo.PIXEL_LENGTH, (y+j) * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.YellowGreen));
                }
            }
            //Trajectory
            trajectory.Add(circleInfo);
            int numCollisions = 0;
            for (int i= 0; i< trajectory.Count; i++)
            {
                CircleRepresentation cI = trajectory[i];
                debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(cI.X, cI.Y), 4, GeometryFriends.XNAStub.Color.Red));
                if ((cI.VelocityX > 0 && i+1 < trajectory.Count && trajectory[i+1].VelocityX<0) || (cI.VelocityX < 0 && i + 1 < trajectory.Count && trajectory[i + 1].VelocityX > 0) && Math.Abs(cI.VelocityX)>30 && Math.Abs(trajectory[i + 1].VelocityX) > 30)
                {
                    string s = "";
                    s += "\nJusto antes del choque: \n";
                    s += "  VX=" + cI.VelocityX.ToString() + " VY=" + cI.VelocityY.ToString() + "\n";
                    s += "\nJusto despues del choque: \n";
                    s += "  VX=" + trajectory[i + 1].VelocityX.ToString() + " VY=" + trajectory[i + 1].VelocityY.ToString() + "\n";

                    debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(numCollisions*5+200, GameInfo.LEVEL_HEIGHT / 2+30*cI.VelocityX/trajectory[i+1].VelocityX), 4 ,GeometryFriends.XNAStub.Color.Cyan));

                    numCollisions++;
                }

                debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(i-trajectory.Count+ GameInfo.LEVEL_WIDTH/2, GameInfo.LEVEL_HEIGHT/2-trajectory[i].VelocityX), 4, GeometryFriends.XNAStub.Color.Purple));
            }
            debugInformation.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(0, GameInfo.LEVEL_HEIGHT / 2), new PointF(GameInfo.LEVEL_WIDTH, GameInfo.LEVEL_HEIGHT / 2), GeometryFriends.XNAStub.Color.Cyan));


            VisualDebug.DrawArrow(ref debugInformation, circleInfo.X, circleInfo.Y,(int)(circleInfo.VelocityX/5), (int)(circleInfo.VelocityY/5), GeometryFriends.XNAStub.Color.Green);
            debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X, circleInfo.Y), 4, GeometryFriends.XNAStub.Color.Red));

            foreach (Platform p in platformList)
            {
                foreach (MoveInformation m in p.moveInfoList)
                {
                    //ToDO: Parabollas should be drawn only once and not each time Update method is called
                    if (p.id==0 && p.leftEdge+20==m.x && m.velocityX==-100) // Strong conditions should be imposed because there are too many parabollas to draw
                    {
                        VisualDebug.DrawParabola(ref debugInformation, m.x * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, m.velocityX, m.isJump ? GameInfo.JUMP_VELOCITYY : 0, GeometryFriends.XNAStub.Color.DarkGreen);
                        debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(m.xlandPoint * GameInfo.PIXEL_LENGTH, m.landingPlatform.yTop * GameInfo.PIXEL_LENGTH), 10, GeometryFriends.XNAStub.Color.DarkGray));
                    }
                }
            }
        }

        private void Change(ref GeometryFriends.XNAStub.Color color)
        {
            if (color == GeometryFriends.XNAStub.Color.Red)
            {
                color = GeometryFriends.XNAStub.Color.White;
            }
            else
            {
                color = GeometryFriends.XNAStub.Color.Red;
            }
        }
    }
}
