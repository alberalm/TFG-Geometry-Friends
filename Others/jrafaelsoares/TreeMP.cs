using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;

namespace GeometryFriendsAgents
{
    //The tree used for the RRT algorithm
    public class TreeMP
    {
        //All the nodes in the tree and its root
        private List<NodeMP> nodes;
        private List<NodeMP> open;
        private List<NodeMP> closed;
        //for bgt
        private bool bgt;
        private float depthAverage;
        private float branchingAverage;
        private int depthSum;
        private int branchingSum;
        private List<NodeMP> nonLeafNodes;
        private List<NodeMP> leafNodes;
        private NodeMP root;
        private NodeMP goal;
        private int visitedNodes;
        //other
        private Random rnd;

        //constructor
        public TreeMP(StateMP initialState, ActionSimulator predictor, List<Moves[]> moves, bool BGT)
        {
            root = new NodeMP(null, initialState, null, predictor, moves);
            nodes = new List<NodeMP>();
            open = new List<NodeMP>();
            closed = new List<NodeMP>();
            visitedNodes = 0;
            bgt = BGT;

            if (bgt)
            {
                depthAverage = 0;
                branchingAverage = 0;
                nonLeafNodes = new List<NodeMP>();
                leafNodes = new List<NodeMP>();
            }

            addNode(root);
            rnd = new Random();
        }

        //add node to the tree and to open list
        public void addNode(NodeMP node)
        {
            nodes.Add(node);
            open.Add(node);
            if (bgt)
            {
                //a new node is always a leaf
                leafNodes.Add(node);
                //update de branching and depth average
                branchingSum++;
                branchingAverage = branchingSum / nodes.Count;
                depthSum++;
                depthAverage = depthSum / leafNodes.Count;

                //the parent of the new node is not a leaf anymore
                if (node.getParent() != null && node.getParent().isLeaf())
                {
                    removeFromLeaf(node.getParent());
                }
            }
        }

        //add node and its children to the tree and to open list
        public void addNodes(NodeMP node, bool first)
        {
            if (!first && !nodes.Contains(node))
            {
                if (node.getChildren().Count == 0)
                {
                    addNode(node);
                    return;
                }
                foreach (NodeMP child in node.getChildren())
                {
                    addNodes(child, false);
                }
            }
        }

        //add node and its children to the tree and to open list
        public void addNodesIterative(NodeMP node, bool first)
        {
            if (first)
            {
                nodes.Add(node);
            }
            nodes.AddRange(node.getChildren());
            foreach(NodeMP child in node.getChildren())
            {
                addNodesIterative(child, false);
            }

        }

        public void removeFromLeaf(NodeMP node)
        {
            leafNodes.Remove(node);
            if (node.getRemainingMoves().Count != 0)
            {
                nonLeafNodes.Add(node);
                node.nonLeaft();
            }
            else
            {
                //test
                int remaining = 0;
            }
        }

        //set the goal
        public void setGoal(NodeMP g)
        {
            goal = g;
        }

        //choose a random node from the open list and return it
        public NodeMP getRandomNode()
        {
            int randNode = rnd.Next(open.Count);
            //when getting a node, it counts for the visited nodes
            visitedNodes++;

            return open[randNode];
        }

        public NodeMP getRandomNonLeafNode()
        {
            if (nonLeafNodes.Count != 0)
            {
                int randNode = rnd.Next(nonLeafNodes.Count);
                return nonLeafNodes[randNode];
            }
            else
            {
                return getRandomLeafNode();
            }

        }

        public NodeMP getRandomLeafNode()
        {
            if (leafNodes.Count != 0)
            {
                int randNode = rnd.Next(leafNodes.Count);
                return leafNodes[randNode];
            }
            else
            {
                return getRandomNonLeafNode();
            }
        }

        public NodeMP getRoot()
        {
            return root;
        }

        public void setRoot(NodeMP node)
        {
            root = node;
        }

        public NodeMP getGoal()
        {
            return goal;
        }

        public List<NodeMP> getOpenNodes()
        {
            return open;
        }

        public List<NodeMP> getNodes()
        {
            return nodes;
        }

        public int getVisitedNodes()
        {
            return visitedNodes;
        }

        public int getTotalNodes()
        {
            return nodes.Count();
        }

        public float ratio()
        {
            return depthAverage / branchingAverage;
        }

        //closes a node that has no more actions to test
        public void closeNode(NodeMP node)
        {
            closed.Add(node);
            open.Remove(node);
            if (bgt)
            {
                nonLeafNodes.Remove(node);
                leafNodes.Remove(node);
            }
        }

        public void addVisitedNode()
        {
            visitedNodes++;
        }

        public void resetTree()
        {
            foreach (NodeMP node in nodes)
            {
                open.Add(node);
            }
            closed = new List<NodeMP>();
        }

        public void removeNode(NodeMP node)
        {
            nodes.Remove(node);
            open.Remove(node);
            closed.Remove(node);
        }

        public void removeNodes(List<NodeMP> nodes)
        {
            foreach (NodeMP node in nodes)
            {
                removeNode(node);
            }
        }
    }
}
