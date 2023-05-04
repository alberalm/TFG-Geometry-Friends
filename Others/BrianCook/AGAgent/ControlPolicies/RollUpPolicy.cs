using System;
using GeometryFriends.AI;

namespace GeometryFriendsAgents
{
    public class RollUpPolicy : Policy<GameState>
    {
        public float x, y, xVelocity;

        public RollUpPolicy(float x, float y, float xVelocity)
        {
            this.x = x;
            this.y = y;
            this.xVelocity = xVelocity;
        }

        public override void Initialize(GameState state, float timeStep)
        {
            base.Initialize(state, timeStep);
        }

        public override Moves GetAction(GameState state)
        {
            Steps++;

            var circle = state.Circle;
            if (circle.VelocityX < xVelocity)
                return Moves.ROLL_RIGHT;
            else if (circle.VelocityX > xVelocity)
                return Moves.ROLL_LEFT;
            else
                return Moves.NO_ACTION;
        }

        public override bool IsFinished(GameState state)
        {
            if (base.IsFinished(state))
            {
                Logger.Write($"TIMEOUT: {this}");
                return true;
            }

            var circle = state.Circle;
            bool isGrounded = state.StateMapper.IsGrounded(circle.X, circle.Y, circle.VelocityY);
            if (isGrounded && circle.Y > y)
            {
                if (xVelocity > 0 && circle.X > x)
                    return true;
                if (xVelocity < 0 && circle.X < x)
                    return true;
            }

            return false;
        }

        public override bool ShouldAvoidCorners(GameState state)
        {
            return false;
        }

        public override string ToString()
        {
            return $"RollUp(corner={x},{y}@{xVelocity},steps={Steps})";
        }
    }
}
