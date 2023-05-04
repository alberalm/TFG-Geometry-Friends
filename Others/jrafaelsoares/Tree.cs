using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;

namespace GeometryFriendsAgents
{
    //The tree used for the RRT algorithm
    public class Tree
    {
        //All the nodes in the tree and its root
        private List<Node> nodes;
        private List<Node> open;
        private List<Node> closed;
        //for bgt
        private bool bgt;
        private float depthAverage;
        private float branchingAverage;
        private int depthSum;
        private int branchingSum;
        private List<Node> nonLeafNodes;
        private List<Node> leafNodes;
        private Node root;
        private Node goal;
        private int visitedNodes;
        //other
        private Random rnd;

        //constructor
        public Tree(State initialState, List<Moves> moves, bool BGT)
        {
            nodes = new List<Node>();
            open = new List<Node>();
            closed = new List<Node>();
            visitedNodes = 0;
            bgt = BGT;

            if (bgt)
            {
                depthAverage = 0;
                branchingAverage = 0;
                nonLeafNodes = new List<Node>();
                leafNodes = new List<Node>();
            }

            rnd = new Random();
        }

        //add node to the tree and to open list
        public void addNode(Node node)
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
                if(node.getParent() != null && node.getParent().isLeaf())
                {
                    removeFromLeaf(node.getParent());
                }
            }
        }

        public void removeFromLeaf(Node node)
        {
            leafNodes.Remove(node);
            if (!nonLeafNodes.Contains(node))
            {
                nonLeafNodes.Add(node);
            }
            node.nonLeaf();
        }

        //set the goal
        public void setGoal(Node g)
        {
            goal = g;
        }

        public void setRoot(Node root)
        {
            this.root = root;
        }

        public Node getRoot()
        {
            return root;
        }

        //choose a random node from the open list and return it
        public Node getRandomNode()
        {
            int randNode = rnd.Next(open.Count);
            //when getting a node, it counts for the visited nodes
            visitedNodes++;
 
            return open[randNode];
        }

        public Node getRandomNonLeafNode()
        {
            if (nonLeafNodes.Count != 0)
            {
                int randNode = rnd.Next(nonLeafNodes.Count);
                return nonLeafNodes[randNode];
            }
            else
            {
                return null;
            }
        }

        public Node getRandomLeafNode()
        {
            if(leafNodes.Count != 0){
                int randNode = rnd.Next(leafNodes.Count);
                return leafNodes[randNode];
            }
            else
            {
                return null;
            }
        }

        public bool getBGT()
        {
            return bgt;
        }

        public Node getGoal()
        {
            return goal;
        }

        public List<Node> getOpenNodes()
        {
            return open;
        }

        public List<Node> getNodes()
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
        public void closeNode(Node node)
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
            foreach(Node node in nodes)
            {
                open.Add(node);
            }
            closed = new List<Node>();
        }
    }
}
