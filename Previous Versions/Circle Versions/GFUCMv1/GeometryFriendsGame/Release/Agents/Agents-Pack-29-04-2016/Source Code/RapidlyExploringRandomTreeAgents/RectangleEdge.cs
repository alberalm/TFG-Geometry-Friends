
namespace GeometryFriendsAgents
{
    public class RectangleEdge
    {
        private RectangleNode _parent;
		private RectangleNode _child;
		private int _action; // Specific to each edge, this represents the action that takes the agent from one state to the other (State AKA node)
		private bool _busy;

		public RectangleEdge (RectangleNode p, RectangleNode c, int a)
		{
			this._parent = p;
			this._child = c;
			this._action = a;
			this._busy = false;
		}

		public void setBusy()
		{
			this._busy = true;
		}

		public void setNotBusy()
		{
			this._busy = false;
		}

		public bool isBusy()
		{
			return this._busy;
		}

		public RectangleNode getParent()
		{
			return this._parent;
		}

		public RectangleNode getChild()
		{
			return this._child;
		}

		public int getAction()
		{
			return this._action;
		}

    }
}
