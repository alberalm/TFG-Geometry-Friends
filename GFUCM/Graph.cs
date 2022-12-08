using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

            public void AddMove(LevelMap.MoveInformation move)
            {
                moves.Add(move);
            }
        }

        public Graph(List<LevelMap.Platform> platforms)
        {
            V = platforms.Count();
            E = 0;
            adj = new List<List<Edge>>();
            // Add every possible move to the directed graph
            for (int i = 0; i < platforms.Count(); i++)
            {
                adj.Add(new List<Edge>());
                foreach (LevelMap.MoveInformation m in platforms[i].moveInfoList)
                {
                    E++;
                    AddMove(m, i, m.landingPlatform.id);
                }
            }

        }

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

        public int V;
        public int E;
        public List<List<Edge>> adj;

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
            LevelMap.MoveInformation move = new LevelMap.MoveInformation();
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
