using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    public abstract class StateValueFunction<TState>
    {
        public abstract void Reset();

        public abstract double GetValue(TState state);

        public abstract void Update(TState state, double value, double alpha);
    }
}
