using System;
using System.Collections.Generic;
using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    public class MoveInformation
    {
        public Platform landingPlatform;
        public Platform departurePlatform;
        public int x;
        public int xlandPoint;
        public int velocityX;
        public MoveType moveType;
        public List<int> diamondsCollected;
        public List<Tuple<float, float>> path;
        public int distanceToObstacle;
        public RectangleShape.Shape shape;
        public bool rightEdgeIsDangerous;
        public Moves moveDuringFlight;
        public bool risky;
        public bool closeLeft;
        public bool closeRight;

        public MoveInformation(MoveInformation other)
        {
            this.departurePlatform = other.departurePlatform;
            this.landingPlatform = other.landingPlatform;
            this.x = other.x;
            this.xlandPoint = other.xlandPoint;
            this.velocityX = other.velocityX;
            this.moveType = other.moveType;
            this.diamondsCollected = new List<int>(other.diamondsCollected);
            this.path = new List<Tuple<float, float>> (other.path);
            this.distanceToObstacle = other.distanceToObstacle;
            this.shape = other.shape;
            this.rightEdgeIsDangerous = other.rightEdgeIsDangerous;
            this.moveDuringFlight = other.moveDuringFlight;
            this.risky = other.risky;
            this.closeLeft = other.closeLeft;
            this.closeRight = other.closeRight;
        }

        public MoveInformation(Platform landingPlatform)
        {
            this.departurePlatform = landingPlatform;
            this.landingPlatform = landingPlatform;
            this.x = 0;
            this.xlandPoint = 0;
            this.velocityX = 0;
            this.moveType = MoveType.NOMOVE;
            this.diamondsCollected = new List<int>();
            this.path = new List<Tuple<float, float>>();
            this.path.Add(new Tuple<float, float>((landingPlatform.rightEdge + landingPlatform.leftEdge) * GameInfo.PIXEL_LENGTH / 2, landingPlatform.yTop * GameInfo.PIXEL_LENGTH));
            this.distanceToObstacle = 0;
            this.moveDuringFlight = Moves.NO_ACTION;
            this.risky = false;
            this.closeLeft = false;
            this.closeRight = false;
        }

        public MoveInformation(Platform landingPlatform, Platform departurePlatform, int x, int xlandPoint, int velocityX, MoveType moveType, List<int> diamondsCollected, List<Tuple<float, float>> path, int distanceToObstacle)
        {
            this.departurePlatform = departurePlatform;
            this.landingPlatform = landingPlatform;
            this.x = x;
            this.xlandPoint = xlandPoint;
            this.velocityX = velocityX;
            this.moveType = moveType;
            this.diamondsCollected = diamondsCollected;
            this.path = path;
            this.distanceToObstacle = distanceToObstacle;
            this.moveDuringFlight = Moves.NO_ACTION;
            if(velocityX > 0)
            {
                rightEdgeIsDangerous = true;
            }
            else
            {
                rightEdgeIsDangerous = false;
            }
            this.risky = false;
        }

        public int DistanceToRollingEdge()
        {
            if (rightEdgeIsDangerous)
            {
                return landingPlatform.rightEdge - xlandPoint;
            }
            else
            {
                return xlandPoint - landingPlatform.leftEdge;
            }
        }

        public int DistanceToOtherEdge()
        {
            if (rightEdgeIsDangerous)
            {
                return xlandPoint - landingPlatform.leftEdge;
            }
            else
            {
                return landingPlatform.rightEdge - xlandPoint;
            }
        }

        private float Value()
        {
            int quarterPointLeft = (3 * landingPlatform.leftEdge + landingPlatform.rightEdge) / 4;
            int quarterPointRight = (landingPlatform.leftEdge + 3 * landingPlatform.rightEdge) / 4;
            int middleDeparture = (departurePlatform.leftEdge + departurePlatform.rightEdge) / 2;
            if (velocityX >= 0)
            {
                return Math.Abs(xlandPoint - quarterPointLeft) / 3 + Math.Abs(velocityX) / 10 - distanceToObstacle + Math.Abs(x - middleDeparture) / 10;
            }
            else
            {
                return Math.Abs(xlandPoint - quarterPointRight) / 3 + Math.Abs(velocityX) / 10 - distanceToObstacle + Math.Abs(x - middleDeparture) / 10;
            }
        }

        // Returns 1 is this is better, -1 if other is better, 0 if not clear or not comparable
        public int CompareCircle(MoveInformation other, CollectibleRepresentation[] initialCollectiblesInfo, List<Platform> platformList)
        {   
            // Here is where we filter movements
            if (landingPlatform.id != other.landingPlatform.id || departurePlatform.id != other.departurePlatform.id)
            {
                return 0;
            }

            if (moveType == MoveType.NOMOVE && other.moveType == MoveType.NOMOVE && diamondsCollected[0] == other.diamondsCollected[0])
            {
                if (Math.Abs(x - initialCollectiblesInfo[diamondsCollected[0]].X / GameInfo.PIXEL_LENGTH) < Math.Abs(other.x - initialCollectiblesInfo[diamondsCollected[0]].X / GameInfo.PIXEL_LENGTH))
                {
                    return 1;
                }
                if (Math.Abs(x - initialCollectiblesInfo[diamondsCollected[0]].X / GameInfo.PIXEL_LENGTH) > Math.Abs(other.x - initialCollectiblesInfo[diamondsCollected[0]].X / GameInfo.PIXEL_LENGTH))
                {
                    return -1;
                }
            }
            if (moveType == MoveType.NOMOVE && other.diamondsCollected.Count == 1 && diamondsCollected[0] == other.diamondsCollected[0] && other.landingPlatform == other.departurePlatform)
            {
                // Other is a jump from platform x to platform x and it was only added because it could reach a diamond
                // Now we have found that we can reach the same diamond without jumping, which will take us less time
                return 1;
            }
            else if (other.moveType == MoveType.NOMOVE && diamondsCollected.Count == 1 && diamondsCollected[0] == other.diamondsCollected[0] && landingPlatform == departurePlatform)
            {
                // Symmetric
                return -1;
            }
            if (moveType == MoveType.NOMOVE || other.moveType == MoveType.NOMOVE) // In general, we want to store these moves, since they don't really afect other moves
            {
                return 0;
            }
            if (Utilities.Contained(diamondsCollected, other.diamondsCollected) && Utilities.Contained(other.diamondsCollected, diamondsCollected)) //diamondsCollected=other.diamondsCollected
            {
                
                // Departure not real
                if (!departurePlatform.real)
                {
                    int y1 = (int)path[0].Item2 / GameInfo.PIXEL_LENGTH;
                    int y2 = (int)other.path[0].Item2 / GameInfo.PIXEL_LENGTH;
                    if(distanceToObstacle <= 6 && other.distanceToObstacle > 6)
                    {
                        return -1;
                    }
                    if (other.distanceToObstacle <= 6 && distanceToObstacle > 6)
                    {
                        return 1;
                    }
                    if (y1 > y2 && DistanceToOtherEdge() > 2 && distanceToObstacle > 6)
                    {
                        return 1;
                    }
                    else if(y1 < y2 && other.DistanceToOtherEdge() > 2 && other.distanceToObstacle > 6)
                    {
                        return -1;
                    }
                }

                // Landing not real
                if (!landingPlatform.real)
                {
                    int y1 = (int)path[path.Count - 1].Item2 / GameInfo.PIXEL_LENGTH;
                    int y2 = (int)other.path[other.path.Count - 1].Item2 / GameInfo.PIXEL_LENGTH;
                    if (y1 > y2)
                    {
                        return 1;
                    }
                    else if (y1 < y2)
                    {
                        return -1;
                    }
                }
                if (!departurePlatform.real)
                {
                    if (Math.Abs(velocityX) < Math.Abs(other.velocityX) && DistanceToOtherEdge() > 3 && distanceToObstacle > 6)
                    {
                        return 1;
                    }
                    else if (Math.Abs(velocityX) > Math.Abs(other.velocityX) && other.DistanceToOtherEdge() > 3 && other.distanceToObstacle > 6)
                    {
                        return -1;
                    }
                }
                int m = GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH;
                if (other.DistanceToOtherEdge() > m && other.DistanceToRollingEdge() > m && (DistanceToOtherEdge() <= m || DistanceToRollingEdge() <= m))
                {
                    return -1;
                }
                if (other.DistanceToOtherEdge() <= m && DistanceToOtherEdge() <= m)
                {
                    if (other.DistanceToOtherEdge() > DistanceToOtherEdge())
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                if (DistanceToOtherEdge() > m && DistanceToRollingEdge() > m && (other.DistanceToOtherEdge() <= m || other.DistanceToRollingEdge() <= m))
                {
                    return 1;
                }
                if (EnoughSpaceToAccelerateCircle(departurePlatform.leftEdge, departurePlatform.rightEdge, x, velocityX + (velocityX < 0 ? -1 : 1) * 20))
                {
                    if (!other.EnoughSpaceToAccelerateCircle(other.departurePlatform.leftEdge, other.departurePlatform.rightEdge, other.x, other.velocityX + (other.velocityX < 0 ? -1 : 1) * 20))
                    {
                        return 1;
                    }
                }
                else if (other.EnoughSpaceToAccelerateCircle(other.departurePlatform.leftEdge, other.departurePlatform.rightEdge, other.x, other.velocityX + (other.velocityX < 0 ? -1 : 1) * 20))
                {
                    return -1;
                }
                if (this.Value() < other.Value())
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            else if (Utilities.Contained(diamondsCollected, other.diamondsCollected) && !Utilities.Contained(other.diamondsCollected, diamondsCollected))//diamondsCollected strictly contained in other.diamondsCollected
            {
                return -1;
            }
            else if (!Utilities.Contained(diamondsCollected, other.diamondsCollected) && Utilities.Contained(other.diamondsCollected, diamondsCollected))//other.diamondsCollected strictly contained in diamondsCollected
            {
                return 1;
            }
            else if (!Utilities.Contained(diamondsCollected, other.diamondsCollected) && !Utilities.Contained(other.diamondsCollected, diamondsCollected))//Incomparable
            {
                return 0;
            }
            return 0;
        }

        // Returns 1 is this is better, -1 if other is better, 0 if not clear or not comparable
        public int CompareRectangle(MoveInformation other, CollectibleRepresentation[] initialCollectiblesInfo, List<Platform> platformList)
        {
            if(departurePlatform.id == other.departurePlatform.id && moveType == MoveType.FALL && other.moveType == MoveType.ADJACENT
                && !other.landingPlatform.real && other.departurePlatform.real)
            {
                foreach(MoveInformation adj in platformList[other.landingPlatform.id].moveInfoList)
                {
                    if((adj.moveType == MoveType.ADJACENT || adj.moveType == MoveType.DROP) && adj.landingPlatform.id == landingPlatform.id)
                    {
                        return -1;
                    }
                }
            }
            if (landingPlatform.id != other.landingPlatform.id || departurePlatform.id != other.departurePlatform.id)
            {
                return 0;
            }
            if(moveType == MoveType.FALL && other.moveType == MoveType.ADJACENT)
            {
                return -1;
            }
            if (moveType == MoveType.BIGHOLEADJ && other.moveType == MoveType.FALL)
            {
                return 1;
            }
            if (moveType == MoveType.BIGHOLEDROP && other.moveType == MoveType.FALL)
            {
                return 1;
            }
            if (moveType == MoveType.FALL && other.moveType == MoveType.BIGHOLEDROP)
            {
                return -1;
            }
            if (moveType == MoveType.NOMOVE && other.moveType == MoveType.NOMOVE)
            {
                if(Utilities.Contained(diamondsCollected, other.diamondsCollected) && Utilities.Contained(other.diamondsCollected, diamondsCollected))
                {
                    if (RectangleShape.CompareShapes(shape, other.shape) != 0)
                    {
                        return RectangleShape.CompareShapes(shape, other.shape);
                    }
                    else
                    {
                        if (Math.Abs(x - initialCollectiblesInfo[diamondsCollected[0]].X / GameInfo.PIXEL_LENGTH) < Math.Abs(other.x - initialCollectiblesInfo[diamondsCollected[0]].X / GameInfo.PIXEL_LENGTH))
                        {
                            return 1;
                        }
                        if (Math.Abs(x - initialCollectiblesInfo[diamondsCollected[0]].X / GameInfo.PIXEL_LENGTH) > Math.Abs(other.x - initialCollectiblesInfo[diamondsCollected[0]].X / GameInfo.PIXEL_LENGTH))
                        {
                            return -1;
                        }
                    }
                }
                else
                {
                    if(Utilities.Contained(diamondsCollected, other.diamondsCollected))
                    {
                        return -1;
                    }
                    if(Utilities.Contained(other.diamondsCollected, diamondsCollected))
                    {
                        return 1;
                    }
                }
            }
            if (moveType == MoveType.NOMOVE && other.diamondsCollected.Count == 1 && diamondsCollected[0] == other.diamondsCollected[0] && other.landingPlatform == other.departurePlatform)
            {
                // Other is a jump from platform x to platform x and it was only added because it could reach a diamond
                // Now we have found that we can reach the same diamond without jumping, which will take us less time
                return 1;
            }
            else if (other.moveType == MoveType.NOMOVE && diamondsCollected.Count == 1 && diamondsCollected[0] == other.diamondsCollected[0] && landingPlatform == departurePlatform)
            {
                // Symmetric
                return -1;
            }
            if (moveType == MoveType.NOMOVE) // In general, we want to store these moves, since they don't really afect other moves
            {
                return 0;
            }
            
            if (Utilities.Contained(diamondsCollected, other.diamondsCollected) && Utilities.Contained(other.diamondsCollected, diamondsCollected)) //diamondsCollected=other.diamondsCollected
            {
                if(moveType == MoveType.FALL && other.moveType == MoveType.FALL)
                {
                    if(shape == RectangleShape.Shape.HORIZONTAL && other.shape != RectangleShape.Shape.HORIZONTAL)
                    {
                        return 1;
                    }
                    if(other.shape == RectangleShape.Shape.HORIZONTAL && shape != RectangleShape.Shape.HORIZONTAL)
                    {
                        return -1;
                    }
                    if(shape == RectangleShape.Shape.SQUARE && other.shape == RectangleShape.Shape.VERTICAL)
                    {
                        return 1;
                    }
                    if (shape == RectangleShape.Shape.VERTICAL && other.shape == RectangleShape.Shape.SQUARE)
                    {
                        return -1;
                    }
                    if (moveDuringFlight != Moves.NO_ACTION && other.moveDuringFlight==Moves.NO_ACTION)
                    {
                        return -1;
                    }
                    if (moveDuringFlight == Moves.NO_ACTION && other.moveDuringFlight != Moves.NO_ACTION)
                    {
                        return 1;
                    }

                }
                if (this.Value() < other.Value())
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            else if (Utilities.Contained(diamondsCollected, other.diamondsCollected) && !Utilities.Contained(other.diamondsCollected, diamondsCollected))//diamondsCollected strictly contained in other.diamondsCollected
            {
                return -1;
            }
            else if (!Utilities.Contained(diamondsCollected, other.diamondsCollected) && Utilities.Contained(other.diamondsCollected, diamondsCollected))//other.diamondsCollected strictly contained in diamondsCollected
            {
                return 1;
            }
            else if (!Utilities.Contained(diamondsCollected, other.diamondsCollected) && !Utilities.Contained(other.diamondsCollected, diamondsCollected))//Incomparable
            {
                return 0;
            }
            return 0;
        }

        public bool IsEqual(MoveInformation other)
        {
            if(departurePlatform.id != other.departurePlatform.id)
            {
                return false;
            }
            if(x != other.x)
            {
                return false;
            }
            if(velocityX != other.velocityX)
            {
                return false;
            }
            if(moveType != other.moveType)
            {
                return false;
            }
            return true;
        }

        protected bool EnoughSpaceToAccelerateCircle(int leftEdge, int rigthEdge, int x, int vx)
        {
            if (vx > 0)
            {
                return vx * vx <= 2 * GameInfo.CIRCLE_ACCELERATION * GameInfo.PIXEL_LENGTH * (x - leftEdge - 1);
            }
            else
            {
                return vx * vx <= 2 * GameInfo.CIRCLE_ACCELERATION * GameInfo.PIXEL_LENGTH * (rigthEdge - 1 - x);
            }
        }
    }
}
