using GeometryFriends.AI;
using System;

namespace GeometryFriendsAgents
{
    public abstract class Policy<TState>
    {
        public int MaxSteps { get; private set; }
        public int Steps { get; protected set; }
        public float TimeStep { get; private set; } = 0.01f;
        public const float MaxTime = 20;
        public float NearestCorner { get; set; } = 1000000;

        int _failures;
        public virtual int Failures { get { return _failures; } }

        bool _failed;
        public virtual bool Failed { get { return _failed; } }

        public Policy()
        {
        }

        public virtual void Initialize(TState state, float timeStep)
        {
            Steps = 0;
            TimeStep = timeStep;
            MaxSteps = (int)(MaxTime / TimeStep);

            _failed = false;
        }

        public abstract Moves GetAction(TState state);

        public virtual bool IsFinished(TState state)
        {
            if (MaxSteps > 0 && Steps > MaxSteps)
            {
                if (!Failed)
                    OnFailed();
                return true;
            }

            return false;
        }

        public virtual bool ShouldAvoidCorners(TState state)
        {
            return false;
        }

        public virtual void OnFailed()
        {
            _failures++;
            _failed = true;
        }
    }
}

