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
            Top, Right, Bottom, Left, Diamond, Other, Agent, None
            // Note: Diamond only if it does not intersect with other obstacle
        };

        public LevelMap()
        {
            platformList = new List<Platform>();
        }

        public enum PixelType
        {
            EMPTY, OBSTACLE, DIAMOND, PLATFORM, RECTANGLE
        };

        public List<Platform> platformList;
        public List<Platform> simplified_platforms = new List<Platform>();
        public Dictionary<Platform, List<Platform>> simplified_to_small = new Dictionary<Platform, List<Platform>>();
        public Dictionary<Platform, Platform> small_to_simplified = new Dictionary<Platform, Platform>();

        public PixelType[,] levelMap = new PixelType[GameInfo.LEVEL_MAP_WIDTH, GameInfo.LEVEL_MAP_HEIGHT]; //x=i, y=j

        public CollectibleRepresentation[] initialCollectiblesInfo;

        public MoveGenerator moveGenerator;

        public List<Platform> GetPlatforms()
        {
            return platformList;
        }

        public void CreateLevelMap(CollectibleRepresentation[] colI, ObstacleRepresentation[] oI, ObstacleRepresentation[] cPI, MoveGenerator moveGenerator = null)
        {
            SetCollectibles(colI);

            SetDefaultObstacles();

            SetObstacles(oI);

            SetObstacles(cPI);

            this.moveGenerator = new MoveGenerator(initialCollectiblesInfo, levelMap);

            /*if(moveGenerator == null)
            {
                this.moveGenerator = new MoveGenerator(initialCollectiblesInfo, levelMap);
            }
            else
            {
                this.moveGenerator = moveGenerator;
            }*/

            IdentifyPlatforms(oI);

            IdentifyPlatforms(cPI);

            IdentifyDefaultPlatforms();

            PlatformUnion();

            //GenerateMoveInformation();

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

        public abstract void GenerateMoveInformation(LevelMapRectangle levelMapRectangle);

        public abstract Platform GetPlatform(int x, int y);


        public void DrawConnections(ref List<DebugInformation> debugInformation)
        {
            foreach (Platform p in simplified_platforms)
            {
                foreach (MoveInformation m in p.moveInfoList)
                {
                    foreach (Tuple<float, float> tup in m.path)
                    {
                        if (m.moveType == MoveType.NOMOVE)
                        {
                            //debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 5, GeometryFriends.XNAStub.Color.LightSeaGreen));
                            continue;
                        }
                        if (m.departurePlatform.real && m.landingPlatform.real)
                        {
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
                                //debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 2, GeometryFriends.XNAStub.Color.LightSeaGreen));
                            }
                        }
                    }
                    //VisualDebug.DrawParabola(ref debugInformation, m.x * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, m.velocityX, m.isJump ? GameInfo.JUMP_VELOCITYY : 0, GeometryFriends.XNAStub.Color.DarkGreen);
                    //debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(m.xlandPoint * GameInfo.PIXEL_LENGTH, m.landingPlatform.yTop * GameInfo.PIXEL_LENGTH), 10, GeometryFriends.XNAStub.Color.DarkGray));
                }
            }
        }
    }
}
