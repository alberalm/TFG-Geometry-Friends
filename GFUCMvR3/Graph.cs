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

            public Diamond(int id)
            {
                this.id = id;
                this.isAbovePlatform = new List<int>();
            }
        }

        public class Node
        {
            public List<MoveInformation> plan;
            public List<bool> caught;
            public int numCaught;
            public bool is_risky;

            public Node(List<MoveInformation> plan, List<bool> caught, int numCaught, bool is_risky)
            {
                this.plan = plan;
                this.caught = caught;
                this.numCaught = numCaught;
                this.is_risky = is_risky;
            }

            public int Value()
            {
                int v = 0;
                int pot = 1;
                foreach (bool b in caught)
                {
                    if (b)
                    {
                        v += pot;
                    }
                    pot *= 2;
                }
                if (is_risky)
                {
                    return -v;
                }
                else
                {
                    return v;
                }
            }
        }

        public List<Diamond> collectibles;
        public CollectibleRepresentation[] initialCollectibles;
        public List<Platform> platforms;
        public Stopwatch sw;
        public bool planIsComplete;

        public Graph(List<Platform> platforms, CollectibleRepresentation[] collectibles)
        {
            sw = new Stopwatch();
            this.platforms = platforms;
            this.initialCollectibles = collectibles;
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
            }
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
            planIsComplete = false;
            Dictionary<int, int> diamonds = new Dictionary<int, int>();
            List<Diamond> newList = new List<Diamond>();
            foreach (CollectibleRepresentation c in uncaught)
            {
                int index = GetDiamondID(c);
                if(index != -1)
                {
                    foreach(Diamond d in collectibles) {
                        if (d.id == index) {
                            newList.Add(d);
                            diamonds[index] = newList.Count - 1;
                        }
                    }
                }
            }
            collectibles = newList;
            int not_risky_nodes = 1;
            List<Node> queue = new List<Node>();
            List<bool> auxlist = Enumerable.Repeat(false, collectibles.Count).ToList();
            HashSet<Tuple<int, int>> seen = new HashSet<Tuple<int, int>>();
            Node best_sol = new Node(new List<MoveInformation>(), auxlist, 0, false);
            queue.Add(new Node(new List<MoveInformation> { new MoveInformation(platforms[src]) }, auxlist, 0, false));
            sw.Restart();
            while (queue.Count > 0)
            {
                Node n = queue[0];
                queue.RemoveAt(0);
                if (!n.is_risky)
                {
                    not_risky_nodes--;
                }
                // Process move
                MoveInformation move = n.plan[n.plan.Count - 1];
                foreach (int d in move.diamondsCollected)
                {
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
                Tuple<int, int> node_tuple = new Tuple<int, int>(n.plan[n.plan.Count - 1].landingPlatform.id, n.Value());
                // If we already have visited a similar node or enough time has passed, continue
                if (seen.Contains(node_tuple) || sw.ElapsedMilliseconds >= 500)
                {
                    continue;
                }
                seen.Add(node_tuple);
                // This is for incomplete solutions
                if (n.numCaught > best_sol.numCaught || (n.numCaught == best_sol.numCaught && best_sol.is_risky && !n.is_risky))
                {
                    best_sol = n;
                }
                if (n.numCaught == collectibles.Count)
                {
                    n.plan.RemoveAt(0);
                    planIsComplete = true;
                    if (!n.is_risky || not_risky_nodes == 0)
                    {
                        return n.plan;
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
                            bool new_node_is_risky = n.is_risky || m.risky || (previous_move != null && m.IsEqual(previous_move));
                            if (!new_node_is_risky)
                            {
                                not_risky_nodes++;
                            }
                            queue.Add(new Node(newPlan, newcaught, n.numCaught, new_node_is_risky));
                        }
                    }
                    if (not_risky_nodes == 0 && planIsComplete)
                    {
                        return best_sol.plan;
                    }
                }
            }
            // If we only find risky solutions or incomplete ones, we return the one that catches the most diamonds possible
            return best_sol.plan;
        }
    }
}
