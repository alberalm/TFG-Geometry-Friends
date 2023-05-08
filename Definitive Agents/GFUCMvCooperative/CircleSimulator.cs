using GeometryFriends.AI;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using static GeometryFriendsAgents.LevelMap;

namespace GeometryFriendsAgents
{
    public class CircleSimulator
    {
        public CollectibleRepresentation[] initialCollectiblesInfo;
        public PixelType[,] levelMap;

        List<MoveInformation> parabolas = new List<MoveInformation>();

        public CircleSimulator(CollectibleRepresentation[] initialCollectiblesInfo, PixelType[,] levelMap)
        {
            this.initialCollectiblesInfo = initialCollectiblesInfo;
            this.levelMap = levelMap;
        }

        public Platform GetPlatform(ref List<Platform> platformList, int x, int y)
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
                if (levelMap[x + i, y + j] == PixelType.RECTANGLE)
                {
                    xcollide = x + i;
                    ycollide = y + j;
                    break;
                }
            }

            foreach (Platform p in platformList)
            {
                if (p.yTop == ycollide && p.leftEdge <= xcollide && xcollide <= p.rightEdge)
                {
                    return p;
                }
            }         
            return new Platform(-2);
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

        public bool EnoughSpaceToAccelerate(int leftEdge, int rigthEdge, int x, int vx)
        {
            if (Math.Abs(vx) == GameInfo.VELOCITY_STEP_PHISICS || vx==0)
            {
                return true;
            }
            if (vx > 0)
            {
                return vx * vx <= 2 * GameInfo.CIRCLE_ACCELERATION * GameInfo.PIXEL_LENGTH * (x - leftEdge - 1);
            }
            else
            {
                return vx * vx <= 2 * GameInfo.CIRCLE_ACCELERATION * GameInfo.PIXEL_LENGTH * (rigthEdge - 1 - x);
            }
        }
        private List<int> CollectDiamonds(float x_tfloat, float y_tfloat)
        {
            List<int> l = new List<int>();
            for (int i = 0; i < initialCollectiblesInfo.Length; i++)
            {
                CollectibleRepresentation coll = initialCollectiblesInfo[i];
                if(Math.Abs(coll.X- x_tfloat)+ Math.Abs(coll.Y - y_tfloat)< GameInfo.SEMI_COLLECTIBLE_HEIGHT + GameInfo.CIRCLE_RADIUS + 3)
                {
                    l.Add(i);
                }
            }
            return l;
        }

        public List<MoveInformation> SimulateMove(ref List<Platform> platformList, float x_0, float y_0, float vx_0, float vy_0, ref MoveInformation m, float dt)
        {
            bool flag = false;
            if (vy_0 == 0)
            {
                flag = true;
            }
            
            float t = 0;
            float x_tfloat = x_0;
            float y_tfloat = y_0;
            int x_t = (int)(x_0 / GameInfo.PIXEL_LENGTH); // Center of the ball
            int y_t = (int)(y_0 / GameInfo.PIXEL_LENGTH); // Center of the ball

            int NUM_REBOUNDS = 10;
            int j = 0;
            List<MoveInformation> moves = new List<MoveInformation>();
            CollisionType cct = CircleIntersectsWithObstacle(x_t, y_t);
            
            while (cct != CollisionType.Bottom && j <= NUM_REBOUNDS)
            {
                m.path.Add(new Tuple<float, float>(x_tfloat, y_tfloat));
                List<int> diamonds_collected = CollectDiamonds(x_tfloat, y_tfloat);
                if (diamonds_collected.Count>0)
                {
                    foreach(int d in diamonds_collected)
                    {
                        if (!m.diamondsCollected.Contains(d))
                        {
                            m.diamondsCollected.Add(d);
                        }
                    }                    
                }
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
                    return new List<MoveInformation>();
                }
                else if (cct == CollisionType.None)
                {

                }
                else if(cct == CollisionType.Agent)
                {
                    if(vy_0 - t * GameInfo.GRAVITY <= 0)
                    {
                        break;
                    }
                }
                else //Left, Right, Top or Agent
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

            if (cct != CollisionType.Bottom && cct != CollisionType.Agent)
            {
                m.landingPlatform = new Platform(-2);
                return new List<MoveInformation>();
            }
            m.xlandPoint = x_t;
            m.landingPlatform = GetPlatform(ref platformList, x_t, y_t);
            for (int i = 0; i < m.diamondsCollected.Count; i++)
            {
                int d = m.diamondsCollected[i];
                if (BelongsToPlatform(m.departurePlatform, initialCollectiblesInfo[d]) || BelongsToPlatform(m.landingPlatform, initialCollectiblesInfo[d]))
                {
                    m.diamondsCollected.RemoveAt(i);
                    i--;
                }
            }
            m.rightEdgeIsDangerous = vx_0 > 0;
            if (flag)
            {
                parabolas.Add(m);
            }
            
            if (cct == CollisionType.Agent)
            {
                MoveInformation newm = new MoveInformation(m);
                int y = y_t;

                while (y_t == y) { 
                    t += dt;
                    x_tfloat = x_0 + vx_0 * t;
                    y_tfloat = (float)(y_0 - vy_0 * t + GameInfo.GRAVITY * Math.Pow(t, 2) / 2);
                    x_t = (int)(x_tfloat / GameInfo.PIXEL_LENGTH);
                    y_t = (int)(y_tfloat / GameInfo.PIXEL_LENGTH);
                }
                
                moves = SimulateMove(ref platformList, x_tfloat, y_tfloat, vx_0, vy_0 - t * GameInfo.GRAVITY, ref newm, dt);
            }
            moves.Add(m);
            return moves;
        }

        private Tuple<float, float> NewVelocityAfterCollision(float vx, float vy, CollisionType cct) // Do not call this function with cct=other, bottom, agent or none
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

        protected bool BelongsToPlatform(Platform platform, CollectibleRepresentation d)
        {
            return platform.yTop - d.Y / GameInfo.PIXEL_LENGTH <= 2 * GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH + GameInfo.COLLECTIBLE_SIZE[2]
                && platform.leftEdge <= d.X / GameInfo.PIXEL_LENGTH && platform.rightEdge >= d.X / GameInfo.PIXEL_LENGTH;
        }

        // Returns a value of type CircleCollision={Top, Right, Bottom, Left, Diamond, None}
        public CollisionType CircleIntersectsWithObstacle(int x, int y) //(x,y) is the center of the circle given in levelMapCircle coordinates
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
                        else if (levelMap[x + i, y + j] == PixelType.RECTANGLE && j==4)
                        {
                            return CollisionType.Agent;
                        }
                    }
                    else
                    {
                        return CollisionType.Other;
                    }
                }
            }
            if (diamond)
            {
                return CollisionType.Diamond;
            }
            return CollisionType.None;
        }

        public int GetDiamondCollected(int x, int y)//Check this function. Possible bugs
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
    }
}