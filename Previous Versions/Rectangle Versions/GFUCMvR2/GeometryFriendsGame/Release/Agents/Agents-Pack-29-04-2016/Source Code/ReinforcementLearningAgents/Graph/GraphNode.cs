using GeometryFriendsAgents.WorldObjects;
using System.Collections.Generic;

namespace GeometryFriendsAgents.Model {
    class GraphNode {

        private Platform platform;

        public bool visited;

        public bool needsVisit() {
            return platform.PercentageCollectiblesCaught < 100;
        }

        public Platform Platform {
            get { return platform; }
            set { platform = value; }
        }

        private List<GraphLink> adjacentNodes;

        public List<GraphLink> AdjacentNodes {
            get { return adjacentNodes; }
            set { adjacentNodes = value; }
        }

        public void addAjacentNode(GraphLink link) {
            adjacentNodes.Add(link);
        }

        public GraphNode(Platform p) {
            this.platform = p;
            adjacentNodes = new List<GraphLink>();
            visited = false;
        }

        public override string ToString() {
            string s = "Node: PlatformId:";
            s+= platform.ID + " Platform Top:" + platform.Top + " Adjacent Nodes: ";
            foreach(GraphLink l in adjacentNodes) {
                s+= l.ToString() + " ";
            }
            return s;
        }
    }
}
