using System;
using GeometryFriends.AI;

namespace GeometryFriendsAgents
{
    public class RollToPolicy : Policy<GameState>
    {
        public float targetX, targetXvelocity, y;
        public const float XTolerance = 10;
        public const float XVelocityTolerance = 10;
        public static bool IsDebug = false;

        static RollingVPolicy _vPolicy;
        public static RollingVPolicy VPolicy { get { if (_vPolicy == null) _vPolicy = RollingVPolicy.Load(); return _vPolicy; } }
        static RollingVTable _vTable;
        public static RollingVTable VTable { get { if (_vTable == null) _vTable = RollingVTable.Load(); return _vTable; } }
        public bool IsRollOff { get; set; }

        public RollToPolicy(float targetX, float targetXvelocity, float y)
        {
            this.targetX = targetX;
            this.targetXvelocity = targetXvelocity;
            this.y = y;
        }

        public override void Initialize(GameState state, float timeStep)
        {
            base.Initialize(state, timeStep);
        }

        public override Moves GetAction(GameState state)
        {
            Steps++;

            var circle = state.Circle;
            var x = circle.X;
            var deltaX = targetX - x;
            if (deltaX > 200)
                return Moves.ROLL_RIGHT;
            else if (deltaX < -200)
                return Moves.ROLL_LEFT;

            var rollingState = new RollingState()
            {
                X = x,
                XVelocity = circle.VelocityX,
                targetX = targetX,
                targetXVelocity = targetXvelocity
            };

            return VPolicy.GetAction(rollingState);
        }

        public override bool IsFinished(GameState state)
        {
            if (base.IsFinished(state))
            {
                Logger.Write($"TIMEOUT: {this}");
                return true;
            }

            var circle = state.Circle;
            if (circle.Y > y + GamePhysics.CIRCLE_RADIUS)
                return true;

            var finished = Math.Abs(targetX - circle.X) < XTolerance && Math.Abs(targetXvelocity - circle.VelocityX) < XVelocityTolerance;
            return finished;
        }

        public override bool ShouldAvoidCorners(GameState state)
        {
            return Math.Abs(state.Circle.X - targetX) > GamePhysics.CIRCLE_RADIUS;
        }

        public override string ToString()
        {
            if (IsRollOff)
                return $"RollOff({targetX},{targetXvelocity},steps={Steps})";
            else
                return $"RollTo({targetX},{targetXvelocity},steps={Steps})";
        }
    }
}
