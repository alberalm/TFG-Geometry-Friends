using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace GeometryFriendsAgents
{
    public class Graph
    {
        public class Diamond
        {
            public int id;
            public List<int> isAbovePlatform;
            public List<MoveInformation> moves;
            public List<Platform> platforms;

            public Diamond(int id)
            {
                this.id = id;
                this.moves = new List<MoveInformation>();
                this.platforms = new List<Platform>();
                this.isAbovePlatform = new List<int>();
            }
        }

        public class Node
        {
            public List<MoveInformation> plan;
            public List<bool> caught;
            public int numCaught;
            public int depth;

            public Node(List<MoveInformation> plan, List<bool> caught, int numCaught, int depth)
            {
                this.plan = plan;
                this.caught = caught;
                this.numCaught = numCaught;
                this.depth = depth;
            }
        }

        public int V;
        public int E;
        public List<Diamond> collectibles;
        public CollectibleRepresentation[] initialCollectibles;
        public List<Platform> platforms;
        public Stopwatch sw;
        public bool PlanIsComplete = false;

        public Graph(List<Platform> platforms, CollectibleRepresentation[] collectibles)
        {
            sw = new Stopwatch();
            this.platforms = platforms;
            this.initialCollectibles = collectibles;
            V = platforms.Count();
            E = 0;
            this.collectibles = new List<Diamond>();
            for(int i = 0; i < collectibles.Length; i++)
            {
                this.collectibles.Add(new Diamond(i));
            }
            // Add every possible move to the directed graph
            for (int i = 0; i < platforms.Count(); i++)
            {
                foreach(int d in platforms[i].ReachableCollectiblesLandingInThisPlatform())
                {
                    this.collectibles[d].isAbovePlatform.Add(i);
                }
                foreach (MoveInformation m in platforms[i].moveInfoList)
                {
                    E++;
                    for(int k = 0; k < m.diamondsCollected.Count; k++)
                    {
                        this.collectibles[k].moves.Add(m);
                        this.collectibles[k].platforms.Add(platforms[i]);
                    }
                }
            }
            foreach(Diamond d in this.collectibles)
            {
                if(d.isAbovePlatform.Count() > 0)
                {
                    for(int i = 0; i < d.moves.Count; i++)
                    {
                        MoveInformation m = d.moves[i];
                        if(!d.isAbovePlatform.Contains(m.landingPlatform.id) || !d.isAbovePlatform.Contains(m.departurePlatform.id))
                        {
                            d.moves.Remove(m);
                            i--;
                        }
                    }
                }
            }
        }

        public bool EveryCollectibleCanBeCollected()
        {
            foreach(Diamond d in collectibles)
            {
                if(d.platforms.Count == 0)
                {
                    return false;
                }
            }
            return true;
        }

        public int GetDiamondID(CollectibleRepresentation c)
        {
            for(int i = 0; i < initialCollectibles.Length; i++)
            {
                CollectibleRepresentation other = initialCollectibles[i];
                if(c.X == other.X && c.Y == other.Y)
                {
                    return i;
                }
            }
            return -1;
        }

        public List<MoveInformation> SearchAlgorithm(int src, CollectibleRepresentation[] uncaught, MoveInformation previous_move)
        {
            PlanIsComplete = false;
            Dictionary<int, int> diamonds = new Dictionary<int, int>();
            List<Diamond> newList = new List<Diamond>();
            int count = 0;
            List<MoveInformation> reserve_plan = null;
            foreach(CollectibleRepresentation c in uncaught)
            {
                int index = GetDiamondID(c);
                if(index != -1)
                {
                    foreach(Diamond d in collectibles) {
                        if (d.id == index) {
                            newList.Add(d);
                            diamonds[index] = count;
                            count++;
                        }
                    }
                }
            }
            collectibles = newList;
            List<Node> queue = new List<Node>();
            Node sol = new Node(null, null, 0, 0);
            List<bool> auxlist = Enumerable.Repeat(false,collectibles.Count).ToList();
            queue.Add(new Node(new List<MoveInformation> { new MoveInformation(platforms[src]) }, auxlist, 0, 0));
            int limit = collectibles.Count;
            foreach(Diamond d in collectibles)
            {
                if(d.isAbovePlatform.Count > 0)
                {
                    limit--;
                }
            }
            sw.Restart();
            while (queue.Count > 0)
            {
                Node n = queue[0];
                queue.RemoveAt(0);
                // If depth is too high (more than #platforms * #collectibles), we our representation does not have any solution
                if (n.depth > platforms.Count * Math.Max(limit, 3) || sw.ElapsedMilliseconds >= 500)
                {
                    continue;
                }
                // Process move
                MoveInformation move = n.plan[n.plan.Count - 1];
                foreach (int d in move.diamondsCollected)
                {
                    //Arreglado?
                    if (diamonds.ContainsKey(d) && !n.caught[diamonds[d]])
                    {
                        n.caught[diamonds[d]] = true;
                        n.numCaught++;
                    }
                }
                //Process platform
                for (int i = 0; i < collectibles.Count; i++)
                {
                    if (!n.caught[i])
                    {
                        if (collectibles[i].isAbovePlatform.Contains(move.landingPlatform.id))
                        {
                            n.caught[i] = true;
                            n.numCaught++;
                        }
                    }
                }
                // This is for incomplete solutions
                if (n.numCaught > sol.numCaught)
                {
                    sol = n;
                }
                if (n.numCaught == collectibles.Count)
                {
                    n.plan.RemoveAt(0);
                    bool plan_is_risky = false;
                    for (int i = 0; i < n.plan.Count && !plan_is_risky; i++)
                    {
                        plan_is_risky = plan_is_risky || n.plan[i].risky;
                        if (previous_move != null && n.plan[i].IsEqual(previous_move))
                        {
                            plan_is_risky = true;
                        }
                    }
                    if (!plan_is_risky)
                    {
                        PlanIsComplete = true;
                        return n.plan;
                    }
                    else if (reserve_plan == null)
                    {
                        reserve_plan = n.plan;
                    }
                }
                else
                {
                    foreach(MoveInformation m in move.landingPlatform.moveInfoList)
                    {
                        if (m.departurePlatform.id != m.landingPlatform.id)
                        {
                            List<MoveInformation> newPlan = new List<MoveInformation>(n.plan);
                            List<bool> newcaught = new List<bool>(n.caught);
                            newPlan.Add(m);
                            queue.Add(new Node(newPlan, newcaught, n.numCaught, n.depth + 1));
                        }
                    }
                }
            }
            // If we find no complete solution, we return the one that catches the most diamonds possible
            if (reserve_plan != null)
            {
                PlanIsComplete = true;
                return reserve_plan;
            }
            if (sol.plan == null || sol.plan.Count==0)
            {
                return new List<MoveInformation>();
            }
            else
            {
                sol.plan.RemoveAt(0);
                return sol.plan;
            }
        }
    }
}
