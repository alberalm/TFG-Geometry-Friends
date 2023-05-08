using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    public abstract class TabularStateValueFunction<TState> : StateValueFunction<TState> 
    {
        protected double[] values;

        public abstract int NumValues { get; }
        public abstract int GetIndex(TState state);

        public override void Reset()
        {
            values = new double[NumValues];
        }

        public override double GetValue(TState state)
        {
            return values[GetIndex(state)];
        }

        public virtual void SetIndexValue(int index, double value)
        {
            values[index] = value;
        }

        public virtual double GetIndexValue(int index)
        {
            return values[index];
        }

        public override void Update(TState state, double value, double alpha)
        {
            var index = GetIndex(state);
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
