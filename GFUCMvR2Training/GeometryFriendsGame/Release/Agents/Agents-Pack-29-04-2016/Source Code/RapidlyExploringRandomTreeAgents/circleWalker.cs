using GeometryFriends.AI;
using System;
using System.Collections;

namespace GeometryFriendsAgents
{
    public class CircleWalker
    {
        private CirclePID controller;
        private Path path;
        private static int currentPathPosition;
        private static int currentWalkingPoint;
        private ArrayList walkingPoints;
        private static float maxVelocityCircle = 200;

        public CircleWalker(Path p)
        {
            controller = new CirclePID();
            path = p;
            walkingPoints = new ArrayList();

            float desiredVelocity = 0;
            Point point;
            currentPathPosition = 0;
            currentWalkingPoint = 0;
            for (int i = 0; i < path.path.Count; i++)
            {
                point = (Point)path.path[i];
                float xPoint = point.x;
                float yPoint = point.y;

                if (point.TurningPoint || point.DiamondAbove)
                {
                    desiredVelocity = 0;
                }
                else if (point.StartGap)
                {
                    i++;
                    point = (Point)path.path[i];
                    if (point.Platform.maxWidth() - point.Platform.minWidth() < 200)
                    {
                        desiredVelocity = maxVelocityCircle * (point.Platform.maxWidth() - point.Platform.minWidth()) / 200;
                    }
                    else
                    {
                        desiredVelocity = maxVelocityCircle;
                    }
                }
                else if (point.Fall || point.FallGap)
                {
                    desiredVelocity = 0;
                }
                else if (point.ToPlatform)
                {
                    if (point.Platform.maxWidth() - point.Platform.minWidth() > 200)
                        desiredVelocity = maxVelocityCircle;
                    else
                        desiredVelocity = maxVelocityCircle * (point.Platform.maxWidth() - point.Platform.minWidth())/200;
                }
                else
                {
                    desiredVelocity = maxVelocityCircle;
                }
                walkingPoints.Add(new CircleWalkingPoint(xPoint, yPoint, desiredVelocity, point.numberOfCollectibles, point.Platform));
            }
        }

        public CircleWalkingPoint getFirstPoint()
        {
            return (CircleWalkingPoint)walkingPoints[0];
        }

        public CircleWalkingPoint hasReached(CircleWalkingPoint wp, float x, float y, int numberOfCollectibles)
        {
            if (Math.Abs(x - wp.XCoordinate) < 50 && Math.Abs(y - wp.YCoordinate) < 50)
            {
                if( numberOfCollectibles <= wp.Collectibles)
                    if (!(currentWalkingPoint + 1 >= walkingPoints.Count))
                    {
                        currentWalkingPoint++;
                        currentPathPosition++;
                    }
            }
            else
            if( numberOfCollectibles <= wp.Collectibles)
            {
                if( x < wp.Obs.maxWidth() && x > wp.Obs.minWidth())
                {
                    if (Math.Abs(y - wp.YCoordinate) < 100)
                    {
                        if (!(currentWalkingPoint + 1 >= walkingPoints.Count))
                        {
                            CircleWalkingPoint aux = (CircleWalkingPoint)walkingPoints[currentWalkingPoint + 1];
                            if (x < aux.Obs.maxWidth() && x > aux.Obs.minWidth() && y < aux.Obs.maxHeight())
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



            return (CircleWalkingPoint)walkingPoints[currentWalkingPoint];
        }

        private static float firstPointX = 0;

        public Moves WalkToPoint(CircleWalkingPoint wp, float v, float x, float y, float agentSize)
        {
            Point p = (Point)path.path[currentPathPosition];
            if(currentPathPosition == 1)
            {
                firstPointX = x;
            }

            if(p.ToPlatform)
            {
                float limit = Math.Abs(wp.YCoordinate - y) * (Math.Abs(v)/Math.Abs(wp.XCoordinate - x));
                if(wp.Obs.maxWidth() - wp.Obs.minWidth() < 400)
                {
                    limit += ((wp.Obs.maxWidth() - wp.Obs.minWidth()) / 400)*50;
                }
                if (limit < 100)
                    limit = 100;

                bool isRightJump = false;
                bool isLeftJump = false;
                float aux = 0;
                if( currentPathPosition <= 1)
                    aux = firstPointX;
                else
                    aux = ((Point)path.path[currentPathPosition - 1]).x;
                if (aux > p.x)
                {
                    isRightJump = true;
                }else
                {
                    isLeftJump = true;
                }

                if (Math.Abs(wp.XCoordinate - x) <= limit)
                {
                    if((isRightJump && x > wp.XCoordinate && v < 0) || (isLeftJump && x < wp.XCoordinate && v > 0))
                        return Moves.JUMP;
                    else
                        return normalWalk(wp, v, x, y);
                }
                else
                    return normalWalk(wp, v, x, y);
            }
            else if (p.DiamondAbove)
            {
                if (Math.Abs(wp.XCoordinate - x) < 80)
                {
                    return Moves.JUMP;
                }
                else
                {
                    return normalWalk(wp, v, x, y);
                }
            }
            else if (p.Fall || p.FallGap)
            {
                if(Math.Abs(wp.XCoordinate - x) < 200)
                {
                    return normalWalk(wp, v, x, y);
                }
                else
                {
                    if (x < wp.XCoordinate)
                        return Moves.ROLL_RIGHT;
                    else
                        return Moves.ROLL_LEFT;
                }
            }

            else if (p.StartGap)
            {
                if (Math.Abs(wp.XCoordinate - x) < 100 && Math.Abs(v) > 20)
                    return Moves.JUMP;
                else
                    return normalWalk(wp, v, x, y);
            }

            else if (p.Gap)
            {
                currentPathPosition++;
                return Moves.NO_ACTION;
            }

            else if (p.TurningPoint)
            {
                if (Math.Abs(wp.XCoordinate - x) < 100)
                    return normalWalk(wp, v, x, y);
                else
                {
                    if (x < wp.XCoordinate)
                        return Moves.ROLL_RIGHT;
                    else
                        return Moves.ROLL_LEFT;
                }
            }
            else
            {
                if (Math.Abs(wp.XCoordinate - x) < 100)
                    return normalWalk(wp, v, x, y);
                else
                {
                    if (x < wp.XCoordinate)
                        return Moves.ROLL_RIGHT;
                    else
                        return Moves.ROLL_LEFT;
                }
            }

        }

        private Moves normalWalk(CircleWalkingPoint wp, float v, float x, float y)
        {
            if (wp.XCoordinate < x)
            {
                if (x - wp.XCoordinate < 150)
                {
                    if (Math.Abs(v) > 25)
                        return controller.calculateAction(x, Math.Abs(v), wp.Velocity, 0, 0.1F); // 0.1 value for step is a placeholder, must be replaced with the real time value
                    else
                        return Moves.ROLL_LEFT;
                }
                else
                {
                    return Moves.ROLL_LEFT;
                }
            }
            else
            {
                if (wp.XCoordinate - x < 150)
                {
                    if (Math.Abs(v) > 25)
                        return controller.calculateAction(x, v, wp.Velocity, 1, 0.1F); // 0.1 value for step is a placeholder, must be replaced with the real time value
                    else
                        return Moves.ROLL_RIGHT;
                }
                else
                {
                    return Moves.ROLL_RIGHT;
                }
            }
        }

    }
}
