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

        public int V;
        public int E;
        public List<List<Edge>> adj;
        public List<Diamond> collectibles;
        public List<LevelMap.Platform> platforms;
        public bool hasFinished;

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

        private List<LevelMap.MoveInformation> AuxSearch(int source, bool[] areCaught, LevelMap.MoveInformation lastMove)
        {
            if (hasFinished)
            {
                return null;
            }
            // Process last move to catch mid-air collectibles
            if (lastMove != null)
            {
                foreach (int id in lastMove.diamondsCollected)
                {
                    areCaught[id] = true;
                }
            }
            int toBeCaught = 0;
            // Process platform
            for (int i = 0; i < areCaught.Length; i++)
            {
                areCaught[i] = areCaught[i] || collectibles[i].isAbovePlatform == source;
                if (!areCaught[i])
                {
                    toBeCaught++;
                }
            }
            if (toBeCaught == 0)
            {
                hasFinished = true;
                return new List<LevelMap.MoveInformation>() { lastMove };
            }
            List<LevelMap.MoveInformation> solution = null;
            Parallel.For(0, this.platforms[source].moveInfoList.Count, i =>
            {
                LevelMap.MoveInformation m = this.platforms[source].moveInfoList[i];
                List<LevelMap.MoveInformation> sol2 = AuxSearch(m.landingPlatform.id, areCaught, m);
                if (sol2 != null)
                {
                    if (lastMove != null)
                    {
                        sol2.Add(lastMove);
                    }
                    solution = sol2;
                }
            });
            return solution;
        }

        public List<LevelMap.MoveInformation> SearchAlgorithm(int src)
        {
            bool[] caught = new bool[collectibles.Count];
            for (int i = 0; i < collectibles.Count; i++) // Probably unnecessary
            {
                caught[i] = false;
            }
            hasFinished = false;
            List<LevelMap.MoveInformation> sol = AuxSearch(src, caught, null);
            sol.Reverse();
            return sol;
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
