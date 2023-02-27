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
        

        public ActionSelector(Dictionary<CollectibleRepresentation,int> collectibleId, Learning l, Graph graph)
        {
            this.collectibleId = collectibleId;
            this.l = l;
            this.graph = graph;
        }

        protected MoveInformation DiamondsCanBeCollectedFrom(Platform p, List<CollectibleRepresentation> remaining, int agentX, MoveInformation next_move)
        {
            int mindistance = 4000;
            MoveInformation move = null;
            foreach (MoveInformation m in p.moveInfoList)
            {
                if (m.landingPlatform.id == p.id){
                    foreach(int d in m.diamondsCollected)
                    {
                        if (CollectiblesIds(remaining).Contains(d))
                        {
                            foreach (Graph.Diamond diamond in graph.collectibles)
                            {
                                if  (diamond.id == d)
                                {
                                    if (diamond.isAbovePlatform == p.id || m.moveType == MoveType.NOMOVE)
                                    {
                                        if (Math.Abs(m.x - agentX) < mindistance && (next_move == null || !next_move.diamondsCollected.Contains(d)))
                                        {
                                            move = m;
                                            mindistance = Math.Abs(m.x - agentX);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return move;
        }

        protected List<int> CollectiblesIds(List<CollectibleRepresentation> colI)
        {
            List<int> l = new List<int>();
            foreach(CollectibleRepresentation c in colI)
            {
                l.Add(collectibleId[c]);
            }
            return l;
        }
        
      }
} 
    

