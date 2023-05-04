using System;
using GeometryFriends.AI;

namespace GeometryFriendsAgents
{
    public class StopPolicy : Policy<GameState>
    {
        public const float stoppedXvel = 8f;

        public override void Initialize(GameState state, float timeStep)
        {
            base.Initialize(state, timeStep);
        }

        public override Moves GetAction(GameState state)
        {
            Steps++;

            var isGrounded = state.StateMapper.IsGrounded(state.Circle.X, state.Circle.Y, state.Circle.VelocityY);
            if (!isGrounded)
                return Moves.NO_ACTION;

            if (state.Circle.VelocityX > 0)
                return Moves.ROLL_LEFT;
            else if (state.Circle.VelocityX < 0)
                return Moves.ROLL_RIGHT;
            else
                return Moves.NO_ACTION;
        }

        public override bool IsFinished(GameState state)
        {
            return Math.Abs(state.Circle.VelocityX) < stoppedXvel && state.StateMapper.IsGrounded(state.Circle.X, state.Circle.Y, state.Circle.VelocityY);
        }

        public override string ToString()
        {
            return $"Stop";
        }
    }
}
