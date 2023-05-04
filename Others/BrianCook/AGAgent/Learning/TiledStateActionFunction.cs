using System;

namespace GeometryFriendsAgents
{
    public class TiledStateActionFunction<TState, TAction> : StateActionFunction<TState, TAction> 
    {
        public override void Reset()
        {
            throw new NotImplementedException();
        }

        public override double GetValue(TState state, TAction action)
        {
            throw new NotImplementedException();
        }

        public override void Update(TState state, TAction action, double value, double alpha)
        {
            throw new NotImplementedException();
        }
    }
}
