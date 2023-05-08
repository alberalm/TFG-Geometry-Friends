using GeometryFriends.AI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    [Serializable]
    public class RollingVPolicy : TabularPolicy<RollingState, Moves>
    {
        public override int NumStates => RollingVTable.NUM_VALUES;

        public static string DefaultFilename = @"Models/RollingVPolicy.csv";

        public RollingVPolicy()
        {
            Reset();
        }

        public override int GetIndex(RollingState state)
        {
            return RollingVTable.GetStateIndex(state);
        }

        public override Moves GetAction(RollingState state)
        {
            var action = base.GetAction(state);
            if (state.targetXVelocity < 0)
            {
                if (action == Moves.ROLL_LEFT)
                    return Moves.ROLL_RIGHT;
                else if (action == Moves.ROLL_RIGHT)
                    return Moves.ROLL_LEFT;
            }
            return action;
        }

        public void Save()
        {
            Save(DefaultFilename);
        }

        static public RollingVPolicy Load()
        {
            return Load(DefaultFilename);
        }

        static public RollingVPolicy Load(string filename)
        {
            var table = new RollingVPolicy();
            var actions = table.LoadActions(filename);
            if (actions != null)
            {
                if (actions.Length != table.NumStates)
                    throw new ApplicationException($"Expected {table.NumStates} actions in {filename} but got {actions.Length}");
                table.actions = actions;
            }

            return table;
        }

        protected override string GetActionString(Moves action)
        {
            return action.ToString();
        }

        protected override Moves ParseActionString(string text)
        {
            Moves move;
            if (Enum.TryParse(text, out move))
                return move;

            throw new ApplicationException($"Invalid move {text}");
        }
    }
}
