using GeometryFriends.AI;
using System;
using System.Linq;

namespace GeometryFriendsAgents
{
    public class CompositePolicy : Policy<GameState>
    {
        int index = 0;
        Policy<GameState>[] policies;

        public override void Initialize(GameState state, float timeStep)
        {
            base.Initialize(state, timeStep);
            index = 0;
            policies[index].Initialize(state, timeStep);
        }

        public CompositePolicy(params Policy<GameState>[] policies)
        {
            this.policies = policies;
        }

        public TChild GetChildPolicy<TChild>() where TChild : Policy<GameState>
        {
            foreach (var child in policies)
                if (child is TChild)
                    return child as TChild;
            return null;
        }

        public override Moves GetAction(GameState state)
        {
            Steps++;
            if (index >= policies.Length)
                return Moves.NO_ACTION;
            var policy = policies[index];
            if (policy.IsFinished(state))
            {
                index++;
                if (index >= policies.Length)
                    return Moves.NO_ACTION;
                policy = policies[index];
                policy.Initialize(state, TimeStep);
            }
            return policy.GetAction(state);
        }

        public override bool IsFinished(GameState state)
        {
            return index >= policies.Length;
        }

        public override bool ShouldAvoidCorners(GameState state)
        {
            if (index >= policies.Length)
                return false;
            return policies[index].ShouldAvoidCorners(state);
        }

        public override int Failures => Math.Max(base.Failures, policies.Max(p => p.Failures));

        public override bool Failed => policies.Where(p => p.Failed).Count() > 0;

        public override void OnFailed()
        {
            base.OnFailed();
            foreach (var policy in policies)
                policy.OnFailed();
        }

        public override string ToString()
        {
            return $"[{string.Join(",", (object[])policies)}] at index [{index}]";
        }
    }
}
