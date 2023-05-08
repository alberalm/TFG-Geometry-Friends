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
            for (int i = 0; i < platformList.Count; i++)
            {
                platformList[i].id = i;
            }
        }

        protected override void GenerateMoveInformation()
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
                Parallel.For(0, num_velocities, i =>
                {
                    moveGenerator.GenerateFallC(ref platformList, k, i, velocity_step);
                });

                // Parabolic JUMPS
                Parallel.For(p.leftEdge + 1, p.rightEdge, x =>
                {
                    Parallel.For(0, num_velocities + 1, i =>
                    {
                        moveGenerator.GenerateJump(ref platformList, k, i, x, velocity_step);
                    });
                });

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

        public void SimulateMove(float x_0, float y_0, float vx_0, float vy_0, ref MoveInformation m)
        {
            moveGenerator.trajectoryAdder.circleSimulator.SimulateMove(ref platformList, x_0, y_0, vx_0, vy_0, ref m);
        }

        public void DrawConnectionsVertex(ref List<DebugInformation> debugInformation)
        {
            moveGenerator.trajectoryAdder.circleSimulator.DrawConnectionsVertex(ref debugInformation);
        }
    }
}
