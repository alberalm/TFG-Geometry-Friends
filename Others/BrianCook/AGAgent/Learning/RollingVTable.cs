using GeometryFriends.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    [Serializable]
    public class RollingVTable : TabularStateValueFunction<RollingState>
    {
        public const int MAX_X = 200;
        public const int MAX_X_VELOCITY = 200;

        public const int X_RESOLUTION = 4;
        public const int X_VELOCITY_RESOLUTION = 10;
        public const int NUM_X = MAX_X * 2 / X_RESOLUTION;
        public const int NUM_X_VELOCITY = MAX_X_VELOCITY * 2 / X_VELOCITY_RESOLUTION;
        public const int NUM_TARGET_VELOCITY = MAX_X_VELOCITY / X_VELOCITY_RESOLUTION;

        public const int NUM_VALUES = NUM_X * NUM_X_VELOCITY * NUM_TARGET_VELOCITY;

        public override int NumValues => NUM_VALUES;

        public static string DefaultFilename = @"Models/RollingVTable.csv";

        public RollingVTable()
        {
            Reset();
        }

        static public int GetStateIndex(RollingState state)
        {
            int deltaX = (int)Math.Round(state.X - state.targetX);
            int xVel = (int)Math.Round(state.XVelocity);
            int xTargetVel = (int)Math.Round(state.targetXVelocity);
            if (xTargetVel < 0)
            {
                xTargetVel = -xTargetVel;
                deltaX = -deltaX;
                xVel = -xVel;
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

            return GetStateIndex(xIndex, xVelIndex, xTargetVelIndex);
        }

        static public int GetStateIndex(int xIndex, int xVelIndex, int xTargetVelIndex)
        {
            return xIndex * NUM_X_VELOCITY * NUM_TARGET_VELOCITY +
                xVelIndex * NUM_TARGET_VELOCITY +
                xTargetVelIndex;
        }

        public static void GetXRange(int xIndex, out int fromX, out int toX)
        {
            fromX = xIndex * X_RESOLUTION - MAX_X;
            toX = fromX + X_RESOLUTION;
        }

        public static void GetXVelocityRange(int xVelIndex, out int fromXVel, out int toXVel)
        {
            fromXVel = xVelIndex * X_VELOCITY_RESOLUTION - MAX_X_VELOCITY;
            toXVel = fromXVel + X_VELOCITY_RESOLUTION;
        }

        public static void GetTargetXVelocityRange(int xTargetVelIndex, out int fromTargetXVel, out int toTargetXVel)
        {
            fromTargetXVel = xTargetVelIndex * X_VELOCITY_RESOLUTION;
            toTargetXVel = fromTargetXVel + X_VELOCITY_RESOLUTION;
        }

        public static void GetStateValues(int state, out int x, out int xVelocity, out int targetXVelocity)
        {
            var xTargetVelIndex = state % NUM_TARGET_VELOCITY;
            state /= NUM_TARGET_VELOCITY;
            var xVelIndex = state % NUM_X_VELOCITY;
            state /= NUM_X_VELOCITY;
            var xIndex = state;

            x = xIndex * X_RESOLUTION - MAX_X + X_RESOLUTION / 2;
            xVelocity = xVelIndex * X_VELOCITY_RESOLUTION - MAX_X_VELOCITY + X_VELOCITY_RESOLUTION / 2;
            targetXVelocity = xTargetVelIndex * X_VELOCITY_RESOLUTION + X_VELOCITY_RESOLUTION / 2;
        }

        public override int GetIndex(RollingState state)
        {
            return GetStateIndex(state);
        }

        public void Save()
        {
            Save(DefaultFilename);
        }

        static public RollingVTable Load()
        {
            return Load(DefaultFilename);
        }

        static public RollingVTable Load(string filename)
        {
            var table = new RollingVTable();
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
