using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Debug;

namespace GeometryFriendsAgents
{
    public class NodeMP
    {
        private NodeMP parent;
        private List<NodeMP> children;
        private StateMP state;
        //indicate how many nodes it takes to reach this one
        private int treeDepth;
        //the action the parent made to get here
        private Moves[] previousActions;
        //actions that can still be tested
        private List<Moves[]> remainingMoves;
        private bool remainingSTPActions;
        //simulator
        ActionSimulator predictor;
        //debug info
        private List<DebugInformation> debugInfo;
        //bgt
        private bool leaf;

        public NodeMP(NodeMP p, StateMP s, Moves[] actions, ActionSimulator pred, List<Moves[]> moves)
        {
            parent = p;
            state = s;
            treeDepth = 0;
            previousActions = actions;
            predictor = pred;
            children = new List<NodeMP>();
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

        public NodeMP getParent()
        {
            return parent;
        }

        public List<NodeMP> getChildren()
        {
            return children;
        }

        public StateMP getState()
        {
            return state;
        }

        public Moves[] getActions()
        {
            return previousActions;
        }

        public ActionSimulator getPredictor()
        {
            return predictor;
        }

        public List<DebugInformation> getDebugInfo()
        {
            return debugInfo;
        }

        public void addChild(NodeMP child)
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

        public Moves[] getMoveAndRemove(int randAction)
        {
            if(remainingMoves.Count == 0)
            {
                return new Moves[] { Moves.NO_ACTION, Moves.NO_ACTION };
            }
            Moves[] randomAction = remainingMoves[randAction];
            remainingMoves.Remove(randomAction);
            return randomAction;
        }

        public Moves[] getMove(int randAction)
        {
            if (remainingMoves.Count == 0)
            {
                return new Moves[] { Moves.NO_ACTION, Moves.NO_ACTION };
            }
            Moves[] randomAction = remainingMoves[randAction];
            return randomAction;
        }

        public List<Moves[]> getRemainingMoves()
        {
            return remainingMoves;
        }

        //connect this node to the given one by maintaining the parent but changing the children
        public void connectNodes(NodeMP node)
        {
            this.children = node.getChildren();
            this.remainingMoves = node.getRemainingMoves();
        }

        public void removeMove(Moves[] moves)
        {
            //remainingMoves.Remove(remainingMoves.SingleOrDefault(m => (m[0] == moves[0] && m[1] == moves[1])));
            foreach (Moves[] move in remainingMoves)
            {
                if (move[0] == moves[0] && move[1] == moves[1])
                {
                    remainingMoves.Remove(move);
                    return;
                }
            }
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

        public void resetNode(List<Moves[]> moves, TreeMP t)
        {
            remainingMoves = moves;
            foreach(NodeMP child in children)
            {
                remainingMoves.Remove(child.getActions());
            }
            if(remainingMoves.Count == 0)
            {
                t.closeNode(this);
            }
        }

        public void removeChild(NodeMP node)
        {
            children.Remove(node);
        }

        public void removeChildren()
        {
            children.Clear();
        }

        public void setAction(Moves[] a)
        {
            previousActions = a;
        }

        public void setTreeDepht(int td)
        {
            treeDepth = td;
        }

        public void setRABool(bool rA)
        {
            remainingSTPActions = rA;
        }

        public NodeMP clone()
        {
            NodeMP newNode = new NodeMP(this.parent, this.state, this.previousActions, this.predictor, this.remainingMoves);
            foreach(NodeMP child in this.children)
            {
                newNode.addChild(child);
            }
            newNode.setTreeDepht(this.getTreeDepth());
            newNode.setRABool(this.remainingSTPActions);
            if(newNode.getChildren().Count != 0)
            {
                newNode.nonLeaft();
            }
            return newNode;
        }
    }
}
