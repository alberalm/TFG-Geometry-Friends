using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    public abstract class State
    {
        public abstract bool IsFinal();
        public abstract double Reward();
        public override abstract string ToString();
    }
}
