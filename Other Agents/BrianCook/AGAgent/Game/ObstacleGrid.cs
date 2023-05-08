using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace GeometryFriendsAgents
{
    public class ObstacleGrid
    {
        public int Width, Height;
        public List<RectangleF> obstacles;
        public Point[] Corners;

        public ObstacleGrid(int width, int height, int border, ICollection<ObstacleRepresentation> nativeObstacles)
        {
            Width = width;
            Height = height;
            obstacles = new List<RectangleF>();

            var corners = new List<Point>();
            foreach (var obstacle in nativeObstacles)
            {
                int halfWidth = (int)(obstacle.Width / 2);
                int halfHeight = (int)(obstacle.Height / 2);
                int minX = (int)obstacle.X - halfWidth;
                int maxX = (int)obstacle.X + halfWidth;
                int minY = (int)obstacle.Y - halfHeight;
                int maxY = (int)obstacle.Y + halfHeight;

                corners.Add(new Point(minX, minY));
                corners.Add(new Point(minX, maxY - 1));
                corners.Add(new Point(maxX - 1, minY));
                corners.Add(new Point(maxX - 1, maxY - 1));
                obstacles.Add(new RectangleF(obstacle.X - obstacle.Width / 2, obstacle.Y - obstacle.Height / 2, obstacle.Width, obstacle.Height));
            }
            bool changed = false;
            do
            {
                changed = false;
                foreach (var corner in corners)
                {
                    var matches = corners.Where(c => Helpers.Distance(corner, c) <= 5f);
                    if (matches.Count() > 1)
                    {
                        corners = corners.Where(c => Helpers.Distance(corner, c) > 5f).ToList();
                        changed = true;
                        break;
                    }
                }
            } while (changed);

            Corners = corners.ToArray();
            obstacles.Add(new RectangleF(0, 0, border, height));
            obstacles.Add(new RectangleF(width - border, 0, border, height));
            obstacles.Add(new RectangleF(0, 0, width, border));
            obstacles.Add(new RectangleF(0, height - border, width, border));
        }

        public void GetDistanceToNearestObstacle(PointF origin, float radius, out float minDistance)
        {
            minDistance = float.MaxValue;

            foreach (var obstacle in obstacles)
            {
                var leftDelta = obstacle.Left - origin.X;
                var rightDelta = obstacle.Right - origin.X;
                var topDelta = obstacle.Top - origin.Y;
                var bottomDelta = obstacle.Bottom - origin.Y;
                float distance;
                if (rightDelta < 0)
                {
                    if (bottomDelta < 0)
                        distance = (float)Math.Sqrt(rightDelta * rightDelta + bottomDelta * bottomDelta);
                    else if (topDelta > 0)
                        distance = (float)Math.Sqrt(rightDelta * rightDelta + topDelta * topDelta);
                    else
                        distance = Math.Abs(rightDelta);
                }
                else if (leftDelta > 0)
                {
                    if (bottomDelta < 0)
                        distance = (float)Math.Sqrt(leftDelta * leftDelta + bottomDelta * bottomDelta);
                    else if (topDelta > 0)
                        distance = (float)Math.Sqrt(leftDelta * leftDelta + topDelta * topDelta);
                    else
                        distance = Math.Abs(leftDelta);
                }
                else
                {
                    if (bottomDelta < 0)
                        distance = Math.Abs(bottomDelta);
                    else if (topDelta > 0)
                        distance = Math.Abs(topDelta);
                    else
                        distance = 0;
                }

                minDistance = Math.Min(minDistance, distance);
            }

            minDistance -= radius;
        }

        public float GetNearestCornerDistanceSquared(Point point)
        {
            var nearest = int.MaxValue;
            foreach (var corner in Corners)
            {
                int deltaX = point.X - corner.X;
                int deltaY = point.Y - corner.Y;
                nearest = Math.Min(nearest, deltaX * deltaX + deltaY * deltaY);
            }
            return nearest;
        }

        public bool IsCollision(PointF origin, float radius)
        {
            if (origin.X < 0 || origin.X > Width || origin.Y < 0 || origin.Y > Height)
                return true;

            foreach (var obstacle in obstacles)
            {
                var leftDelta = obstacle.Left - origin.X;
                if (leftDelta > radius)
                    continue;

                var rightDelta = obstacle.Right - origin.X;
                if (rightDelta < -radius)
                    continue;

                var topDelta = obstacle.Top - origin.Y;
                if (topDelta > radius)
                    continue;

                var bottomDelta = obstacle.Bottom - origin.Y;
                if (bottomDelta < -radius)
                    continue;
                return true;
            }
            return false;
        }
        public bool GetAdjustedLocation(PointF origin, float radius, float maxCollisionDepth, out PointF adjusted)
        {
            adjusted = origin;
            foreach (var obstacle in obstacles)
            {
                var leftDelta = obstacle.Left - origin.X;
                var rightDelta = obstacle.Right - origin.X;
                var topDelta = obstacle.Top - origin.Y;
                var bottomDelta = obstacle.Bottom - origin.Y;

                PointF collision;
                if (rightDelta < 0)
                {
                    if (bottomDelta < 0)
                        collision = new PointF(obstacle.Right, obstacle.Bottom);
                    else if (topDelta > 0)
                        collision = new PointF(obstacle.Right, obstacle.Top);
                    else
                        collision = new PointF(obstacle.Right, origin.Y);
                }
                else if (leftDelta > 0)
                {
                    if (bottomDelta < 0)
                        collision = new PointF(obstacle.Left, obstacle.Bottom);
                    else if (topDelta > 0)
                        collision = new PointF(obstacle.Left, obstacle.Top);
                    else
                        collision = new PointF(obstacle.Left, origin.Y);
                }
                else
                {
                    if (bottomDelta < 0)
                        collision = new PointF(origin.X, obstacle.Bottom);
                    else if (topDelta > 0)
                        collision = new PointF(origin.X, obstacle.Top);
                    else
                        collision = new PointF(origin.X, origin.Y);
                }

                var delta = Helpers.Subtract(origin, collision);
                var distance = (float)Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
                if (distance < radius - maxCollisionDepth)
                    return false;
                if (distance < radius)
                    adjusted = Helpers.Add(collision, Helpers.Times(Helpers.Divide(delta, distance), radius));
            }
            return true;
        }

        public void GetDistanceToCollision(PointF origin, PointF direction, float circleRadius, out float minDistance, out CircleContactPoint collisionPoint)
        {
            bool debugXX = direction.X != 0;

            minDistance = float.MaxValue;
            collisionPoint = CircleContactPoint.None;
            if (direction == PointF.Empty)
                return;
            var point1 = new Vector3f(origin.X, origin.Y, 1);
            var point2 = new Vector3f(origin.X + direction.X, origin.Y + direction.Y, 1);
            var lineOfTravel = Vector3f.CrossProduct(point1, point2);
            var a = lineOfTravel.x;
            var b = lineOfTravel.y;
            var c = lineOfTravel.z;
            var divisorSquared = (float)(a * a + b * b);
            var divisor = (float)Math.Sqrt(divisorSquared);
            Func<float, float, float> getDistanceFromCornerToLineOfTravel = (x, y) =>
            {
                return Math.Abs(a * x + b * y + c) / divisor;
            };
            Func<float, float, float> getSquaredDistanceToCornerCollision = (x, y) =>
            {
                var d = getDistanceFromCornerToLineOfTravel(x, y);
                if (d < circleRadius)
                {
                    var collisionX = (b * (b * x - a * y) - a * c) / divisorSquared;
                    var collisionY = (a * (-b * x + a * y) - b * c) / divisorSquared;
                    var distance = (float)Helpers.Distance(origin, new PointF(collisionX, collisionY));
                    distance -= (float)Math.Sqrt(circleRadius * circleRadius - d * d);
                    return distance * distance;
                }
                else
                    return float.MaxValue;
            };

            foreach (var obstacle in obstacles)
            {
                if (direction.X > 0 && origin.X <= obstacle.Left)
                {
                    var deltaX = obstacle.Left - origin.X - circleRadius;
                    var deltaY = direction.Y / direction.X * deltaX;
                    if (origin.Y + deltaY >= obstacle.Top && origin.Y + deltaY <= obstacle.Bottom)
                    {
                        var distance = deltaX * deltaX + deltaY * deltaY;
                        if (deltaX <= 0)
                            distance = 0;
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            collisionPoint = CircleContactPoint.Right;
                        }
                    }
                    else
                    {
                        var distance = getSquaredDistanceToCornerCollision(obstacle.Left, obstacle.Top);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            collisionPoint = CircleContactPoint.Other;
                        }
                        distance = getSquaredDistanceToCornerCollision(obstacle.Left, obstacle.Bottom);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            collisionPoint = CircleContactPoint.Other;
                        }
                    }
                }
                else if (direction.X < 0 && origin.X >= obstacle.Right)
                {
                    var deltaX = obstacle.Right - origin.X + circleRadius;
                    var deltaY = direction.Y / direction.X * deltaX;
                    if (origin.Y + deltaY >= obstacle.Top && origin.Y + deltaY <= obstacle.Bottom)
                    {
                        var distance = deltaX * deltaX + deltaY * deltaY;
                        if (deltaX >= 0)
                            distance = 0;
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            collisionPoint = CircleContactPoint.Left;
                        }
                    }
                    else
                    {
                        var distance = getSquaredDistanceToCornerCollision(obstacle.Right, obstacle.Top);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            collisionPoint = CircleContactPoint.Other;
                        }
                        distance = getSquaredDistanceToCornerCollision(obstacle.Right, obstacle.Bottom);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            collisionPoint = CircleContactPoint.Other;
                        }
                    }
                }
                if (direction.Y > 0 && origin.Y <= obstacle.Top)
                {
                    var deltaY = obstacle.Top - origin.Y - circleRadius;
                    var deltaX = direction.X / direction.Y * deltaY;
                    if (origin.X + deltaX >= obstacle.Left && origin.X + deltaX <= obstacle.Right)
                    {
                        var distance = deltaX * deltaX + deltaY * deltaY;
                        if (deltaY <= 0)
                            distance = 0;
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            collisionPoint = CircleContactPoint.Bottom;
                        }
                    }
                    else
                    {
                        var distance = getSquaredDistanceToCornerCollision(obstacle.Left, obstacle.Top);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            collisionPoint = CircleContactPoint.Other;
                        }
                        distance = getSquaredDistanceToCornerCollision(obstacle.Right, obstacle.Top);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            collisionPoint = CircleContactPoint.Other;
                        }
                    }
                }
                else if (direction.Y < 0 && origin.Y >= obstacle.Bottom)
                {
                    var deltaY = obstacle.Bottom - origin.Y + circleRadius;
                    var deltaX = direction.X / direction.Y * deltaY;
                    if (origin.X + deltaX >= obstacle.Left && origin.X + deltaX <= obstacle.Right)
                    {
                        var distance = deltaX * deltaX + deltaY * deltaY;
                        if (deltaY >= 0)
                            distance = 0;
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            collisionPoint = CircleContactPoint.Top;
                        }
                    }
                    else
                    {
                        var distance = getSquaredDistanceToCornerCollision(obstacle.Left, obstacle.Bottom);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            collisionPoint = CircleContactPoint.Other;
                        }
                        distance = getSquaredDistanceToCornerCollision(obstacle.Right, obstacle.Bottom);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            collisionPoint = CircleContactPoint.Other;
                        }
                    }
                }
            }
            Debug.Assert(collisionPoint != CircleContactPoint.None);
            if (collisionPoint != CircleContactPoint.None)
                minDistance = (float)Math.Sqrt(minDistance);
        }

        public float GetDistanceToGround(float x, float y, float circleRadius)
        {
            float minDistance = float.MaxValue;

            foreach (var obstacle in obstacles)
                if (y <= obstacle.Top && x >= obstacle.Left && x <= obstacle.Right)
                {
                    var distance = obstacle.Top - y - circleRadius;
                    minDistance = Math.Min(minDistance, distance);
                }
            if (minDistance > 1000)
                throw new ApplicationException($"No ground at {x},{y}");

            return minDistance;
        }
    }
}
