using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    class Graph
    {
        public class Edge
        {
            public int to;
            public List<LevelMap.MoveInformation> moves;

            public Edge(int to)
            {
                this.to = to;
                moves = new List<LevelMap.MoveInformation>();
            }

            public Edge(int to, LevelMap.MoveInformation m)
            {
                this.to = to;
                moves = new List<LevelMap.MoveInformation>() { m };
            }
        }

        public class Diamond
        {
            public int id;
            public int isAbovePlatform; // -1 means there is no platform, if there are several platforms, we pick the highest one
            public List<LevelMap.MoveInformation> moves;
            public List<LevelMap.Platform> platforms;

            public Diamond(int id)
            {
                this.id = id;
                this.moves = new List<LevelMap.MoveInformation>();
                this.platforms = new List<LevelMap.Platform>();
                this.isAbovePlatform = -1;
            }
        }

        public class Node
        {
            public List<LevelMap.MoveInformation> plan;
            public List<bool> caught;
            public int numCaught;
            public int depth;

            public Node(List<LevelMap.MoveInformation> plan, List<bool> caught, int numCaught, int depth)
            {
                this.plan = plan;
                this.caught = caught;
                this.numCaught = numCaught;
                this.depth = depth;
            }
        }

        public int V;
        public int E;
        public List<List<Edge>> adj;
        public List<Diamond> collectibles;
        public CollectibleRepresentation[] initialCollectibles;
        public List<LevelMap.Platform> platforms;

        public Graph(int V)
        {
            this.V = V;
            E = 0;
            adj = new List<List<Edge>>();
            for (int i = 0; i < V; i++)
            {
                adj.Add(new List<Edge>());
            }
        }

        public Graph(List<LevelMap.Platform> platforms, CollectibleRepresentation[] collectibles)
        {
            this.platforms = platforms;
            this.initialCollectibles = collectibles;
            V = platforms.Count();
            E = 0;
            adj = new List<List<Edge>>();
            this.collectibles = new List<Diamond>();
            for(int i = 0; i < collectibles.Length; i++)
            {
                this.collectibles.Add(new Diamond(i));
            }
            // Add every possible move to the directed graph
            for (int i = 0; i < platforms.Count(); i++)
            {
                adj.Add(new List<Edge>());
                foreach(int d in platforms[i].ReachableCollectiblesLandingInThisPlatform())
                {
                    if(this.collectibles[d].isAbovePlatform == -1)
                    {
                        this.collectibles[d].isAbovePlatform = i;
                    }
                }
                foreach (LevelMap.MoveInformation m in platforms[i].moveInfoList)
                {
                    E++;
                    AddMove(m, i, m.landingPlatform.id);
                    for(int k = 0; k < m.diamondsCollected.Count; k++)
                    {
                        this.collectibles[k].moves.Add(m);
                        this.collectibles[k].platforms.Add(platforms[i]);
                    }
                }
            }
            foreach(Diamond d in this.collectibles)
            {
                if(d.isAbovePlatform != -1)
                {
                    for(int i = 0; i < d.moves.Count; i++)
                    {
                        LevelMap.MoveInformation m = d.moves[i];
                        if(m.landingPlatform.id != d.isAbovePlatform || m.departurePlatform.id != d.isAbovePlatform)
                        {
                            d.moves.Remove(m);
                            i--;
                        }
                    }
                    // There should be just one move after this
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

        public List<LevelMap.MoveInformation> SearchAlgorithm(int src, CollectibleRepresentation[] uncaught)
        {
            List<Diamond> newList = new List<Diamond>();
            foreach(CollectibleRepresentation c in uncaught)
            {
                int index = GetDiamondID(c);
                if(index != -1)
                {
                    foreach(Diamond d in collectibles) {
                        if (d.id == index) {
                            newList.Add(d);
                        }
                    }
                }
            }
            collectibles = newList;
            List<Node> queue = new List<Node>();
            Node sol = new Node(null, null, 0, 0);
            List<bool> auxlist = Enumerable.Repeat(false,collectibles.Count).ToList();
            queue.Add(new Node(new List<LevelMap.MoveInformation> { new LevelMap.MoveInformation(platforms[src]) }, auxlist, 0, 0));
            while (queue.Count > 0)
            {
                Node n = queue[0];
                queue.RemoveAt(0);
                // If depth is too high (more than #platforms * #collectibles), we our representation does not have any solution
                if (n.depth > platforms.Count * collectibles.Count)
                {
                    continue;
                }
                // Process move
                LevelMap.MoveInformation move = n.plan[n.plan.Count - 1];
                foreach (int d in move.diamondsCollected)
                {
                    if (!n.caught[d])
                    {
                        n.caught[d] = true;
                        n.numCaught++;
                    }
                }
                //Process platform
                for (int i = 0; i < collectibles.Count; i++)
                {
                    if (!n.caught[i])
                    {
                        if(collectibles[i].isAbovePlatform == move.landingPlatform.id)
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
                    sol.plan.RemoveAt(0);
                    return sol.plan;
                }
                else
                {
                    foreach(LevelMap.MoveInformation m in move.landingPlatform.moveInfoList)
                    {
                        if (m.departurePlatform.id != m.landingPlatform.id)
                        {
                            List<LevelMap.MoveInformation> newPlan = new List<LevelMap.MoveInformation>(n.plan);
                            List<bool> newcaught = new List<bool>(n.caught);
                            newPlan.Add(m);
                            queue.Add(new Node(newPlan, newcaught, n.numCaught, n.depth + 1));
                        }
                    }
                }
            }
            // If we find no complete solution, we return the one that catches the most diamonds possible
            sol.plan.RemoveAt(0);
            return sol.plan;
        }
        
        public void AddMove(LevelMap.MoveInformation move, int from, int to)
        {
            // Need the "to" vertex for the inverted graph
            foreach(Edge e in adj[from])
            {
                if(e.to == to)
                {
                    e.moves.Add(move);
                    return;
                }
            }
            // There is still no Edge connecting from and to
            adj[from].Add(new Edge(to, move));
        }

        public bool IsThereEdge(int from, int to)
        {
            if (from >= V || to >= V)
            {
                return false;
            }
            foreach (Edge e in adj[from])
            {
                if (e.to == to)
                {
                    return true;
                }
            }
            return false;
        }

        public Graph Reverse()
        {
            // Does not contain real moves, just reverse adjacent vertices
            Graph g = new Graph(V);
            g.E = E;
            LevelMap.MoveInformation move = null;
            for (int i = 0; i < V; i++)
            {
                foreach (Edge w in adj[i])
                {
                    g.AddMove(move, w.to, i);
                }
            }
            return g;
        }
    }
}
