using GeometryFriends;
using GeometryFriends.AI;
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

        public enum MoveType
        {
            JUMP, FALL, NOMOVE
        };

        public enum CircleCollisionType
        {
            Top, Right, Bottom, Left, Diamond, Other, None
            // Note: Diamond only if it does not intersect with other obstacle
            // TODO: Change this
        };

        public class Platform : IComparable<Platform>
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

            public List<int> ReachableCollectiblesLandingInThisPlatform()
            {
                List<int> rc = new List<int>();
                foreach(MoveInformation m in moveInfoList)
                {
                    if (m.departurePlatform.id == m.landingPlatform.id)
                    {
                        foreach (int d in m.diamondsCollected)
                        {
                            if (!rc.Contains(d))
                            {
                                rc.Add(d);
                            }
                        }
                    }
                }
                return rc;
            }

            public List<int> ReachableCollectiblesLandingInOtherPlatform()
            {
                List<int> rc = new List<int>();
                foreach (MoveInformation m in moveInfoList)
                {
                    if (m.departurePlatform.id != m.landingPlatform.id) 
                    { 
                        foreach (int d in m.diamondsCollected)
                        {
                            if (!rc.Contains(d))
                            {
                                rc.Add(d);
                            }
                        }
                    }
                }
                return rc;
            }

            // Returns 1 if this is less than other, -1 if other is less than this, 0 if equal
            public int CompareTo(Platform other)
            {
                if(this.yTop == other.yTop)
                {
                    return this.leftEdge.CompareTo(other.leftEdge);
                }
                return yTop.CompareTo(other.yTop);
            }
        }

        public class MoveInformation
        {
            public Platform landingPlatform;
            public Platform departurePlatform;
            public int x;
            public int xlandPoint;
            public int velocityX;
            public MoveType moveType;
            public List<int> diamondsCollected;
            public List<Tuple<float,float>> path;
            public int distanceToObstacle;
           
            public MoveInformation(Platform landingPlatform)
            {
                this.departurePlatform = null;
                this.landingPlatform = landingPlatform;
                this.x = 0;
                this.xlandPoint = 0;
                this.velocityX = 0;
                this.moveType = MoveType.NOMOVE;
                this.diamondsCollected = new List<int>();
                this.path = null;
                this.distanceToObstacle = 0;
            }

            public MoveInformation(Platform landingPlatform, Platform departurePlatform, int x, int xlandPoint, int velocityX, MoveType moveType, List<int> diamondsCollected, List<Tuple<float, float>> path, int distanceToObstacle)
            {
                this.departurePlatform = departurePlatform;
                this.landingPlatform = landingPlatform;
                this.x = x;
                this.xlandPoint = xlandPoint;
                this.velocityX = velocityX;
                this.moveType = moveType;
                this.diamondsCollected = diamondsCollected;
                this.path = path;
                this.distanceToObstacle = distanceToObstacle;
            }

            public int DistanceToRollingEdge()
            {
                if  (velocityX >= 0)
                {
                    return landingPlatform.rightEdge - xlandPoint;
                }
                else
                {
                    return xlandPoint - landingPlatform.leftEdge;
                }
            }

            public int DistanceToOtherEdge()
            {
                if (velocityX >= 0)
                {
                    return xlandPoint - landingPlatform.leftEdge;
                }
                else
                {
                    return landingPlatform.rightEdge - xlandPoint;
                }
            }

            private float Value()
            {
                int quarterPointLeft = (3 * landingPlatform.leftEdge + landingPlatform.rightEdge) / 4;
                int quarterPointRight = (landingPlatform.leftEdge + 3 * landingPlatform.rightEdge) / 4;
                int middleDeparture = (departurePlatform.leftEdge + departurePlatform.rightEdge) / 2;
                if (velocityX >= 0)
                {
                    return Math.Abs(xlandPoint - quarterPointLeft) / 3 + Math.Abs(velocityX) / 10 - distanceToObstacle + Math.Abs(x - middleDeparture) / 10;
                }
                else
                {
                    return Math.Abs(xlandPoint - quarterPointRight) / 3 + Math.Abs(velocityX) / 10 - distanceToObstacle + Math.Abs(x - middleDeparture) / 10;
                }
            }

            // Returns 1 is this is better, -1 if other is better, 0 if not clear or not comparable
            public int Compare(MoveInformation other, CollectibleRepresentation[] initialCollectiblesInfo)
            {
                // Here is where we filter movements
                if (landingPlatform.id != other.landingPlatform.id || departurePlatform.id != other.departurePlatform.id)
                {
                    return 0;
                }
                if (moveType == MoveType.NOMOVE && other.moveType == MoveType.NOMOVE && diamondsCollected[0] == other.diamondsCollected[0])
                {
                    if (Math.Abs(x - initialCollectiblesInfo[diamondsCollected[0]].X / GameInfo.PIXEL_LENGTH) < Math.Abs(other.x - initialCollectiblesInfo[diamondsCollected[0]].X / GameInfo.PIXEL_LENGTH))
                    {
                        return 1;
                    }
                    if (Math.Abs(x - initialCollectiblesInfo[diamondsCollected[0]].X / GameInfo.PIXEL_LENGTH) > Math.Abs(other.x - initialCollectiblesInfo[diamondsCollected[0]].X / GameInfo.PIXEL_LENGTH))
                    {
                        return -1;
                    }
                }
                if (moveType == MoveType.NOMOVE && other.diamondsCollected.Count==1 && diamondsCollected[0] == other.diamondsCollected[0] && other.landingPlatform==other.departurePlatform)
                {
                    // Other is a jump from platform x to platform x and it was only added because it could reach a diamond
                    // Now we have found that we can reach the same diamond without jumping, which will take us less time
                    return 1;
                }
                else if(other.moveType == MoveType.NOMOVE && diamondsCollected.Count == 1 && diamondsCollected[0] == other.diamondsCollected[0] && landingPlatform == departurePlatform)
                {
                    // Symmetric
                    return -1;
                }
                if (moveType == MoveType.NOMOVE) // In general, we want to store these moves, since they don't really afect other moves
                {
                    return 0;
                }
                if (Contained(diamondsCollected,other.diamondsCollected) && Contained(other.diamondsCollected,diamondsCollected)) //diamondsCollected=other.diamondsCollected
                {
                    int m = GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH;
                    /*if(other.landingPlatform.id == 4 && other.departurePlatform.id == 5)
                    {
                        int a = 1;
                    }*/
                    if(other.DistanceToOtherEdge()>m && other.DistanceToRollingEdge() > m && (DistanceToOtherEdge() <= m || DistanceToRollingEdge() <= m))
                    {
                        return -1;
                    }
                    if(other.DistanceToOtherEdge() <= m && DistanceToOtherEdge() <= m)
                    {
                        if(other.DistanceToOtherEdge() > DistanceToOtherEdge())
                        {
                            return -1;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                    if (DistanceToOtherEdge() > m && DistanceToRollingEdge() > m && (other.DistanceToOtherEdge() <= m || other.DistanceToRollingEdge() <= m))
                    {
                        return 1;
                    }
                    if (this.Value() < other.Value())
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else if (Contained(diamondsCollected, other.diamondsCollected) && !Contained(other.diamondsCollected, diamondsCollected))//diamondsCollected strictly contained in other.diamondsCollected
                {
                    return -1;
                }
                else if (!Contained(diamondsCollected, other.diamondsCollected) && Contained(other.diamondsCollected, diamondsCollected))//other.diamondsCollected strictly contained in diamondsCollected
                {
                    return 1;
                }
                else if (!Contained(diamondsCollected, other.diamondsCollected) && !Contained(other.diamondsCollected, diamondsCollected))//Incomparable
                {
                    return 0;
                }
                return 0;
            }
        }

        public List<Platform> platformList;

        private readonly int[] COLLECTIBLE_SIZE = new int[] { 1, 2, 3, 3, 2, 1 };//Divided by 2
        private readonly int[] CIRCLE_SIZE = new int[] { 3, 4, 5, 5, 5, 5, 5, 5, 4, 3 };//Divided by 2

        public PixelType[,] levelMap = new PixelType[GameInfo.LEVEL_MAP_WIDTH, GameInfo.LEVEL_MAP_HEIGHT]; //x=i, y=j

        public CollectibleRepresentation[] initialCollectiblesInfo;

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
        
        public static bool Contained<T>(List<T> l1, List<T> l2) //returns if l1 is contained in l2
        {
            foreach(T e in l1)
            {
                if (!l2.Contains(e))
                {
                    return false;
                }

            }
            return true;
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

        public List<Platform> GetPlatforms()
        {
            return platformList;
        }

        public Platform PlatformBelowCircle(CircleRepresentation cI)
        {
            for(int i=0; i<platformList.Count; i++)
            {
                if (cI.Y / GameInfo.PIXEL_LENGTH < platformList[i].yTop && cI.X / GameInfo.PIXEL_LENGTH >= platformList[i].leftEdge && cI.X / GameInfo.PIXEL_LENGTH <= platformList[i].rightEdge)
                {
                    return platformList[i];
                }
            }
            return new Platform(-1);
        }

        public Platform CirclePlatform(CircleRepresentation cI)
        {
            for (int i = 0; i < platformList.Count; i++)
            {
                if (cI.Y / GameInfo.PIXEL_LENGTH + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH <= platformList[i].yTop+1 && 
                    cI.Y / GameInfo.PIXEL_LENGTH + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH >= platformList[i].yTop-10 && 
                    cI.X / GameInfo.PIXEL_LENGTH >= platformList[i].leftEdge-1 && cI.X / GameInfo.PIXEL_LENGTH <= platformList[i].rightEdge+1)
                {
                    return platformList[i];
                }
            }
            return new Platform(-1);
        }

        public bool AtBorder(CircleRepresentation cI, Platform p, ref Moves currentAction)
        {
            if(p.id != -1)
            {
                if(Math.Abs(p.rightEdge - cI.X / GameInfo.PIXEL_LENGTH) <= 1) // Ball at right edge
                {
                    currentAction = Moves.ROLL_LEFT;
                    return true;
                }
                else if(Math.Abs(p.leftEdge - cI.X / GameInfo.PIXEL_LENGTH) <= 1) // Ball at left edge
                {
                    currentAction = Moves.ROLL_RIGHT;
                    return true;
                }
            }
            else
            {
                CircleRepresentation cI2 = new CircleRepresentation();
                cI2.X = cI.X - GameInfo.PIXEL_LENGTH;
                cI2.Y = cI.Y;
                if (CirclePlatform(cI2).id != -1) // Ball at right edge
                {
                    currentAction = Moves.ROLL_LEFT;
                    return true;
                }
                cI2.X = cI.X + GameInfo.PIXEL_LENGTH;
                if (CirclePlatform(cI2).id != -1) // Ball at left edge
                {
                    currentAction = Moves.ROLL_RIGHT;
                    return true;
                }
            }
            return false;
        }

        public void CreateLevelMap(CollectibleRepresentation[] colI, ObstacleRepresentation[] oI, ObstacleRepresentation[] cPI, bool phisics)
        {
            SetCollectibles(colI);

            SetDefaultObstacles();

            SetObstacles(oI);

            SetObstacles(cPI);

            IdentifyPlatforms(oI);

            IdentifyPlatforms(cPI);

            IdentifyDefaultPlatforms();

            PlatformUnion();

            GenerateMoveInformation(phisics);

            // DEBUG
            String s = "\n";
            s += "Number of platforms: " + platformList.Count.ToString() + "\n";
            for (int i = 0; i < platformList.Count; i++)
            {
                Platform p = platformList[i];
                s += "Platform id = " + p.id + "\n";
                s += "      Left edge = " + p.leftEdge + "\n";
                s += "      Right edge = " + p.rightEdge + "\n";
                s += "      Ytop = " + p.yTop + "\n";
                s += "      Moves = " + p.moveInfoList.Count+ "\n";
                foreach(MoveInformation m in p.moveInfoList)
                {
                    s += "           Type = " + m.moveType.ToString() + " X= " + m.x+ " VX= " + m.velocityX + " LandingPlatform= " + m.landingPlatform.id + " Collectibles caught= ";
                    foreach (int d in m.diamondsCollected)
                    {
                        s += d.ToString() + " ";
                    }
                    s += "\n";
                }
            }

            //Log.LogInformation(s, true);

            // DEBUG Trajectories
            /*s = "\n";
            s += "Number of platforms: " + platformList.Count.ToString() + "\n";
            for (int i = 0; i < graph.V; i++)
            {
                int j = 1;
                Platform p = platformList[i];
                foreach (MoveInformation m in p.moveInfoList)
                {
                    s += "      Movement " + j.ToString() + "\n";
                    s += "          Jump Point = (" + m.x + "," + p.yTop + ")" + "\n";
                    s += "          X Velocity = " + m.velocityX + "\n";
                    s += "          Land Platf = " + m.landingPlatform.id + "\n";
                    i++;
                }
            }
            Log.LogInformation(s, true);*/
            /*
            if (!graph.EveryCollectibleCanBeCollected())
            {
                // Advanced movements with collisions are required (TODO)
            }
            */
            // TODO: graph.SearchAlgorithm();
        }

        private void SetCollectibles(CollectibleRepresentation[] colI)
        {
            initialCollectiblesInfo = colI;
            foreach (CollectibleRepresentation d in colI)
            {
                int xMap = (int)(d.X / GameInfo.PIXEL_LENGTH);
                int yMap = (int)(d.Y / GameInfo.PIXEL_LENGTH);

                for (int x = xMap - 3; x < xMap + 3; x++)
                {
                    int i = x - xMap + 3;
                    for (int k = -COLLECTIBLE_SIZE[i]; k < COLLECTIBLE_SIZE[i]; k++)
                    {
                        if(x < GameInfo.LEVEL_MAP_WIDTH && x >= 0 && yMap + k < GameInfo.LEVEL_MAP_HEIGHT && yMap + k >= 0)
                        {
                            levelMap[x, yMap + k] = PixelType.DIAMOND;
                        }
                        
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
                        if (x < GameInfo.LEVEL_MAP_WIDTH && x >= 0 && y < GameInfo.LEVEL_MAP_HEIGHT && y >= 0)
                        {
                            levelMap[x, y] = PixelType.OBSTACLE;
                        }
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
                int leftEdge = xMap - width / 2;
                int rightEdge = xMap + width / 2 - 1;
                int yTop = yMap - height / 2;
                prevPlatform = false;

                for (int x = leftEdge; x <= rightEdge; x++)
                {
                    CircleCollisionType col = CircleIntersectsWithObstacle(x, yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH);
                    bool flag = col != CircleCollisionType.None && col != CircleCollisionType.Diamond;
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
                    platformList.Add(new Platform(platformList.Count, yTop, xleft, rightEdge, new List<MoveInformation>()));
                }
            }
        }

        private void IdentifyDefaultPlatforms()
        {
            //Bottom obstacle
            bool prevPlatform = false;
            int xleft = 0;
            for (int x = GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; x < GameInfo.LEVEL_MAP_WIDTH - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; x++)
            {
                int yTop = GameInfo.LEVEL_MAP_HEIGHT - 5;
                CircleCollisionType col = CircleIntersectsWithObstacle(x, yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH);
                bool flag = col != CircleCollisionType.None && col != CircleCollisionType.Diamond;
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
            for (int i = 0; i < platformList.Count(); i++)
            {
                Platform p1 = platformList[i];
                for (int j = i + 1; j < platformList.Count(); j++)
                {
                    Platform p2 = platformList[j];
                    bool join = false;
                    if (p1.yTop == p2.yTop)
                    {
                        if (p1.rightEdge + 1 == p2.leftEdge)
                        {
                            levelMap[p1.rightEdge + 1, p1.yTop] = PixelType.PLATFORM;
                            join = true;
                        }
                        else if (p2.rightEdge + 1 == p1.leftEdge)
                        {
                            levelMap[p2.rightEdge + 1, p1.yTop] = PixelType.PLATFORM;
                            join = true;
                        }
                        else if (p1.leftEdge >= p2.leftEdge && p1.leftEdge <= p2.rightEdge)
                        {
                            join = true;
                        }
                        else if (p2.leftEdge >= p1.leftEdge && p2.leftEdge <= p1.rightEdge)
                        {
                            join = true;
                        }
                        if (join)
                        {
                            p1.leftEdge = Math.Min(p1.leftEdge, p2.leftEdge);
                            p1.rightEdge = Math.Max(p1.rightEdge, p2.rightEdge);
                            platformList.Remove(p2);
                            i--;
                            break;
                        }
                    }
                }
            }
            // Rename id
            platformList.Sort();
            for (int i = 0; i < platformList.Count(); i++)
            {
                platformList[i].id = i;
            }
        }

        // Returns a value of type CircleCollision={Top, Right, Bottom, Left, Diamond, None}
        private CircleCollisionType CircleIntersectsWithObstacle(int x, int y) //(x,y) is the denter of the circle given in levelMap coordinates
        {
            bool diamond = false; // Obstacles and platforms have more priority than diamonds
            for (int i = -GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i < GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i++)
            {
                for (int j = -CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j < CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j++)
                {
                    if ((x + i) >= 0 && (y + j) >= 0 && (x + i) < levelMap.GetLength(0) && (y + j) < levelMap.GetLength(1)) // Check
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
                        else if (levelMap[x + i, y + j] == PixelType.DIAMOND)
                        {
                            diamond = true;
                        }
                    }
                }
            }
            if (diamond)
            {
                return CircleCollisionType.Diamond;
            }
            return CircleCollisionType.None;
        }

        private void GenerateMoveInformation(bool phisics)
        {
            int num_velocities, velocity_step;
            if (phisics)
            {
                num_velocities = GameInfo.NUM_VELOCITIES_PHISICS;
                velocity_step = GameInfo.VELOCITY_STEP_PHISICS;

            }
            else
            {
                num_velocities = GameInfo.NUM_VELOCITIES_QLEARNING;
                velocity_step = GameInfo.VELOCITY_STEP_QLEARNING;
            }
            for (int k = 0; k < platformList.Count; k++) {
                Platform p = platformList[k];

                // Parabolic FALLS
                Parallel.For(0, num_velocities, i =>
                {
                    int vx = (i + 1) * velocity_step;
                    if (EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, p.rightEdge + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH, vx))
                    {
                        AddTrajectory(ref p, vx, MoveType.FALL, p.rightEdge + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH);
                    }
                    if (EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, p.leftEdge - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH, -vx))
                    {
                        AddTrajectory(ref p, -vx, MoveType.FALL, p.leftEdge - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH);
                    }
                });

                // Parabolic JUMPS
                Parallel.For(p.leftEdge + 1, p.rightEdge, x =>
                {
                    Parallel.For(0, num_velocities + 1, i =>
                    {
                        int vx = i * velocity_step;
                        if (EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, x, vx))
                        {
                            AddTrajectory(ref p, vx, MoveType.JUMP, x);
                        }
                        if (EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, x, -vx))
                        {
                            AddTrajectory(ref p, -vx, MoveType.JUMP, x);
                        }
                    });
                });

                Parallel.For(p.leftEdge, p.rightEdge + 1, x =>
                {
                    AddTrajectory(ref p, 0, MoveType.NOMOVE, x);
                });
                
            }
        }

        private void AddTrajectory(ref Platform p, int vx, MoveType moveType, int x)
        {
            // Any trajectory with distance <= 10 should be safe to not collide (?)
            MoveInformation m = new MoveInformation(new Platform(-1), p, x, 0, vx, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);
            if (moveType==MoveType.JUMP)
            {
                SimulateMove(x * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, vx, (int)GameInfo.JUMP_VELOCITYY, ref m);
            }
            else if(moveType == MoveType.FALL)
            {
                SimulateMove(x * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, vx, 0, ref m);
            }
            else if (moveType == MoveType.NOMOVE)
            {
                if(CircleIntersectsWithObstacle(x,p.yTop- GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) == CircleCollisionType.Diamond)
                {
                    m.landingPlatform = p;
                    m.xlandPoint = x;
                    m.path.Add(new Tuple<float, float>(x * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH));
                    m.diamondsCollected.Add(GetDiamondCollected(x, p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH));
                }
            }

            lock (platformList)
            {
                bool addIt = true;
                if (m.landingPlatform.id != -2 && m.landingPlatform.id!=-1 && (m.diamondsCollected.Count > 0 || p.id != m.landingPlatform.id))
                {
                    for(int i = 0; i < p.moveInfoList.Count; i++)
                    {
                        int add = m.Compare(p.moveInfoList[i],initialCollectiblesInfo);
                        if(add == -1)
                        {
                            addIt = false;
                            break;
                        }
                        else if(add == 1)
                        {
                            addIt = false;
                            p.moveInfoList[i] = m;
                            //What if it is strictly better than more than one current move?
                            break;
                        }
                    }
                    if (addIt)
                    {
                        p.moveInfoList.Add(m);
                    }
                }
            }
        }

        private int GetDiamondCollected(int x, int y)//Check this function. Possible bugs
        {
            int min = 0;
            float d = 10;
            for (int i = 0; i < initialCollectiblesInfo.Length; i++)
            {
                CollectibleRepresentation coll = initialCollectiblesInfo[i];
                float dist = (float)(Math.Pow(coll.X / GameInfo.PIXEL_LENGTH - x, 2) + Math.Pow(coll.Y / GameInfo.PIXEL_LENGTH - y, 2)) / GameInfo.PIXEL_LENGTH;
                if (dist <= 5 + 3 / Math.Sqrt(2)) // Sufficiently close to be the diamond it collides with
                {
                    min = i;
                    break;
                }
                if (dist < d)
                {
                    min = i;
                    d = dist;
                }
            }
            
            return min;
        }

        private bool BelongsToPlatform(Platform platform, CollectibleRepresentation d)
        {
            return platform.yTop - d.Y / GameInfo.PIXEL_LENGTH <= 2 * GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH + COLLECTIBLE_SIZE[2]
                && platform.leftEdge <= d.X / GameInfo.PIXEL_LENGTH && platform.rightEdge >= d.X / GameInfo.PIXEL_LENGTH;
        }

        public void SimulateMove(float x_0, float y_0, float vx_0, float vy_0, ref MoveInformation m)
        {
            float dt = 0.005f;
            float t = 0;
            float x_tfloat = x_0;
            float y_tfloat = y_0;
            int x_t = (int)(x_0 / GameInfo.PIXEL_LENGTH); // Center of the ball
            int y_t = (int)(y_0 / GameInfo.PIXEL_LENGTH); // Center of the ball

            int NUM_REBOUNDS = 10;
            int j = 0;
            CircleCollisionType cct = CircleIntersectsWithObstacle(x_t, y_t);
            while (cct != CircleCollisionType.Bottom && j <= NUM_REBOUNDS)
            {
                m.path.Add(new Tuple<float, float>(x_tfloat, y_tfloat));
                if (cct == CircleCollisionType.Diamond)
                {
                    int d = GetDiamondCollected(x_t, y_t);
                    if (!m.diamondsCollected.Contains(d))
                    {
                        m.diamondsCollected.Add(d);
                    }
                }
                else if (cct == CircleCollisionType.Other)
                {
                    m.landingPlatform = new Platform(-2);
                    return;
                }
                else if (cct == CircleCollisionType.None)
                {

                }
                else //Left, Right or Top
                {
                    Tuple<float, float> new_v = NewVelocityAfterCollision(vx_0, (float)(vy_0 - GameInfo.GRAVITY * (t - dt)), cct);
                    x_0 = x_0 + vx_0 * (t - dt);
                    y_0 = y_0 - vy_0 * (t - dt) + (float)(GameInfo.GRAVITY * Math.Pow((t - dt), 2) / 2);
                    vx_0 = new_v.Item1;
                    vy_0 = new_v.Item2;
                    t = 0;
                    j++;
                }
                // Calculate if distance to obstacle is low, but only if obstacle is not below or above
                if(m.distanceToObstacle > (int)(GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH))
                {
                    for (int i = -m.distanceToObstacle; i < m.distanceToObstacle; i++)
                    {
                        for (int k = 1 - (int) (GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH); k < (int)(GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH); k++)
                        {
                            if (x_t + i >=0 && x_t + i< GameInfo.LEVEL_MAP_WIDTH && (levelMap[x_t + i, y_t + k] == PixelType.OBSTACLE || levelMap[x_t + i, y_t + k] == PixelType.PLATFORM))
                            {
                                if ((int)(i * i + k * k) < m.distanceToObstacle * m.distanceToObstacle)
                                {
                                    m.distanceToObstacle = (int)Math.Sqrt(i * i + k * k);
                                }
                            }
                        }
                    }
                }
                t += dt;
                x_tfloat = x_0 + vx_0 * t;
                y_tfloat = (float)(y_0 - vy_0 * t + GameInfo.GRAVITY * Math.Pow(t, 2) / 2);
                x_t = (int)(x_tfloat / GameInfo.PIXEL_LENGTH);
                y_t = (int)(y_tfloat / GameInfo.PIXEL_LENGTH);
                cct = CircleIntersectsWithObstacle(x_t, y_t);
            }
            if (cct != CircleCollisionType.Bottom)
            {
                m.landingPlatform = new Platform(-2);
                return;
            }
            m.xlandPoint = x_t;
            m.landingPlatform = GetPlatform(x_t, y_t);
            for(int i = 0; i < m.diamondsCollected.Count; i++)
            {
                int d = m.diamondsCollected[i];
                if(BelongsToPlatform(m.departurePlatform, initialCollectiblesInfo[d]) || BelongsToPlatform(m.landingPlatform, initialCollectiblesInfo[d])){
                    m.diamondsCollected.RemoveAt(i);
                    i--;
                }
            }
 
        }

        private Tuple<float, float> NewVelocityAfterCollision(float vx, float vy, CircleCollisionType cct) // Do not call this function with cct=other, bottom or none
        {
            // TODO -> Train an AI?
            if (cct == CircleCollisionType.Left || cct == CircleCollisionType.Right)
            {
                return new Tuple<float, float>(-vx / 3, vy / 3);
            }
            else if (cct == CircleCollisionType.Top)
            {
                return new Tuple<float, float>(vx / 3, -vy / 3);
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
                int j = 4;
                if (levelMap[x + i, y + j] == PixelType.PLATFORM)
                {
                    xcollide = x + i;
                    ycollide = y + j;
                    break;
                }
                if (levelMap[x + i, y + j] == PixelType.OBSTACLE)
                {
                    xcollide = x + i;
                    ycollide = y + j;
                }

            }
            foreach (Platform p in platformList)
            {
                if (p.yTop == ycollide && p.leftEdge <= xcollide && xcollide <= p.rightEdge)
                {
                    return p;
                }
            }
            //This point shouldn't be reachable. If it is, we can debug it
            int[] z = new int[] { 3, 4, 5, 5, 5, 5, 5, 5, 4, 3 };//Divided by 2
            //int aux = z[-1];
            //Bugs when the collision isn't whith the top of an obstacle aka platform 
            return new Platform(-2);
        }

        private bool EnoughSpaceToAccelerate(int leftEdge, int rigthEdge, int x, int vx)
        {
            if (vx > 0)
            {
                return vx * vx <= 2*GameInfo.ACCELERATION * GameInfo.PIXEL_LENGTH * (x - leftEdge-1);//Más conservador que como estaba
            }
            else
            {
                return vx * vx <= 2 * GameInfo.ACCELERATION * GameInfo.PIXEL_LENGTH * (rigthEdge-1 - x);//Más conservador que como estaba
            }
           
        }

        public void DrawLevelMap(ref List<DebugInformation> debugInformation)
        {
            for (int x = 0; x < GameInfo.LEVEL_MAP_WIDTH; x++)
            {
                for (int y = 0; y < GameInfo.LEVEL_MAP_HEIGHT; y++)
                {
                    if (levelMap[x, y] == PixelType.OBSTACLE)
                    {
                        if ((x+y)%2==0)
                        {
                            debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Red));
                        }
                        else{
                            debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.White));
                        }

                    }
                    else if (levelMap[x, y] == PixelType.EMPTY)
                    {
                        //debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF position, Size size, XNAStub.Color color););
                    }
                    else if (levelMap[x, y] == PixelType.DIAMOND)
                    {
                        debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Purple));

                    } else if (levelMap[x, y] == PixelType.PLATFORM)
                    {
                        debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Chocolate));
                    }
                }
            }

            foreach (Platform p in platformList)
            {
                debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF((p.leftEdge + p.rightEdge) * GameInfo.PIXEL_LENGTH / 2, p.yTop * GameInfo.PIXEL_LENGTH), p.id.ToString(), GeometryFriends.XNAStub.Color.Black));
            }
            int count = 0;
            foreach(CollectibleRepresentation c in initialCollectiblesInfo)
            {
                debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(c.X, c.Y), count.ToString(), GeometryFriends.XNAStub.Color.Black));
                count++;
            }
        }

        public void DrawConnections(ref List<DebugInformation> debugInformation)
        {
            foreach (Platform p in platformList)
            {
                foreach (MoveInformation m in p.moveInfoList)
                {

                    foreach (Tuple<float, float> tup in m.path)
                    {
                        if (m.moveType == MoveType.NOMOVE)
                        {
                            debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 5, GeometryFriends.XNAStub.Color.LightSeaGreen));
                            continue;
                        }
                        if (m.landingPlatform.id > p.id)
                        {
                            debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 2, GeometryFriends.XNAStub.Color.DarkGreen));
                        }
                        else if (m.landingPlatform.id < p.id)
                        {
                            debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 2, GeometryFriends.XNAStub.Color.GreenYellow));
                        }
                        else
                        {
                            debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 2, GeometryFriends.XNAStub.Color.LightSeaGreen));
                        }
                    }
                    //VisualDebug.DrawParabola(ref debugInformation, m.x * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, m.velocityX, m.isJump ? GameInfo.JUMP_VELOCITYY : 0, GeometryFriends.XNAStub.Color.DarkGreen);
                    //debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(m.xlandPoint * GameInfo.PIXEL_LENGTH, m.landingPlatform.yTop * GameInfo.PIXEL_LENGTH), 10, GeometryFriends.XNAStub.Color.DarkGray));

                }
            }

        }
    }
}
