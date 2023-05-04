using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    public abstract class TabularPolicy<TState, TAction> : PolicyBase<TState, TAction> 
    {
        protected TAction[] actions;

        public abstract int NumStates { get; }
        public abstract int GetIndex(TState state);
        protected abstract string GetActionString(TAction action);
        protected abstract TAction ParseActionString(string text);

        public override void Reset()
        {
            actions = new TAction[NumStates];
        }

        public override TAction GetAction(TState state)
        {
            return actions[GetIndex(state)];
        }

        public override void Update(TState state, TAction action, double alpha)
        {
            var index = GetIndex(state);
            actions[index] = action;
        }

        public virtual void SetIndexAction(int index, TAction action)
        {
            actions[index] = action;
        }

        public void Save(string filename)
        {
            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine(NumStates);
                for (int i = 0; i < NumStates; i++)
                    writer.WriteLine(actions[i]);
            }
        }

        protected TAction[] LoadActions(string filename)
        {
            if (!File.Exists(filename))
                return null;

            using (var reader = new StreamReader(filename))
            {
                int numValues = int.Parse(reader.ReadLine().Trim());
                var actions = new TAction[numValues];
                for (int i = 0; i < numValues; i++)
                    actions[i] = ParseActionString(reader.ReadLine().Trim());
                return actions;
            }
        }
    }
}
