using System;

namespace GeometryFriendsAgents
{
    [Serializable]
    public class RollingState
    {
        public float X;
        public float targetX;
        public float XVelocity;
        public float targetXVelocity;

        public override string ToString()
        {
            return string.Format("(x:{0} target:{1} delta:{2} xVel:{3} targetVel:{4})", X, targetX, targetX - X, XVelocity, targetXVelocity);
        }
    }
}
