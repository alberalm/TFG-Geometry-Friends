using GeometryFriendsAgents.WorldObjects;
using System.Collections.Generic;

namespace GeometryFriendsAgents.Model {
    abstract class Graph {

        private int nextGraphLinkId = 0;

        protected List<GraphNode> nodes;

        protected List<GraphLink> links;

        public Graph(List<Platform> platforms) {
            nodes = new List<GraphNode>();
            for (int i = platforms.Count - 1; i >= 0; i--) {
                GraphNode newNode = new GraphNode(platforms[i]);
                nodes.Add(newNode);
            }

            links = new List<GraphLink>();
            CheckConnection();
        }


        internal GraphNode GetGraphNode(WorldObjects.Platform platform) {
            if (platform != null) {
                foreach (GraphNode node in nodes) {
                    if (node.Platform.ID.Equals(platform.ID)) {
                        return node;
                    }
                }
            }
            return null;
        }

        internal GraphLink GetGraphLink(int id) {
            foreach (GraphLink link in links) {
                if (link.ID == id) {
                    return link;
                }
            }
            return null;
        }

        protected void CheckConnection() {
            List<GraphNode> processedGraphNodes = new List<GraphNode>();
            while (nodes.Count > 0) {
                GraphNode analysingNode = nodes[0];
                nodes.RemoveAt(0);
                List<GraphNode> testedNodes = new List<GraphNode>();
                foreach (GraphNode test in nodes) {
                    List<Platform> dummyPlatforms = CreatePlatforms(analysingNode, test);
                    List<Platform> dummiesToRemove = new List<Platform>();
                    foreach (GraphNode n in nodes) {
                        if (n.Equals(test)) {
                            continue;
                        }
                        foreach (Platform dummy in dummyPlatforms) {
                            if (dummy.Colides(n.Platform)) {
                                dummiesToRemove.Add(dummy);
                            }
                        }
                    }
                    foreach (Platform p in dummiesToRemove) {
                        dummyPlatforms.Remove(p);
                    }
                    if (dummyPlatforms.Count > 0) {
                        CreateConnection(analysingNode, test, dummyPlatforms);
                    }
                }
                processedGraphNodes.Add(analysingNode);
            }
            nodes.AddRange(processedGraphNodes);
        }

        protected abstract void CreateConnection(GraphNode analysingNode, GraphNode test, List<Platform> dummyPlatforms);

        protected abstract List<Platform> CreatePlatforms(GraphNode analysingNode, GraphNode test);

        internal List<GraphLink> GetNextNodeLinks(GraphNode myNode) {
            List<GraphLink> toReturn = new List<GraphLink>();

            int minPercentage = int.MaxValue;
            HashSet<GraphNode> nextPlatform = new HashSet<GraphNode>();

            foreach (GraphLink link in myNode.AdjacentNodes) {
                if (link.ToNode.Platform.PercentageCollectiblesCaught < minPercentage) {
                    minPercentage = link.ToNode.Platform.PercentageCollectiblesCaught;
                    nextPlatform.Clear();
                    nextPlatform.Add(GetGraphNode(link.ToNode.Platform));
                } else if (link.ToNode.Platform.PercentageCollectiblesCaught == minPercentage) {
                    nextPlatform.Add(GetGraphNode(link.ToNode.Platform));
                }
            }

            GraphNode selectedNode = null;
            int numberOfConnections = int.MinValue;

            foreach (GraphNode node in nextPlatform) {
                if (node.AdjacentNodes.Count > numberOfConnections) {
                    selectedNode = node;
                    numberOfConnections = selectedNode.AdjacentNodes.Count;
                }
            }

            return GetLinks(myNode, selectedNode);
        }

        private List<GraphLink> GetLinks(GraphNode myNode, GraphNode seletecedNode) {
            List<GraphLink> toReturn = new List<GraphLink>();
            foreach (GraphLink link in myNode.AdjacentNodes) {
                if (link.ToNode.Platform.ID == seletecedNode.Platform.ID) {
                    toReturn.Add(link);
                }
            }
            return toReturn;
        }


        public override string ToString() {
            string s = "Graph: ";
            foreach (GraphNode n in nodes) {
                s += "\n\t" + n.ToString();
            }
            return s;
        }

        protected int GetLinkID() {
            return nextGraphLinkId++;
        }

        internal List<GraphLink> GetNextLinks(Platform agentPlatform) {
            GraphNode startNode = GetGraphNode(agentPlatform);

            List<GraphNode> nodesToVisit = new List<GraphNode>();

            foreach (GraphNode node in nodes) {
                if (node.Platform.PercentageCollectiblesCaught != 100) {
                    nodesToVisit.Add(node);
                }
            }

            DFS.DFS search = new DFS.DFS(nodesToVisit);

            GraphNode nextNode = null;

            if(startNode!=null)
                nextNode = search.DFSSearch(startNode);

            List<GraphLink> linksToNode = new List<GraphLink>();

            foreach (GraphLink link in startNode.AdjacentNodes) {
                if (link.ToNode == nextNode) {
                    linksToNode.Add(link);
                }
            }

            linksToNode.Sort();
            return linksToNode;
        }
    }
}
