using System.Collections.Generic;
using GeometryFriends.AI;

namespace GeometryFriendsAgents
{
    //Tree for use with general Simulator
    public class TreeSimulator : Tree
    {

        //constructor
        public TreeSimulator(State initialState, Simulator sim, List<Moves> moves, bool BGT) : base(initialState, moves, BGT)
        {

            setRoot(new NodeSimulator(null, initialState, 0, sim, moves));
            addNode(getRoot());
        }

    }
}
