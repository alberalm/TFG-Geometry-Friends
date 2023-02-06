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
    class LevelMapRectangle : LevelMap
    {
        List<Tuple<float, float>> list_top_left = new List<Tuple<float, float>>();
        List<Tuple<float, float>> list_top_right = new List<Tuple<float, float>>();
        List<Tuple<float, float>> list_bottom_left = new List<Tuple<float, float>>();
        List<Tuple<float, float>> list_bottom_right = new List<Tuple<float, float>>();

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
        
        private CollisionType CornerIntersect(Tuple<double, double> top_left, Tuple<double, double> top_right, Tuple<double, double> bottom_left, Tuple<double, double> bottom_right, ref MoveInformation m)
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
                    if(InsideRectangle(min_x, min_y, max_x, max_y, i, j))
                    {
                        switch (levelMap[i, j])
                        {
                            case PixelType.PLATFORM:
                                m.landingPlatform=GetPlatform(i, j);
                                return CollisionType.Bottom;
                            case PixelType.OBSTACLE:
                                if (j == (int)max_y.Item2 / GameInfo.PIXEL_LENGTH)
                                {
                                    m.landingPlatform = GetPlatform(i, j);
                                    return CollisionType.Bottom;
                                }
                                else if(i== (int)min_x.Item1 / GameInfo.PIXEL_LENGTH)
                                {
                                    return CollisionType.Left;
                                }
                                else if(i == (int)max_x.Item1 / GameInfo.PIXEL_LENGTH)
                                {
                                    return CollisionType.Right;
                                }
                                else if (j==(int)min_y.Item2 / GameInfo.PIXEL_LENGTH)
                                {
                                    return CollisionType.Top;
                                }
                                return CollisionType.Other;
                            case PixelType.DIAMOND:
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
                                break;
                            default:
                                break;
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

        private double AngularVelocity(double vx_0)
        {
            // if vx_0 = 200 -> return 0.5
            // if vx_0 = 100 -> return 1.25
            // if vx_0 = 300 -> return 0.3125
            if(vx_0 == 100)
            {
                return 0.5;
            }
            else if(vx_0 == 200)
            {
                return 1.25;
            }
            else if(vx_0 == 300)
            {
                return 0.3125;
            }
            if (vx_0>=0)
            {
                return 100 / (vx_0 + 100);
            }
            else
            {
                return -AngularVelocity(-vx_0);
            }
        }

        public void SimulateMove(double x_0, double y_0, double vx_0, double vy_0, ref MoveInformation m, RectangleShape.Shape s)
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
            int NUM_REBOUNDS = 10;
            int j = 0;

            // Initiate with t = 0.1
            double t = 0.1f;
            x_t = x_t + vx_t * t;
            y_t = y_t + vy_t * t + GameInfo.GRAVITY * Math.Pow(t, 2) / 2;
            angle -= angular_velocity * t;
            m.path.Add(new Tuple<float, float>((float)x_t, (float)y_t));
            vy_t = vy_t + t * GameInfo.GRAVITY;

            CollisionType cct = CollisionType.None, last_collision = CollisionType.None;
            while (cct != CollisionType.Bottom && j <= NUM_REBOUNDS)
            {
                angle -= angular_velocity * dt;
                // Check if it has collided with something & update speed if necessary
                cct = CornerIntersect(
                    new Tuple<double, double>(x_t + radius * Math.Cos(Math.PI - shape_angle + angle), y_t - radius * Math.Sin(Math.PI - shape_angle + angle)),//topleft
                    new Tuple<double, double>(x_t + radius * Math.Cos(shape_angle + angle), y_t - radius * Math.Sin(shape_angle + angle)),//topright
                    new Tuple<double, double>(x_t + radius * Math.Cos(Math.PI + shape_angle + angle), y_t - radius * Math.Sin(Math.PI + shape_angle + angle)),//bottomleft
                    new Tuple<double, double>(x_t + radius * Math.Cos(-shape_angle + angle), y_t - radius * Math.Sin(-shape_angle + angle)), ref m);//bottom right
                m.path.Add(new Tuple<float, float>((float)x_t, (float)y_t));
                if (cct == CollisionType.None || cct == CollisionType.Diamond)
                {
                    // Calculate new corner coordinates
                    top_left = new Tuple<double, double>(x_t + radius * Math.Cos(Math.PI - shape_angle + angle), y_t - radius * Math.Sin(Math.PI - shape_angle + angle));
                    top_right = new Tuple<double, double>(x_t + radius * Math.Cos(shape_angle + angle), y_t - radius * Math.Sin(shape_angle + angle));
                    bottom_left = new Tuple<double, double>(x_t + radius * Math.Cos(Math.PI + shape_angle + angle), y_t - radius * Math.Sin(Math.PI + shape_angle + angle));
                    bottom_right = new Tuple<double, double>(x_t + radius * Math.Cos(-shape_angle + angle), y_t - radius * Math.Sin(-shape_angle + angle));
                    
                    if (vx_0 == 400 && s==RectangleShape.Shape.SQUARE)
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
                x_t = x_t + vx_t * dt;
                y_t = y_t + vy_t * dt + GameInfo.GRAVITY * Math.Pow(dt, 2) / 2;
                m.path.Add(new Tuple<float, float>((float)x_t, (float)y_t));
                vy_t = vy_t + dt * GameInfo.GRAVITY;
            }
            if (j > NUM_REBOUNDS)
            {
                int a = 0;
            }
        }

        protected override bool EnoughSpaceToAccelerate(int leftEdge, int rigthEdge, int x, int vx)
        {
            //TODO
            return true;
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
                SimulateMove(x * GameInfo.PIXEL_LENGTH, p.yTop*GameInfo.PIXEL_LENGTH - RectangleShape.fheight(s) / 2, vx, 0, ref m, s);
            }
            else if (moveType == MoveType.DROP)
            {
                m.xlandPoint = x;
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
                        if(RectangleIntersectsWithObstacle(x, collisionY, sh) == CollisionType.Diamond)
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
                if (m.landingPlatform.id != -2 && m.landingPlatform.id != -1)
                { 
                    p.moveInfoList.Add(m); 
                }
            }
                /*bool addIt = true;
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
                */
            
        }

        protected override void GenerateMoveInformation()
        {
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

                    // ADJACENT MOVES
                    Parallel.For(k + 1, platformList.Count, i =>
                    {
                        Platform p2 = platformList[i];
                        if (p2.real)
                        {
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
                        }
                    });

                    // TILT MOVES
                    Parallel.For(0, platformList.Count, i =>
                    {
                        Platform p2 = platformList[i];
                        if (p2.real)
                        {
                            RectangleShape.Shape s = RectangleShape.Shape.VERTICAL;
                            if (p.yTop - p2.yTop > 0 && p.yTop - p2.yTop < 12)
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
                        }
                    });

                    //FALL MOVES
                    foreach(RectangleShape.Shape s in GameInfo.SHAPES)
                    {
                        if (p.shapes[(int)s]) {
                            //Parallel.For(0, GameInfo.NUM_VELOCITIES_RECTANGLE, i =>
                            for (int i = 0; i < GameInfo.NUM_VELOCITIES_RECTANGLE; i++)
                            {
                                int vx = (i + 1) * GameInfo.VELOCITY_STEP_RECTANGLE;
                                if (levelMap[p.rightEdge + 1, p.yTop] != PixelType.PLATFORM && levelMap[p.rightEdge + 1, p.yTop] != PixelType.OBSTACLE)
                                {
                                    if (EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, p.rightEdge, vx))
                                    {
                                        AddTrajectory(ref p, vx, MoveType.FALL, p.rightEdge + Math.Max(5 - i / 2, 1), s, new Platform(-1));
                                    }
                                }
                                if (levelMap[p.leftEdge - 1, p.yTop] != PixelType.PLATFORM && levelMap[p.leftEdge - 1, p.yTop] != PixelType.OBSTACLE)
                                {
                                    if (EnoughSpaceToAccelerate(p.leftEdge, p.rightEdge, p.leftEdge, -vx))
                                    {
                                        AddTrajectory(ref p, -vx, MoveType.FALL, p.leftEdge - Math.Max(5 - i / 2, 1), s, new Platform(-1));
                                    }
                                }
                            }//);
                        }
                    }
                }
                else
                {
                    // DROP MOVES
                    RectangleShape.Shape s = RectangleShape.Shape.VERTICAL;
                    AddTrajectory(ref p, 0, MoveType.DROP, (p.rightEdge + p.leftEdge) / 2 + 1, s, new Platform(-1));
                }
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
    }
}
