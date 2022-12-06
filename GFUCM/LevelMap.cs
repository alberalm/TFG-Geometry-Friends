using GeometryFriends;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class LevelMap
    {
        public enum pixelType
        {
            EMPTY, OBSTACLE, DIAMOND, PLATFORM
        };
        public struct Platform
        {
            public int id;
            public int yTop;
            public int leftEdge;
            public int rightEdge;
            public List<MoveInformation> moveInfoList;

            public Platform(int id, int yTop, int leftEdge, int rightEdge, List<MoveInformation> moveInfoList)
            {
                this.id = id;
                this.yTop = yTop;
                this.leftEdge = leftEdge;
                this.rightEdge = rightEdge;
                this.moveInfoList = moveInfoList;
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

        private int[] COLLECTIBLE_SIZE = new int[] { 1, 2, 3, 3, 2, 1 };//Divided by 2

        public pixelType[,] levelMap = new pixelType[GameInfo.LEVEL_MAP_WIDTH , GameInfo.LEVEL_MAP_HEIGHT]; //x=i, y=j

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

        public pixelType[,] GetlevelMap()
        {
            return levelMap;
        }

        public void CreatelevelMap(CollectibleRepresentation[] colI, ObstacleRepresentation[] oI, ObstacleRepresentation[] cPI)
        {
            SetCollectibles(colI);

            SetDefaultObstacles();

            SetObstacles(oI);

            SetObstacles(cPI);

            IdentifyPlatforms(oI, cPI);
            PlatformUnion();
            //DEBUG
            String s = "\n";
            foreach (Platform p in platformList)
            {
   
                s += "Numero id = " + p.id+"\n";
                s += "      Left edge = " + p.leftEdge + "\n";
                s += "      Right edge = " + p.rightEdge + "\n";
                s += "      Ytop = " + p.yTop + "\n";
            }
            Log.LogInformation(s, true);
        }
        private void PlatformUnion()
        {
            foreach (Platform p1 in platformList)
            {
                foreach (Platform p2 in platformList)
                {
                    if (p1.yTop == p2.yTop)
                    {
                        if(p1.rightEdge+2== p2.leftEdge)//Union is needed
                        {
                            levelMap[p1.rightEdge + 1, p1.yTop] = pixelType.PLATFORM;

                            platformList.Add(new Platform(p1.id, p1.yTop, p1.leftEdge, p2.rightEdge, new List<MoveInformation>()));
                            platformList.Remove(p1);
                            platformList.Remove(p2);
                            //Each time two platforms are unified, process starts again (I don't know how foreach works when iterable is modified inside the construction)
                            PlatformUnion();
                            return;
                        }
                        if (p2.rightEdge + 2 == p1.leftEdge)//Union is needed
                        {
                            levelMap[p2.rightEdge + 1, p1.yTop] = pixelType.PLATFORM;

                            platformList.Add(new Platform(p1.id, p1.yTop, p2.leftEdge, p1.rightEdge, new List<MoveInformation>()));
                            platformList.Remove(p1);
                            platformList.Remove(p2);

                            PlatformUnion();
                            return;
                        }
                    }
                }
            }
            //No platform union is needed

            //Rename id
            int i = 0;
            List<Platform> newPlatformList = new List<Platform>();
            foreach (Platform p in platformList)
            {
                newPlatformList.Add(new Platform(i, p.yTop, p.leftEdge, p.rightEdge, new List<MoveInformation>()));
                i++;
            }
            platformList = newPlatformList;
        }
        private void IdentifyPlatforms(ObstacleRepresentation[] oI, ObstacleRepresentation[] cPI)
        {
            //TODO cPI
            platformList = new List<Platform>();
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
                    bool flag = false;
                    //Square=Cirle
                    for (int i = x-GameInfo.CIRCLE_RADIUS/ GameInfo.PIXEL_LENGTH; i<= x + GameInfo.CIRCLE_RADIUS/ GameInfo.PIXEL_LENGTH; i++)
                    {
                        for(int j=yTop-1; j> yTop-2* GameInfo.CIRCLE_RADIUS/ GameInfo.PIXEL_LENGTH; j--)
                        {
                            if(levelMap[i,j]==pixelType.OBSTACLE|| levelMap[i, j] == pixelType.PLATFORM)
                            {
                                flag = true;
                            }
                        }
                    }
                    if (!flag)
                    {
                        levelMap[x, yTop] = pixelType.PLATFORM;
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

            //Bottom obstacle
            prevPlatform = false;
            xleft = 0;
            for (int x = GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; x < GameInfo.LEVEL_MAP_WIDTH- GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; x++)
            {
                int yTop = GameInfo.LEVEL_MAP_HEIGHT - 5;
                bool flag = false;
                //Square=Cirle
                for (int i = x - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i <= x + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i++)
                {
                    for (int j = yTop - 1; j > yTop - 2 * GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; j--)
                    {
                        if (levelMap[i, j] == pixelType.OBSTACLE || levelMap[i, j] == pixelType.PLATFORM)
                        {
                            flag = true;
                        }
                    }
                }
                if (!flag)
                {
                    levelMap[x, yTop] = pixelType.PLATFORM;
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
                platformList.Add(new Platform(platformList.Count, GameInfo.LEVEL_MAP_HEIGHT - 5, xleft, GameInfo.LEVEL_MAP_WIDTH - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH-1, new List<MoveInformation>()));
            }

        }
        private void SetDefaultObstacles()
        {
            //Top obstacle
            for (int x = 0; x < GameInfo.LEVEL_MAP_WIDTH; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    levelMap[x, y] = pixelType.OBSTACLE;
                }
            }
            //Right obstacle
            for (int x = GameInfo.LEVEL_MAP_WIDTH-5; x < GameInfo.LEVEL_MAP_WIDTH; x++)
            {
                for (int y = 0; y < GameInfo.LEVEL_MAP_HEIGHT; y++)
                {
                    levelMap[x, y] = pixelType.OBSTACLE;
                }
            }
            //Bottom obstacle
            for (int x = 0; x < GameInfo.LEVEL_MAP_WIDTH; x++)
            {
                for (int y = GameInfo.LEVEL_MAP_HEIGHT-5; y < GameInfo.LEVEL_MAP_HEIGHT; y++)
                {
                    levelMap[x, y] = pixelType.OBSTACLE;
                }
            }
            //Left obstacle
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < GameInfo.LEVEL_MAP_HEIGHT; y++)
                {
                    levelMap[x, y] = pixelType.OBSTACLE;
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

                for (int x = xMap - width/2; x < (xMap + width/2); x++)
                {
                    for (int y = yMap- height / 2; y < (yMap + height/2); y++)
                    {
                        levelMap[x, y] = pixelType.OBSTACLE;
                        
                    }
                }
            }
        }

        private void SetCollectibles(CollectibleRepresentation[] colI)
        {

            foreach (CollectibleRepresentation d in colI)
            {
                int xMap = (int)(d.X / GameInfo.PIXEL_LENGTH);
                int yMap = (int)(d.Y / GameInfo.PIXEL_LENGTH);
                
                for (int x = xMap-3; x < xMap+3; x++)
                {
                    int i = x - xMap + 3;
                    for (int k =-COLLECTIBLE_SIZE[i]; k< COLLECTIBLE_SIZE[i]; k++)
                    {
                        levelMap[x, yMap + k] = pixelType.DIAMOND;
                        
                    }
                }
            }
        }


        public void debug(ref List<DebugInformation> debugInformation)
        {
            GeometryFriends.XNAStub.Color color = GeometryFriends.XNAStub.Color.Red;
            for (int x = 0; x < GameInfo.LEVEL_MAP_WIDTH; x++)
            {
                for (int y = 0; y < GameInfo.LEVEL_MAP_HEIGHT; y++)
                {

                    if (levelMap[x, y] == pixelType.OBSTACLE)
                    {
                        debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH-4, GameInfo.PIXEL_LENGTH-4), color));
                        change(ref color);

                    }
                    else if (levelMap[x, y] == pixelType.EMPTY)
                    {
                        //debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF position, Size size, XNAStub.Color color););
                    }
                    else if (levelMap[x, y] == pixelType.DIAMOND)
                    {//DIAMOND
                        debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.ForestGreen));

                    } else if(levelMap[x, y] == pixelType.PLATFORM)
                    {
                        debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Chocolate));

                    }
                }

            }

        }
        private void change(ref GeometryFriends.XNAStub.Color color)
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
