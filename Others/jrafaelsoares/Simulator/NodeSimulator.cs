using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Debug;

namespace GeometryFriendsAgents
{
    public class NodeSimulator : Node
    {
        Simulator simulator;

        public NodeSimulator(Node p, State s, Moves action, Simulator sim, List<Moves> moves) : base(p, s, action, moves)
        {
            simulator = sim;
        }

        public Simulator getSimulator()
        {
            return simulator;
        }

    }
}
