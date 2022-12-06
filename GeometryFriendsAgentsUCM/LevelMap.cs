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
             EMPTY,OBSTACLE, DIAMOND
        };

       
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
        }

        private void SetDefaultObstacles()
        {
            //Bottom obstacle
            for (int x = 0; x < GameInfo.LEVEL_MAP_WIDTH; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    levelMap[x, y] = pixelType.OBSTACLE;
                }
            }
            //Right obstacle
            for (int x = GameInfo.LEVEL_MAP_WIDTH-3; x < GameInfo.LEVEL_MAP_WIDTH; x++)
            {
                for (int y = 0; y < GameInfo.LEVEL_MAP_HEIGHT; y++)
                {
                    levelMap[x, y] = pixelType.OBSTACLE;
                }
            }
            //Top obstacle
            for (int x = 0; x < GameInfo.LEVEL_MAP_WIDTH; x++)
            {
                for (int y = GameInfo.LEVEL_MAP_HEIGHT-3; y < GameInfo.LEVEL_MAP_HEIGHT; y++)
                {
                    levelMap[x, y] = pixelType.OBSTACLE;
                }
            }
            //Left obstacle
            for (int x = 0; x < 3; x++)
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
                    for (int k = 0; k< COLLECTIBLE_SIZE[i]; k++)
                    {
                        levelMap[x, yMap + k] = pixelType.DIAMOND;
                        levelMap[x, yMap - k] = pixelType.DIAMOND;
                    }
                }
            }
        }


       /* public void print()
        {
            //Log.LogInformation("Tamaño matriz: " + levelMap.GetLength(0).ToString() + "x" + levelMap.GetLength(1).ToString(), true);
            String s = "\n";
            for (int i = levelMap.GetLength(0) / 2; i < levelMap.GetLength(0); i++)
            {
                for (int j = 0; j < levelMap.GetLength(1); j++)
                {
                    if (levelMap[i, j] == -1)
                    {
                        s += "1";
                    }
                    else if (levelMap[i, j] == 0)
                    {
                        s += "0";
                    }
                    else
                    {
                        s += "1";
                    }

                }
                s += "\n";
            }
            Log.LogInformation(s, true);
        }*/

        public void debug(ref List<DebugInformation> debugInformation)
        {
            GeometryFriends.XNAStub.Color color = GeometryFriends.XNAStub.Color.Red;
            for (int x = 0; x < GameInfo.LEVEL_MAP_WIDTH; x++)
            {
                for (int y = 0; y < GameInfo.LEVEL_MAP_HEIGHT; y++)
                {

                    if (levelMap[x, y] == pixelType.OBSTACLE)
                    {
                        debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x* GameInfo.PIXEL_LENGTH, y* GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), color));
                        change(ref color);

                    }
                    else if (levelMap[x, y] == pixelType.EMPTY)
                    {
                        //debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF position, Size size, XNAStub.Color color););
                    }
                    else
                    {//DIAMOND
                        debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x* GameInfo.PIXEL_LENGTH, y* GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.ForestGreen));
        
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
