using System;

namespace GeometryFriendsAgents
{
    class RectangleActions : Actions
    {
        private static Random rndAction;

		public RectangleActions () : base(5)
		{
			rndAction = new Random();
		}

		public override int getRandomAction ()
		{
			return rndAction.Next(this.getNrActions());
		}
    }
}
