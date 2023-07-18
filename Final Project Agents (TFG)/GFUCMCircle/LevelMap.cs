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
    public abstract class LevelMap
    {
        public enum CollisionType
        {
            Top, Right, Bottom, Left, Diamond, Other, None
            // Note: Diamond only if it does not intersect with other obstacle
        };

        public LevelMap()
        {
            platformList = new List<Platform>();
        }

        public enum PixelType
        {
            EMPTY, OBSTACLE, DIAMOND, PLATFORM
        };

        public List<Platform> platformList;

        public PixelType[,] levelMap = new PixelType[GameInfo.LEVEL_MAP_WIDTH, GameInfo.LEVEL_MAP_HEIGHT]; //x=i, y=j

        public CollectibleRepresentation[] initialCollectiblesInfo;

        public List<Platform> GetPlatforms()
        {
            return platformList;
        }

        public void CreateLevelMap(CollectibleRepresentation[] colI, ObstacleRepresentation[] oI, ObstacleRepresentation[] cPI)
        {
            SetCollectibles(colI);

            SetDefaultObstacles();

            SetObstacles(oI);

            SetObstacles(cPI);

            IdentifyPlatforms(oI);

            IdentifyPlatforms(cPI);

            IdentifyDefaultPlatforms();

            PlatformUnion();

            GenerateMoveInformation();
                       
        }

        protected void SetCollectibles(CollectibleRepresentation[] colI)
        {
            initialCollectiblesInfo = colI;
            foreach (CollectibleRepresentation d in colI)
            {
                int xMap = (int)(d.X / GameInfo.PIXEL_LENGTH);
                int yMap = (int)(d.Y / GameInfo.PIXEL_LENGTH);

                for (int x = xMap - 3; x < xMap + 3; x++)
                {
                    int i = x - xMap + 3;
                    for (int k = -GameInfo.COLLECTIBLE_SIZE[i]; k < GameInfo.COLLECTIBLE_SIZE[i]; k++)
                    {
                        if(x < GameInfo.LEVEL_MAP_WIDTH && x >= 0 && yMap + k < GameInfo.LEVEL_MAP_HEIGHT && yMap + k >= 0)
                        {
                            levelMap[x, yMap + k] = PixelType.DIAMOND;
                        }
                        
                    }
                }
            }
        }

        protected void SetDefaultObstacles()
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

        protected void SetObstacles(ObstacleRepresentation[] oI)
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

        protected abstract void IdentifyPlatforms(ObstacleRepresentation[] oI);

        protected abstract void IdentifyDefaultPlatforms();

        protected abstract void PlatformUnion();

        protected abstract void GenerateMoveInformation();

        protected bool BelongsToPlatform(Platform platform, CollectibleRepresentation d)
        {
            return platform.yTop - d.Y / GameInfo.PIXEL_LENGTH <= 2 * GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH + GameInfo.COLLECTIBLE_SIZE[2]
                && platform.leftEdge <= d.X / GameInfo.PIXEL_LENGTH && platform.rightEdge >= d.X / GameInfo.PIXEL_LENGTH;
        }

        public abstract Platform GetPlatform(int x, int y);

        protected abstract bool EnoughSpaceToAccelerate(int leftEdge, int rigthEdge, int x, int vx);



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
                        else
                        {
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

                    }
                    else if (levelMap[x, y] == PixelType.PLATFORM)
                    {
                        debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, y * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Chocolate));
                    }
                }
            }

            debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(0, 0), new Size(GameInfo.LEVEL_WIDTH, 5 * GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.Black));

            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(50, 10), "None", GeometryFriends.XNAStub.Color.Chocolate));
            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(125, 10), "Square", GeometryFriends.XNAStub.Color.Brown));
            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(225, 10), "Horizontal", GeometryFriends.XNAStub.Color.Purple));
            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(350, 10), "Square+Horizontal", GeometryFriends.XNAStub.Color.Orange));
            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(550, 10), "Vertical", GeometryFriends.XNAStub.Color.Yellow));
            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(675, 10), "Square+Vertical", GeometryFriends.XNAStub.Color.Green));
            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(850, 10), "Vertical+Horizontal", GeometryFriends.XNAStub.Color.Red));
            debugInformation.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(1100, 10), "All", GeometryFriends.XNAStub.Color.Blue));

            foreach (Platform p in platformList)
            {
                GeometryFriends.XNAStub.Color color = GeometryFriends.XNAStub.Color.Black; 
                int suma = 0;
                for(int i = 0; i < 3; i++)
                {
                    suma += p.shapes[i] ? (int) (Math.Pow(2, i)) : 0;
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
                    debugInformation.Add(DebugInformationFactory.CreateRectangleDebugInfo(new PointF(x * GameInfo.PIXEL_LENGTH, p.yTop * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), color));
                }
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
