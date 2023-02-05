using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    class LevelMapRectangle : LevelMap
    {
        public override Platform GetPlatform(int x, int y)
        {
            throw new NotImplementedException();
        }

        public override void SimulateMove(float x_0, float y_0, float vx_0, float vy_0, ref MoveInformation m)
        {
            
        }

        protected override bool EnoughSpaceToAccelerate(int leftEdge, int rigthEdge, int x, int vx)
        {
            return false;
        }

        private void AddTrajectory(ref Platform p, int vx, MoveType moveType, int x, RectangleShape.Shape s, Platform landing)
        {
            MoveInformation m = new MoveInformation(landing, p, x, 0, vx, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);

            if (moveType == MoveType.TILT)
            {
                if (vx > 0)
                {
                    m.xlandPoint = landing.leftEdge;
                }
                else
                {
                    m.xlandPoint = landing.rightEdge;
                }
                m.path.Add(new Tuple<float, float>(x * GameInfo.PIXEL_LENGTH, (p.yTop - RectangleShape.height(s) / 2) * GameInfo.PIXEL_LENGTH));
                m.shape = s;
            }
            else if (moveType == MoveType.FALL)
            {
                
            }
            else if (moveType == MoveType.DROP)
            {

            }
            else if (moveType == MoveType.ADJACENT)
            {
                m.xlandPoint = x;
                m.path.Add(new Tuple<float, float>(x * GameInfo.PIXEL_LENGTH, (p.yTop - RectangleShape.height(s) / 2) * GameInfo.PIXEL_LENGTH));
                m.shape = s;
            }
            else if (moveType == MoveType.NOMOVE)
            {
                if (RectangleIntersectsWithObstacle(x, p.yTop, s) == CollisionType.Diamond)
                {
                    m.xlandPoint = x;
                    m.path.Add(new Tuple<float, float>(x * GameInfo.PIXEL_LENGTH, (p.yTop - RectangleShape.height(s) / 2) * GameInfo.PIXEL_LENGTH));
                    m.diamondsCollected = GetDiamondCollected(x, p.yTop, s);
                    m.shape = s;
                }
            }
            lock (platformList)
            {
                bool addIt = true;
                if (m.landingPlatform.id != -2 && m.landingPlatform.id != -1 && (m.diamondsCollected.Count > 0 || p.id != m.landingPlatform.id))
                {
                    for (int i = 0; i < p.moveInfoList.Count; i++)
                    {
                        int add = m.Compare(p.moveInfoList[i], initialCollectiblesInfo);
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

        protected override void GenerateMoveInformation()
        {
            for (int k = 0; k < platformList.Count; k++)
            {
                Platform p = platformList[k];

                // NOMOVES
                Parallel.For(p.leftEdge, p.rightEdge + 1, x =>
                {
                    foreach(RectangleShape.Shape s in GameInfo.SHAPES)
                    {
                        AddTrajectory(ref p, 0, MoveType.NOMOVE, x, s, p);
                    }
                });

                // ADJACENT MOVES
                Parallel.For(k + 1, platformList.Count - 1, i =>
                {
                    Platform p2 = platformList[i];
                    foreach (RectangleShape.Shape s in GameInfo.SHAPES)
                    {
                        if (p2.yTop == p.yTop && p.shapes[(int)s] && p2.shapes[(int)s])
                        {
                            if (p2.leftEdge == p.rightEdge)
                            {
                                AddTrajectory(ref p, 1, MoveType.ADJACENT, p.rightEdge, s, p2);
                                AddTrajectory(ref p2, -1, MoveType.ADJACENT, p.rightEdge, s, p);
                            }
                            else if (p.leftEdge == p2.rightEdge)
                            {
                                AddTrajectory(ref p, -1, MoveType.ADJACENT, p.leftEdge, s, p2);
                                AddTrajectory(ref p2, 1, MoveType.ADJACENT, p.leftEdge, s, p);
                            }
                        }
                    }
                });

                // TILT MOVES
                Parallel.For(0, platformList.Count - 1, i =>
                {
                    Platform p2 = platformList[i];
                    RectangleShape.Shape s = RectangleShape.Shape.VERTICAL;
                    if (p.yTop - p2.yTop > 0 && p.yTop - p2.yTop < 5)
                    {
                        if (p2.leftEdge - p.rightEdge <= 3 && p2.leftEdge - p.rightEdge >= 0)
                        {
                            AddTrajectory(ref p, 1, MoveType.TILT, p.rightEdge, s, p2);
                        }
                        else if (p.leftEdge - p2.rightEdge <= 3 && p.leftEdge - p2.rightEdge >= 0)
                        {
                            AddTrajectory(ref p, -1, MoveType.TILT, p.leftEdge, s, p2);
                        }
                    }
                });

                // DROP MOVES
                Parallel.For(0, platformList.Count - 1, i =>
                {
                    Platform p2 = platformList[i];
                    RectangleShape.Shape s = RectangleShape.Shape.HORIZONTAL;
                    if (p.yTop - p2.yTop > 0 && p.yTop - p2.yTop < 5)
                    {
                        if (p2.leftEdge - p.rightEdge <= 3 && p2.leftEdge - p.rightEdge >= 0)
                        {
                            AddTrajectory(ref p, 1, MoveType.TILT, p.rightEdge, s, p2);
                        }
                        else if (p.leftEdge - p2.rightEdge <= 3 && p.leftEdge - p2.rightEdge >= 0)
                        {
                            AddTrajectory(ref p, -1, MoveType.TILT, p.leftEdge, s, p2);
                        }
                    }
                });
            }
        }

        protected override void IdentifyDefaultPlatforms()
        {
            //Bottom obstacle
            bool prevPlatform = false;
            int xleft = 0;
            foreach (RectangleShape.Shape s in GameInfo.SHAPES)
            {
                for (int x = RectangleShape.width(s) / 2; x < GameInfo.LEVEL_MAP_WIDTH - RectangleShape.width(s) / 2; x++)
                {
                    int yTop = GameInfo.LEVEL_MAP_HEIGHT - 5;
                    CollisionType col = RectangleIntersectsWithObstacle(x, yTop, s);
                    bool flag = col != CollisionType.None && col != CollisionType.Diamond;
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
                            platformList.Add(new Platform(platformList.Count, yTop, xleft, x - 1, new List<MoveInformation>(), s));
                        }
                        prevPlatform = false;
                    }
                }
                if (prevPlatform)
                {
                    platformList.Add(new Platform(platformList.Count, GameInfo.LEVEL_MAP_HEIGHT - 5, xleft, GameInfo.LEVEL_MAP_WIDTH - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH - 1, new List<MoveInformation>()));
                }
            }
        }

        private CollisionType RectangleIntersectsWithObstacle(int x, int y, RectangleShape.Shape s)//x is the center of the rectangle and y is the base of the rectangle
        {
            bool diamond = false; // Obstacles and platforms have more priority than diamonds
            for (int i = -RectangleShape.width(s) / 2; i <= RectangleShape.width(s) / 2; i++)
            {
                for (int j = 1; j<= RectangleShape.height(s); j++)
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

        private List<int> GetDiamondCollected(int x, int y, RectangleShape.Shape s)//x is the center of the rectangle and y is the base of the rectangle
        {
            List<int> ret = new List<int>();
            for (int i = -RectangleShape.width(s) / 2; i <= RectangleShape.width(s) / 2; i++)
            {
                for (int j = 1; j <= RectangleShape.height(s); j++)
                {
                    if ((x + i) >= 0 && (y - j) >= 0 && (x + i) < levelMap.GetLength(0) && (y - j) < levelMap.GetLength(1))
                    {
                        if (levelMap[x + i, y - j] == PixelType.DIAMOND)
                        {
                            for(int d = 0; d < initialCollectiblesInfo.Length; d++)
                            {
                                CollectibleRepresentation c = initialCollectiblesInfo[d];
                                if(Math.Abs(x + i-c.X/GameInfo.PIXEL_LENGTH) + Math.Abs(x + i - c.X / GameInfo.PIXEL_LENGTH) <= 3)
                                {
                                    if (!ret.Contains(d))
                                    {
                                        ret.Add(d);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return ret;
        }

        protected override void IdentifyPlatforms(ObstacleRepresentation[] oI)
        {
            bool prevPlatform = false;
            int xleft = 0;
           
            foreach(RectangleShape.Shape s in GameInfo.SHAPES)
            {
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
                        CollisionType col = RectangleIntersectsWithObstacle(x, yTop, s);
                        bool flag = col != CollisionType.None && col != CollisionType.Diamond;
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
                                platformList.Add(new Platform(platformList.Count, yTop, xleft, x - 1, new List<MoveInformation>(), s));
                            }
                            prevPlatform = false;
                        }
                    }
                    if (prevPlatform)
                    {
                        platformList.Add(new Platform(platformList.Count, yTop, xleft, rightEdge, new List<MoveInformation>(), s));
                    }
                }
            }
        }

        protected override void PlatformUnion()
        {
            for (int i = 0; i < platformList.Count; i++)
            {
                Platform p1 = platformList[i];
                for (int j = i + 1; j < platformList.Count(); j++)
                {
                    Platform p2 = platformList[j];
                    if (p1.yTop == p2.yTop)
                    {
                        if(p1.leftEdge <= p2.leftEdge && p1.rightEdge > p2.leftEdge)
                        {
                            if (p1.leftEdge < p2.leftEdge)
                            {
                                Platform pLeft = new Platform(platformList.Count, p1.yTop, p1.leftEdge, p2.leftEdge, new List<MoveInformation>());
                                Array.Copy(p1.shapes, pLeft.shapes, p1.shapes.Length);
                                platformList.Add(pLeft);
                            }
                            if (p1.rightEdge == p2.rightEdge)
                            {
                                p2.CombineShapes(p1.shapes);
                                platformList.RemoveAt(i);
                                i--;
                                break;
                            }
                            else
                            {
                                Platform pMiddle = new Platform(platformList.Count, p1.yTop, p2.leftEdge, Math.Min(p2.rightEdge,p1.rightEdge), new List<MoveInformation>());
                                Array.Copy(p1.shapes, pMiddle.shapes, p1.shapes.Length);
                                pMiddle.CombineShapes(p2.shapes);
                                platformList.Add(pMiddle);
                                Platform pRight = new Platform(platformList.Count, p1.yTop, Math.Min(p2.rightEdge, p1.rightEdge), Math.Max(p2.rightEdge, p1.rightEdge), new List<MoveInformation>());
                                if(p2.rightEdge > p1.rightEdge)
                                {
                                    Array.Copy(p2.shapes, pRight.shapes, p1.shapes.Length);
                                }
                                else
                                {
                                    Array.Copy(p1.shapes, pRight.shapes, p1.shapes.Length);
                                }
                                platformList.Add(pRight);
                                platformList.RemoveAt(i);
                                platformList.RemoveAt(j-1);
                                i--;
                                j--;
                                break;
                            }
                        }
                        else if (p2.leftEdge <= p1.leftEdge && p2.rightEdge > p1.leftEdge)
                        {
                            if (p2.leftEdge < p1.leftEdge)
                            {
                                Platform pLeft = new Platform(platformList.Count, p1.yTop, p2.leftEdge, p1.leftEdge, new List<MoveInformation>());
                                Array.Copy(p2.shapes, pLeft.shapes, p1.shapes.Length);
                                platformList.Add(pLeft);
                            }
                            if (p2.rightEdge == p1.rightEdge)
                            {
                                p1.CombineShapes(p2.shapes);
                                platformList.RemoveAt(j);
                                j--;
                                continue;
                            }
                            else
                            {
                                Platform pMiddle = new Platform(platformList.Count, p1.yTop, p1.leftEdge, Math.Min(p1.rightEdge, p2.rightEdge), new List<MoveInformation>());
                                Array.Copy(p2.shapes, pMiddle.shapes, p1.shapes.Length);
                                pMiddle.CombineShapes(p1.shapes);
                                platformList.Add(pMiddle);
                                Platform pRight = new Platform(platformList.Count, p1.yTop, Math.Min(p1.rightEdge, p2.rightEdge), Math.Max(p1.rightEdge, p2.rightEdge), new List<MoveInformation>());
                                if (p2.rightEdge > p1.rightEdge)
                                {
                                    Array.Copy(p2.shapes, pRight.shapes, p1.shapes.Length);
                                }
                                else
                                {
                                    Array.Copy(p1.shapes, pRight.shapes, p1.shapes.Length);
                                }
                                platformList.Add(pRight);
                                platformList.RemoveAt(i);
                                platformList.RemoveAt(j-1);
                                i--;
                                j--;
                                break;
                            }
                        }
                    }
                }
            }
            // Rename id
            platformList.Sort();
            for (int i = 0; i < platformList.Count; i++)
            {
                platformList[i].id = i;
            }
            int length = platformList.Count - 1;
            for (int i = 0; i < length; i++)
            {
                Platform pLeft = platformList[i];
                Platform pRight = platformList[i+1];
                if(pLeft.yTop == pRight.yTop && pRight.leftEdge - pLeft.rightEdge < 3 * GameInfo.VERTICAL_RECTANGLE_HEIGHT / (5 * GameInfo.PIXEL_LENGTH)
                    && pRight.leftEdge - pLeft.rightEdge > GameInfo.HORIZONTAL_RECTANGLE_HEIGHT/GameInfo.PIXEL_LENGTH)
                {
                    Platform newP = new Platform(platformList.Count, pLeft.yTop, pLeft.rightEdge + 1, pRight.leftEdge - 1, new List<MoveInformation>());
                    newP.real = false;
                    newP.shapes[(int) RectangleShape.Shape.HORIZONTAL] = true;
                    platformList.Add(newP);
                }
            }
        }
    }
}
