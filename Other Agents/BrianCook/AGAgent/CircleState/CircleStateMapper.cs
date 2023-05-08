using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GeometryFriendsAgents
{
    public class CircleStateMapper
    {
        public GameArea gameArea;
        public ObstacleGrid obstacleGrid;
        public Point debugCollisionPoint;
        public bool EnableDebugging = false;
        public float circleRadius = GamePhysics.CIRCLE_RADIUS;
        float groundEpsilon = GamePhysics.GROUND_EPSILON;   
        double groundMaxVelocityY = GamePhysics.GROUND_MAX_Y_VELOCITY;  

        public CircleStateMapper(GameArea gameArea, ObstacleGrid grid)
        {
            this.gameArea = gameArea;
            obstacleGrid = grid;
        }

        public CircleState CreateState(CircleRepresentation circle)
        {
            return CreateState(circle.X, circle.Y, circle.VelocityX, circle.VelocityY);
        }

        public CircleState CreateState(float x, float y, float xVelocity, float yVelocity)
        {
            float collisionDistance;
            CircleContactPoint collisionPoint;
            obstacleGrid.GetDistanceToCollision(new PointF(x, y), new PointF(xVelocity, yVelocity), circleRadius, out collisionDistance, out collisionPoint);

            bool isGrounded = IsGrounded(x, y, yVelocity);
            var state = new CircleState(x, y, xVelocity, yVelocity, collisionDistance, collisionPoint, isGrounded);
            return state;
        }

        public void UpdateCollisionInfo(ref CircleState state)
        {
            var location = GetLocation(ref state);
            var velocity = GetVelocity(ref state);

            obstacleGrid.GetDistanceToCollision(location, velocity, circleRadius, out state.collisionDistance, out state.collisionPoint);
            state.isGrounded = IsGrounded(location.X, location.Y, velocity.Y);
        }

        public bool CheckOnCorner(float x, float y, float yVelocity)
        {
            if (Math.Abs(yVelocity) >= groundMaxVelocityY)
                return false;

            float groundDistance;
            CircleContactPoint groundPoint;
            obstacleGrid.GetDistanceToCollision(new PointF(x, y), new PointF(0, 1), circleRadius, out groundDistance, out groundPoint);
            return (groundPoint == CircleContactPoint.Other && groundDistance <= groundEpsilon && Math.Abs(yVelocity) < groundMaxVelocityY);
        }

        public float GetDistanceToGround(float x, float y, float circleRadius)
        {
            return obstacleGrid.GetDistanceToGround(x, y, circleRadius);
        }

        public bool IsGrounded(float x, float y, float yVelocity)
        {
            if (Math.Abs(yVelocity) >= groundMaxVelocityY)
                return false;

            float groundDistance = obstacleGrid.GetDistanceToGround(x, y, circleRadius);
            return groundDistance <= groundEpsilon;
        }
        public void GetCollectibles(PointF location, List<int> collectibles)
        {
            collectibles.Clear();
            var radius = gameArea.GoalRadius + gameArea.CircleRadius;
            for (int i = 0; i < gameArea.Collectibles.Count; i++)
            {
                var collectible = gameArea.Collectibles[i];
                var distance = Helpers.Distance(location, new PointF(collectible.X, collectible.Y));
                if (distance < radius)
                    collectibles.Add(i);
            }
        }

        public PointF GetCollectible(int index)
        {
            var collectible = gameArea.Collectibles[index];
            return new PointF(collectible.X, collectible.Y);
        }

        public PointF GetLocation(ref CircleState state)
        {
            return new PointF(state.x, state.y);
        }

        public PointF GetVelocity(ref CircleState state)
        {
            return new PointF(state.xVelocity, state.yVelocity);
        }

        public string GetString(ref CircleState state)
        {
            var location = GetLocation(ref state);
            var velocity = GetVelocity(ref state);
            return string.Format("(pos:{0} vel:{1} collide:{2}/{3} ground:{4})",
                location, velocity, state.collisionPoint, state.collisionDistance, state.isGrounded);
        }

        public static CircleStateMapper Create(GameArea gameArea, ObstacleGrid obstacleGrid)
        {
            return new CircleStateMapper(gameArea, obstacleGrid);
        }
    }
}
