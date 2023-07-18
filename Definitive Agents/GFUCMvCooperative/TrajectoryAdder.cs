using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static GeometryFriendsAgents.LevelMap;
using static GeometryFriendsAgents.RectangleShape;

namespace GeometryFriendsAgents
{
    public class TrajectoryAdder
    {
        public CollectibleRepresentation[] initialCollectiblesInfo;
        public PixelType[,] levelMap;
        public RectangleSimulator rectangleSimulator;
        public CircleSimulator circleSimulator;

        public TrajectoryAdder(CollectibleRepresentation[] initialCollectiblesInfo, PixelType[,] levelMap)
        {
            this.initialCollectiblesInfo = initialCollectiblesInfo;
            this.levelMap = levelMap;
            this.rectangleSimulator = new RectangleSimulator(initialCollectiblesInfo, levelMap);
            this.circleSimulator = new CircleSimulator(initialCollectiblesInfo, levelMap);
        }

        private double AngularVelocity(double vx_0)
        {
            // if vx_0 = 200 -> return 0.5
            // if vx_0 = 100 -> return 1.25
            // if vx_0 = 300 -> return 0.3125
            // if vx_0 = 400 -> return 0.28

            if (vx_0 > 0)
            {
                return 204.35f / Math.Pow(vx_0, 1.12);
            }
            else if (vx_0 < 0)
            {
                return -AngularVelocity(-vx_0);
            }
            else
            {
                return 0;
            }
        }

        private CollisionType RectangleIntersectsWithObstacle(int x, int y, RectangleShape.Shape s)//x is the center of the rectangle and y is the base of the rectangle
        {
            bool diamond = false; // Obstacles and platforms have more priority than diamonds
            for (int i = -RectangleShape.width(s) / 2; i <= RectangleShape.width(s) / 2; i++)
            {
                for (int j = 1; j <= RectangleShape.height(s); j++)
                {
                    if ((x + i) >= 0 && (y - j) >= 0 && (x + i) < levelMap.GetLength(0) && (y - j) < levelMap.GetLength(1))
                    {
                        if (levelMap[x + i, y - j] == PixelType.OBSTACLE || levelMap[x + i, y - j] == PixelType.PLATFORM)
                        {
                            if (j == 0)
                            {
                                return CollisionType.Bottom;
                            }
                            else if (j == RectangleShape.height(s) - 1)
                            {
                                return CollisionType.Top;
                            }
                            else if (i == -RectangleShape.width(s) / 2)
                            {
                                return CollisionType.Left;
                            }
                            else if (i == RectangleShape.width(s) / 2 - 1)
                            {
                                return CollisionType.Right;
                            }

                        }
                        else if (levelMap[x + i, y - j] == PixelType.DIAMOND)
                        {
                            diamond = true;
                        }
                    }
                }
            }
            if (diamond)
            {
                return CollisionType.Diamond;
            }
            return CollisionType.None;
        }

        public void AddToPlatform(ref List<Platform> platformList, ref Platform p, ref MoveInformation m,
            Func<MoveInformation, CollectibleRepresentation[], List<Platform>, int> method)
        {
            lock (platformList)
            {
                bool addIt = true;
                if (m.landingPlatform.id != -2 && m.landingPlatform.id != -1 && (m.diamondsCollected.Count > 0 || p.id != m.landingPlatform.id))
                {
                    for (int i = 0; i < p.moveInfoList.Count; i++)
                    {
                        int add = method(p.moveInfoList[i], initialCollectiblesInfo, platformList);
                        if (add == -1)
                        {
                            addIt = false;
                            break;
                        }
                        else if (add == 1)
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

        public void AddAdjacent(ref List<Platform> platformList, ref Platform p, int vx, int x, RectangleShape.Shape s, Platform landing)
        {
            MoveInformation m = new MoveInformation(landing, p, x, 0, vx, MoveType.ADJACENT, new List<int>(), new List<Tuple<float, float>>(), 10)
            {
                xlandPoint = x
            };

            m.path.Add(new Tuple<float, float>(x * GameInfo.PIXEL_LENGTH, (p.yTop - RectangleShape.height(s) / 2) * GameInfo.PIXEL_LENGTH));
            m.shape = s;

            AddToPlatform(ref platformList, ref p, ref m, m.CompareRectangle);
        }

        public void AddDrop(ref List<Platform> platformList, ref Platform p, int vx, MoveType moveType, int x, RectangleShape.Shape s, Platform landing)
        {
            MoveInformation m = new MoveInformation(landing, p, x, 0, vx, moveType, new List<int>(), new List<Tuple<float, float>>(), 10)
            {
                xlandPoint = x,
                shape = s
            };

            int collisionY = GameInfo.LEVEL_MAP_HEIGHT;
            // Top, Right, Bottom, Left, Diamond, Other, None
            foreach (Platform platform in platformList)
            {
                if (platform.leftEdge <= x && platform.rightEdge >= x && platform.yTop < collisionY && platform.yTop > p.yTop)
                {
                    collisionY = platform.yTop;
                    m.landingPlatform = platform;
                }
            }
            for (int y = p.yTop; y < collisionY; y++)
            {
                foreach (RectangleShape.Shape sh in GameInfo.SHAPES)
                {
                    if (RectangleIntersectsWithObstacle(x, y, sh) == CollisionType.Diamond)
                    {
                        List<int> catched = rectangleSimulator.GetDiamondCollected(x, y, sh);
                        foreach (int d in catched)
                        {
                            if (!m.diamondsCollected.Contains(d))
                            {
                                m.diamondsCollected.Add(d);
                            }
                        }
                    }
                }
                m.path.Add(new Tuple<float, float>(x * GameInfo.PIXEL_LENGTH, (y - RectangleShape.height(s) / 2) * GameInfo.PIXEL_LENGTH));
            }
            AddToPlatform(ref platformList, ref p, ref m, m.CompareRectangle);
        }

        public void AddTilt(ref List<Platform> platformList, ref Platform p, int vx, MoveType moveType, int x, RectangleShape.Shape s, Platform landing)
        {
            MoveInformation m = new MoveInformation(landing, p, x, 0, vx, moveType, new List<int>(), new List<Tuple<float, float>>(), 10)
            {
                xlandPoint = vx > 0 ? landing.leftEdge : landing.rightEdge,
                shape = s,
                risky = Math.Abs(vx) > 1
            };

            m.path.Add(new Tuple<float, float>(x * GameInfo.PIXEL_LENGTH, (p.yTop - RectangleShape.height(s) / 2) * GameInfo.PIXEL_LENGTH));

            AddToPlatform(ref platformList, ref p, ref m, m.CompareRectangle);
        }

        public void AddMonoSideDrop(ref List<Platform> platformList, ref Platform p, int vx, MoveType moveType, int x, RectangleShape.Shape s, Platform landing)
        {
            AddDrop(ref platformList, ref p, vx, moveType, x, s, landing);
        }

        public void AddBigHoleAdj(ref List<Platform> platformList, ref Platform p, int vx, MoveType moveType, int x, RectangleShape.Shape s, Platform landing)
        {
            MoveInformation m = new MoveInformation(landing, p, x, 0, vx, moveType, new List<int>(), new List<Tuple<float, float>>(), 10)
            {
                shape = s,
                xlandPoint = vx < 0 ? landing.rightEdge : landing.leftEdge
            };

            m.path.Add(new Tuple<float, float>(x * GameInfo.PIXEL_LENGTH, (p.yTop - RectangleShape.height(s) / 2) * GameInfo.PIXEL_LENGTH));
            int max_x = m.velocityX < 0 ? m.departurePlatform.leftEdge : m.landingPlatform.leftEdge;
            int min_x = m.velocityX > 0 ? m.departurePlatform.rightEdge : m.landingPlatform.rightEdge;
            for (int d = 0; d < initialCollectiblesInfo.Length; d++)
            {
                CollectibleRepresentation diamond = initialCollectiblesInfo[d];

                if (diamond.X / GameInfo.PIXEL_LENGTH >= min_x && diamond.X / GameInfo.PIXEL_LENGTH <= max_x &&
                    (diamond.Y + GameInfo.SEMI_COLLECTIBLE_HEIGHT) / GameInfo.PIXEL_LENGTH >= m.departurePlatform.yTop - RectangleShape.height(RectangleShape.Shape.VERTICAL)
                    && (diamond.Y - GameInfo.SEMI_COLLECTIBLE_HEIGHT) / GameInfo.PIXEL_LENGTH <= m.departurePlatform.yTop)
                {
                    if (!m.diamondsCollected.Contains(d))
                    {
                        m.diamondsCollected.Add(d);
                    }
                }
            }

            AddToPlatform(ref platformList, ref p, ref m, m.CompareRectangle);
        }

        public void AddBigHoleDrop(ref List<Platform> platformList, ref Platform p, int vx, MoveType moveType, int x, RectangleShape.Shape s, Platform landing)
        {
            MoveInformation m = new MoveInformation(landing, p, x, 0, vx, moveType, new List<int>(), new List<Tuple<float, float>>(), 10)
            {
                x = vx > 0 ? p.rightEdge : p.leftEdge,
                risky = true
            };

            int collisionY = GameInfo.LEVEL_MAP_HEIGHT;
            // Top, Right, Bottom, Left, Diamond, Other, None
            foreach (Platform platform in platformList)
            {
                if (platform.leftEdge <= x && platform.rightEdge >= x && platform.yTop < collisionY && platform.yTop > p.yTop)
                {
                    collisionY = platform.yTop;
                    m.landingPlatform = platform;
                }
            }
            for (int y = p.yTop; y < collisionY; y++)
            {
                m.path.Add(new Tuple<float, float>(x * GameInfo.PIXEL_LENGTH, (y - RectangleShape.height(s) / 2) * GameInfo.PIXEL_LENGTH));
            }

            AddToPlatform(ref platformList, ref p, ref m, m.CompareRectangle);
        }

        public void AddNoMoveR(ref List<Platform> platformList, ref Platform p, int vx, MoveType moveType, int x, RectangleShape.Shape s, Platform landing)
        {
            MoveInformation m = new MoveInformation(landing, p, x, 0, vx, moveType, new List<int>(), new List<Tuple<float, float>>(), 10)
            {
                xlandPoint = x,
                shape = s,
                diamondsCollected = new List<int>()
            };

            m.path.Add(new Tuple<float, float>(x * GameInfo.PIXEL_LENGTH, (p.yTop - RectangleShape.height(s) / 2) * GameInfo.PIXEL_LENGTH));

            // NOTE: Be aware of possible NOMOVEs that combine several shapes
            for (int i = 0; i < initialCollectiblesInfo.Length; i++)
            {
                CollectibleRepresentation d = initialCollectiblesInfo[i];
                if (Math.Abs(d.X / GameInfo.PIXEL_LENGTH - x) <= RectangleShape.fwidth(s) / (2 * GameInfo.PIXEL_LENGTH) &&
                    d.Y + 4 * GameInfo.PIXEL_LENGTH > p.yTop * GameInfo.PIXEL_LENGTH - RectangleShape.fheight(s) && d.Y < p.yTop * GameInfo.PIXEL_LENGTH)
                {
                    m.diamondsCollected.Add(i);
                }
            }

            if (RectangleIntersectsWithObstacle(x, p.yTop, s) == CollisionType.Diamond)
            {
                List<int> others = rectangleSimulator.GetDiamondCollected(x, p.yTop, s);
                foreach (int d in others)
                {
                    if (!m.diamondsCollected.Contains(d))
                    {
                        m.diamondsCollected.Add(d);
                    }
                }
            }

            if (s == RectangleShape.Shape.VERTICAL)
            {
                double ytop = p.yTop * GameInfo.PIXEL_LENGTH - RectangleShape.fheight(s);
                for (double xtop = x * GameInfo.PIXEL_LENGTH - RectangleShape.fwidth(s) / 2; xtop <= x * GameInfo.PIXEL_LENGTH + RectangleShape.fwidth(s) / 2; xtop++)
                {
                    for (int d = 0; d < initialCollectiblesInfo.Length; d++)
                    {
                        CollectibleRepresentation c = initialCollectiblesInfo[d];
                        if (Math.Abs(c.X - xtop) + Math.Abs(c.Y - ytop) <= GameInfo.SEMI_COLLECTIBLE_HEIGHT)
                        {
                            if (!m.diamondsCollected.Contains(d))
                            {
                                m.diamondsCollected.Add(d);
                            }
                        }
                    }
                }
            }

            AddToPlatform(ref platformList, ref p, ref m, m.CompareRectangle);
        }

        public void AddFallR(ref List<Platform> platformList, ref Platform p, int vx, MoveType moveType, int x, RectangleShape.Shape s, Platform landing, Moves moveDuringFlight = Moves.NO_ACTION)
        {
            MoveInformation m = new MoveInformation(landing, p, x, 0, vx, moveType, new List<int>(), new List<Tuple<float, float>>(), 10)
            {
                xlandPoint = x,
                shape = s,
                diamondsCollected = new List<int>(),
                moveDuringFlight = moveDuringFlight
            };

            rectangleSimulator.SimulateMove(ref platformList, x * GameInfo.PIXEL_LENGTH, p.yTop * GameInfo.PIXEL_LENGTH - RectangleShape.fheight(s) / 2, vx, 0, ref m, s);

            if (m.landingPlatform.real)
            {
                AddToPlatform(ref platformList, ref p, ref m, m.CompareRectangle);
            }
        }

        public void AddNoMoveC(ref List<Platform> platformList, ref Platform p, int vx, MoveType moveType, int x)
        {
            MoveInformation m = new MoveInformation(new Platform(-1), p, x, 0, vx, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);

            if (circleSimulator.CircleIntersectsWithObstacle(x, p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) == CollisionType.Diamond)
            {
                m.landingPlatform = p;
                m.xlandPoint = x;
                m.path.Add(new Tuple<float, float>(x * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH));
                m.diamondsCollected.Add(circleSimulator.GetDiamondCollected(x, p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH));
            }

            AddToPlatform(ref platformList, ref p, ref m, m.CompareRectangle);
        }

        public void AddFallC(ref List<Platform> platformList, ref Platform p, int vx, MoveType moveType, int x)
        {
            MoveInformation m = new MoveInformation(new Platform(-1), p, x, 0, vx, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);
            List<MoveInformation> move_list = circleSimulator.SimulateMove(ref platformList, x * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, vx, 0, ref m, 0.015f);
            foreach (MoveInformation move in move_list)
            {
                MoveInformation moveaux = move;
                AddToPlatform(ref platformList, ref p, ref moveaux, move.CompareCircle);
            }
        }

        public void AddJump(ref List<Platform> platformList, ref Platform p, int vx, MoveType moveType, int x, LevelMapRectangle levelMapRectangle)
        {
            MoveInformation m = new MoveInformation(new Platform(-1), p, x, 0, vx, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);
            List<MoveInformation> move_list = circleSimulator.SimulateMove(ref platformList, x * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, vx, (int)GameInfo.JUMP_VELOCITYY, ref m, 0.015f);
            foreach(MoveInformation move in move_list)
            {
                MoveInformation moveaux = move;
                
                bool valid = true;
                
                int xPlatform = move.xlandPoint;
                int yPlatform = move.landingPlatform.yTop + RectangleShape.height(RectangleShape.Shape.HORIZONTAL);
                foreach (Platform platform in levelMapRectangle.platformList)
                {
                    if (platform.yTop == yPlatform && platform.leftEdge < xPlatform && platform.rightEdge >= xPlatform)
                    {
                         valid=platform.real;
                         break;
                    }
                }
                
                if (!move.landingPlatform.real && !valid)
                {
                    //Shouldn't add move
                }
                else
                {
                    if (move.departurePlatform.real || move.landingPlatform.real || move.diamondsCollected.Count != 0)
                    {
                        AddToPlatform(ref platformList, ref p, ref moveaux, move.CompareCircle);
                    }
                }                
            }
        }
    }
}