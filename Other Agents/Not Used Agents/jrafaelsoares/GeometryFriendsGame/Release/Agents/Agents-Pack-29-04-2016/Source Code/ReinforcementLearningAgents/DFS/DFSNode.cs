using GeometryFriendsAgents.Model;
using System.Collections.Generic;

namespace GeometryFriendsAgents.DFS {
    class DFSNode {

        private Queue<GraphNode> nodesPassed;

        public Queue<GraphNode> NodesPassed {
            get { return nodesPassed; }
        }

        private GraphNode node;

        public GraphNode Node {
            get { return node; }
        }

        private int depth;

        public int Depth {
            get { return depth; }
        }

        private int nCollectibles;

        public int NumberCollectiblesCaught {
            get{ return nCollectibles;}
        }

        public DFSNode(GraphNode node) {
            this.node = node;
            nodesPassed = new Queue<GraphNode>();
            nodesPassed.Enqueue(this.node);
            depth = 1;
            nCollectibles = node.Platform.NumberCollectiblesCaught;
        }

        public DFSNode(GraphNode node, DFSNode parent) {
            this.node = node;
            nodesPassed = new Queue<GraphNode>(parent.NodesPassed);
            if (!nodesPassed.Contains(this.Node)) {
                nCollectibles = parent.NumberCollectiblesCaught + node.Platform.Collectibles.Count;
            }
            nodesPassed.Enqueue(this.node);
            depth = parent.Depth + 1;
            
        }

        public bool GoalTest(List<GraphNode> nodesToVisit) {

            if (depth >= ((node.Platform.Model.Platforms.Count - 1) * 2)) {
                return true;
            }

            foreach (GraphNode n in nodesToVisit) {
                if (!nodesPassed.Contains(n)) {
                    return false;
                }
            }
            return true;
        }
    }
}
