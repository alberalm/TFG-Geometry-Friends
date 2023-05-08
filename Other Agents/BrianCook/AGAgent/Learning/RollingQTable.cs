using GeometryFriends.AI;
using System;

namespace GeometryFriendsAgents
{
    [Serializable]
    public class RollingQTable : TabularStateActionFunction<RollingState, CircleOperator>
    {
        const int NUM_ACTIONS = 3;

        const int MAX_X = 200;
        const int MAX_X_VELOCITY = 200;

        const int X_RESOLUTION = 4;
        const int X_VELOCITY_RESOLUTION = 10;
        const int NUM_X = MAX_X * 2 / X_RESOLUTION;
        const int NUM_X_VELOCITY = MAX_X_VELOCITY * 2 / X_VELOCITY_RESOLUTION;
        const int NUM_TARGET_VELOCITY = MAX_X_VELOCITY / X_VELOCITY_RESOLUTION;

        const int NUM_VALUES = NUM_X * NUM_X_VELOCITY * NUM_TARGET_VELOCITY * NUM_ACTIONS;

        public override int NumValues => NUM_VALUES;

        public RollingQTable()
        {
            Reset();
        }

        public override int GetIndex(RollingState state, CircleOperator action)
        {
            int deltaX = (int)(state.X - state.targetX + 0.5f);
            int xVel = (int)(state.XVelocity + 0.5f);
            int xTargetVel = (int)(state.targetXVelocity + 0.5f);
            var move = action.Move;
            if (xTargetVel < 0)
            {
                xTargetVel = -xTargetVel;
                deltaX = -deltaX;
                xVel = -xVel;
                if (move == Moves.ROLL_LEFT)
                    move = Moves.ROLL_RIGHT;
                else if (move == Moves.ROLL_RIGHT)
                    move = Moves.ROLL_LEFT;
            }

            var xIndex = (deltaX + MAX_X) / X_RESOLUTION;
            if (xIndex < 0)
                xIndex = 0;
            else if (xIndex >= NUM_X)
                xIndex = NUM_X - 1;

            var xVelIndex = (xVel + MAX_X_VELOCITY) / X_VELOCITY_RESOLUTION;
            if (xVelIndex < 0)
                xVelIndex = 0;
            else if (xVelIndex >= NUM_X_VELOCITY)
                xVelIndex = NUM_X_VELOCITY - 1;

            var xTargetVelIndex = xTargetVel / X_VELOCITY_RESOLUTION;
            if (xTargetVelIndex < 0)
                xTargetVelIndex = 0;
            else if (xTargetVelIndex >= NUM_TARGET_VELOCITY)
                xTargetVelIndex = NUM_TARGET_VELOCITY - 1;
            var actionIndex = (int)move;

            return xIndex * NUM_X_VELOCITY * NUM_TARGET_VELOCITY * NUM_ACTIONS +
                xVelIndex * NUM_TARGET_VELOCITY * NUM_ACTIONS +
                xTargetVelIndex * NUM_ACTIONS +
                actionIndex;
        }

        static public RollingQTable FromFile(string filename)
        {
            var table = new RollingQTable();
            var values = LoadValues(filename);
            if (values != null)
            {
                if (values.Length != table.NumValues)
                    throw new ApplicationException($"Expected {table.NumValues} values in {filename} but got {values.Length}");
                table.values = values;
            }
            return table;
        }
    }
}
