using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using static GeometryFriendsAgents.LevelMap;

namespace GeometryFriendsAgents
{
    public class MoveGenerator
    {
        public TrajectoryAdder trajectoryAdder;
        public CollectibleRepresentation[] initialCollectiblesInfo;
        public PixelType[,] levelMapRectangle;

        public MoveGenerator(CollectibleRepresentation[] initialCollectiblesInfo, PixelType[,] levelMap)
        {
            this.initialCollectiblesInfo = initialCollectiblesInfo;
            this.levelMapRectangle = levelMap;
            this.trajectoryAdder = new TrajectoryAdder(initialCollectiblesInfo, levelMap);
        }

        public Platform GetPlatform(List<Platform> platformList, int x, int y)
        {
            foreach (Platform p in platformList)
            {
                if (p.leftEdge <= x && p.rightEdge >= x && p.yTop == y)
                {
                    return p;
                }
            }
            return new Platform(-2);
        }

        public void GenerateAdjacent(ref List<Platform> platformList, int k)
        {
            // ADJACENT MOVES
            Platform p = platformList[k];
            for (int i = k + 1; i < platformList.Count; i++)
            {
                Platform p2 = platformList[i];
                if (p2.real)
                {
                    foreach (RectangleShape.Shape s in GameInfo.SHAPES)
                    {
                        if (p2.yTop == p.yTop && p.shapes[(int)s] && p2.shapes[(int)s])
                        {
                            if (p2.leftEdge == p.rightEdge || (i == k + 1 && p2.leftEdge == p.rightEdge + 1))
                            {
                                if (p.ShapesAreEqual(p2))
                                {
                                    p.rightEdge = p2.rightEdge;
                                    platformList.RemoveAt(i);
                                    i--;
                                    for (int j = i; j < platformList.Count; j++)
                                    {
                                        platformList[j].id = j;
                                    }
                                }
                                else
                                {
                                    if (p2.leftEdge == p.rightEdge)
                                    {
                                        p2.leftEdge++;
                                    }
                                    trajectoryAdder.AddAdjacent(ref platformList, ref p, 1, p.rightEdge, s, p2);
                                    trajectoryAdder.AddAdjacent(ref platformList, ref p2, -1, p2.leftEdge, s, p);
                                }
                            }
                            else if (p.leftEdge == p2.rightEdge || p.leftEdge == p2.rightEdge + 1)
                            {
                                if (p.ShapesAreEqual(p2))
                                {
                                    p.leftEdge = p2.leftEdge;
                                    platformList.RemoveAt(i);
                                    i--;
                                    for (int j = i; j < platformList.Count; j++)
                                    {
                                        platformList[j].id = j;
                                    }
                                }
                                else
                                {
                                    if (p.leftEdge == p2.rightEdge)
                                    {
                                        p.leftEdge++;
                                    }
                                    trajectoryAdder.AddAdjacent(ref platformList, ref p, -1, p.leftEdge, s, p2);
                                    trajectoryAdder.AddAdjacent(ref platformList, ref p2, 1, p2.rightEdge, s, p);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (p2.yTop == p.yTop)
                    {
                        if (p2.leftEdge == p.rightEdge + 1)
                        {
                            trajectoryAdder.AddAdjacent(ref platformList, ref p, p2.rightEdge - p2.leftEdge < 8 ? 50 : 15 * (p2.rightEdge - p2.leftEdge), p.rightEdge, RectangleShape.Shape.HORIZONTAL, p2);
                            trajectoryAdder.AddAdjacent(ref platformList, ref p2, p2.rightEdge - p2.leftEdge < 8 ? -50 : -15 * (p2.rightEdge - p2.leftEdge), p2.leftEdge, RectangleShape.Shape.HORIZONTAL, p);
                        }
                        else if (p.leftEdge == p2.rightEdge + 1)
                        {
                            trajectoryAdder.AddAdjacent(ref platformList, ref p, p2.rightEdge - p2.leftEdge < 8 ? -50 : -15 * (p2.rightEdge - p2.leftEdge), p.leftEdge, RectangleShape.Shape.HORIZONTAL, p2);
                            trajectoryAdder.AddAdjacent(ref platformList, ref p2, p2.rightEdge - p2.leftEdge < 8 ? 50 : 15 * (p2.rightEdge - p2.leftEdge), p2.rightEdge, RectangleShape.Shape.HORIZONTAL, p);
                        }
                    }
                }
            }
        }

        public void GenerateDrop(ref List<Platform> platformList, int k)
        {
            // DROP MOVES
            Platform p = platformList[k];
            RectangleShape.Shape s = RectangleShape.Shape.VERTICAL;
            trajectoryAdder.AddDrop(ref platformList, ref p, 0, MoveType.DROP, (p.rightEdge + p.leftEdge) / 2 + 1, s, new Platform(-1));
        }

        public void GenerateTilt(ref List<Platform> platformList, int k, int i)
        {
            Platform p = platformList[k];
            Platform p2 = platformList[i];
            if (p2.real)
            {
                if (p.shapes[(int)RectangleShape.Shape.VERTICAL])
                {
                    RectangleShape.Shape s = RectangleShape.Shape.VERTICAL;
                    // NOTE: Be aware 13 creates impossible moves
                    if (p.yTop - p2.yTop > 0 && p.yTop - p2.yTop < 14)
                    {
                        if (p2.leftEdge - p.rightEdge <= RectangleShape.width(RectangleShape.Shape.VERTICAL)
                            && p2.leftEdge - p.rightEdge >= 0)
                        {
                            if(p.yTop - p2.yTop > 11)
                            {
                                int vx = trajectoryAdder.rectangleSimulator.CalculateMaxVelocity(platformList, p, false);
                                trajectoryAdder.AddTilt(ref platformList, ref p, vx, MoveType.HIGHTILT, p.rightEdge, s, p2);
                            }
                            else
                            {
                                trajectoryAdder.AddTilt(ref platformList, ref p, 1, MoveType.TILT, p.rightEdge, s, p2);
                            }
                        }
                        else if (p.leftEdge - p2.rightEdge <= RectangleShape.width(RectangleShape.Shape.VERTICAL) + 1
                            && p.leftEdge - p2.rightEdge >= 0)
                        {
                            if (p.yTop - p2.yTop > 11)
                            {
                                int vx = trajectoryAdder.rectangleSimulator.CalculateMinVelocity(platformList, p, false);
                                trajectoryAdder.AddTilt(ref platformList, ref p, vx, MoveType.HIGHTILT, p.leftEdge, s, p2);
                            }
                            else
                            {
                                trajectoryAdder.AddTilt(ref platformList, ref p, -1, MoveType.TILT, p.leftEdge, s, p2);
                            }
                        }
                    }
                    if(p.yTop - p2.yTop > 11 && p.yTop - p2.yTop < 19)
                    {
                        if (p2.leftEdge - p.rightEdge <= RectangleShape.width(RectangleShape.Shape.VERTICAL)
                            && p2.leftEdge - p.rightEdge >= 0)
                        {
                            if (p.yTop - p2.yTop <= 14)
                            {
                                trajectoryAdder.AddTilt(ref platformList, ref p, 1, MoveType.CIRCLETILT, p.rightEdge, s, p2);
                            }
                            else
                            {
                                int vx = trajectoryAdder.rectangleSimulator.CalculateMaxVelocity(platformList, p, false);
                                trajectoryAdder.AddTilt(ref platformList, ref p, vx, MoveType.CIRCLETILT, p.rightEdge, s, p2);
                            }
                        }
                         else if (p.leftEdge - p2.rightEdge <= RectangleShape.width(RectangleShape.Shape.VERTICAL) + 1
                            && p.leftEdge - p2.rightEdge >= 0)
                        {
                            if (p.yTop - p2.yTop <= 14)
                            {
                                
                                trajectoryAdder.AddTilt(ref platformList, ref p, -1, MoveType.CIRCLETILT, p.leftEdge, s, p2);
                            }
                            else
                            {
                                int vx = trajectoryAdder.rectangleSimulator.CalculateMinVelocity(platformList, p, false);
                                trajectoryAdder.AddTilt(ref platformList, ref p, vx, MoveType.CIRCLETILT, p.leftEdge, s, p2);
                            }
                        }
                    }
                }
                else if (p.shapes[(int)RectangleShape.Shape.SQUARE])
                {
                    RectangleShape.Shape s = RectangleShape.Shape.SQUARE;
                    if (p2.leftEdge - p.rightEdge <= RectangleShape.width(RectangleShape.Shape.SQUARE)
                        && p2.leftEdge - p.rightEdge >= 0)
                    {
                        int max_height = p.yTop - RectangleShape.height(RectangleShape.Shape.SQUARE);
                        bool flag = true;
                        while (flag && p.yTop - max_height < RectangleShape.height(RectangleShape.Shape.VERTICAL))
                        {
                            for (int x = p.rightEdge - GameInfo.RECTANGLE_AREA / (GameInfo.PIXEL_LENGTH * GameInfo.PIXEL_LENGTH * (p.yTop - max_height));
                                x < p.rightEdge + GameInfo.RECTANGLE_AREA / (GameInfo.PIXEL_LENGTH * GameInfo.PIXEL_LENGTH * (p.yTop - max_height)); x++)
                            {
                                if (levelMapRectangle[x, max_height] == PixelType.OBSTACLE)
                                {
                                    flag = false;
                                    max_height++;
                                    break;
                                }
                            }
                            max_height--;
                        }
                        if (p.yTop - p2.yTop > 0 && p.yTop - p2.yTop < (p.yTop - max_height) / 2)
                        {
                            trajectoryAdder.AddTilt(ref platformList, ref p, 1, MoveType.TILT, p.rightEdge, s, p2);
                        }
                    }
                    else if (p.leftEdge - p2.rightEdge <= RectangleShape.width(RectangleShape.Shape.SQUARE) + 1
                        && p.leftEdge - p2.rightEdge >= 0)
                    {
                        int max_height = p.yTop - RectangleShape.height(RectangleShape.Shape.SQUARE);
                        bool flag = true;
                        while (flag && p.yTop - max_height < RectangleShape.height(RectangleShape.Shape.VERTICAL))
                        {
                            for (int x = p.rightEdge - GameInfo.RECTANGLE_AREA / (GameInfo.PIXEL_LENGTH * GameInfo.PIXEL_LENGTH * (p.yTop - max_height));
                                x < p.rightEdge + GameInfo.RECTANGLE_AREA / (GameInfo.PIXEL_LENGTH * GameInfo.PIXEL_LENGTH * (p.yTop - max_height)); x++)
                            {
                                if (levelMapRectangle[x, max_height] == PixelType.OBSTACLE)
                                {
                                    flag = false;
                                    max_height++;
                                    break;
                                }
                            }
                            max_height--;
                        }
                        if (p.yTop - p2.yTop > 0 && p.yTop - p2.yTop < (p.yTop - max_height) / 2)
                        {
                            trajectoryAdder.AddTilt(ref platformList, ref p, -1, MoveType.TILT, p.leftEdge, s, p2);
                        }
                    }
                }
            }
        }

        public void GenerateMonoSideDrop(ref List<Platform> platformList, int k, int i)
        {
            Platform p = platformList[k];
            RectangleShape.Shape s = RectangleShape.Shape.VERTICAL;
            // Right moves
            int x = p.rightEdge + 1;
            while (x < GameInfo.LEVEL_MAP_WIDTH && levelMapRectangle[x, p.yTop] != PixelType.OBSTACLE && levelMapRectangle[x, p.yTop] != PixelType.PLATFORM)
            {
                x++;
            }
            if (levelMapRectangle[x, p.yTop] == PixelType.OBSTACLE && x - p.rightEdge > RectangleShape.width(RectangleShape.Shape.VERTICAL) + 1
                && x - p.rightEdge < RectangleShape.width(RectangleShape.Shape.HORIZONTAL))
            {
                trajectoryAdder.AddMonoSideDrop(ref platformList, ref p, 1, MoveType.MONOSIDEDROP, (x + p.rightEdge) / 2 + 1, s, new Platform(-1));
            }
            // Left moves
            x = p.leftEdge - 1;
            while (x >= 0 && levelMapRectangle[x, p.yTop] != PixelType.OBSTACLE && levelMapRectangle[x, p.yTop] != PixelType.PLATFORM)
            {
                x--;
            }
            if (levelMapRectangle[x, p.yTop] == PixelType.OBSTACLE && p.leftEdge - x > RectangleShape.width(RectangleShape.Shape.VERTICAL) + 1
                && p.leftEdge - x < RectangleShape.width(RectangleShape.Shape.HORIZONTAL))
            {
                trajectoryAdder.AddMonoSideDrop(ref platformList, ref p, -1, MoveType.MONOSIDEDROP, (x + p.leftEdge) / 2 + 1, s, new Platform(-1));
            }
        }

        public void GenerateBigHoleAdj(ref List<Platform> platformList, int k, int i)
        {
            Platform p = platformList[k];
            RectangleShape.Shape s = RectangleShape.Shape.VERTICAL;
            // Right moves
            int x = p.rightEdge + 1;
            while (x < GameInfo.LEVEL_MAP_WIDTH && levelMapRectangle[x, p.yTop] != PixelType.OBSTACLE && levelMapRectangle[x, p.yTop] != PixelType.PLATFORM)
            {
                x++;
            }
            if (levelMapRectangle[x, p.yTop] == PixelType.PLATFORM &&
                x - p.rightEdge > 1 + RectangleShape.width(RectangleShape.Shape.HORIZONTAL) / 2)
            {
                if (x - p.rightEdge < RectangleShape.width(RectangleShape.Shape.HORIZONTAL))
                {
                    trajectoryAdder.AddBigHoleAdj(ref platformList, ref p, 1, MoveType.BIGHOLEADJ, (x + p.rightEdge) / 2 + 1, s, GetPlatform(platformList, x, p.yTop));
                }
                else
                {
                    Platform p2 = GetPlatform(platformList, x, p.yTop);
                    int vx = trajectoryAdder.rectangleSimulator.CalculateMaxVelocity(platformList, p, true);
                    trajectoryAdder.AddBigHoleAdj(ref platformList, ref p, vx, MoveType.WIDEADJ, p.rightEdge + 1, s, p2);
                }
            }
            // Left moves
            x = p.leftEdge - 1;
            while (x >= 0 && levelMapRectangle[x, p.yTop] != PixelType.OBSTACLE && levelMapRectangle[x, p.yTop] != PixelType.PLATFORM)
            {
                x--;
            }
            if (levelMapRectangle[x, p.yTop] == PixelType.PLATFORM &&
                p.leftEdge - x > 1 + RectangleShape.width(RectangleShape.Shape.HORIZONTAL) / 2)
            {
                if (p.leftEdge - x < RectangleShape.width(RectangleShape.Shape.HORIZONTAL))
                {
                    trajectoryAdder.AddBigHoleAdj(ref platformList, ref p, -1, MoveType.BIGHOLEADJ, (x + p.leftEdge) / 2 + 1, s, GetPlatform(platformList, x, p.yTop));
                }
                else
                {
                    Platform p2 = GetPlatform(platformList, x, p.yTop);
                    int vx = trajectoryAdder.rectangleSimulator.CalculateMinVelocity(platformList, p, true);
                    trajectoryAdder.AddBigHoleAdj(ref platformList, ref p, vx, MoveType.WIDEADJ, p.leftEdge - 1, s, p2);
                }
            }
        }

        public void GenerateBigHoleDrop(ref List<Platform> platformList, int k, int i)
        {
            Platform p = platformList[k];
            RectangleShape.Shape s = RectangleShape.Shape.SQUARE; //Shape not critical
            // Right moves
            int x = p.rightEdge + 1;
            while (x < GameInfo.LEVEL_MAP_WIDTH && levelMapRectangle[x, p.yTop] != PixelType.OBSTACLE && levelMapRectangle[x, p.yTop] != PixelType.PLATFORM)
            {
                x++;
            }
            if (levelMapRectangle[x, p.yTop] == PixelType.PLATFORM &&
                x - p.rightEdge > 1 + RectangleShape.width(RectangleShape.Shape.HORIZONTAL) / 2 &&
                x - p.rightEdge < 16)
            {
                trajectoryAdder.AddBigHoleDrop(ref platformList, ref p, x - p.rightEdge, MoveType.BIGHOLEDROP, (x + p.rightEdge) / 2 + 1, s, new Platform(-1));
            }
            // Left moves
            x = p.leftEdge - 1;
            while (x >= 0 && levelMapRectangle[x, p.yTop] != PixelType.OBSTACLE && levelMapRectangle[x, p.yTop] != PixelType.PLATFORM)
            {
                x--;
            }
            if (levelMapRectangle[x, p.yTop] == PixelType.PLATFORM &&
                p.leftEdge - x > 1 + RectangleShape.width(RectangleShape.Shape.HORIZONTAL) / 2 &&
                p.leftEdge - x < 16)
            {
                trajectoryAdder.AddBigHoleDrop(ref platformList, ref p, x - p.leftEdge, MoveType.BIGHOLEDROP, (x + p.leftEdge) / 2 + 1, s, new Platform(-1));
            }
        }

        public void GenerateNoMoveR(ref List<Platform> platformList, int k, int x)
        {
            Platform p = platformList[k];
            foreach (RectangleShape.Shape s in GameInfo.SHAPES)
            {
                if (p.shapes[(int)s])
                {
                    trajectoryAdder.AddNoMoveR(ref platformList, ref p, 0, MoveType.NOMOVE, x, s, p);
                }
            }
        }

        public void GenerateFallR(ref List<Platform> platformList, int k, int i, RectangleShape.Shape s)
        {
            Platform p = platformList[k];
            int vx = (i + 1) * GameInfo.VELOCITY_STEP_RECTANGLE;
            if (levelMapRectangle[p.rightEdge + 1, p.yTop] != PixelType.PLATFORM && levelMapRectangle[p.rightEdge + 1, p.yTop] != PixelType.OBSTACLE)
            {
                int min_left = p.leftEdge;
                while (min_left >= 0 && levelMapRectangle[min_left, p.yTop] == PixelType.PLATFORM)
                {
                    min_left--;
                }
                if (i == 0 || trajectoryAdder.rectangleSimulator.EnoughSpaceToAccelerate(min_left, p.rightEdge, p.rightEdge, vx))
                {
                    trajectoryAdder.AddFallR(ref platformList, ref p, vx, MoveType.FALL, p.rightEdge + Math.Max(5 - i / 2, 1), s, new Platform(-1));
                    trajectoryAdder.AddFallR(ref platformList, ref p, vx, MoveType.FALL, p.rightEdge + Math.Max(5 - i / 2, 1), s, new Platform(-1), Moves.MOVE_LEFT);
                    trajectoryAdder.AddFallR(ref platformList, ref p, vx, MoveType.FALL, p.rightEdge + Math.Max(5 - i / 2, 1), s, new Platform(-1), Moves.MOVE_RIGHT);
                }
            }
            if (levelMapRectangle[p.leftEdge - 1, p.yTop] != PixelType.PLATFORM && levelMapRectangle[p.leftEdge - 1, p.yTop] != PixelType.OBSTACLE)
            {
                int max_right = p.rightEdge;
                while (max_right < GameInfo.LEVEL_MAP_WIDTH && levelMapRectangle[max_right, p.yTop] == PixelType.PLATFORM)
                {
                    max_right++;
                }
                if (i == 0 || trajectoryAdder.rectangleSimulator.EnoughSpaceToAccelerate(p.leftEdge, max_right, p.leftEdge, -vx))
                {
                    trajectoryAdder.AddFallR(ref platformList, ref p, -vx, MoveType.FALL, p.leftEdge - Math.Max(5 - i / 2, 1), s, new Platform(-1));
                    trajectoryAdder.AddFallR(ref platformList, ref p, -vx, MoveType.FALL, p.leftEdge - Math.Max(5 - i / 2, 1), s, new Platform(-1), Moves.MOVE_LEFT);
                    trajectoryAdder.AddFallR(ref platformList, ref p, -vx, MoveType.FALL, p.leftEdge - Math.Max(5 - i / 2, 1), s, new Platform(-1), Moves.MOVE_RIGHT);
                }
            }
        }

        public void GenerateNoMoveC(ref List<Platform> platformList, int k, int x)
        {
            Platform p = platformList[k];
            trajectoryAdder.AddNoMoveC(ref platformList, ref p, 0, MoveType.NOMOVE, x);
        }

        public void GenerateFallC(ref List<Platform> platformList, int k, int i, int velocity_step)
        {
            Platform p = platformList[k];
            int vx = (i + 1) * velocity_step;
            if (trajectoryAdder.circleSimulator.EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, p.rightEdge + Math.Max(0, 8 - i), vx))
            {
                trajectoryAdder.AddFallC(ref platformList, ref p, vx, MoveType.FALL, p.rightEdge + Math.Max(0, 8 - i));
            }
            if (trajectoryAdder.circleSimulator.EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, p.leftEdge - Math.Max(0, 8 - i), -vx))
            {
                trajectoryAdder.AddFallC(ref platformList, ref p, -vx, MoveType.FALL, p.leftEdge - Math.Max(0, 8 - i));
            }
        }

        public void GenerateJump(ref List<Platform> platformList, int k, int i, int x, int velocity_step, LevelMapRectangle levelMapRectangle)
        {
            Platform p = platformList[k];
            int vx = i * velocity_step;
            
            if (trajectoryAdder.circleSimulator.EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, x, vx))
            {
                trajectoryAdder.AddJump(ref platformList, ref p, vx, MoveType.JUMP, x, levelMapRectangle);
            }
            if (trajectoryAdder.circleSimulator.EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, x, -vx))
            {
                trajectoryAdder.AddJump(ref platformList, ref p, -vx, MoveType.JUMP, x, levelMapRectangle);
            }
        }

        public MoveInformation GenerateNewJump(List<Platform> platformList, Platform departurePlatform, Platform landingPlatform, int xLandPoint, int yLandPoint)
        {
            int velocity_step = GameInfo.VELOCITY_STEP_PHISICS;
            // Here we calculate from where the circle needs to jump, and with which velocity, to reach the rectangle without intersecting it
            float height = yLandPoint * GameInfo.PIXEL_LENGTH - (yLandPoint * GameInfo.PIXEL_LENGTH + GameInfo.CIRCLE_RADIUS);
            RectangleShape.Shape shape = RectangleShape.GetShape(new RectangleRepresentation(0, 0, 0, 0, height));
            MoveInformation best_jump = null;
            float width = GameInfo.RECTANGLE_AREA / height;
            float minx = GameInfo.PIXEL_LENGTH * xLandPoint - width / 2;
            float maxx = GameInfo.PIXEL_LENGTH * xLandPoint + width / 2;
            float miny = GameInfo.PIXEL_LENGTH * yLandPoint + GameInfo.CIRCLE_RADIUS;
            float maxy = miny + height;
            // Lets temporarily suppose it is horizontal shape
            for (int x = departurePlatform.leftEdge; x <= departurePlatform.rightEdge; x++)
            {
                for (int i = 0; i < GameInfo.NUM_VELOCITIES_PHISICS; i++)
                {
                    int v_x = velocity_step * i;
                    if(trajectoryAdder.circleSimulator.EnoughSpaceToAccelerate(departurePlatform.leftEdge, departurePlatform.rightEdge, x, v_x))
                    {
                        MoveInformation aux = new MoveInformation(landingPlatform, departurePlatform, x, xLandPoint, v_x, MoveType.JUMP, new List<int>(), new List<Tuple<float, float>>(), 10); ;
                        List<MoveInformation> moves = trajectoryAdder.circleSimulator.SimulateMove(ref platformList,
                            x * GameInfo.PIXEL_LENGTH,
                            (departurePlatform.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH,
                            v_x,
                            GameInfo.JUMP_VELOCITYY,
                            ref aux, 0.015f);
                        MoveInformation new_jump = null;
                        foreach (MoveInformation m in moves)
                        {
                            if(m.landingPlatform.id == landingPlatform.id)
                            {
                                new_jump = m;
                                break;
                            }
                        }
                        if (new_jump != null)
                        {
                            // Check if the new move intersects
                            bool intersects = false;
                            foreach (Tuple<float, float> tup in new_jump.path)
                            {
                                if (tup.Item1 + GameInfo.CIRCLE_RADIUS > minx && tup.Item1 - GameInfo.CIRCLE_RADIUS < maxx && tup.Item2 + GameInfo.CIRCLE_RADIUS > miny && tup.Item2 - GameInfo.CIRCLE_RADIUS < maxy)
                                {
                                    intersects = true;
                                    break;
                                }
                            }
                            if (!intersects)
                            {
                                if (best_jump == null || new_jump.CompareCircle(best_jump, initialCollectiblesInfo, platformList) == -1)
                                {
                                    best_jump = new_jump;
                                }
                            }
                        }
                    }
                }
            }
            return best_jump;
        }
    }
}