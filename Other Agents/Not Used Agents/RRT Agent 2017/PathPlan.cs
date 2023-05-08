using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GeometryFriends.AI;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Perceptions.Information;
using System.Drawing;

namespace GeometryFriendsAgents
{
    public class PathPlan
    {

        private List<Point> pathPoints;
        private List<Point> originalCleanedPathPoints;
        private List<Point> originalPathPoints;
        private float cleanRollMargin = 10;
        private float cleanJumpMargin = 10;
        private int totalCollectibles = 0;
        private int currentPoint = 0;
        private int maxFailure = 2;
        private float circleMaxJump = 400;
        private bool cutPlan;


        public PathPlan(bool c)
        {
            pathPoints = new List<Point>();
            originalCleanedPathPoints = new List<Point>();
            originalPathPoints = new List<Point>();
            cutPlan = c;
        }
        
        public void setTotalCollectibles(int col)
        {
            totalCollectibles = col;
        }

        public int getTotalCollectibles()
        {
            return totalCollectibles;
        }

        public List<Point> getPathPoints()
        {
            return pathPoints;
        }

        public void addPoint(Point point)
        {
            pathPoints.Insert(0, point);
        }

        public void addPointEnd(Point point)
        {
            pathPoints.Add(point);
        }

        //remove first point on the list after indicated that the agent passed through this point
        public void nextPoint()
        {
            if (cutPlan)
            {
                originalCleanedPathPoints[currentPoint].passedThrough();
            }
            else
            {
                originalPathPoints[currentPoint].passedThrough();
            }            
            currentPoint++;
            pathPoints.RemoveAt(0);
        }

        public void setCurrentPoint(int i)
        {
            currentPoint = i;
        }

        public int getCurrentPoint()
        {
            return currentPoint;
        }

        //check if the agent is keeps failing the action on the same point
        public bool checkIfConstantFail()
        {
            if (cutPlan)
            {
                return checkIfConstantFailAux(originalCleanedPathPoints);
            }
            else
            {
                return checkIfConstantFailAux(originalPathPoints);
            }
            
        }

        public bool checkIfConstantFailAux(List<Point> points)
        {
            //in case it only has one action
            if (points.Count == 1)
            {
                if (points[0].getTimesPassed() >= maxFailure)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            //check if stuck on other points
            for (int i = 0; i < points.Count - 1; i++)
            {
                if (points[i].getTimesPassed() - points[i + 1].getTimesPassed() >= maxFailure)
                {
                    return true;
                }
            }
            //check if stuck on the last point
            if (points[points.Count - 1].getTimesPassed() - points[points.Count - 2].getTimesPassed() >= maxFailure)
            {
                return true;
            }
            return false;
        }

        //circle only
        //TODO - a cleanPlan for the rectangle
        public List<DebugInformation> cleanPlan(ObstacleRepresentation[] platforms, List<DiamondInfo> diamonds, Rectangle area, float circleRadius, bool first)
        {
            //TODO - check other cases
            bool stopFor = false;

            if(pathPoints.Count != 0) {
                //if the first action is JUMP and there is no diamond above, then it is not necessary 
                int margin = 10;
                bool above = false;
                if (first && pathPoints[0].getAction() == Moves.JUMP)
                {
                    foreach(DiamondInfo diamond in diamonds)
                    {
                        if((diamond.getX() + circleRadius >= pathPoints[0].getPosX() - circleRadius - margin &&
                           diamond.getX() + circleRadius <= pathPoints[0].getPosX() + circleRadius + margin) ||
                           (diamond.getX() - circleRadius >= pathPoints[0].getPosX() - circleRadius - margin &&
                           diamond.getX() - circleRadius <= pathPoints[0].getPosX() + circleRadius + margin) &&
                           Math.Abs(diamond.getY() - pathPoints[0].getPosY()) <= circleMaxJump)
                        {
                            above = true;
                        }
                    }
                    if (!above)
                    {
                        while (pathPoints[0].getAction() == Moves.JUMP)
                        {
                            pathPoints.RemoveAt(0);
                        }
                    }
                }
                
                //check if agent keeps on the same platform or returning to the same platform without catching a diamond
                for (int i = 0; i < pathPoints.Count; i++)
                {
                    //get the furthest state where the agent is the same platform at the same Y level and with the same number of caught diamonds
                    for(int j = pathPoints.Count - 1; j > i; j--)
                    {
                        //if the agent has caught the same number of diamonds
                        //if the agent is at the same Y level                        
                        if(pathPoints[j].getUncaughtColl().Count == pathPoints[i].getUncaughtColl().Count &&
                            Math.Abs(pathPoints[j].getPosY() - pathPoints[i].getPosY()) < cleanRollMargin)
                        {
                            //if the platform is the same and there is no other blocking the way nor it is a jump
                            Platform platformJ = onPlatform(pathPoints[j].getPosX(), pathPoints[j].getPosY(), platforms, area);
                            Platform platformI = onPlatform(pathPoints[i].getPosX(), pathPoints[i].getPosY(), platforms, area);
                            if(platformJ != null && platformI != null &&
                                platformJ.getX() == platformI.getX() && platformJ.getY() == platformI.getY() &&
                                !blockingPlatform(pathPoints[i].getPosX(), pathPoints[i].getPosY(), pathPoints[j].getPosX(), pathPoints[j].getPosY(), platforms, circleRadius) &&
                                pathPoints[j].getAction() != Moves.JUMP)
                            {
                                //then remove all points until j 
                                pathPoints.RemoveRange(i, Math.Abs(j - i));
                                cleanPlan(platforms, diamonds, area, circleRadius, false);
                                stopFor = true;
                                break;
                            }
                        }
                    }
                    if (stopFor)
                    {
                        break;
                    }
                }
            }
            return debugCleanPlan();
        }

        private bool blockingPlatform(float x, float y, float oX, float oY, ObstacleRepresentation[] platforms, float cRad)
        {
            foreach(ObstacleRepresentation platform in platforms)
            {
                if(((platform.X > x && platform.X < oX) || (platform.X < x && platform.X > oX)) &&
                    ((y - cRad > platform.Y - platform.Height/2 && y - cRad < platform.Y + platform.Height/2) ||
                    (y + cRad > platform.Y - platform.Height/2 && y + cRad < platform.Y + platform.Height / 2)))
                {
                    return true;
                }
            }
            return false;
        }

        public Platform onPlatform(float x, float y, ObstacleRepresentation[] platforms, Rectangle area)
        {
            float margin = 25;
            foreach (ObstacleRepresentation platform in platforms)
            {
                if (x >= (platform.X - platform.Width / 2) &&
                   x <= (platform.X + platform.Width / 2) &&
                   y + 10 >= (platform.Y - platform.Height / 2 - margin) &&
                   y + 10 <= (platform.Y + platform.Height / 2 + margin))
                {
                    return new Platform(platform.X, platform.Y, platform.Width, platform.Height);
                }
            }
            if (y + 10 >= 720)
            {
                return new Platform(0, area.Bottom, 0, 0);
            }
            return null;
        }

        public Platform onPlatform(float x, float yH, float yW, ObstacleRepresentation[] platforms, Rectangle area)
        {
            float margin = 50;
            foreach (ObstacleRepresentation platform in platforms)
            {
                if (x >= (platform.X - platform.Width / 2) &&
                   x <= (platform.X + platform.Width / 2) &&
                   ((yH + 10 >= (platform.Y - platform.Height / 2 - margin) &&
                   yH + 10 <= (platform.Y + platform.Height / 2 + margin)) ||
                   (yW + 10 >= (platform.Y - platform.Height / 2 - margin) &&
                   yW + 10 <= (platform.Y + platform.Height / 2 + margin))))
                {
                    return new Platform(platform.X, platform.Y, platform.Width, platform.Height);
                }
            }
            if (yH + 10 >= 720 && yW + 10 >= 720)
            {
                return new Platform(0, area.Bottom, 0, 0);
            }
            return null;
        }


        public List<DebugInformation> debugCleanPlan()
        {
            List<DebugInformation> debugInfo = new List<DebugInformation>();

            for(int i = 1; i < pathPoints.Count; i++)
            {
                debugInfo.Add(GeometryFriends.AI.Debug.DebugInformationFactory.CreateLineDebugInfo(new PointF(pathPoints[i - 1].getPosX(), pathPoints[i - 1].getPosY()), new PointF(pathPoints[i].getPosX(), pathPoints[i].getPosY()), new GeometryFriends.XNAStub.Color(255, 0, 255)));
            }

            return debugInfo;
        }

        public void saveOriginal()
        {
            List<Point> points;
            if (cutPlan)
            {
                points = originalCleanedPathPoints;
            }
            else
            {
                points = originalPathPoints;
            }

            foreach(Point point in pathPoints)
            {
                points.Add(point);
            }
        }

        public List<Point> getOriginalPoints()
        {
            if (cutPlan)
            {
                return originalCleanedPathPoints;
            }
            else
            {
                return originalPathPoints;
            }            
        }

        public void setOriginalPoints(List<Point> points)
        {
            if (cutPlan)
            {
                originalCleanedPathPoints = points;
            }
            else
            {
                originalPathPoints = points;
            }
        }

        public PathPlan clone()
        {
            PathPlan newPlan = new PathPlan(cutPlan);

            newPlan.setTotalCollectibles(this.totalCollectibles);

            foreach(Point point in this.pathPoints)
            {
                newPlan.addPointEnd(point);
            }

            return newPlan;
        }

    }
}
