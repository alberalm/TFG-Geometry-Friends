using System.Collections.Generic;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends;

namespace GeometryFriendsAgents
{
    //Tree for use with general Simulator
    public class TreeGS : Tree
    {

        //constructor
        public TreeGS(State initialState, ActionSimulator predictor, List<Moves> moves, bool BGT) : base(initialState, moves, BGT)
        {
            setRoot(new NodeGS(null, initialState, 0, predictor, moves));
            addNode(getRoot());
        }

    }
}
