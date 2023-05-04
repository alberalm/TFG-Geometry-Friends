using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public class TransitionResult<TState>
    {
        public TState State;
        public float Likelihood;

        public TransitionResult()
        {
        }

        public TransitionResult(TState state, float likelihood)
        {
            State = state;
            Likelihood = likelihood;
        }

        public override string ToString()
        {
            return string.Format("[L={0}:{1}]", Likelihood, State);
        }
    }
}
