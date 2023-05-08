using GeometryFriends.AI;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    class LevelMapRectangle : LevelMap
    {
        public List<Platform> simplified_platforms = new List<Platform>();
        public Dictionary<Platform, List<Platform>> simplified_to_small = new Dictionary<Platform, List<Platform>>();
        public Dictionary<Platform, Platform> small_to_simplified = new Dictionary<Platform, Platform>();

        // Returns the highest real platform (remember simplified_platforms contains first all the real platforms and then the ficticious)
        public Platform PlatformBelowRectangle(RectangleRepresentation rI)
        {
            Platform p = new Platform(-1);
            p.yTop = GameInfo.LEVEL_MAP_HEIGHT;
            for (int i = 0; i < simplified_platforms.Count; i++)
            {
                if (rI.Y / GameInfo.PIXEL_LENGTH < simplified_platforms[i].yTop &&
                    rI.X / GameInfo.PIXEL_LENGTH >= simplified_platforms[i].leftEdge &&
                    rI.X / GameInfo.PIXEL_LENGTH <= simplified_platforms[i].rightEdge &&
                    p.yTop > simplified_platforms[i].yTop)
                {
                    p = simplified_platforms[i];
                }
            }
            return p;
        }

        public Platform RectanglePlatform(RectangleRepresentation rI)
        {
            Platform current_platform = new Platform(-1);
            for (int i = 0; i < platformList.Count; i++)
            {
                if ((rI.Y + rI.Height / 2) / GameInfo.PIXEL_LENGTH <= platformList[i].yTop + 3 &&
                    (rI.Y + rI.Height / 2) / GameInfo.PIXEL_LENGTH >= platformList[i].yTop - 3 &&
                    rI.X / GameInfo.PIXEL_LENGTH >= platformList[i].leftEdge - 1 && rI.X / GameInfo.PIXEL_LENGTH <= platformList[i].rightEdge + 1)
                {
                    // If height is less than square's height, it must be the right platform
                    if (rI.Height < GameInfo.SQUARE_HEIGHT)
                    {
                        return platformList[i];
                    }
                    // Otherwise, it may be the height has not updated correctly
                    else
                    {
                        current_platform = platformList[i];
                        break;
                    }
                }
            }
            RectangleShape.Shape s = RectangleShape.GetShape(rI);
            for (int i = 0; i < platformList.Count; i++)
            {
                if (rI.Y / GameInfo.PIXEL_LENGTH + RectangleShape.width(s) / 2 <= platformList[i].yTop + 1 &&
                    rI.Y / GameInfo.PIXEL_LENGTH + RectangleShape.width(s) / 2 >= platformList[i].yTop - 10 &&
                    rI.X / GameInfo.PIXEL_LENGTH >= platformList[i].leftEdge - 1 && rI.X / GameInfo.PIXEL_LENGTH <= platformList[i].rightEdge + 1)
                {
                    if (current_platform.id == -1 || platformList[i].yTop < current_platform.yTop)
                    {
                        current_platform = platformList[i];
                    }
                    return current_platform;
                }
            }
            return current_platform;
        }

        public override Platform GetPlatform(int x, int y)
        {
            foreach(Platform p in platformList)
            {
                if(p.leftEdge <= x && p.rightEdge >= x && p.yTop == y)
                {
                    return p;
                }
            }
            return new Platform(-2);
        }

        public void DrawConnectionsVertex(ref List<DebugInformation> debugInformation)
        {
            moveGenerator.trajectoryAdder.rectangleSimulator.DrawConnectionsVertex(ref debugInformation);
        }

        protected override void GenerateMoveInformation()
        {
            // ADJACENT MOVES
            for (int k = 0; k < platformList.Count; k++)
            {
                Platform p = platformList[k];
                if (p.real)
                {
                    moveGenerator.GenerateAdjacent(ref platformList, k);
                }
            }

            // DROP MOVES
            for (int k = 0; k < platformList.Count; k++)
            {
                Platform p = platformList[k];
                if (!p.real)
                {
                    moveGenerator.GenerateDrop(ref platformList, k);
                }
            }

            for (int k = 0; k < platformList.Count; k++)
            {
                Platform p = platformList[k];
                if (p.real)
                {
                    // NOMOVES
                    Parallel.For(p.leftEdge, p.rightEdge + 1, x =>
                    {
                        moveGenerator.GenerateNoMoveR(ref platformList, k, x);
                    });

                    // TILT and HIGHTILT MOVES
                    Parallel.For(0, platformList.Count, i =>
                    {
                        moveGenerator.GenerateTilt(ref platformList, k, i);
                    });

                    // MONOSIDEDROP
                    Parallel.For(0, platformList.Count, i =>
                    {
                        moveGenerator.GenerateMonoSideDrop(ref platformList, k, i);
                    });

                    //FALL MOVES
                    foreach (RectangleShape.Shape s in GameInfo.SHAPES)
                    {
                        if (p.shapes[(int)s])
                        {
                            //for (int i = 0; i < GameInfo.NUM_VELOCITIES_RECTANGLE; i++)
                            Parallel.For(0, GameInfo.NUM_VELOCITIES_RECTANGLE, i =>
                            {
                                moveGenerator.GenerateFallR(ref platformList, k, i, s);
                            });
                        }
                    }

                    // BIGHOLEADJ
                    Parallel.For(0, platformList.Count, i =>
                    {
                        moveGenerator.GenerateBigHoleAdj(ref platformList, k, i);
                    });

                    // BIGHOLEDROP
                    Parallel.For(0, platformList.Count, i =>
                    {
                        moveGenerator.GenerateBigHoleDrop(ref platformList, k, i);
                    });
                }
            }

            MergePlatforms();
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
                                Platform pLeft = new Platform(platformList[platformList.Count - 1].id + 1, p1.yTop, p1.leftEdge, p2.leftEdge, new List<MoveInformation>());
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
                                Platform pMiddle = new Platform(platformList[platformList.Count - 1].id + 1, p1.yTop, p2.leftEdge, Math.Min(p2.rightEdge,p1.rightEdge), new List<MoveInformation>());
                                Array.Copy(p1.shapes, pMiddle.shapes, p1.shapes.Length);
                                pMiddle.CombineShapes(p2.shapes);
                                if (pMiddle.rightEdge > pMiddle.leftEdge)
                                {
                                    platformList.Add(pMiddle);
                                }
                                Platform pRight = new Platform(platformList[platformList.Count - 1].id + 1, p1.yTop, Math.Min(p2.rightEdge, p1.rightEdge), Math.Max(p2.rightEdge, p1.rightEdge), new List<MoveInformation>());
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
                                Platform pLeft = new Platform(platformList[platformList.Count - 1].id + 1, p1.yTop, p2.leftEdge, p1.leftEdge, new List<MoveInformation>());
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
                                Platform pMiddle = new Platform(platformList[platformList.Count - 1].id + 1, p1.yTop, p1.leftEdge, Math.Min(p1.rightEdge, p2.rightEdge), new List<MoveInformation>());
                                Array.Copy(p2.shapes, pMiddle.shapes, p1.shapes.Length);
                                pMiddle.CombineShapes(p1.shapes);
                                if (pMiddle.rightEdge > pMiddle.leftEdge)
                                {
                                    platformList.Add(pMiddle);
                                }
                                Platform pRight = new Platform(platformList[platformList.Count - 1].id + 1, p1.yTop, Math.Min(p1.rightEdge, p2.rightEdge), Math.Max(p1.rightEdge, p2.rightEdge), new List<MoveInformation>());
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
                    bool rectangle_fits = true;
                    for (int x = pLeft.rightEdge; x <= pRight.leftEdge && rectangle_fits; x++)
                    {
                        for (int y = 1; y < GameInfo.HORIZONTAL_RECTANGLE_HEIGHT / GameInfo.PIXEL_LENGTH; y++)
                        {
                            if(levelMap[x,pRight.yTop-y] == PixelType.OBSTACLE || levelMap[x, pRight.yTop - y] == PixelType.PLATFORM)
                            {
                                rectangle_fits = false;
                                break;
                            }
                        } 
                    }
                    if (rectangle_fits) {
                        Platform newP = new Platform(platformList.Count, pLeft.yTop, pLeft.rightEdge + 1, pRight.leftEdge - 1, new List<MoveInformation>());
                        newP.real = false;
                        newP.shapes[(int)RectangleShape.Shape.HORIZONTAL] = true;
                        platformList.Add(newP);
                    }
                }
            }
        }

        private void MergePlatforms()
        {
            Platform currentPlatform  =  null;
            List<Platform> miniPlatforms = new List<Platform>();
            for  (int i  =  0; i < platformList.Count; i++)
            {
                Platform p = platformList[i];
                if  (currentPlatform == null)
                {
                    currentPlatform = new Platform(simplified_platforms.Count, p.yTop, p.leftEdge, p.rightEdge, new List<MoveInformation>());
                    currentPlatform.real = p.real;
                    miniPlatforms.Add(p);
                    foreach  (MoveInformation m in p.moveInfoList)
                    {
                        if  (m.moveType != MoveType.ADJACENT || !m.departurePlatform.real || !m.landingPlatform.real)
                        {
                            currentPlatform.moveInfoList.Add(new MoveInformation(m));
                        }
                    }
                }
                i++;
                while (i < platformList.Count && Math.Abs(platformList[i].leftEdge - p.rightEdge) <= 1 && platformList[i].yTop == p.yTop)
                {
                    p = platformList[i];
                    miniPlatforms.Add(p);
                    currentPlatform.rightEdge = p.rightEdge;
                    foreach (MoveInformation m in p.moveInfoList)
                    {
                        if (m.moveType != MoveType.ADJACENT || !m.departurePlatform.real || !m.landingPlatform.real)
                        {
                            currentPlatform.moveInfoList.Add(new MoveInformation(m));
                        }
                    }
                    i++;
                }
                i--;
                simplified_platforms.Add(currentPlatform);
                simplified_to_small.Add(currentPlatform, miniPlatforms);
                foreach (Platform plat in miniPlatforms)
                {
                    small_to_simplified.Add(plat, currentPlatform);
                }
                currentPlatform = null;
                miniPlatforms = new List<Platform>();
            }
            foreach(Platform p in simplified_platforms)
            {
                foreach(MoveInformation m in p.moveInfoList)
                {
                    m.landingPlatform = small_to_simplified[m.landingPlatform];
                    m.departurePlatform = p;
                }
            }
            foreach (Platform p in simplified_platforms)
            {
                for (int i = 0; i < p.moveInfoList.Count; i++)
                {
                    MoveInformation m1 = p.moveInfoList[i];
                    for (int j = i+1; j < p.moveInfoList.Count; j++)
                    {
                        MoveInformation m2 = p.moveInfoList[j];
                        int comp = m1.CompareRectangle(m2, initialCollectiblesInfo, simplified_platforms);
                        if(comp == 1)
                        {
                            p.moveInfoList.RemoveAt(j);
                            j--;
                        }
                        else if(comp == -1)
                        {
                            p.moveInfoList.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }
            }
        }

        public bool AtBorder(RectangleRepresentation rI, Platform p, ref Moves currentAction, List<MoveInformation> plan)
        {
            if (p.id == -1)
            {
                RectangleRepresentation rI2 = new RectangleRepresentation();
                rI2.X = rI.X - GameInfo.PIXEL_LENGTH;
                rI2.Y = rI.Y;
                rI2.Height = rI.Height;
                Platform left = RectanglePlatform(rI2);
                rI2.X = rI.X + GameInfo.PIXEL_LENGTH;
                Platform right = RectanglePlatform(rI2);

                if (left.id != -1 && right.id == -1) // Rectangle at right edge
                {
                    currentAction = Moves.MOVE_LEFT;
                    return true;
                }
                else if (left.id == -1 && right.id != -1) // Rectangle at left edge
                {
                    currentAction = Moves.MOVE_RIGHT;
                    return true;
                }
                else if (left.id == -1 && right.id == -1)
                {
                    return false;
                }
                else if (left.yTop > right.yTop)
                {
                    currentAction = Moves.MOVE_RIGHT;
                    return true;
                }
                else if (left.yTop < right.yTop)
                {
                    currentAction = Moves.MOVE_LEFT;
                    return true;
                }
            }
            return false;
        }

        public bool RectangleCanMorphDown(RectangleRepresentation rI)
        {
            double width = GameInfo.RECTANGLE_AREA / (rI.Height * GameInfo.PIXEL_LENGTH);
            int x = (int)rI.X / GameInfo.PIXEL_LENGTH;
            int xleft = x - (int) width / 2 - 1;
            int xright = x + (int)width / 2 + 1;
            for(int y = (int)(rI.Y - rI.Height / 2) / GameInfo.PIXEL_LENGTH + 2;
                y <= (int)(rI.Y + rI.Height / 2) / GameInfo.PIXEL_LENGTH - 1; y++)
            {
                if (!((levelMap[xleft, y] == PixelType.EMPTY || levelMap[xleft, y] == PixelType.DIAMOND) &&
                (levelMap[xright, y] == PixelType.EMPTY || levelMap[xright, y] == PixelType.DIAMOND)))
                {
                    return false;
                }
            }
            return true;
        }

        public bool RectangleCanMorphUp(RectangleRepresentation rI)
        {
            double width = GameInfo.RECTANGLE_AREA / (rI.Height * GameInfo.PIXEL_LENGTH);
            int xleft = (int)rI.X / GameInfo.PIXEL_LENGTH - (int)width / 2;
            int xright = (int)rI.X / GameInfo.PIXEL_LENGTH + (int)width / 2;
            int y = (int)((rI.Y - 3 * rI.Height / 5) / GameInfo.PIXEL_LENGTH) - 1;
            for (int x = xleft; x <= xright; x++)
            {
                if (!(levelMap[x, y] == PixelType.EMPTY || levelMap[x, y] == PixelType.DIAMOND))
                {
                    return false;
                }
            }
            return true;
        }

        // returns platforms <left,right>
        public Tuple<Platform, Platform> AdjacentPlatforms(Platform currentPlatform)
        {
            return moveGenerator.trajectoryAdder.rectangleSimulator.AdjacentPlatforms(platformList, currentPlatform);
        }

        public bool HitsCeiling(RectangleRepresentation rI, Platform current_platform)
        {
            int x = (int)rI.X / GameInfo.PIXEL_LENGTH;
            int y = (int)rI.Y / GameInfo.PIXEL_LENGTH;

            while (y >= 0 && (levelMap[x,y]==PixelType.EMPTY|| levelMap[x, y] == PixelType.DIAMOND))
            {
                y--;
            }
            return (current_platform.yTop - y - 1) * GameInfo.PIXEL_LENGTH < rI.Height + 3;
        }
    }
}
