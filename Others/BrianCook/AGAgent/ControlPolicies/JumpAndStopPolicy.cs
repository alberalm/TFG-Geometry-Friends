using System;
using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    public class JumpAndStopPolicy : Policy<GameState>
    {
        public const float stoppedXvel = 10f;

        int groundedStep = -1;
        CircleRepresentation groundedState;
        bool truncateAtGroundedState = false;
        float startingX, startingY;
        bool hasLanded;
        Moves airAction;

        public JumpAndStopPolicy() : this(Moves.NO_ACTION)
        {
        }

        public JumpAndStopPolicy(Moves airAction)
        {
            this.airAction = airAction;
        }

        public override void Initialize(GameState state, float timeStep)
        {
            base.Initialize(state, timeStep);
            startingX = state.Circle.X;
            startingY = state.Circle.Y;
            hasLanded = false;

            if (!truncateAtGroundedState)
                groundedStep = -1;
        }

        public override Moves GetAction(GameState state)
        {
            Steps++;

            bool isGrounded = state.StateMapper.IsGrounded(state.Circle.X, state.Circle.Y, state.Circle.VelocityY);

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
            if (Steps == 1)
                return Moves.JUMP;

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
            if (Steps < 3)
                return false;
            if (base.IsFinished(state))
                return true;

            bool isGrounded = state.StateMapper.IsGrounded(state.Circle.X, state.Circle.Y, state.Circle.VelocityY);
            if (truncateAtGroundedState && isGrounded && Math.Abs(state.Circle.Y - groundedState.Y) < 5)
                return true;

            return Math.Abs(state.Circle.VelocityX) < stoppedXvel && isGrounded;
        }

        public override bool ShouldAvoidCorners(GameState state)
        {
            return Math.Abs(state.Circle.Y - startingY) > SegmentPlanningProblem.cornerThreshold1 || Math.Abs(state.Circle.X - startingX) > SegmentPlanningProblem.cornerThreshold1;
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
                return $"JumpAndStop(LEFT)";
            if (airAction == Moves.ROLL_RIGHT)
                return $"JumpAndStop(RIGHT)";
            return $"JumpAndStop";
        }
    }
}
