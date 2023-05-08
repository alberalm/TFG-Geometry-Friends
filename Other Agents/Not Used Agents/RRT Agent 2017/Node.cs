using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Debug;

namespace GeometryFriendsAgents
{
    public class Node
    {
        private Node parent;
        private List<Node> children;
        private State state;
        //indicate how many nodes it takes to reach this one
        private int treeDepth;
        //the action the parent made to get here
        private Moves previousAction;
        //actions that can still be tested
        private List<Moves> remainingMoves;
        private bool remainingSTPActions;
        //simulator
        ActionSimulator predictor;
        //debug info
        private List<DebugInformation> debugInfo;
        //bgt
        private bool leaf;

        public Node(Node p, State s, Moves action, ActionSimulator pred, List<Moves> moves)
        {
            parent = p;
            state = s;
            treeDepth = 0;
            previousAction = action;
            predictor = pred;
            children = new List<Node>();
            remainingMoves = moves;
            remainingSTPActions = true;
            debugInfo = new List<DebugInformation>();
            leaf = true;
            //copy debug info from parent
            if (p != null)
            {

                addDebugInfo(p.getDebugInfo());
            }
        }

        private void addNodeToParent()
        {
            parent.addChild(this);
            treeDepth++;
        }

        public void addDebugInfo(List<DebugInformation> dbList)
        {
            debugInfo.AddRange(dbList);
        }

        public Node getParent()
        {
            return parent;
        }

        public List<Node> getChildren()
        {
            return children;
        }

        public State getState()
        {
            return state;
        }

        public Moves getAction()
        {
            return previousAction;
        }

        public ActionSimulator getPredictor()
        {
            return predictor;
        }

        public List<DebugInformation> getDebugInfo()
        {
            return debugInfo;
        }

        public void addChild(Node child)
        {
            children.Add(child);
        }

        public int possibleMovesCount()
        {
            return remainingMoves.Count;
        }

        public int getTreeDepth()
        {
            return treeDepth;
        }

        public Moves getMoveAndRemove(int randAction)
        {
            Moves randomAction = remainingMoves[randAction];
            remainingMoves.Remove(randomAction);
            return randomAction;
        }

        public List<Moves> getRemainingMoves()
        {
            return remainingMoves;
        }

        //connect this node to the given one by maintaining the parent but changing the children
        public void connectNodes(Node node)
        {
            this.children = node.getChildren();
            this.remainingMoves = node.getRemainingMoves();
        }

        public void removeMove(Moves move)
        {
            remainingMoves.Remove(move);
        }

        public void noRemainingSTPActions()
        {
            remainingSTPActions = false;
        }

        public bool anyRemainingSTPActions()
        {
            return remainingSTPActions;
        }

        public int nodeDepth()
        {
            return treeDepth;
        }

        public int branchFactor()
        {
            return children.Count;
        }

        public bool isLeaf()
        {
            return leaf;
        }

        public void nonLeaft()
        {
            leaf = false;
        }

        public void resetNode(List<Moves> moves, Tree t)
        {
            remainingMoves = moves;
            foreach(Node child in children)
            {
                remainingMoves.Remove(child.getAction());
            }
            if(remainingMoves.Count == 0)
            {
                t.closeNode(this);
            }
        }
    }
}
