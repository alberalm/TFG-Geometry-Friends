using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public bool RightEdgeIsDangerous;

        public MoveInformation(MoveInformation other)
        {
            this.departurePlatform = other.departurePlatform;
            this.landingPlatform = other.landingPlatform;
            this.x = other.x;
            this.xlandPoint = other.xlandPoint;
            this.velocityX = other.velocityX;
            this.moveType = other.moveType;
            this.diamondsCollected = other.diamondsCollected;
            this.path = other.path;
            this.distanceToObstacle = other.distanceToObstacle;
            this.shape = other.shape;
            this.RightEdgeIsDangerous = other.RightEdgeIsDangerous;
        }

        public MoveInformation(Platform landingPlatform)
        {
            this.departurePlatform = null;
            this.landingPlatform = landingPlatform;
            this.x = 0;
            this.xlandPoint = 0;
            this.velocityX = 0;
            this.moveType = MoveType.NOMOVE;
            this.diamondsCollected = new List<int>();
            this.path = null;
            this.distanceToObstacle = 0;
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
            if(velocityX > 0)
            {
                RightEdgeIsDangerous = true;
            }
            else
            {
                RightEdgeIsDangerous = false;
            }
        }

        public int DistanceToRollingEdge()
        {
            if (RightEdgeIsDangerous)
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
            if (RightEdgeIsDangerous)
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
        public int CompareCircle(MoveInformation other, CollectibleRepresentation[] initialCollectiblesInfo)
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
            if (moveType == MoveType.NOMOVE) // In general, we want to store these moves, since they don't really afect other moves
            {
                return 0;
            }
            if (Utilities.Contained(diamondsCollected, other.diamondsCollected) && Utilities.Contained(other.diamondsCollected, diamondsCollected)) //diamondsCollected=other.diamondsCollected
            {
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
                    if(adj.moveType == MoveType.ADJACENT && adj.landingPlatform.id == landingPlatform.id)
                    {
                        return -1;
                    }
                }
            }
            if (landingPlatform.id != other.landingPlatform.id || departurePlatform.id != other.departurePlatform.id)
            {
                return 0;
            }
            if (moveType == MoveType.NOMOVE && other.moveType == MoveType.NOMOVE && diamondsCollected[0] == other.diamondsCollected[0])
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
                    if(landingPlatform.yTop == departurePlatform.yTop)
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
    }
}
