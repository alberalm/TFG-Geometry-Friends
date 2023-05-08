using System;

namespace GeometryFriendsAgents
{
    [Serializable]
    public struct CircleState
    {
        public float x;
        public float y;
        public float xVelocity;
        public float yVelocity;
        public float collisionDistance;
        public CircleContactPoint collisionPoint;
        public bool isGrounded;

        public CircleState(float x, float y, float xVel, float yVel, float collisionDistance,
            CircleContactPoint collisionPoint, bool isGrounded)
        {
            this.x = x;
            this.y = y;
            xVelocity = xVel;
            yVelocity = yVel;
            this.collisionDistance = collisionDistance;
            this.collisionPoint = collisionPoint;
            this.isGrounded = isGrounded;
        }

        public CircleState Copy()
        {
            return new CircleState(x, y, xVelocity, yVelocity, collisionDistance, collisionPoint, isGrounded);
        }

        public override string ToString()
        {
            return string.Format("(x:{0} y:{1} xVel:{2} yVel:{3} collide:{4}/{5} grounded:{6})", x, y, xVelocity, yVelocity, collisionPoint, collisionDistance, isGrounded);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (int)x;
                result = (result * 397) ^ (int)y;
                result = (result * 397) ^ (int)xVelocity;
                result = (result * 397) ^ (int)yVelocity;
                result = (result * 397) ^ (int)collisionDistance;
                result = (result * 13) ^ (int)collisionPoint;
                return result;
            }
        }

        public override bool Equals(Object obj)
        {
            return obj is CircleState && this == (CircleState)obj;
        }

        public static bool operator ==(CircleState a, CircleState b)
        {
            return a.x == b.x
                && a.y == b.y
                && a.xVelocity == b.xVelocity
                && a.yVelocity == b.yVelocity
                && a.collisionDistance == b.collisionDistance
                && a.collisionPoint == b.collisionPoint
                && a.isGrounded == b.isGrounded;
        }
        public static bool operator !=(CircleState a, CircleState b)
        {
            return !(a == b);
        }

        public bool IsNull { get { return x == 0 && y == 0 && xVelocity == 0 && yVelocity == 0; } }
    }
}
