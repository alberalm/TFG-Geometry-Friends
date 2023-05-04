using System.IO;

namespace GeometryFriendsAgents
{
    public abstract class TabularStateActionFunction<TState, TAction> : StateActionFunction<TState, TAction> 
    {
        protected double[] values;

        public abstract int NumValues { get; }
        public abstract int GetIndex(TState state, TAction action);

        public override void Reset()
        {
            values = new double[NumValues];
        }

        public override double GetValue(TState state, TAction action)
        {
            return values[GetIndex(state, action)];
        }

        public override void Update(TState state, TAction action, double value, double alpha)
        {
            var index = GetIndex(state, action);
            var prior = values[index];
            values[index] = prior + alpha * (value - prior);
        }

        public void Save(string filename)
        {
            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine(NumValues);
                for (int i = 0; i < NumValues; i++)
                    writer.WriteLine(values[i]);
            }
        }

        static protected double[] LoadValues(string filename)
        {
            if (!File.Exists(filename))
                return null;

            using (var reader = new StreamReader(filename))
            {
                int numValues = int.Parse(reader.ReadLine().Trim());
                var values = new double[numValues];
                for (int i = 0; i < numValues; i++)
                    values[i] = double.Parse(reader.ReadLine().Trim());
                return values;
            }
        }
    }
}
