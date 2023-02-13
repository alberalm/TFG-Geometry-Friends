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
    class LevelMapCircle : LevelMap
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
            bool prevPlatform = false;
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
                    CollisionType col = CircleIntersectsWithObstacle(x, yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH);
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
                CollisionType col = CircleIntersectsWithObstacle(x, yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH);
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

        // Returns a value of type CircleCollision={Top, Right, Bottom, Left, Diamond, None}
        private CollisionType CircleIntersectsWithObstacle(int x, int y) //(x,y) is the denter of the circle given in levelMap coordinates
        {
            bool diamond = false; // Obstacles and platforms have more priority than diamonds
            for (int i = -GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i < GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i++)
            {
                for (int j = -GameInfo.CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j < GameInfo.CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j++)
                {
                    if ((x + i) >= 0 && (y + j) >= 0 && (x + i) < levelMap.GetLength(0) && (y + j) < levelMap.GetLength(1)) // Check
                    {
                        if (levelMap[x + i, y + j] == PixelType.OBSTACLE || levelMap[x + i, y + j] == PixelType.PLATFORM)
                        {
                            if (i == -5)
                            {
                                return CollisionType.Left;
                            }
                            else if (i == 4)
                            {
                                return CollisionType.Right;
                            }
                            else if (j == -5)
                            {
                                return CollisionType.Top;
                            }
                            else if (j == 4)
                            {
                                return CollisionType.Bottom;
                            }
                            else
                            {
                                return CollisionType.Other;
                            }
                        }
                        else if (levelMap[x + i, y + j] == PixelType.DIAMOND && j >= -GameInfo.COLLECTIBLE_INTERSECTION[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH] && j < GameInfo.COLLECTIBLE_INTERSECTION[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH])
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
                    int vx = (i + 1) * velocity_step;
                    if (EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, p.rightEdge + Math.Max(0, 8 - i), vx))
                    {
                        AddTrajectory(ref p, vx, MoveType.FALL, p.rightEdge + Math.Max(0, 8 - i));
                    }
                    if (EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, p.leftEdge - Math.Max(0, 8 - i), -vx))
                    {
                        AddTrajectory(ref p, -vx, MoveType.FALL, p.leftEdge - Math.Max(0, 8 - i));
                    }
                });

                // Parabolic JUMPS
                Parallel.For(p.leftEdge + 1, p.rightEdge, x =>
                {
                    Parallel.For(0, num_velocities + 1, i =>

                    {
                        int vx = i * velocity_step;
                        if (EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, x, vx))
                        {
                            AddTrajectory(ref p, vx, MoveType.JUMP, x);
                        }
                        if (EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, x, -vx))
                        {
                            AddTrajectory(ref p, -vx, MoveType.JUMP, x);
                        }
                    });
                });

                Parallel.For(p.leftEdge, p.rightEdge + 1, x =>
                {
                    AddTrajectory(ref p, 0, MoveType.NOMOVE, x);
                });
            }
        }

        private void AddTrajectory(ref Platform p, int vx, MoveType moveType, int x)
        {
            // Any trajectory with distance <= 10 should be safe to not collide (?)
            MoveInformation m = new MoveInformation(new Platform(-1), p, x, 0, vx, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);

            if (moveType == MoveType.JUMP)
            {
                SimulateMove(x * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, vx, (int)GameInfo.JUMP_VELOCITYY, ref m);
            }
            else if (moveType == MoveType.FALL)
            {
                SimulateMove(x * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, vx, 0, ref m);
            }
            else if (moveType == MoveType.NOMOVE)
            {
                if (CircleIntersectsWithObstacle(x, p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) == CollisionType.Diamond)
                {
                    m.landingPlatform = p;
                    m.xlandPoint = x;
                    m.path.Add(new Tuple<float, float>(x * GameInfo.PIXEL_LENGTH, (p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH));
                    m.diamondsCollected.Add(GetDiamondCollected(x, p.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH));
                }
            }
            lock (platformList)
            {
                bool addIt = true;
                if (m.landingPlatform.id != -2 && m.landingPlatform.id != -1 && (m.diamondsCollected.Count > 0 || p.id != m.landingPlatform.id))
                {
                    for (int i = 0; i < p.moveInfoList.Count; i++)
                    {
                        int add = m.CompareCircle(p.moveInfoList[i], initialCollectiblesInfo);
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
        List<MoveInformation> parabolas = new List<MoveInformation>();
        public void SimulateMove(float x_0, float y_0, float vx_0, float vy_0, ref MoveInformation m)
        {
            bool flag = false;
            if (vy_0 == 0)
            {
                flag = true;
            }
            float dt = 0.005f;
            float t = 0;
            float x_tfloat = x_0;
            float y_tfloat = y_0;
            int x_t = (int)(x_0 / GameInfo.PIXEL_LENGTH); // Center of the ball
            int y_t = (int)(y_0 / GameInfo.PIXEL_LENGTH); // Center of the ball

            int NUM_REBOUNDS = 10;
            int j = 0;
            CollisionType cct = CircleIntersectsWithObstacle(x_t, y_t);
            while (cct != CollisionType.Bottom && j <= NUM_REBOUNDS)
            {
                m.path.Add(new Tuple<float, float>(x_tfloat, y_tfloat));
                if (cct == CollisionType.Diamond)
                {
                    int d = GetDiamondCollected(x_t, y_t);
                    if (!m.diamondsCollected.Contains(d))
                    {
                        m.diamondsCollected.Add(d);
                    }
                }
                else if (cct == CollisionType.Other)
                {
                    m.landingPlatform = new Platform(-2);
                    return;
                }
                else if (cct == CollisionType.None)
                {

                }
                else //Left, Right or Top
                {
                    Tuple<float, float> new_v = NewVelocityAfterCollision(vx_0, (float)(vy_0 - GameInfo.GRAVITY * (t - dt)), cct);
                    x_0 = x_0 + vx_0 * (t - dt);
                    y_0 = y_0 - vy_0 * (t - dt) + (float)(GameInfo.GRAVITY * Math.Pow((t - dt), 2) / 2);
                    vx_0 = new_v.Item1;
                    vy_0 = new_v.Item2;
                    t = 0;
                    j++;
                }
                // Calculate if distance to obstacle is low, but only if obstacle is not below or above
                if (m.distanceToObstacle > (int)(GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH))
                {
                    for (int i = -m.distanceToObstacle; i < m.distanceToObstacle; i++)
                    {
                        for (int k = 1 - (int)(GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH); k < (int)(GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH); k++)
                        {
                            if (x_t + i >= 0 && x_t + i < GameInfo.LEVEL_MAP_WIDTH && (levelMap[x_t + i, y_t + k] == PixelType.OBSTACLE || levelMap[x_t + i, y_t + k] == PixelType.PLATFORM))
                            {
                                if ((int)(i * i + k * k) < m.distanceToObstacle * m.distanceToObstacle)
                                {
                                    m.distanceToObstacle = (int)Math.Sqrt(i * i + k * k);
                                }
                            }
                        }
                    }
                }
                t += dt;
                x_tfloat = x_0 + vx_0 * t;
                y_tfloat = (float)(y_0 - vy_0 * t + GameInfo.GRAVITY * Math.Pow(t, 2) / 2);
                x_t = (int)(x_tfloat / GameInfo.PIXEL_LENGTH);
                y_t = (int)(y_tfloat / GameInfo.PIXEL_LENGTH);
                cct = CircleIntersectsWithObstacle(x_t, y_t);
            }

            if (cct != CollisionType.Bottom)
            {
                m.landingPlatform = new Platform(-2);
                return;
            }
            m.xlandPoint = x_t;
            m.landingPlatform = GetPlatform(x_t, y_t);
            for (int i = 0; i < m.diamondsCollected.Count; i++)
            {
                int d = m.diamondsCollected[i];
                if (BelongsToPlatform(m.departurePlatform, initialCollectiblesInfo[d]) || BelongsToPlatform(m.landingPlatform, initialCollectiblesInfo[d]))
                {
                    m.diamondsCollected.RemoveAt(i);
                    i--;
                }
            }
            m.RightEdgeIsDangerous = vx_0 > 0;
            if (flag)
            {
                parabolas.Add(m);
            }
        }

        private Tuple<float, float> NewVelocityAfterCollision(float vx, float vy, CollisionType cct) // Do not call this function with cct=other, bottom or none
        {
            // TODO -> Train an AI?
            if (cct == CollisionType.Left || cct == CollisionType.Right)
            {
                return new Tuple<float, float>(-vx / 3, vy / 3);
            }
            else if (cct == CollisionType.Top)
            {
                return new Tuple<float, float>(vx / 3, -vy / 3);
            }
            else
            {
                return new Tuple<float, float>(0, 0);
            }
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

        protected int GetDiamondCollected(int x, int y)//Check this function. Possible bugs
        {
            int min = 0;
            float d = 10;
            for (int i = 0; i < initialCollectiblesInfo.Length; i++)
            {
                CollectibleRepresentation coll = initialCollectiblesInfo[i];
                float dist = (float)(Math.Pow(coll.X / GameInfo.PIXEL_LENGTH - x, 2) + Math.Pow(coll.Y / GameInfo.PIXEL_LENGTH - y, 2)) / GameInfo.PIXEL_LENGTH;
                if (dist <= 5 + 3 / Math.Sqrt(2)) // Sufficiently close to be the diamond it collides with
                {
                    min = i;
                    break;
                }
                if (dist < d)
                {
                    min = i;
                    d = dist;
                }
            }

            return min;
        }

        protected override bool EnoughSpaceToAccelerate(int leftEdge, int rigthEdge, int x, int vx)
        {
            if (vx > 0)
            {
                return vx * vx <= 2 * GameInfo.CIRCLE_ACCELERATION * GameInfo.PIXEL_LENGTH * (x - leftEdge - 1);//Más conservador que como estaba
            }
            else
            {
                return vx * vx <= 2 * GameInfo.CIRCLE_ACCELERATION * GameInfo.PIXEL_LENGTH * (rigthEdge - 1 - x);//Más conservador que como estaba
            }
        }

        public void DrawConnectionsVertex(ref List<DebugInformation> debugInformation)
        {
            GeometryFriends.XNAStub.Color color = GeometryFriends.XNAStub.Color.Purple;

            foreach (MoveInformation parabola in parabolas)
            {
                if (parabola.velocityX % 30 == 0)
                {
                    color = GeometryFriends.XNAStub.Color.Purple;
                }
                else
                {
                    color = GeometryFriends.XNAStub.Color.DeepPink;
                }
                foreach (Tuple<float, float> tup in parabola.path)
                {
                    debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 1, color));

                }
            }
            
        }
    }
}
