using System;
using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    public class FallAndStopPolicy : Policy<GameState>
    {
        public const float stoppedXvel = 8f;
        public const float maxGroundedYvel = 10f;
        public bool hasBeenUngrounded = false;
        bool hasLanded;

        int groundedStep = -1;
        CircleRepresentation groundedState;
        bool truncateAtGroundedState = false;
        float startingY;
        Moves airAction;

        public FallAndStopPolicy() : this(Moves.NO_ACTION)
        {
        }

        public FallAndStopPolicy(Moves airAction)
        {
            this.airAction = airAction;
        }

        public override void Initialize(GameState state, float timeStep)
        {
            base.Initialize(state, timeStep);
            hasBeenUngrounded = false;
            startingY = state.Circle.Y;
            hasLanded = false;

            if (!truncateAtGroundedState)
                groundedStep = -1;
        }

        public override Moves GetAction(GameState state)
        {
            Steps++;

            var isGrounded = state.StateMapper.IsGrounded(state.Circle.X, state.Circle.Y, state.Circle.VelocityY);

            if (!truncateAtGroundedState)
            {
                if (!isGrounded)
                    groundedStep = -1;
                else if (groundedStep < 0)
                {
                    groundedStep = Steps;
                    groundedState = state.Circle;
                }
            }
            if (!hasBeenUngrounded)
            {
                if (isGrounded)
                    return Moves.NO_ACTION;
                hasBeenUngrounded = true;
            }

            if (isGrounded && Steps > 3)
                hasLanded = true;
            if (!isGrounded && !hasLanded)
                return airAction;
            if (state.Circle.VelocityX > 0)
                return Moves.ROLL_LEFT;
            else if (state.Circle.VelocityX < 0)
                return Moves.ROLL_RIGHT;
            else
                return Moves.NO_ACTION;
        }

        public override bool IsFinished(GameState state)
        {
            if (base.IsFinished(state))
                return true;

            var isGrounded = state.StateMapper.IsGrounded(state.Circle.X, state.Circle.Y, state.Circle.VelocityY);
            if (truncateAtGroundedState && isGrounded && Math.Abs(state.Circle.Y - groundedState.Y) < 5)
                return true;

            return hasBeenUngrounded && Math.Abs(state.Circle.VelocityX) < stoppedXvel && isGrounded;
        }

        public override bool ShouldAvoidCorners(GameState state)
        {
            return Math.Abs(state.Circle.Y - startingY) > GamePhysics.CIRCLE_RADIUS;
        }

        public bool GetGroundedPoint(out CircleRepresentation state, out int timeSteps)
        {
            if (groundedStep < 0)
            {
                state = new CircleRepresentation();
                timeSteps = -1;
                return false;
            }

            state = groundedState;
            timeSteps = groundedStep;
            return true;
        }

        public void TruncateWhenGrounded()
        {
            truncateAtGroundedState = true;
        }

        public override string ToString()
        {
            if (airAction == Moves.ROLL_LEFT)
                return $"FallAndStop(LEFT)";
            if (airAction == Moves.ROLL_RIGHT)
                return $"FallAndStop(RIGHT)";
            return $"FallAndStop";
        }
    }
}
