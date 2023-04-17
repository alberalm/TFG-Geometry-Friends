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
    public class LevelMapCircle : LevelMap
    {
        Dictionary<int, Platform> small_circle_to_small_rectangle = new Dictionary<int, Platform>();

        public Platform PlatformBelowCircle(CircleRepresentation cI)
        {
            for (int i = 0; i < platformList.Count; i++)
            {
                if (cI.Y / GameInfo.PIXEL_LENGTH < platformList[i].yTop && cI.X / GameInfo.PIXEL_LENGTH >= platformList[i].leftEdge && cI.X / GameInfo.PIXEL_LENGTH <= platformList[i].rightEdge)
                {
                    return platformList[i];
                }
            }
            return new Platform(-1);
        }

        public override Platform GetPlatform(int x, int y)
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

        public Platform CirclePlatform(CircleRepresentation cI)
        {
            for (int i = 0; i < platformList.Count; i++)
            {
                if (cI.Y / GameInfo.PIXEL_LENGTH + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH <= platformList[i].yTop + 1 &&
                    cI.Y / GameInfo.PIXEL_LENGTH + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH >= platformList[i].yTop - 10 &&
                    cI.X / GameInfo.PIXEL_LENGTH >= platformList[i].leftEdge - 1 && cI.X / GameInfo.PIXEL_LENGTH <= platformList[i].rightEdge + 1)
                {
                    return platformList[i];
                }
            }
            return new Platform(-1);
        }

        public bool AtBorder(CircleRepresentation cI, Platform p, ref Moves currentAction, List<MoveInformation> plan)
        {
            if (p.id != -1)
            {
                if (!GameInfo.PHYSICS && plan.Count > 0 && plan[0].moveType == MoveType.FALL)
                {
                    return false;
                }
                if (Math.Abs(p.rightEdge - cI.X / GameInfo.PIXEL_LENGTH) <= 1) // Ball at right edge
                {
                    currentAction = Moves.ROLL_LEFT;
                    return true;
                }
                else if (Math.Abs(p.leftEdge - cI.X / GameInfo.PIXEL_LENGTH) <= 1) // Ball at left edge
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

        protected override void IdentifyPlatforms(ObstacleRepresentation[] oI)
        {
            bool prevPlatform;
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
                    CollisionType col = moveGenerator.trajectoryAdder.circleSimulator.CircleIntersectsWithObstacle(x, yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH);
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

        protected override void IdentifyDefaultPlatforms()
        {
            //Bottom obstacle
            bool prevPlatform = false;
            int xleft = 0;
            for (int x = GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; x < GameInfo.LEVEL_MAP_WIDTH - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; x++)
            {
                int yTop = GameInfo.LEVEL_MAP_HEIGHT - 5;
                CollisionType col = moveGenerator.trajectoryAdder.circleSimulator.CircleIntersectsWithObstacle(x, yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH);
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

        protected override void PlatformUnion()
        {
            for (int i = 0; i < platformList.Count(); i++)
            {
                Platform p1 = platformList[i];
                for (int j = i + 1; j < platformList.Count(); j++)
                {
                    Platform p2 = platformList[j];
                    bool join = false;

                    if (p1.yTop == p2.yTop && ((p1.real && p2.real) ||
                        (!p1.real && !p2.real && small_circle_to_small_rectangle[p1.id].real == small_circle_to_small_rectangle[p2.id].real)))
                    {
                        if (p1.rightEdge + 1 == p2.leftEdge)
                        {
                            if (p1.real)
                            {
                                levelMap[p1.rightEdge + 1, p1.yTop] = PixelType.PLATFORM;
                            }
                            join = true;
                        }
                        else if (p2.rightEdge + 1 == p1.leftEdge)
                        {
                            if (p1.real)
                            {
                                levelMap[p2.rightEdge + 1, p1.yTop] = PixelType.PLATFORM;
                            }
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
                            if (!p2.real)
                            {
                                small_circle_to_small_rectangle.Remove(p2.id);
                            }
                            platformList.Remove(p2);
                            i--;
                            break;
                        }
                    }
                    else if(p1.yTop == p2.yTop && !p1.real && !p2.real) // ADJACENT moves
                    {
                        if (p1.rightEdge + 1 == p2.leftEdge || p2.leftEdge == p1.rightEdge)
                        {
                            moveGenerator.trajectoryAdder.AddAdjacent(ref platformList, ref p1, 1, p1.rightEdge, RectangleShape.Shape.HORIZONTAL, p2);
                            moveGenerator.trajectoryAdder.AddAdjacent(ref platformList, ref p2, -1, p2.leftEdge, RectangleShape.Shape.HORIZONTAL, p1);
                        }
                        else if (p2.rightEdge + 1 == p1.leftEdge || p1.leftEdge == p2.rightEdge)
                        {
                            moveGenerator.trajectoryAdder.AddAdjacent(ref platformList, ref p2, 1, p2.rightEdge, RectangleShape.Shape.HORIZONTAL, p1);
                            moveGenerator.trajectoryAdder.AddAdjacent(ref platformList, ref p1, -1, p1.leftEdge, RectangleShape.Shape.HORIZONTAL, p2);
                        }
                    }
                }
            }
            // Rename id
            Dictionary<int, Platform> aux_dict = new Dictionary<int, Platform>();
            platformList.Sort();
            for (int i = 0; i < platformList.Count; i++)
            {
                if (!platformList[i].real)
                {
                    aux_dict.Add(i, small_circle_to_small_rectangle[platformList[i].id]);
                }
                platformList[i].id = i;
            }
            small_circle_to_small_rectangle = aux_dict;
        }

        public override void GenerateMoveInformation()
        {
            int num_velocities, velocity_step;
            if (GameInfo.PHYSICS)
            {
                num_velocities = GameInfo.NUM_VELOCITIES_PHISICS;
                velocity_step = GameInfo.VELOCITY_STEP_PHISICS;
            }
            else
            {
                num_velocities = GameInfo.NUM_VELOCITIES_QLEARNING;
                velocity_step = GameInfo.VELOCITY_STEP_QLEARNING;
            }
            for (int k = 0; k < platformList.Count; k++)
            {
                Platform p = platformList[k];

                // Parabolic FALLS
                if (p.real)
                {
                    Parallel.For(0, num_velocities, i =>
                    {
                        moveGenerator.GenerateFallC(ref platformList, k, i, velocity_step);
                    });
                }

                // Parabolic JUMPS

                Parallel.For(p.leftEdge + 1, p.rightEdge, x =>
                //for (int x = p.leftEdge + 1; x < p.rightEdge; x++)
                {
                    if (p.real)
                    {
                        Parallel.For(0, num_velocities + 1, i =>
                        //for (int i = 0; i < num_velocities + 1; i++)
                        {
                            moveGenerator.GenerateJump(ref platformList, k, i, x, velocity_step);
                        });
                        //}
                    }
                    else
                    {
                        Parallel.For(0, 2, i =>
                        {
                            moveGenerator.GenerateJump(ref platformList, k, i, x, velocity_step);
                        });
                    }
                });
                //}

                Parallel.For(p.leftEdge, p.rightEdge + 1, x =>
                {
                    moveGenerator.GenerateNoMoveC(ref platformList, k, x);
                });
            }
        }

        private void AddTrajectory(ref Platform p, int vx, MoveType moveType, int x)
        {
            // Any trajectory with distance <= 10 should be safe to not collide (?)
            MoveInformation m = new MoveInformation(new Platform(-1), p, x, 0, vx, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);

            if (moveType == MoveType.JUMP)
            {

            }
            else if (moveType == MoveType.FALL)
            {

            }
            else if (moveType == MoveType.NOMOVE)
            {
                
            }
        }

        public List<MoveInformation> SimulateMove(float x_0, float y_0, float vx_0, float vy_0, ref MoveInformation m)
        {
            return moveGenerator.trajectoryAdder.circleSimulator.SimulateMove(ref platformList, x_0, y_0, vx_0, vy_0, ref m);
        }

        public void DrawConnectionsVertex(ref List<DebugInformation> debugInformation)
        {
            moveGenerator.trajectoryAdder.circleSimulator.DrawConnectionsVertex(ref debugInformation);
        }

        public void AddCooperative(List<Platform> platformListRectangle)
        {
            foreach (Platform p in platformListRectangle)
            {
                foreach (RectangleShape.Shape s in GameInfo.SHAPES)
                {
                    if (p.shapes[(int)s])
                    {
                        Platform new_platform = new Platform(platformList.Count);
                        new_platform.yTop = p.yTop - RectangleShape.height(s);
                        new_platform.real = false;
                        new_platform.shapes[(int)s] = true; // This is the height of the platform above the real platform where the rectangle is
                        for (int x = p.leftEdge; x <= p.rightEdge; x++)
                        {
                            CollisionType col = moveGenerator.trajectoryAdder.circleSimulator.CircleIntersectsWithObstacle(x, new_platform.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH);
                            if (col != CollisionType.None && col != CollisionType.Diamond && col != CollisionType.Agent)
                            {
                                if (new_platform.leftEdge != 0)
                                {
                                    platformList.Add(new_platform);
                                    small_circle_to_small_rectangle.Add(new_platform.id, p);
                                    new_platform = new Platform(platformList.Count);
                                    new_platform.real = false;
                                    new_platform.shapes[(int)s] = true;
                                    new_platform.yTop = p.yTop - RectangleShape.height(s);
                                }
                            }
                            else
                            {
                                if (new_platform.leftEdge == 0)
                                {
                                    new_platform.leftEdge = x;
                                }
                                new_platform.rightEdge = x;
                                levelMap[x, new_platform.yTop] = PixelType.RECTANGLE;
                            }
                        }
                        if (new_platform.leftEdge != 0)
                        {
                            platformList.Add(new_platform);
                            small_circle_to_small_rectangle.Add(new_platform.id, p);
                        }
                    }
                }
            }

            PlatformUnion();
        }

        public void MergeCooperative(ref Dictionary<int,int> circle_to_rectangle, Dictionary<Platform, Platform> small_to_simplified_rectangle)
        {
            foreach (Platform p in platformList)
            {
                if (p.real)
                {
                    simplified_platforms.Add(new Platform(p) { id = simplified_platforms.Count });
                    simplified_to_small.Add(simplified_platforms[simplified_platforms.Count - 1], new List<Platform> { p });
                    small_to_simplified.Add(p, simplified_platforms[simplified_platforms.Count - 1]);
                }
            }

            foreach (Platform p in platformList)
            {
                if (!p.real)
                {
                    bool contained = false;

                    foreach(List<Platform> l in simplified_to_small.Values)
                    {
                        if (l.Contains(p))
                        {
                            contained = true;
                            break;
                        }
                    }

                    if (contained)
                    {
                        continue;
                    }

                    Platform simplified_p = new Platform(simplified_platforms.Count, p.yTop, p.leftEdge, p.rightEdge, new List<MoveInformation>(p.moveInfoList));
                    simplified_p.real = false;
                    List<Platform> small_list_not_real = new List<Platform>{ p };
                    small_to_simplified.Add(p, simplified_p);

                    bool change = true;
                    while (change)
                    {
                        change = false;
                        foreach(Platform platform in platformList)
                        {
                            if (!platform.real &&
                                small_to_simplified_rectangle[small_circle_to_small_rectangle[p.id]] == small_to_simplified_rectangle[small_circle_to_small_rectangle[platform.id]]
                                && !small_list_not_real.Contains(platform) && platform.rightEdge >= simplified_p.leftEdge && platform.leftEdge <= simplified_p.rightEdge)
                            {
                                if (platform.yTop > simplified_p.yTop)
                                {
                                    simplified_p.yTop = platform.yTop;
                                }
                                if (platform.leftEdge < simplified_p.leftEdge)
                                {
                                    simplified_p.leftEdge = platform.leftEdge;
                                }
                                if (platform.rightEdge > simplified_p.rightEdge)
                                {
                                    simplified_p.rightEdge = platform.rightEdge;
                                }
                                foreach (MoveInformation m in platform.moveInfoList)
                                {
                                    simplified_p.moveInfoList.Add(new MoveInformation(m));
                                }

                                small_list_not_real.Add(platform);
                                small_to_simplified.Add(platform, simplified_p);
                                change = true;
                            }
                        }
                    }
                    simplified_to_small.Add(simplified_p, small_list_not_real);
                    simplified_platforms.Add(simplified_p);
                    circle_to_rectangle.Add(simplified_p.id, small_to_simplified_rectangle[small_circle_to_small_rectangle[p.id]].id);
                }
            }

            //Filtrado de movimientos
            foreach (Platform simplified_p in simplified_platforms)
            {
                for (int i = 0; i < simplified_p.moveInfoList.Count; i++)
                {
                    MoveInformation m = simplified_p.moveInfoList[i];
                    m.landingPlatform = small_to_simplified[m.landingPlatform];
                    m.departurePlatform = simplified_p;
                    if((m.landingPlatform.id == m.departurePlatform.id && m.diamondsCollected.Count == 0)
                        || (m.landingPlatform.id != m.departurePlatform.id && !m.landingPlatform.real && !m.departurePlatform.real && m.moveType != MoveType.ADJACENT))
                    {
                        simplified_p.moveInfoList.RemoveAt(i);
                        i--;
                    }
                }
            }

            //Filtrado de movimientos
            foreach (Platform simplified_p in simplified_platforms)
            {
                for (int i = 0; i < simplified_p.moveInfoList.Count; i++)
                {
                    MoveInformation m1 = simplified_p.moveInfoList[i];
                    for (int j = i + 1; j < simplified_p.moveInfoList.Count; j++)
                    {
                        MoveInformation m2 = simplified_p.moveInfoList[j];
                        int comp = m1.CompareCircle(m2, initialCollectiblesInfo, simplified_platforms);
                        if (comp == 1)
                        {
                            simplified_p.moveInfoList.RemoveAt(j);
                            j--;
                        }
                        else if (comp == -1)
                        {
                            simplified_p.moveInfoList.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }
            }
        }
    }
}
