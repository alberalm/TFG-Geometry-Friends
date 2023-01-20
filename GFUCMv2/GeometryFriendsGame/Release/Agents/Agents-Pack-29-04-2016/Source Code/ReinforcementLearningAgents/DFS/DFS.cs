using GeometryFriendsAgents.Model;
using System.Collections.Generic;

namespace GeometryFriendsAgents.DFS {
    class DFS {

        private List<GraphNode> nodesToVisit;

        public DFS(List<GraphNode> nodesToVisit) {
            this.nodesToVisit = nodesToVisit;
        }


        public GraphNode DFSSearch(GraphNode startNode) {

            DFSNode node = new DFSNode(startNode);

            DFSNode selectedNode = node;
            int nCollectibles = 0;


            Queue<DFSNode> frontier = new Queue<DFSNode>();
            frontier.Enqueue(node);
            while (true) {
                if (frontier.Count == 0) {
                    break;
                }
                node = frontier.Dequeue();
                if (node.GoalTest(nodesToVisit)) {
                    selectedNode = node;
                    break;
                }

                if (node.NumberCollectiblesCaught > nCollectibles) {
                    nCollectibles = node.NumberCollectiblesCaught;
                    selectedNode = node;
                }

                List<GraphLink> adjacentLinks = node.Node.AdjacentNodes;
                HashSet<GraphNode> adjNodes = new HashSet<GraphNode>();

                foreach (GraphLink adjLink in adjacentLinks) {
                    adjNodes.Add(adjLink.ToNode);
                }

                foreach (GraphNode adjNode in adjNodes) {
                    frontier.Enqueue(new DFSNode(adjNode,node));
                }
            }

            if (selectedNode.NodesPassed.Count > 1) {
                selectedNode.NodesPassed.Dequeue(); // remove startNode
                return selectedNode.NodesPassed.Dequeue();
            } else {
                return null; // no path
            }
        }



    }
}
