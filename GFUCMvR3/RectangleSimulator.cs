using GeometryFriends.AI;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using static GeometryFriendsAgents.LevelMap;

namespace GeometryFriendsAgents
{
    public class RectangleSimulator
    {
        public CollectibleRepresentation[] initialCollectiblesInfo;
        public PixelType[,] levelMap;
        List<Tuple<float, float>> list_top_left = new List<Tuple<float, float>>();
        List<Tuple<float, float>> list_top_right = new List<Tuple<float, float>>();
        List<Tuple<float, float>> list_bottom_left = new List<Tuple<float, float>>();
        List<Tuple<float, float>> list_bottom_right = new List<Tuple<float, float>>();

        public RectangleSimulator(CollectibleRepresentation[] initialCollectiblesInfo, PixelType[,] levelMap)
        {
            this.initialCollectiblesInfo = initialCollectiblesInfo;
            this.levelMap = levelMap;
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

        public void DrawConnectionsVertex(ref List<DebugInformation> debugInformation)
        {
            foreach (Tuple<float, float> tup in list_bottom_left)
            {
                debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 2, GeometryFriends.XNAStub.Color.Red));

            }
            foreach (Tuple<float, float> tup in list_bottom_right)
            {
                debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 2, GeometryFriends.XNAStub.Color.Orange));

            }
            foreach (Tuple<float, float> tup in list_top_left)
            {
                debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 2, GeometryFriends.XNAStub.Color.Blue));

            }
            foreach (Tuple<float, float> tup in list_top_right)
            {
                debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 2, GeometryFriends.XNAStub.Color.Black));
            }
        }

        public bool EnoughSpaceToAccelerate(int leftEdge, int rightEdge, int x, int vx)
        {
            if (vx > 0)
            {
                return vx * vx <= 2 * GameInfo.RECTANGLE_ACCELERATION * (GameInfo.PIXEL_LENGTH * (x - leftEdge - 1) - (GameInfo.VERTICAL_RECTANGLE_HEIGHT - GameInfo.HORIZONTAL_RECTANGLE_HEIGHT) / 2);
            }
            else
            {
                return vx * vx <= 2 * GameInfo.RECTANGLE_ACCELERATION * (GameInfo.PIXEL_LENGTH * (rightEdge - 1 - x) - (GameInfo.VERTICAL_RECTANGLE_HEIGHT - GameInfo.HORIZONTAL_RECTANGLE_HEIGHT) / 2);
            }
        }

        // returns platforms <left,right>
        public Tuple<Platform, Platform> AdjacentPlatforms(List<Platform> platformList, Platform currentPlatform)
        {
            Platform left = new Platform(-1);
            Platform right = new Platform(-1);
            foreach (Platform p in platformList)
            {
                if (p.rightEdge == currentPlatform.leftEdge - 1 && p.real && p.yTop == currentPlatform.yTop)
                {
                    left = p;
                }
                else if (p.leftEdge == currentPlatform.rightEdge + 1 && p.real && p.yTop == currentPlatform.yTop)
                {
                    right = p;
                }
            }
            return new Tuple<Platform, Platform>(left, right);
        }

        public int CalculateMaxVelocity(List<Platform> platformList, Platform p, int edge)
        {
            Platform next_platform = AdjacentPlatforms(platformList, p).Item1;

            // Squared, need to perform square root at the end
            double target_velocity = 0; // do not count initial platform to leave some margin

            while (next_platform.id != -1 && next_platform.shapes[(int)RectangleShape.Shape.VERTICAL])
            {
                target_velocity += 2 * GameInfo.RECTANGLE_ACCELERATION * (next_platform.rightEdge - next_platform.leftEdge);
                next_platform = AdjacentPlatforms(platformList, next_platform).Item1;
            }

            return (int) Math.Sqrt(target_velocity) * GameInfo.PIXEL_LENGTH;
        }

        public int CalculateMinVelocity(List<Platform> platformList, Platform p, int edge)
        {
            Platform next_platform = AdjacentPlatforms(platformList, p).Item2;

            // Squared, need to perform square root at the end
            double target_velocity = 0; // do not count initial platform to leave some margin

            while (next_platform.id != -1 && next_platform.shapes[(int)RectangleShape.Shape.VERTICAL])
            {
                target_velocity += 2 * GameInfo.RECTANGLE_ACCELERATION * (next_platform.rightEdge - next_platform.leftEdge);
                next_platform = AdjacentPlatforms(platformList, next_platform).Item2;
            }

            return -(int)Math.Sqrt(target_velocity * GameInfo.PIXEL_LENGTH);
        }

        public List<int> GetDiamondCollected(int x, int y, RectangleShape.Shape s)//x is the center of the rectangle and y is the base of the rectangle
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
                            for (int d = 0; d < initialCollectiblesInfo.Length; d++)
                            {
                                CollectibleRepresentation c = initialCollectiblesInfo[d];
                                if (Math.Abs(x + i - c.X / GameInfo.PIXEL_LENGTH) + Math.Abs(y - j - c.Y / GameInfo.PIXEL_LENGTH) <= 3)
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

        private Tuple<double, double> CornerAtLeftSide(Tuple<double, double> top_left, Tuple<double, double> top_right, Tuple<double, double> bottom_left, Tuple<double, double> bottom_right, Tuple<double, double> current)
        {
            if (current.Item1 == top_left.Item1 && current.Item2 == top_left.Item2)
            {
                return bottom_left;
            }
            else if (current.Item1 == top_right.Item1 && current.Item2 == top_right.Item2)
            {
                return top_left;
            }
            else if (current.Item1 == bottom_left.Item1 && current.Item2 == bottom_left.Item2)
            {
                return bottom_right;
            }
            else
            {
                return top_right;
            }
        }

        private CollisionType CornerIntersect(ref List<Platform> platformList, Tuple<double, double> top_left, Tuple<double, double> top_right, Tuple<double, double> bottom_left, Tuple<double, double> bottom_right, ref MoveInformation m, bool right)
        {
            Tuple<double, double> min_x, max_x, min_y, max_y;
            min_x = MinimumX(top_left, top_right, bottom_left, bottom_right);
            max_y = CornerAtLeftSide(top_left, top_right, bottom_left, bottom_right, min_x);
            max_x = CornerAtLeftSide(top_left, top_right, bottom_left, bottom_right, max_y);
            min_y = CornerAtLeftSide(top_left, top_right, bottom_left, bottom_right, max_x);
            bool picks_diamond = false;

            List<int> ret = new List<int>();
            for (int i = (int)min_x.Item1 / GameInfo.PIXEL_LENGTH; i <= (int)max_x.Item1 / GameInfo.PIXEL_LENGTH; i++)
            {
                for (int j = (int)min_y.Item2 / GameInfo.PIXEL_LENGTH; j <= (int)max_y.Item2 / GameInfo.PIXEL_LENGTH; j++)
                {
                    if (InsideRectangle(min_x, min_y, max_x, max_y, i, j))
                    {
                        if (0 <= i && i < GameInfo.LEVEL_MAP_WIDTH && 0 <= j && j < GameInfo.LEVEL_MAP_HEIGHT)
                        {
                            if (levelMap[i, j] == PixelType.PLATFORM || levelMap[i, j] == PixelType.OBSTACLE)
                            {
                                if (j == (int)max_y.Item2 / GameInfo.PIXEL_LENGTH)
                                {
                                    if (m.moveDuringFlight != Moves.NO_ACTION)
                                    {
                                        m.landingPlatform = GetPlatform(platformList, i, j);
                                        return CollisionType.Bottom;
                                    }
                                    if (right)
                                    {
                                        if (GetPlatform(platformList, i, j).yTop == GetPlatform(platformList, (int)(2 * max_x.Item1 + min_x.Item1) / (3 * GameInfo.PIXEL_LENGTH), j).yTop)
                                        {
                                            m.landingPlatform = GetPlatform(platformList, (int)(2 * max_x.Item1 + min_x.Item1) / (3 * GameInfo.PIXEL_LENGTH), j);
                                            return CollisionType.Bottom;
                                        }
                                        else
                                        {
                                            return CollisionType.Other;
                                        }
                                    }
                                    else
                                    {
                                        if (GetPlatform(platformList, i, j).yTop == GetPlatform(platformList, (int)(max_x.Item1 + 2 * min_x.Item1) / (3 * GameInfo.PIXEL_LENGTH), j).yTop)
                                        {
                                            m.landingPlatform = GetPlatform(platformList, (int)(max_x.Item1 + 2 * min_x.Item1) / (3 * GameInfo.PIXEL_LENGTH), j);
                                            return CollisionType.Bottom;
                                        }
                                        else
                                        {
                                            return CollisionType.Other;
                                        }
                                    }
                                }
                                else if (i - (int)min_x.Item1 / GameInfo.PIXEL_LENGTH <= 1)
                                {
                                    return CollisionType.Left;
                                }
                                else if ((int)max_x.Item1 / GameInfo.PIXEL_LENGTH - i <= 1)
                                {
                                    return CollisionType.Right;
                                }
                                else if (j == (int)min_y.Item2 / GameInfo.PIXEL_LENGTH)
                                {
                                    return CollisionType.Top;
                                }
                                return CollisionType.Other;
                            }
                            else if (levelMap[i, j] == PixelType.DIAMOND)
                            {
                                picks_diamond = true;
                                for (int d = 0; d < initialCollectiblesInfo.Length; d++)
                                {
                                    CollectibleRepresentation c = initialCollectiblesInfo[d];
                                    if (Math.Abs(i - c.X / GameInfo.PIXEL_LENGTH) + Math.Abs(j - c.Y / GameInfo.PIXEL_LENGTH) <= 3)
                                    {
                                        if (!ret.Contains(d))
                                        {
                                            ret.Add(d);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            return CollisionType.Other;
                        }
                    }

                }
            }
            if (picks_diamond)
            {
                foreach (int d in ret)
                {
                    if (!m.diamondsCollected.Contains(d))
                    {
                        m.diamondsCollected.Add(d);
                    }
                }
                return CollisionType.Diamond;
            }
            else
            {
                return CollisionType.None;
            }
        }

        private Tuple<double, double> MinimumX(Tuple<double, double> top_left, Tuple<double, double> top_right, Tuple<double, double> bottom_left, Tuple<double, double> bottom_right)
        {
            Tuple<double, double> min = top_left;
            if (top_right.Item1 < min.Item1)
            {
                min = top_right;
            }
            if (bottom_left.Item1 < min.Item1)
            {
                min = bottom_left;
            }
            if (bottom_right.Item1 < min.Item1)
            {
                min = bottom_right;
            }
            return min;
        }

        private bool InsideRectangle(Tuple<double, double> min_x, Tuple<double, double> min_y, Tuple<double, double> max_x, Tuple<double, double> max_y, int candidate_x, int candidate_y)
        {
            double x = candidate_x * GameInfo.PIXEL_LENGTH;
            double y = candidate_y * GameInfo.PIXEL_LENGTH;
            double x_0, y_0, x_1, y_1;

            // Top left edge

            x_0 = min_x.Item1;
            y_0 = min_x.Item2;

            x_1 = min_y.Item1;
            y_1 = min_y.Item2;


            if ((y - y_0) * (x_1 - x_0) < (x - x_0) * (y_1 - y_0))
            {
                return false;
            }

            // Top right edge

            x_0 = max_x.Item1;
            y_0 = max_x.Item2;

            x_1 = min_y.Item1;
            y_1 = min_y.Item2;

            if ((y - y_0) * (x_1 - x_0) > (x - x_0) * (y_1 - y_0))
            {
                return false;
            }

            // Bottom left edge

            x_0 = min_x.Item1;
            y_0 = min_x.Item2;

            x_1 = max_y.Item1;
            y_1 = max_y.Item2;

            if ((y - y_0) * (x_1 - x_0) > (x - x_0) * (y_1 - y_0))
            {
                return false;
            }

            // Bottom right edge

            x_0 = max_x.Item1;
            y_0 = max_x.Item2;

            x_1 = max_y.Item1;
            y_1 = max_y.Item2;

            if ((y - y_0) * (x_1 - x_0) < (x - x_0) * (y_1 - y_0))
            {
                return false;
            }
            return true;
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

        public void SimulateMove(ref List<Platform> platformList, double x_0, double y_0, double vx_0, double vy_0, ref MoveInformation m, RectangleShape.Shape s)
        {
            Tuple<double, double> top_left, top_right, bottom_left, bottom_right;
            double radius = Math.Sqrt(RectangleShape.fwidth(s) * RectangleShape.fwidth(s) + RectangleShape.fheight(s) * RectangleShape.fheight(s)) / 2;
            double angular_velocity = AngularVelocity(vx_0);
            double shape_angle = Math.Atan(RectangleShape.fheight(s) / RectangleShape.fwidth(s));
            /*
             *    ----------
             *    |       ||
             *    |      | |
             *    |     |  |
             *    |    |   |
             *    |   |    |
             *    |  |     |
             *    | |      |
             *    ||sh_ang |
             *    ----------
             * */
            double angle = 0;
            float dt = 0.005f;
            double x_t = x_0;
            double y_t = y_0;
            double vx_t = vx_0;
            double vy_t = vy_0;
            int NUM_REBOUNDS = 3;
            int j = 0;
            double acc_x = 0;

            // Initiate with t = 0.1
            double t = 0.1f;
            if (s == RectangleShape.Shape.HORIZONTAL && Math.Abs(vx_0) >= 250)
            {
                t = 0.3f;
            }
            /*
             * MOVE_RIGHT y rightEdge: d < 0 y acc_x > 0 -> |vx_t| > |vx_0| -> vx_t^2 = vx_0^2 - 2*a*d
             * MOVE_RIGHT y leftEdge: d > 0 y acc_x > 0 -> |vx_t| < |vx_0| -> vx_t^2 = vx_0^2 - 2*a*d
             * MOVE_LEFT y rightEdge: d < 0 y acc_x < 0 -> |vx_t| < |vx_0| -> vx_t^2 = vx_0^2 - 2*a*d
             * MOVE_LEFT y leftEdge: d > 0 y acc_x < 0 -> |vx_t| > |vx_0| -> vx_t^2 = vx_0^2 - 2*a*d
             * */

            if (m.moveDuringFlight == Moves.MOVE_LEFT)
            {
                acc_x = -GameInfo.RECTANGLE_ACCELERATION;
            }
            else if (m.moveDuringFlight == Moves.MOVE_RIGHT)
            {
                acc_x = GameInfo.RECTANGLE_ACCELERATION;
            }
            if (m.moveDuringFlight != Moves.NO_ACTION && m.departurePlatform.id != -1)
            {
                double d = (m.velocityX > 0 ? m.departurePlatform.rightEdge * GameInfo.PIXEL_LENGTH : m.departurePlatform.leftEdge * GameInfo.PIXEL_LENGTH) - x_0;
                vx_t = Math.Sign(vx_0) * Math.Sqrt(vx_0 * vx_0 - 2 * acc_x * d);
            }

            x_t = x_t + vx_t * t + acc_x * Math.Pow(t, 2) / 2;
            y_t = y_t + vy_t * t + GameInfo.GRAVITY * Math.Pow(t, 2) / 2;
            angular_velocity = m.moveDuringFlight != Moves.NO_ACTION ? AngularVelocity(vx_t) / 2 : AngularVelocity(vx_t);
            angle -= angular_velocity * t;
            m.path.Add(new Tuple<float, float>((float)x_t, (float)y_t));
            vy_t = vy_t + t * GameInfo.GRAVITY;
            vx_t = vx_t + t * acc_x;

            CollisionType cct = CollisionType.None, last_collision = CollisionType.None;
            while (cct != CollisionType.Bottom && j <= NUM_REBOUNDS)
            {
                angle -= angular_velocity * dt;
                // Check if it has collided with something & update speed if necessary
                cct = CornerIntersect(
                    ref platformList,
                    new Tuple<double, double>(x_t + radius * Math.Cos(Math.PI - shape_angle + angle), y_t - radius * Math.Sin(Math.PI - shape_angle + angle)),//topleft
                    new Tuple<double, double>(x_t + radius * Math.Cos(shape_angle + angle), y_t - radius * Math.Sin(shape_angle + angle)),//topright
                    new Tuple<double, double>(x_t + radius * Math.Cos(Math.PI + shape_angle + angle), y_t - radius * Math.Sin(Math.PI + shape_angle + angle)),//bottomleft
                    new Tuple<double, double>(x_t + radius * Math.Cos(-shape_angle + angle), y_t - radius * Math.Sin(-shape_angle + angle)), //bottom right
                    ref m, vx_t >= 0);
                m.path.Add(new Tuple<float, float>((float)x_t, (float)y_t));

                if (cct == CollisionType.None || cct == CollisionType.Diamond)
                {
                    // Calculate new corner coordinates
                    top_left = new Tuple<double, double>(x_t + radius * Math.Cos(Math.PI - shape_angle + angle), y_t - radius * Math.Sin(Math.PI - shape_angle + angle));
                    top_right = new Tuple<double, double>(x_t + radius * Math.Cos(shape_angle + angle), y_t - radius * Math.Sin(shape_angle + angle));
                    bottom_left = new Tuple<double, double>(x_t + radius * Math.Cos(Math.PI + shape_angle + angle), y_t - radius * Math.Sin(Math.PI + shape_angle + angle));
                    bottom_right = new Tuple<double, double>(x_t + radius * Math.Cos(-shape_angle + angle), y_t - radius * Math.Sin(-shape_angle + angle));

                    if (Math.Abs(vx_0) == GameInfo.TESTING_VELOCITY && s == RectangleShape.Shape.HORIZONTAL && m.moveDuringFlight == Moves.NO_ACTION)
                    {
                        list_top_left.Add(new Tuple<float, float>((float)top_left.Item1, (float)top_left.Item2));
                        list_top_right.Add(new Tuple<float, float>((float)top_right.Item1, (float)top_right.Item2));
                        list_bottom_left.Add(new Tuple<float, float>((float)bottom_left.Item1, (float)bottom_left.Item2));
                        list_bottom_right.Add(new Tuple<float, float>((float)bottom_right.Item1, (float)bottom_right.Item2));
                    }
                }
                else
                {
                    angular_velocity = AngularVelocity(vx_t) / (m.moveDuringFlight != Moves.NO_ACTION ? 2 : 1);
                    angle += angular_velocity * dt;
                }
                switch (cct)
                {
                    case CollisionType.Top:
                        if (last_collision != CollisionType.Top)
                        {
                            angular_velocity = -angular_velocity;
                            vx_t = vx_t / 3;
                            j++;
                            last_collision = CollisionType.Top;
                        }
                        break;
                    case CollisionType.Right:
                        if (last_collision != CollisionType.Right)
                        {
                            angular_velocity = 0;
                            vx_t = -vx_t / 2; // Check
                            vy_t = vy_t / 3;
                            j++;
                            last_collision = CollisionType.Right;
                        }
                        break;
                    case CollisionType.Left:
                        if (last_collision != CollisionType.Left)
                        {
                            angular_velocity = 0;
                            vx_t = -vx_t / 2; // Check
                            vy_t = vy_t / 3;
                            j++;
                            last_collision = CollisionType.Left;
                        }
                        break;
                    case CollisionType.Other:
                        return;
                    default:
                        break;
                }
                x_t = x_t + vx_t * dt + acc_x * Math.Pow(dt, 2) / 2;
                y_t = y_t + vy_t * dt + GameInfo.GRAVITY * Math.Pow(dt, 2) / 2;
                m.path.Add(new Tuple<float, float>((float)x_t, (float)y_t));
                vy_t = vy_t + dt * GameInfo.GRAVITY;
                vx_t = vx_t + dt * acc_x;
            }
            m.xlandPoint = (int)x_t / GameInfo.PIXEL_LENGTH;
            m.x = m.velocityX > 0 ? m.departurePlatform.rightEdge : m.departurePlatform.leftEdge;
        }

    }
}