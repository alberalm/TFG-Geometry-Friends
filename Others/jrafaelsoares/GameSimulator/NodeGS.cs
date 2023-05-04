using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Debug;

namespace GeometryFriendsAgents
{
    public class NodeGS : Node
    {
        ActionSimulator simulator;

        public NodeGS(Node p, State s, Moves action, ActionSimulator predictor, List<Moves> moves) : base(p, s, action, moves)
        {
            simulator = predictor;
        }

        public ActionSimulator getSimulator()
        {
            return simulator;
        }

    }
}
