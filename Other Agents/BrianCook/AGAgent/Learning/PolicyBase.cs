using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    public abstract class PolicyBase<TState, TAction>
    {
        public abstract void Reset();

        public abstract TAction GetAction(TState state);

        public abstract void Update(TState state, TAction action, double alpha);
    }
}
