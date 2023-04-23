using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public abstract class ActionSelector
    {
        public Dictionary<CollectibleRepresentation, int> collectibleId;
        public Learning l;
        public int target_position = 0;
        public int target_velocity = 0;
        public float brake_distance = 0;
        public float acceleration_distance = 0;
        public Graph graph;
        public SetupMaker setupMaker;
        public MoveInformation move;

        public ActionSelector(Dictionary<CollectibleRepresentation, int> collectibleId, Learning l, Graph graph, SetupMaker setupMaker)
        {
            this.collectibleId = collectibleId;
            this.l = l;
            this.graph = graph;
            this.setupMaker = setupMaker;
        }

        protected abstract MoveInformation DiamondsCanBeCollectedFrom(CircleRepresentation cI, RectangleRepresentation rI, Platform p, List<CollectibleRepresentation> remaining, int agentX);

        protected List<int> CollectiblesIds(List<CollectibleRepresentation> colI)
        {
            List<int> l = new List<int>();
            foreach (CollectibleRepresentation c in colI)
            {
                l.Add(collectibleId[c]);
            }
            return l;
        }
    }
} 
    

