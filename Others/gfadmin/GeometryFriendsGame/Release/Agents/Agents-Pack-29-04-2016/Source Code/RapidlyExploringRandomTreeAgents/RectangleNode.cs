using System.Collections;

namespace GeometryFriendsAgents
{
    public class RectangleNode
    {
        private ArrayList _edges;
        public bool [] _busyActions;
        public bool _busyTiltRight;
        public bool _busyTiltLeft;
        public bool _busyMorphRight;
        public bool _busyMorphLeft;
		private RectangleNode _parent;
		private State _state;

		public RectangleNode ()
		{
			this._edges = new ArrayList();
			this._parent = null;
			this._state = null;
            this._busyActions = new bool[5];
            _busyActions[0] = false;
            _busyActions[1] = false;
            _busyActions[2] = false;
			_busyActions[3] = true;
            _busyActions[4] = true;
            _busyMorphLeft = true;
            _busyMorphRight = true;
            _busyTiltLeft = true;
            _busyTiltRight = true;
		}

		public RectangleNode(State s)
		{
			this._edges = new ArrayList();
			this._parent = null;
            this._state = s;
            this._busyActions = new bool[5];
            _busyActions[0] = false;
            _busyActions[1] = false;
            _busyActions[2] = false;
			_busyActions[3] = true;
            _busyActions[4] = true;
            _busyMorphLeft = true;
            _busyMorphRight = true;
            _busyTiltLeft = true;
            _busyTiltRight = true;
		}

        public void setActionBusy(int action)
        {
            _busyActions[action] = true;
        }

        public bool isActionBusy(int action)
        {
            return (bool) _busyActions[action];
        }

		public State getState()
		{
			return this._state;
		}

		public bool allActionsBusy()
		{
			foreach(bool b in _busyActions)
			{
				if(!b)
					return false;
			}
			return true;
		}

		public bool isBusy()
		{
            if (this.allActionsBusy())
            {
                if (_edges.Count == 0)
                    return true;
            }
            return false;
		}

		public int getNrNodes()
		{
			return this._edges.Count;
		}

		public RectangleNode getParent()
		{
			return this._parent;
		}

		public void setParent(RectangleNode p)
		{
			this._parent = p;
		}

		public void addEdge(RectangleNode child, int action)
		{
			RectangleEdge newEdge = new RectangleEdge(this, child, action);
            newEdge.setBusy();
			this._edges.Add(newEdge);
			child.setParent(this);
		}

		public void removeEdge(RectangleNode child)
		{
			foreach(RectangleEdge i in this._edges)
			{
				if( i.getChild().Equals(child) )
				{
					this._edges.Remove(i);
					return;
				}
			}
		}

		public bool isConnected(RectangleNode n)
		{
			foreach(RectangleEdge i in this._edges)
			{
				if( i.getChild().Equals(n) )	// Might need implementation of Equals in order to guarantee functionality
					return true;
			}
			return false;
		}
    }
}
