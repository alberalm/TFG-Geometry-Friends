using GeometryFriends.AI;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Xml.Linq;

namespace GeometryFriendsAgents
{
    class LevelMapRectangle : LevelMap
    {
        List<Tuple<float, float>> list_top_left = new List<Tuple<float, float>>();
        List<Tuple<float, float>> list_top_right = new List<Tuple<float, float>>();
        List<Tuple<float, float>> list_bottom_left = new List<Tuple<float, float>>();
        List<Tuple<float, float>> list_bottom_right = new List<Tuple<float, float>>();
        public List<Platform> simplified_platforms = new List<Platform>();
        public Dictionary<Platform, List<Platform>> simplified_to_small = new Dictionary<Platform, List<Platform>>();
        public Dictionary<Platform, Platform> small_to_simplified = new Dictionary<Platform, Platform>();

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
            RectangleShape.Shape s = RectangleShape.GetShape(rI);
            for (int i = 0; i < platformList.Count; i++)
            {
                if (rI.Y / GameInfo.PIXEL_LENGTH + RectangleShape.height(s) / 2 <= platformList[i].yTop + 3 &&
                    rI.Y / GameInfo.PIXEL_LENGTH + RectangleShape.height(s) / 2 >= platformList[i].yTop - 3 &&
                    rI.X / GameInfo.PIXEL_LENGTH >= platformList[i].leftEdge - 1 && rI.X / GameInfo.PIXEL_LENGTH <= platformList[i].rightEdge + 1)
                {
                    return platformList[i];
                }
            }
            for (int i = 0; i < platformList.Count; i++)
            {
                if (rI.Y / GameInfo.PIXEL_LENGTH + RectangleShape.width(s) / 2 <= platformList[i].yTop + 1 &&
                    rI.Y / GameInfo.PIXEL_LENGTH + RectangleShape.width(s) / 2 >= platformList[i].yTop - 10 &&
                    rI.X / GameInfo.PIXEL_LENGTH >= platformList[i].leftEdge - 1 && rI.X / GameInfo.PIXEL_LENGTH <= platformList[i].rightEdge + 1)
                {
                    return platformList[i];
                }
            }
            return new Platform(-1);
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

        private Tuple<double, double> CornerAtLeftSide(Tuple<double, double> top_left, Tuple<double, double> top_right, Tuple<double, double> bottom_left, Tuple<double, double> bottom_right, Tuple<double, double> current)
        {
            if(current.Item1 == top_left.Item1 && current.Item2 == top_left.Item2)
            {
                return bottom_left;
            }
            else if (current.Item1 == top_right.Item1 && current.Item2 == top_right.Item2)
            {
                return top_left;
            }
            else if(current.Item1 == bottom_left.Item1 && current.Item2 == bottom_left.Item2)
            {
                return bottom_right;
            }
            else
            {
                return top_right;
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
            
            
            if((y - y_0) * (x_1 - x_0) < (x - x_0) * (y_1 - y_0))
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
        
        private CollisionType CornerIntersect(Tuple<double, double> top_left, Tuple<double, double> top_right, Tuple<double, double> bottom_left, Tuple<double, double> bottom_right, ref MoveInformation m, bool right)
        {
            Tuple<double, double> min_x = top_left, max_x, min_y, max_y;
            min_x = MinimumX(top_left, top_right, bottom_left, bottom_right);
            max_y = CornerAtLeftSide(top_left, top_right, bottom_left, bottom_right, min_x);
            max_x = CornerAtLeftSide(top_left, top_right, bottom_left, bottom_right, max_y);
            min_y = CornerAtLeftSide(top_left, top_right, bottom_left, bottom_right, max_x);
            bool picks_diamond = false;
            
            List<int> ret = new List<int>();
            for (int i = (int) min_x.Item1/GameInfo.PIXEL_LENGTH; i <= (int) max_x.Item1 / GameInfo.PIXEL_LENGTH; i++)
            {
                for(int j = (int) min_y.Item2 / GameInfo.PIXEL_LENGTH; j <= (int) max_y.Item2 / GameInfo.PIXEL_LENGTH; j++)
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
                                        m.landingPlatform = GetPlatform(i , j);
                                        return CollisionType.Bottom;
                                    }
                                    if (right)
                                    {
                                        if (GetPlatform(i, j).yTop == GetPlatform((int)(2 * max_x.Item1 + min_x.Item1) / (3 * GameInfo.PIXEL_LENGTH), j).yTop)
                                        {
                                            m.landingPlatform = GetPlatform((int)(2 * max_x.Item1 + min_x.Item1) / (3 * GameInfo.PIXEL_LENGTH), j);
                                            return CollisionType.Bottom;
                                        }
                                        else
                                        {
                                            return CollisionType.Other;
                                        }
                                    }
                                    else
                                    {
                                        if (GetPlatform(i, j).yTop == GetPlatform((int)(max_x.Item1 + 2 * min_x.Item1) / (3 * GameInfo.PIXEL_LENGTH), j).yTop)
                                        {
                                            m.landingPlatform = GetPlatform((int)(max_x.Item1 + 2 * min_x.Item1) / (3 * GameInfo.PIXEL_LENGTH), j);
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

        private double AngularVelocity(double vx_0, Moves moveDuringFlight)
        {
            // if vx_0 = 200 -> return 0.5
            // if vx_0 = 100 -> return 1.25
            // if vx_0 = 300 -> return 0.3125
            // if vx_0 = 400 -> return 0.28
            
            if (vx_0 > 0)
            {
                if(moveDuringFlight==Moves.MOVE_RIGHT && vx_0 >= 250)
                {
                    return 0.15f;
                }
                else
                {
                    return 204.35f / Math.Pow(vx_0, 1.12);
                }
            }
            else if(vx_0 < 0)
            {
                if(moveDuringFlight == Moves.MOVE_RIGHT)
                {
                    return -AngularVelocity(-vx_0, Moves.MOVE_LEFT);
                }
                if (moveDuringFlight == Moves.MOVE_LEFT)
                {
                    return -AngularVelocity(-vx_0, Moves.MOVE_RIGHT);
                }
                return -AngularVelocity(-vx_0, moveDuringFlight);
            }
            else
            {
                return 0;
            }
        }

        public void SimulateMove(double x_0, double y_0, double vx_0, double vy_0, ref MoveInformation m, RectangleShape.Shape s)
        {
            Tuple<double, double> top_left, top_right, bottom_left, bottom_right;
            double radius = Math.Sqrt(RectangleShape.fwidth(s) * RectangleShape.fwidth(s) + RectangleShape.fheight(s) * RectangleShape.fheight(s)) / 2;
            double angular_velocity = AngularVelocity(vx_0,m.moveDuringFlight);
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
            if (s == RectangleShape.Shape.HORIZONTAL && Math.Abs(vx_0)>=250)
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
            if(m.moveDuringFlight != Moves.NO_ACTION && m.departurePlatform.id != -1)
            {
                double d = (m.velocityX > 0 ? m.departurePlatform.rightEdge * GameInfo.PIXEL_LENGTH : m.departurePlatform.leftEdge * GameInfo.PIXEL_LENGTH) - x_0;
                vx_t = Math.Sign(vx_0) * Math.Sqrt(vx_0 * vx_0 - 2 * acc_x * d);
            }

            x_t = x_t + vx_t * t + acc_x * Math.Pow(t, 2) / 2;
            y_t = y_t + vy_t * t + GameInfo.GRAVITY * Math.Pow(t, 2) / 2;
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
                    
                    if (m.departurePlatform.id == -1)
                    {
                        list_top_left.Add(new Tuple<float, float>((float)top_left.Item1, (float)top_left.Item2));
                        list_top_right.Add(new Tuple<float, float>((float)top_right.Item1, (float)top_right.Item2));
                        list_bottom_left.Add(new Tuple<float, float>((float)bottom_left.Item1, (float)bottom_left.Item2));
                        list_bottom_right.Add(new Tuple<float, float>((float)bottom_right.Item1, (float)bottom_right.Item2));
                    }
                }
                else
                {
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

        protected override bool EnoughSpaceToAccelerate(int leftEdge, int rightEdge, int x, int vx)
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

        private void AddTrajectory(ref Platform p, int vx, MoveType moveType, int x, RectangleShape.Shape s, Platform landing, Moves moveDuringFlight = Moves.NO_ACTION)
        {
            MoveInformation m = new MoveInformation(landing, p, x, 0, vx, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);
            m.moveDuringFlight = moveDuringFlight;

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
                m.shape = s;
                SimulateMove(x * GameInfo.PIXEL_LENGTH, p.yTop * GameInfo.PIXEL_LENGTH - RectangleShape.fheight(s) / 2, vx, 0, ref m, s);
                if (!m.landingPlatform.real)
                {
                    return;
                }
            }
            else if (moveType == MoveType.DROP || moveType == MoveType.MONOSIDEDROP)
            {
                m.xlandPoint = x;
                m.shape = s;
                int collisionY = GameInfo.LEVEL_MAP_HEIGHT;
                // Top, Right, Bottom, Left, Diamond, Other, None
                foreach(Platform platform in platformList)
                {
                    if(platform.leftEdge <= x && platform.rightEdge >= x && platform.yTop < collisionY && platform.yTop > p.yTop)
                    {
                        collisionY = platform.yTop;
                        m.landingPlatform = platform;
                    }
                }
                for(int y = p.yTop; y < collisionY; y++)
                {
                    foreach(RectangleShape.Shape sh in GameInfo.SHAPES)
                    {
                        if(RectangleIntersectsWithObstacle(x, y, sh) == CollisionType.Diamond)
                        {
                            List<int> catched= GetDiamondCollected(x, y, sh);
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
            }
            else if (moveType == MoveType.ADJACENT)
            {
                m.xlandPoint = x;
                m.path.Add(new Tuple<float, float>(x * GameInfo.PIXEL_LENGTH, (p.yTop - RectangleShape.height(s) / 2) * GameInfo.PIXEL_LENGTH));
                m.shape = s;
            }
            else if (moveType == MoveType.NOMOVE)
            {
                m.xlandPoint = x;
                m.path.Add(new Tuple<float, float>(x * GameInfo.PIXEL_LENGTH, (p.yTop - RectangleShape.height(s) / 2) * GameInfo.PIXEL_LENGTH));
                // NOTE: Be aware of possible NOMOVEs that combine several shapes
                m.diamondsCollected = CollectsDiamonds(x, p.yTop, s);
                if(RectangleIntersectsWithObstacle(x, p.yTop, s) == CollisionType.Diamond)
                {
                    List<int> others = GetDiamondCollected(x, p.yTop, s);
                    foreach(int d in others)
                    {
                        if (!m.diamondsCollected.Contains(d))
                        {
                            m.diamondsCollected.Add(d);
                        }
                    }
                }
                if(s == RectangleShape.Shape.VERTICAL)
                {
                    double ytop = p.yTop * GameInfo.PIXEL_LENGTH - RectangleShape.fheight(s);
                    for (double xtop = x * GameInfo.PIXEL_LENGTH - RectangleShape.fwidth(s) / 2; xtop  <= x * GameInfo.PIXEL_LENGTH + RectangleShape.fwidth(s) / 2; xtop++)
                    {
                        for(int d =0; d< initialCollectiblesInfo.Length; d++)
                        {
                            CollectibleRepresentation c = initialCollectiblesInfo[d];
                            if (Math.Abs(c.X-xtop)+ Math.Abs(c.Y - ytop) <=32)
                            {
                                if (!m.diamondsCollected.Contains(d))
                                {
                                    m.diamondsCollected.Add(d);
                                }
                            }
                        }
                    }
                }
                m.shape = s;
            }
            
            lock (platformList)
            {
                bool addIt = true;
                if (m.landingPlatform.id != -2 && m.landingPlatform.id != -1 && (m.diamondsCollected.Count > 0 || p.id != m.landingPlatform.id))
                {
                    for (int i = 0; i < p.moveInfoList.Count; i++)
                    {
                        int add = m.CompareRectangle(p.moveInfoList[i], initialCollectiblesInfo, platformList);
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
                if (p.real)
                {
                    // ADJACENT MOVES
                    for(int i=k + 1; i < platformList.Count; i++)
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
                                            for(int j = i; j < platformList.Count; j++)
                                            {
                                                platformList[j].id = j;
                                            }
                                        }
                                        else
                                        {
                                            if(p2.leftEdge == p.rightEdge)
                                            {
                                                p2.leftEdge++;
                                            }
                                            AddTrajectory(ref p, 1, MoveType.ADJACENT, p.rightEdge, s, p2);
                                            AddTrajectory(ref p2, -1, MoveType.ADJACENT, p2.leftEdge, s, p);
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
                                            AddTrajectory(ref p, -1, MoveType.ADJACENT, p.leftEdge, s, p2);
                                            AddTrajectory(ref p2, 1, MoveType.ADJACENT, p2.rightEdge, s, p);
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
                                    AddTrajectory(ref p, 15 * (p2.rightEdge - p2.leftEdge), MoveType.ADJACENT, p.rightEdge, RectangleShape.Shape.HORIZONTAL, p2);
                                    AddTrajectory(ref p2, -15 * (p2.rightEdge - p2.leftEdge), MoveType.ADJACENT, p2.leftEdge, RectangleShape.Shape.HORIZONTAL, p);
                                }
                                else if (p.leftEdge == p2.rightEdge + 1)
                                {
                                    AddTrajectory(ref p, -15 * (p2.rightEdge - p2.leftEdge), MoveType.ADJACENT, p.leftEdge, RectangleShape.Shape.HORIZONTAL, p2);
                                    AddTrajectory(ref p2, 15 * (p2.rightEdge - p2.leftEdge), MoveType.ADJACENT, p2.rightEdge, RectangleShape.Shape.HORIZONTAL, p);
                                }
                            }
                        }
                    }
                }
            }

            for(int k = 0; k < platformList.Count; k++)
            {
                Platform p = platformList[k];
                if (!p.real)
                {
                    // DROP MOVES
                    RectangleShape.Shape s = RectangleShape.Shape.VERTICAL;
                    AddTrajectory(ref p, 0, MoveType.DROP, (p.rightEdge + p.leftEdge) / 2 + 1, s, new Platform(-1));
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
                        foreach (RectangleShape.Shape s in GameInfo.SHAPES)
                        {
                            if (p.shapes[(int)s])
                            {
                                AddTrajectory(ref p, 0, MoveType.NOMOVE, x, s, p);
                            }
                        }
                    });

                    // TILT MOVES
                    Parallel.For(0, platformList.Count, i =>
                    {
                        Platform p2 = platformList[i];
                        if (p2.real)
                        {
                            if (p.shapes[(int)RectangleShape.Shape.VERTICAL])
                            {
                                RectangleShape.Shape s = RectangleShape.Shape.VERTICAL;
                                // NOTE: Be aware 13 creates impossible moves
                                if (p.yTop - p2.yTop > 0 && p.yTop - p2.yTop < 12)
                                {
                                    if (p2.leftEdge - p.rightEdge <= RectangleShape.width(RectangleShape.Shape.VERTICAL)
                                        && p2.leftEdge - p.rightEdge >= 0)
                                    {
                                        AddTrajectory(ref p, 1, MoveType.TILT, p.rightEdge, s, p2);
                                    }
                                    else if (p.leftEdge - p2.rightEdge <= RectangleShape.width(RectangleShape.Shape.VERTICAL) + 1
                                        && p.leftEdge - p2.rightEdge >= 0)
                                    {
                                        AddTrajectory(ref p, -1, MoveType.TILT, p.leftEdge, s, p2);
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
                                            if (levelMap[x, max_height] == PixelType.OBSTACLE)
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
                                        AddTrajectory(ref p, 1, MoveType.TILT, p.rightEdge, s, p2);
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
                                            if (levelMap[x, max_height] == PixelType.OBSTACLE)
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
                                        AddTrajectory(ref p, -1, MoveType.TILT, p.leftEdge, s, p2);
                                    }
                                }
                            }
                        }
                    });

                    // MONOSIDEDROP
                    Parallel.For(0, platformList.Count, i =>
                    {
                        RectangleShape.Shape s = RectangleShape.Shape.VERTICAL;
                        // Right moves
                        int x = p.rightEdge + 1;
                        while (x < GameInfo.LEVEL_MAP_WIDTH && levelMap[x, p.yTop] != PixelType.OBSTACLE && levelMap[x, p.yTop] != PixelType.PLATFORM)
                        {
                            x++;
                        }
                        if (levelMap[x, p.yTop] == PixelType.OBSTACLE && x - p.rightEdge > RectangleShape.width(RectangleShape.Shape.VERTICAL) + 1
                            && x - p.rightEdge < RectangleShape.width(RectangleShape.Shape.HORIZONTAL)) // may be wrong
                        {
                            AddTrajectory(ref p, 1, MoveType.MONOSIDEDROP, (x + p.rightEdge) / 2 + 1, s, new Platform(-1));
                        }
                        // Left moves
                        x = p.leftEdge - 1;
                        while (x >= 0 && levelMap[x, p.yTop] != PixelType.OBSTACLE && levelMap[x, p.yTop] != PixelType.PLATFORM)
                        {
                            x--;
                        }
                        if (levelMap[x, p.yTop] == PixelType.OBSTACLE && p.leftEdge - x > RectangleShape.width(RectangleShape.Shape.VERTICAL) + 1
                            && p.leftEdge - x < RectangleShape.width(RectangleShape.Shape.HORIZONTAL)) // may be wrong
                        {
                            AddTrajectory(ref p, -1, MoveType.MONOSIDEDROP, (x + p.leftEdge) / 2 + 1, s, new Platform(-1));
                        }
                    });

                    //FALL MOVES
                    foreach (RectangleShape.Shape s in GameInfo.SHAPES)
                    {
                        if (p.shapes[(int)s])
                        {
                            //for (int i = 0; i < GameInfo.NUM_VELOCITIES_RECTANGLE; i++)
                            Parallel.For(0, GameInfo.NUM_VELOCITIES_RECTANGLE, i =>
                            {
                                int vx = (i + 1) * GameInfo.VELOCITY_STEP_RECTANGLE;
                                if (levelMap[p.rightEdge + 1, p.yTop] != PixelType.PLATFORM && levelMap[p.rightEdge + 1, p.yTop] != PixelType.OBSTACLE)
                                {
                                    int min_left = p.leftEdge;
                                    while (min_left >= 0 && levelMap[min_left, p.yTop] == PixelType.PLATFORM)
                                    {
                                        min_left--;
                                    }
                                    if (i == 0 || EnoughSpaceToAccelerate(min_left, p.rightEdge, p.rightEdge, vx))
                                    {
                                        AddTrajectory(ref p, vx, MoveType.FALL, p.rightEdge + Math.Max(5 - i / 2, 1), s, new Platform(-1));
                                        AddTrajectory(ref p, vx, MoveType.FALL, p.rightEdge + Math.Max(5 - i / 2, 1), s, new Platform(-1), Moves.MOVE_LEFT);
                                        AddTrajectory(ref p, vx, MoveType.FALL, p.rightEdge + Math.Max(5 - i / 2, 1), s, new Platform(-1), Moves.MOVE_RIGHT);
                                    }
                                }
                                if (levelMap[p.leftEdge - 1, p.yTop] != PixelType.PLATFORM && levelMap[p.leftEdge - 1, p.yTop] != PixelType.OBSTACLE)
                                {
                                    int max_right = p.rightEdge;
                                    while (max_right < GameInfo.LEVEL_MAP_WIDTH && levelMap[max_right, p.yTop] == PixelType.PLATFORM)
                                    {
                                        max_right++;
                                    }
                                    if (i == 0 || EnoughSpaceToAccelerate(p.leftEdge, max_right, p.leftEdge, -vx))
                                    {
                                        AddTrajectory(ref p, -vx, MoveType.FALL, p.leftEdge - Math.Max(5 - i / 2, 1), s, new Platform(-1));
                                        AddTrajectory(ref p, -vx, MoveType.FALL, p.leftEdge - Math.Max(5 - i / 2, 1), s, new Platform(-1), Moves.MOVE_LEFT);
                                        AddTrajectory(ref p, -vx, MoveType.FALL, p.leftEdge - Math.Max(5 - i / 2, 1), s, new Platform(-1), Moves.MOVE_RIGHT);
                                    }
                                }
                            //}
                            });
                        }
                    }
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

        private List<int> CollectsDiamonds(int x, int y, RectangleShape.Shape shape)
        {
            List<int> ret = new List<int>();
            for(int i = 0; i < initialCollectiblesInfo.Length; i++)
            {
                CollectibleRepresentation d = initialCollectiblesInfo[i];
                if(Math.Abs(d.X/GameInfo.PIXEL_LENGTH - x) <= RectangleShape.fwidth(shape) / (2 * GameInfo.PIXEL_LENGTH) &&
                    d.Y + 4 * GameInfo.PIXEL_LENGTH > y * GameInfo.PIXEL_LENGTH - RectangleShape.fheight(shape) && d.Y < y * GameInfo.PIXEL_LENGTH)
                {
                    ret.Add(i);
                }
            }
            return ret;
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
                                if(Math.Abs(x + i - c.X/GameInfo.PIXEL_LENGTH) + Math.Abs(y - j - c.Y / GameInfo.PIXEL_LENGTH) <= 3)
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
                                platformList.Add(pMiddle);
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
                                platformList.Add(pMiddle);
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
    }
}
