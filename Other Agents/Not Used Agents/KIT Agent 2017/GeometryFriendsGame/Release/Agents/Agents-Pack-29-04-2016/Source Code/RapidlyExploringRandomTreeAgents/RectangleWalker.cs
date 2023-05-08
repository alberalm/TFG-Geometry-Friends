using GeometryFriends.AI;
using System;
using System.Collections;

namespace GeometryFriendsAgents
{
    public class RectangleWalker
    {
        private RectanglePID controller;
        private Path path;
        private static int currentPathPosition;
        private static int currentWalkingPoint;
        private ArrayList walkingPoints;
        private static float maxVelocityRectangle = 200;
        private static bool goBack = false;
        private static ArrayList lastAction = new ArrayList();
        private static float goBackVelocity = 25;
        private static int numberOfTimesBack = 0;
        private static int iterForBackupPlan = 0;

        public RectangleWalker(Path p)
        {
            controller = new RectanglePID();
            path = p;
            walkingPoints = new ArrayList();

            float desiredVelocity = 0;
            Point point;
            currentPathPosition = 0;
            currentWalkingPoint = 0;
            float distanceToMorph = 1200;
            float errorSize = 0;
            float minVelocity = 250;
            for (int i = 0; i < path.path.Count; i++)
            {
                bool yCondition = false;
                point = (Point)path.path[i];
                float xPoint = point.x;
                float yPoint = point.y;

                
                if(point.DiamondAbove)	// Checked & implemented previous features	// DONE
                {
        	        distanceToMorph = 96;
        	        errorSize = 2;
        	        minVelocity = 550;
                    desiredVelocity = 0;
                }
                else if (point.Fall)		// Checked & implemented previous features	// DONE
                {
        	        distanceToMorph = 150;
        	        errorSize = 3;
        	        minVelocity = 555;

        	        if(point.DistancePlatform <= 20)
        	        {
        		        desiredVelocity = 0;
                        if (i != 0 && Math.Abs(((Point)path.path[i - 1]).x - point.x) > 100)
                            distanceToMorph = 100;
                        else
                            distanceToMorph = 400;

        	        }
        	        else if(point.DistancePlatform > 350)
        	        {
        		        desiredVelocity = 500;
        	        }
        	        else
        	        {
        		        desiredVelocity = ((float) point.DistancePlatform)/350.0F * 500;
        	        }

        	        desiredVelocity = 0;
                }
                else if(point.FallGap)		// Checked & implemented previous features	// DONE
                {
        	        distanceToMorph = 5;
        	        errorSize = 3;
        	        minVelocity = 5;
        	        desiredVelocity = 20;
                }
                else if (point.Tilt)		// Checked & implemented previous features	// DONE
                {
                    if (i != 0)
                        distanceToMorph = getWidth(((Point)path.path[i - 1]).Size)+50;
                    else
                        distanceToMorph = 192;
        	        errorSize = 15;
        	        minVelocity = 555;
                    desiredVelocity = 500;
                    
                    //Point aux_point = (Point)path.path[i + 1];

                    //if (aux_point.Platform.maxWidth() - point.Platform.minWidth() < 200)
                    //{
                    //    desiredVelocity = maxVelocityRectangle * (aux_point.Platform.maxHeight() - point.Platform.maxHeight()) / 96;
                    //}
                    //else
                    //{
                    //    desiredVelocity = maxVelocityRectangle;
                    //}
                }
                else if (point.TurningPoint)		// Checked & implemented previous features	// DONE
                {
                    distanceToMorph = 150;
                    errorSize = 3;
                    minVelocity = 250;
                    desiredVelocity = 0;
                }
                else if (point.Morph)		// Checked & implemented previous features
                {
        	        distanceToMorph = 1200;
        	        errorSize = 3;
        	        minVelocity = 250;
                    yCondition = true;

			        if (i > 0)
                    {
                        Point aux_point = (Point)path.path[i - 1];
                        float distanceToMorphZone = Math.Abs(aux_point.x - point.x);
                        if (distanceToMorphZone < 200)
                        {
                            desiredVelocity = maxVelocityRectangle * distanceToMorphZone / 200;
                        }
                        else
                        {
                            desiredVelocity = maxVelocityRectangle;
                        }
                    }
                    else
                        desiredVelocity = maxVelocityRectangle;

                }
                else						// Checked & implemented previous features // DONE
                {
        	        distanceToMorph = 1200;
        	        errorSize = 10;
        	        minVelocity = 555;
        	        desiredVelocity = 500;
                }
                walkingPoints.Add(new RectangleWalkingPoint(xPoint, yPoint, desiredVelocity, point.numberOfCollectibles, point.Platform, distanceToMorph, errorSize, minVelocity, yCondition));
            }
        }

        public RectangleWalkingPoint getFirstPoint()
        {
            return (RectangleWalkingPoint)walkingPoints[0];
        }

        private float getWidth(float size)
        {
            return (96 * 96) / size;
        }

        public RectangleWalkingPoint hasReached(RectangleWalkingPoint wp, float x, float y, int numberOfCollectibles, float sizeOfAgent)
        {
            Point p = (Point)path.path[currentPathPosition];
            float threshold = getWidth(sizeOfAgent);
            if (threshold > 96)
                threshold = 10;
            if(p.Morph && !p.StartGap && !p.Tilt && !p.TurningPoint && !p.Gap && !p.DiamondAbove && !p.FallGap && !p.Fall)
            {
                if(Math.Abs(sizeOfAgent - p.Size) < 8)
                {
                    currentWalkingPoint++;
                    currentPathPosition++;
                }
            }
            else if(p.Fall || p.FallGap)
            {
                if( y > wp.YCoordinate - 32)
                {
                    if (numberOfCollectibles <= wp.Collectibles)
                    {
                        if(!(currentWalkingPoint + 1 >= walkingPoints.Count))
                        {
                            currentWalkingPoint++;
                            currentPathPosition++;
                        }
                    }
                }
            }else if (Math.Abs(x - wp.XCoordinate) < getWidth(sizeOfAgent) / 2 && Math.Abs(y - wp.YCoordinate) < sizeOfAgent/2)
            {
                if( numberOfCollectibles <= wp.Collectibles)
                    if (!(currentWalkingPoint + 1 >= walkingPoints.Count))
                    {
                        if (p.Morph)
                        {
                            if (Math.Abs(sizeOfAgent - p.Size) < 10)
                            {
                                currentWalkingPoint++;
                                currentPathPosition++;
                            }
                        }
                        else
                        {
                            currentWalkingPoint++;
                            currentPathPosition++;
                        }
                    }
            }
            else if( numberOfCollectibles <= wp.Collectibles)
            {
                if( x < wp.Obs.maxWidth() && x > wp.Obs.minWidth())
                {
                    if (Math.Abs(y - wp.YCoordinate) < 100)
                    {
                        if (!(currentWalkingPoint + 1 >= walkingPoints.Count))
                        {
                            RectangleWalkingPoint aux = (RectangleWalkingPoint)walkingPoints[currentWalkingPoint + 1];
                            if (x < aux.Obs.maxWidth() && x > aux.Obs.minWidth() && y < aux.Obs.maxHeight() && Math.Abs(y-aux.Obs.maxHeight()) < 98)
                            {
                                if (numberOfCollectibles >= aux.Collectibles)
                                {
                                    currentWalkingPoint++;
                                    currentPathPosition++;
                                }
                            }
                        }
                    }
                }
            }

            return (RectangleWalkingPoint)walkingPoints[currentWalkingPoint];
        }

        public Moves gainBalance(float targetX, float currentX, float velocity, float desiredSize, float agentSize)
        {
            Moves act;

            if(lastAction.Count > 999)
            {
                lastAction.RemoveRange(0, 100);
            }
            else
            {
                lastAction.Add(currentX);
            }

            float totalsum = 0;
            foreach(float sum in lastAction)
            {
                totalsum += sum;
            }

            totalsum = totalsum / lastAction.Count;

            if (Math.Abs(velocity) < 2 && Math.Abs(totalsum-currentX) < 2 && lastAction.Count > 900)
            {
                goBack = true;
            }
            else if (Math.Abs(velocity) > goBackVelocity)
            {
                goBack = false;
                numberOfTimesBack = 0;
                iterForBackupPlan = 0;
            }
            else if (numberOfTimesBack > 1000)
            {
                numberOfTimesBack = 0;
                iterForBackupPlan = 0;
                goBackVelocity = 25;
            }

            if (goBack)
            {
                numberOfTimesBack++;
                if(numberOfTimesBack%50 == 0)
                    goBackVelocity += 10;
                if(numberOfTimesBack > 500 && iterForBackupPlan < 500)
                {
                    iterForBackupPlan++;
                    if (currentX < targetX)
                    {
                        act = Moves.MOVE_RIGHT;    //MOVE RIGHT
                    }
                    else
                    {
                        act = Moves.MOVE_LEFT;    //MOVE LEFT
                    }
                }
                else
                if (currentX < targetX)
                {
                    act = Moves.MOVE_LEFT;    //MOVE LEFT
                }
                else
                {
                    act = Moves.MOVE_RIGHT;    //MOVE RIGHT
                }
            }
            else
            {
                act = Moves.NO_ACTION;
            }
            return act;
        }

        public Moves WalkToPoint(RectangleWalkingPoint wp, float v, float x, float y, float agentSize)
        {
            Point p = (Point)path.path[currentPathPosition];
            float sizeOfAgent= 0;

            if(wp.yCondition)   // Special Condition for Morph
            {
                sizeOfAgent = agentSize;
            }
            else
            {
                sizeOfAgent = 1000;
            }

            if (Math.Abs(x - wp.XCoordinate) < wp.distanceToMorph && Math.Abs(agentSize - p.Size) > wp.errorSize && Math.Abs(v) < wp.minVelocity && Math.Abs(y - wp.YCoordinate) < sizeOfAgent / 2)
            {
                if (agentSize > p.Size)
                {
                    return Moves.MORPH_DOWN;
                }
                else
                {
                    return Moves.MORPH_UP;
                }
            }
            else
            {
                Moves act;
                if ((act = gainBalance(wp.XCoordinate, x, v, p.Size, agentSize)) != 0)
                {
                    return act;
                }
                if (Math.Abs(wp.XCoordinate - x) < wp.distanceToMorph)
                    return normalWalk(wp, v, x, y, wp.distanceToMorph);
                else
                {
                    if (p.FallGap)   // Special Condition for FallGap
                    {
                        if (agentSize > 60)
                            return Moves.MORPH_DOWN;
                    }
                    if (x < wp.XCoordinate)
                        return Moves.MOVE_RIGHT;
                    else
                        return Moves.MOVE_LEFT;
                }
            }

        }

        private Moves normalWalk(RectangleWalkingPoint wp, float v, float x, float y, float distanceForController)
        {
            if (wp.XCoordinate < x && v > 0)
                return Moves.MOVE_LEFT;
            if (wp.XCoordinate > x && v < 0)
                return Moves.MOVE_RIGHT;

            if (wp.XCoordinate < x)
            {
                if (x - wp.XCoordinate < distanceForController)
                {
                    if (Math.Abs(v) > 10)
                        return controller.calculateAction(x, v, wp.Velocity, 0, 0.1F); // 0.1 value for step is a placeholder, must be replaced with the real time value
                    else
                        return Moves.MOVE_LEFT;
                }
                else
                {
                    return Moves.MOVE_LEFT;
                }
            }
            else
            {
                if (wp.XCoordinate - x < distanceForController)
                {
                    if (Math.Abs(v) > 10)
                        return controller.calculateAction(x, v, wp.Velocity, 1, 0.1F); // 0.1 value for step is a placeholder, must be replaced with the real time value
                    else
                        return Moves.MOVE_RIGHT;
                }
                else
                {
                    return Moves.MOVE_RIGHT;
                }
            }
        }
    }
}
