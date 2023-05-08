using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    public class Utils
    {
        private float radius;
        private float rectangleArea;
        private Platform ground;
        private Rectangle area;
        List<Platform> Platforms;

        public Utils(Platform g, float r, Rectangle a)
        {
            ground = g;
            radius = r;
            area = a;
        }

        public Utils(List<Platform> platforms, Platform g, float r, Rectangle a)
        {
            ground = g;
            radius = r;
            area = a;
            Platforms = platforms;
        }

        public void setPlatforms(List<Platform> plat)
        {
            Platforms = plat;
        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                               PLATFORMS AND DIAMONDS                                 ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        public Platform getDiamondPlatform(float x, float y)
        {
            Platform possiblePlatform = ground;

            foreach (Platform platform in Platforms)
            {
                //if the platform is higher than the possible one and the diamond is within it x range
                if (y < platform.getY() &&
                    x >= platform.getX() - platform.getWidth() / 2 && x <= platform.getX() + platform.getWidth() / 2 &&
                    platform.getY() < possiblePlatform.getY())
                {
                    possiblePlatform = platform;
                }
            }
            return possiblePlatform;
        }
        
        public Platform onPlatform(float x, float y)
        {
            float margin = 25;
            float yMargin = 50;
            return onPlatform(x, y, margin, yMargin);
        }

        public Platform onPlatform(float x, float y, float margin)
        {
            float yMargin = 50;
            return onPlatform(x, y, margin, yMargin);
        }

        public Platform onPlatform(float x, float y, float margin, float yMargin)
        {
            foreach (Platform platform in Platforms)
            {
                if (x >= (platform.getX() - platform.getWidth() / 2) &&
                   x <= (platform.getX() + platform.getWidth() / 2) &&
                   y + 10 >= (platform.getY() - platform.getHeight() / 2 - margin) &&
                   y + 10 <= (platform.getY() + platform.getHeight() / 2 + margin))
                {
                    return platform;
                }
            }
            if (y + yMargin >= 720)
            {
                return ground;
            }
            return null;
        }

        //the rectangle sometimes has the width and heigh switched
        public Platform onPlatformRectangle(float x, float yH, float yW)
        {
            float margin = 50;
            foreach (Platform platform in Platforms)
            {
                if (x >= (platform.getX() - platform.getWidth() / 2) &&
                   x <= (platform.getX() + platform.getWidth() / 2) &&
                   ((yH + 10 >= (platform.getY() - platform.getHeight() / 2 - margin) &&
                   yH + 10 <= (platform.getY() + platform.getHeight() / 2 + margin)) ||
                   (yW + 10 >= (platform.getY() - platform.getHeight() / 2 - margin) &&
                   yW + 10 <= (platform.getY() + platform.getHeight() / 2 + margin))))
                {
                    return platform;
                }
            }
            if (yH + 10 >= 720 && yW + 10 >= 720)
            {
                return ground;
            }
            return null;
        }

        //check if two platforms are the same
        public bool samePlatform(Platform p1, Platform p2)
        {
            if (p1 != null && p2 != null &&
               Math.Round(p1.getX()) == Math.Round(p2.getX()) &&
               Math.Round(p1.getY()) == Math.Round(p2.getY()))
            {
                return true;
            }

            return false;
        }

        //agent x, agent height, point pos and platform
        //y is not necessary for this function should only be called if the points are in the same platform
        public bool obstacleBetween(float x1, float x2, Platform platform)
        {
            foreach (Platform otherPlatform in Platforms)
            {
                //check if there is an obstacle in the platform
                if (otherPlatform.getX() - otherPlatform.getWidth() / 2 >= platform.getX() - platform.getWidth() / 2 &&
                    otherPlatform.getX() + otherPlatform.getWidth() / 2 <= platform.getX() + platform.getWidth() / 2 &&
                    otherPlatform.getY() + otherPlatform.getHeight() / 2 >= platform.getY() - platform.getHeight() / 2 - radius * 2)
                {
                    //check if the agent and the other point are on different sides
                    if (x1 < otherPlatform.getX() && x2 > otherPlatform.getX() ||
                       x1 > otherPlatform.getX() && x2 < otherPlatform.getX())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        //TODO check which method is the best to know if there is an obstacle between two points
        public bool blockingPlatform(float x, float y, float oX, float oY, float cRad)
        {
            foreach (Platform platform in Platforms)
            {
                if (((platform.getX() > x && platform.getX() < oX) || (platform.getX() < x && platform.getX() > oX)) &&
                    ((y > platform.getY() - platform.getHeight() / 2 && y < platform.getY() + platform.getHeight() / 2) ||
                    (y + cRad > platform.getY() - platform.getHeight() / 2 && y + cRad < platform.getY() + platform.getHeight() / 2)))
                {
                    return true;
                }
            }
            return false;
        }

        public Platform platformBelow(float x, float y)
        {
            List<Platform> platformsBelow = new List<Platform>();

            foreach (Platform platform in Platforms)
            {
                if (x >= (platform.getX() - platform.getWidth() / 2) &&
                   x <= (platform.getX() + platform.getWidth() / 2) &&
                   y <= platform.getY())
                {
                    //make sure the highest platform is chosen if there are more than 1 platform below the agent
                    if (platformsBelow.Count == 1 && platformsBelow[0].getY() > platform.getY())
                    {
                        platformsBelow.RemoveAt(0);
                    }
                    if (platformsBelow.Count == 0)
                    {
                        platformsBelow.Add(platform);
                    }

                }
            }
            if (platformsBelow.Count == 1)
            {
                return platformsBelow[0];
            }
            if (platformsBelow.Count == 0)
            {
                return ground;
            }
            return null;
        }

        //check if circle is on rectangle
        public bool onRectangle(float x, float y, float rX, float rY, float rH, float rW, float margin)
        {
            if (x - 5 > rX - rW / 2 &&
                   x + 5 < rX + rW / 2 &&
                   y + 10 > rY - rH / 2 - margin &&
                   y + 10 < rY + rH / 2 + margin)
            {
                return true;
            }
            return false;
        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                      DISTANCE                                        ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        public double minDistance(float x1, float y1, float x2, float y2, Platform platform)
        {
            double distance = 0;
            //if platform is between Ys
            if ((platform.getY() < y1 && platform.getY() > y2) || (platform.getY() < y2 && platform.getY() > y1))
            {
                //if a point is at the left of the platform but not below
                if (x1 - (platform.getX() - platform.getWidth() / 2) < 0)
                {
                    //distance = Math.Abs(y2 - y1) + x2 - (platform.getX() - platform.getWidth() / 2);
                    distance = Math.Abs(x2 - (platform.getX() - platform.getWidth() / 2)) + eucDist(x1, (platform.getX() - platform.getWidth() / 2), y1, y2);
                }
                else if (x2 - (platform.getX() - platform.getWidth() / 2) < 0)
                {
                    //distance = Math.Abs(y2 - y1) + x2 - (platform.getX() - platform.getWidth() / 2);
                    distance = Math.Abs(x1 - (platform.getX() - platform.getWidth() / 2)) + eucDist((platform.getX() - platform.getWidth() / 2), x2, y1, y2);
                }
                //if a point is at the right of the platform but not below 
                else if (x1 - (platform.getX() + platform.getWidth() / 2) > 0)
                {
                    //distance = Math.Abs(y2 - y1) + (platform.getX() + platform.getWidth() / 2) - x2;
                    distance = Math.Abs(x2 - (platform.getX() + platform.getWidth() / 2)) + eucDist(x1, (platform.getX() + platform.getWidth() / 2), y1, y2);
                }
                else if (x2 - (platform.getX() + platform.getWidth() / 2) > 0)
                {
                    //distance = Math.Abs(y2 - y1) + (platform.getX() + platform.getWidth() / 2) - x2;
                    distance = Math.Abs(x1 - (platform.getX() + platform.getWidth() / 2)) + eucDist((platform.getX() + platform.getWidth() / 2), x2, y1, y2);
                }
                else
                {
                    distance = Math.Min(Math.Abs(y2 - y1) + (x1 - (platform.getX() - platform.getWidth() / 2)) + (x2 - (platform.getX() - platform.getWidth() / 2)), Math.Abs(y2 - y1) + (platform.getX() + platform.getWidth() / 2) - x1 + (platform.getX() + platform.getWidth() / 2) - x2);
                }
            }
            //if platform is between Xs
            else
            {
                //if a point is at the left of the platform but not below
                if (y1 - (platform.getY() - platform.getHeight() / 2) < 0)
                {
                    //distance = Math.Abs(y2 - y1) + x2 - (platform.getX() - platform.getWidth() / 2);
                    distance = Math.Abs(y2 - (platform.getY() - platform.getHeight() / 2)) + eucDist(x1, x2, y1, (platform.getY() - platform.getHeight() / 2));
                }
                else if (y2 - (platform.getY() - platform.getHeight() / 2) < 0)
                {
                    //distance = Math.Abs(y2 - y1) + x2 - (platform.getX() - platform.getWidth() / 2);
                    distance = Math.Abs(y1 - (platform.getY() - platform.getHeight() / 2)) + eucDist(x1, x2, (platform.getY() - platform.getHeight() / 2), y2);
                }
                //if a point is at the right of the platform but not below 
                else if (y1 - (platform.getY() + platform.getHeight() / 2) > 0)
                {
                    //distance = Math.Abs(y2 - y1) + (platform.getX() + platform.getWidth() / 2) - x2;
                    distance = Math.Abs(y2 - (platform.getY() + platform.getHeight() / 2)) + eucDist(x1, x2, y1, (platform.getY() + platform.getHeight() / 2));
                }
                else if (y2 - (platform.getY() + platform.getHeight() / 2) > 0)
                {
                    //distance = Math.Abs(y2 - y1) + (platform.getX() + platform.getWidth() / 2) - x2;
                    distance = Math.Abs(y1 - (platform.getY() + platform.getHeight() / 2)) + eucDist(x1, x2, (platform.getY() + platform.getHeight() / 2), y2);
                }
                else
                {
                    distance = Math.Min(Math.Abs(x2 - x1) + (y1 - (platform.getY() - platform.getHeight() / 2)) + (y2 - (platform.getY() - platform.getHeight() / 2)), Math.Abs(x2 - x1) + (platform.getY() + platform.getHeight() / 2) - y1 + (platform.getY() + platform.getHeight() / 2) - y2);
                }
            }
            return distance;
        }

        //euclidean distance
        public double eucDist(float x1, float x2, float y1, float y2)
        {
            return (Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                         ACTIONS                                      ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

            //circle
        public Moves randomAction(Node node, Random rnd, float jumpBias)
        {
            Moves action;

            if (rnd.NextDouble() > jumpBias || node.possibleMovesCount() <= 1)
            {
                //choose from all moves
                action = node.getMove(rnd.Next(node.possibleMovesCount()));
            }
            else
            {
                int random = rnd.Next(node.possibleMovesCount());
                if (node.getRemainingMoves()[random] == Moves.JUMP)
                {
                    random = (random + 1) % node.possibleMovesCount();
                }
                action = node.getMove(random);
            }

            return action;
        }

        public Moves randomActionRectangle(Node node, Random rnd, float morphBias)
        {
            //Random rnd = new Random();
            Moves action;

            if (rnd.NextDouble() > morphBias || node.possibleMovesCount() <= 1 ||
               (node.possibleMovesCount() == 2 && node.getRemainingMoves().Exists(x => x == Moves.MORPH_UP) &&
               node.getRemainingMoves().Exists(x => x == Moves.MORPH_DOWN)))
            {
                //choose from all moves
                action = node.getMove(rnd.Next(node.possibleMovesCount()));
            }
            else
            {
                //chose a move that is not jump unless it is the only move left
                int random = rnd.Next(node.possibleMovesCount());
                while (node.getRemainingMoves()[random] == Moves.MORPH_UP || node.getRemainingMoves()[random] == Moves.MORPH_DOWN)
                {
                    random = (random + 1) % node.possibleMovesCount();
                }
                action = node.getMoveAndRemove(random);
            }

            return action;
        }

        public Moves randomAction(NodeMP node, List<Moves[]> possibleMoves, Random rnd, float jumpBias)
        {
            //Random rnd = new Random();
            Moves[] action;

            if (rnd.NextDouble() > jumpBias || possibleMoves.Count <= 1)
            {
                //choose from all moves
                action = possibleMoves[rnd.Next(possibleMoves.Count)];
            }
            else
            {
                //chose a move that is not jump unless it is the only move left
                action = possibleMoves[rnd.Next(possibleMoves.Count)];
            }

            if ((action[0] == Moves.ROLL_LEFT || action[0] == Moves.ROLL_RIGHT || action[0] == Moves.JUMP) && (action[1] == Moves.MOVE_LEFT || action[1] == Moves.MOVE_RIGHT || action[1] == Moves.MORPH_UP || action[1] == Moves.MORPH_DOWN))
            {
                return action[0];
            }
            else
            {
                return action[1];
            }
        }

        public Moves randomActionRectangle(NodeMP node, List<Moves[]> possibleMoves, Random rnd, float morphBias)
        {
            //Random rnd = new Random();
            Moves[] action;

            if (rnd.NextDouble() > morphBias || node.possibleMovesCount() <= 1 ||
               (possibleMoves.Count == 2 && (possibleMoves.Exists(x => x[0] == Moves.MORPH_UP) || possibleMoves.Exists(x => x[1] == Moves.MORPH_UP)) &&
               (possibleMoves.Exists(x => x[0] == Moves.MORPH_DOWN) || possibleMoves.Exists(x => x[1] == Moves.MORPH_DOWN))))
            {
                //choose from all moves
                action = possibleMoves[rnd.Next(possibleMoves.Count)];
            }
            else
            {
                //chose a move that is not jump unless it is the only move left
                action = possibleMoves[rnd.Next(possibleMoves.Count)];
            }

            if ((action[0] == Moves.MOVE_LEFT || action[0] == Moves.MOVE_RIGHT || action[0] == Moves.MORPH_UP || action[0] == Moves.MORPH_DOWN) && (action[1] == Moves.MOVE_LEFT || action[1] == Moves.MOVE_RIGHT || action[1] == Moves.MORPH_UP || action[1] == Moves.MORPH_DOWN))
            {
                return action[0];
            }
            else
            {
                return action[1];
            }
        }


        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                         PLAN                                         ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        //used when trying to recover a plan where the agent is not on a platform and state of the plan
        public PathPlan joinPlans(PathPlan correction, PathPlan original, Point connectionPoint)
        {
            List<Point> points = original.getPathPoints();
            int i;
            for (i = 0; i < points.Count; i++)
            {
                if (points[i].getPosX() == connectionPoint.getPosX() && points[i].getPosY() == connectionPoint.getPosY() &&
                    points[i].getUncaughtColl().Count == connectionPoint.getUncaughtColl().Count)
                {
                    break;
                }
            }
            for (int j = i; j < points.Count; j++)
            {
                correction.addPoint(points[j]);
            }

            return correction;
        }


        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                 AGENT SETUP                                          ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        //Organize the diamonds by height and platform
        public List<DiamondInfo> setupDiamonds(CollectibleRepresentation[] collectiblesInfo, int[,] levelLayout)
        {
            List<DiamondInfo> Diamonds = new List<DiamondInfo>();
            Platform platform;
            int i;

            foreach (CollectibleRepresentation diamond in collectiblesInfo)
            {
                //create diamond info
                platform = getDiamondPlatform(diamond.X, diamond.Y);

                //the levelLayout is used to calculate the areas of bias of the diamond
                DiamondInfo diamondInfo = new DiamondInfo(diamond.X, diamond.Y, platform, levelLayout);

                //add diamond to the list according to its position and platform - the highest diamonds on the highest platforms come first
                i = 0;
                if (Diamonds.Count == 0)
                {
                    Diamonds.Add(diamondInfo);
                }
                else
                {
                    foreach (DiamondInfo dInfo in Diamonds)
                    {
                        //if the diamond belongs to a higher platform
                        if (diamondInfo.getPlatform().getY() < dInfo.getPlatform().getY())
                        {
                            Diamonds.Insert(i, diamondInfo);
                            break;
                        }
                        else if (diamondInfo.getPlatform().getY() == dInfo.getPlatform().getY())
                        {
                            //if the diamond is in a platform at the same height but is higher than the other
                            if (diamondInfo.getY() < dInfo.getY())
                            {
                                Diamonds.Insert(i, diamondInfo);
                                break;
                            }
                        }
                        //if it is at the end of the list, then it is the lowest 
                        if (i == Diamonds.Count - 1)
                        {
                            Diamonds.Add(diamondInfo);
                            break;
                        }
                        i++;
                    }
                }
            }
            //give the diamonds their respective id
            for (i = 0; i < Diamonds.Count; i++)
            {
                Diamonds[i].setId(i);
                Diamonds[i].getPlatform().addDiamondOn(i);
            }

            return Diamonds;
        }

        public List<Platform> setupPlatforms(ObstacleRepresentation[] obstaclesInfo, ObstacleRepresentation[] yellowObstacles, ObstacleRepresentation[] greenObstacles)
        {
            Platforms = new List<Platform>();
            foreach (ObstacleRepresentation platform in obstaclesInfo)
            {
                Platforms.Add(new Platform(platform.X, platform.Y, platform.Width, platform.Height, PlatformType.Black));
            }
            foreach (ObstacleRepresentation platform in yellowObstacles)
            {
                Platforms.Add(new Platform(platform.X, platform.Y, platform.Width, platform.Height, PlatformType.Yellow));
            }
            foreach (ObstacleRepresentation platform in greenObstacles)
            {
                Platforms.Add(new Platform(platform.X, platform.Y, platform.Width, platform.Height, PlatformType.Green));
            }
            Platforms.Add(ground);

            return Platforms;
        }

        //create a layout where the platforms are 1 and the other areas are 0
        public int[,] getLevelLayout(ObstacleRepresentation[] obstaclesInfo, Rectangle area)
        {
            int[,] levelLayout = new int[area.Right, area.Bottom];
            //platform boundaries
            int lX, rX, tY, bY;

            foreach (ObstacleRepresentation platform in obstaclesInfo)
            {
                lX = (int)Math.Round(platform.X - platform.Width / 2);
                rX = (int)Math.Round(platform.X + platform.Width / 2);
                tY = (int)Math.Round(platform.Y - platform.Height / 2);
                bY = (int)Math.Round(platform.Y + platform.Height / 2);

                //assign the value 1 to each position that containts a platform
                for (int i = lX; i <= rX; i++)
                {
                    for (int j = tY; j <= bY; j++)
                    {
                        if (i < levelLayout.GetLength(0) && j < levelLayout.GetLength(1))
                            levelLayout[i, j] = 1;
                    }
                }
            }
            return levelLayout;
        }

        public ObstacleRepresentation[] joinObstacles(ObstacleRepresentation[] o1, ObstacleRepresentation[] o2)
        {
            ObstacleRepresentation[] oInfo = new ObstacleRepresentation[o1.GetLength(0) + o2.GetLength(0)];
            int i;
            for (i = 0; i < o1.GetLength(0); i++)
            {
                oInfo[i] = o1[i];
            }

            for (int j = 0; j < o2.GetLength(0); j++, i++)
            {
                oInfo[i] = o2[j];
            }

            return oInfo;
        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                         OTHER                                        ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        public bool lineIntersection(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {

            if ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4) == 0)
            {
                return false;
            }

            float pX = ((x1 * y2 - y1 * x2) * (x3 - x4) - (x1 - x2) * (x3 * y4 - y3 * x4)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));

            float pY = ((x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));

            if (x1 >= x2)
            {
                if (!(x2 <= pX && pX <= x1))
                {
                    return false;
                }
            }
            else
            {
                if (!(x1 <= pX && pX <= x2)) { return false; }
            }
            if (x3 >= x4)
            {
                if (!(x4 <= pX && pX <= x3)) { return false; }
            }
            else
            {
                if (!(x3 <= pX && pX <= x4)) { return false; }
            }
            if (y1 >= y2)
            {
                if (!(y2 <= pY && pY <= y1)) { return false; }
            }
            else
            {
                if (!(y1 <= pY && pY <= y2)) { return false; }
            }
            if (y3 >= y4)
            {
                if (!(y4 <= pY && pY <= y3)) { return false; }
            }
            else
            {
                if (!(y3 <= pY && pY <= y4)) { return false; }
            }
            return true;
        }

        public float setRectangleArea(float height)
        {
            rectangleArea = height * height;
            return rectangleArea;
        }

        public float getRectangleWidth(float height)
        {
            return rectangleArea / height;
        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                       DEBUG                                          ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/
        public void writeStart(int character)
        {
            string filePath;
            string start = "Level Started";

            //search circle
            if (character == 0)
            {
                filePath = @".\searchtimeCircle.txt";
            }
            //search rectangle
            else
            {
                filePath = @".\searchtimeRectangle.txt";
            }
            

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, true))
            {
                file.WriteLine(start);
            }
        }

        //For tests:  1- search; 2- completion
        public void writeTimeToFile(int type, int character, Stopwatch searchTime, float gSpeed)
        {
            /*float timeTaken = searchTime.ElapsedMilliseconds * 0.001f * gSpeed;
            string timeText = timeTaken.ToString();
            String filePath;

            //search circle
            if (type == 1 && character == 0)
            {
                filePath = @".\searchtimeCircle.txt";
            }
            //search rectangle
            else if(type == 1 && character == 1)
            {
                filePath = @".\searchtimeRectangle.txt";
            }
            //completion circle
            else if (type == 2 && character == 0)
            {
                filePath = @".\completiontimeCircle.txt";
            }
            //completion rectangle
            else
            {
                filePath = @".\completiontimeRectangle.txt";
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, true))
            {
                file.WriteLine(timeText);
            }

            //if (type == 1)
            //{
            //    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@".\searchtime.txt", true))
            //    {
            //        file.WriteLine(timeText);
            //    }
            //}
            //else if (type == 2)
            //{
            //    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@".\completiontime.txt", true))
            //    {
            //        file.WriteLine(timeText);
            //    }
            //}
            */
        }

        public void writeTimeToFile(int type, int character, Stopwatch searchTime, int eNO, int eNT, int tN, float gSpeed)
        {
            float timeTaken = searchTime.ElapsedMilliseconds * 0.001f * gSpeed;
            string timeText = timeTaken.ToString();
            String filePath;

            //search circle
            if (type == 1 && character == 0)
            {
                filePath = @".\searchtimeCircle.txt";
            }
            //search rectangle
            else 
            {
                filePath = @".\searchtimeRectangle.txt";
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, true))
            {
                file.WriteLine(timeText);
                file.WriteLine("Explored Nodes (Once): " + eNO);
                file.WriteLine("Explored Nodes (Total): " + eNT);
                file.WriteLine("TotalNodes: " + tN);
            }

        }

        public void writeHeight(float height)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@".\rectangleHeightCircle.txt", true))
            {
                file.WriteLine(height);
            }
        }

        

    }
}
