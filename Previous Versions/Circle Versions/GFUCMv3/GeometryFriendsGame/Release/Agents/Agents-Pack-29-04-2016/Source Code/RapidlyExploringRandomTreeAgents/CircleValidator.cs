using System;

namespace GeometryFriendsAgents
{
	class CircleValidator : Validator
	{
		private int radiusOfCircle = 40;

        public override Boolean validate(RectangleNode newnode, RectangleNode oldnode, InfoDomain domain, Graph graph) { return false; }

		public override Boolean validate(CircleNode newnode, CircleNode oldnode, InfoDomain domain, Graph graph)
		{
			State newstate = newnode.getState();
			State oldstate = oldnode.getState();

			if (outsideOfMap(newstate))
				return false;
			else
			{
				if (isVerticalMovement(oldstate, newstate) || newstate.getAction()==2)
				{
					if (thereIsWallBetweenV(newstate, oldstate, domain))
						return false;
					else
						return true;
				}
				else
				{
					if (thereIsWallBetweenH(newstate, oldstate, domain))
						return false;
					else
						return true;
				}
			}
		}

		protected override Boolean outsideOfMap(State newstate)
		{
			return !(((newstate.getPosX() - radiusOfCircle) >= 40) && ((newstate.getPosX() + radiusOfCircle) <= 1240) && ((newstate.getPosY() - radiusOfCircle) >= 40) && ((newstate.getPosY() + radiusOfCircle) <= 760));
		}

		protected override Boolean isVerticalMovement(State oldstate, State newstate)
		{
			float oldpy = (int)(oldstate.getPosY() - 40) / 8;
			float newpy = (int)(newstate.getPosY() - 40) / 8;

			return oldpy != newpy;
		}

		protected override Boolean thereIsWallBetweenV(State newstate, State oldstate, InfoDomain domain)
		{
			float oldpx = (oldstate.getPosX() - 40) / 8;
			float oldpy = (oldstate.getPosY() - 40) / 8;
			float newpx = (newstate.getPosX() - 40) / 8;
			float newpy = (newstate.getPosY() - 40) / 8;

			int ioldpx = (int)oldpx;
			int inewpx = (int)newpx;
			int ioldpy = (int)oldpy;
			int inewpy = (int)newpy;

			int startx;
			int endx;
			int starty;
			int endy;

			int[,] table = domain.Table;

			//int maxHeight = 300;

            if(newstate.getAction() == 3)
            {
                starty = (int)(newstate.getPosY() - 40 - radiusOfCircle) / 8;
                endy = (int)(oldstate.getPosY() - 40 + radiusOfCircle) / 8;
                startx = (int)(oldstate.getPosX() - 40 - radiusOfCircle) / 8;
                endx = (int)(oldstate.getPosX() - 40 + radiusOfCircle) / 8;
            }
            else
            if (newstate.getAction() == 2)
            {
                float collectibley = 720;
                for (int i = oldstate.sizeOfCaughtCollectible()*2; i < newstate.sizeOfCaughtCollectible()*2; i += 2)
                    if (newstate.getCaughtCollectible(i + 1) < collectibley)
                        collectibley = newstate.getCaughtCollectible(i + 1);

                starty = (int)(collectibley + 32 - 40) / 8;
                if (starty < 0)
                    starty = 0;
                endy = (int)(newstate.getPosY() - 40 + radiusOfCircle) / 8;
                startx = (int)(newstate.getPosX() - 40 - radiusOfCircle) / 8;
                endx = (int)(newstate.getPosX() - 40 + radiusOfCircle) / 8;
            }
            else
            {
                if (inewpy > ioldpy)
                {
                    starty = (int)(oldstate.getPosY() - 40 - radiusOfCircle) / 8;
                    endy = (int)(newstate.getPosY() - 40 + radiusOfCircle) / 8;
                }
                else
                {
                    starty = (int)(newstate.getPosY() - 40 - radiusOfCircle) / 8;
                    endy = (int)(oldstate.getPosY() - 40 + radiusOfCircle) / 8;
                }
                startx = (int)(newstate.getPosX() - 40 - radiusOfCircle) / 8;
                endx = (int)(newstate.getPosX() - 40 + radiusOfCircle) / 8;
            }

            for (; starty < /*<=?*/ endy; starty++)
            {
                for (int iterx = startx; iterx < /*<=?*/ endx; iterx++)
                {
                    if (table[starty, iterx] == 1 || table[starty, iterx] == 3)
                        return true;
                }
            }
			return false;
		}

		protected override Boolean thereIsWallBetweenH(State newstate, State oldstate, InfoDomain domain)
		{
			float oldpx = (oldstate.getPosX() - 40) / 8;
			float oldpy = (oldstate.getPosY() - 40) / 8;
			float newpx = (newstate.getPosX() - 40) / 8;
			float newpy = (newstate.getPosY() - 40) / 8;

			int ioldpx = (int)oldpx;
			int inewpx = (int)newpx;
			int ioldpy = (int)oldpy;
			int inewpy = (int)newpy;

			int startx;
			int endx;
			int starty;
			int endy;

			int[,] table = domain.Table;

			// If horizontal movement
			if (inewpy == ioldpy)
			{
				if (inewpx > ioldpx)
				{
					startx = (int)(oldstate.getPosX() - 40 + 40) / 8;
					endx = (int)(newstate.getPosX() - 40 + 40) / 8; ;
				}
				else
				{
					startx = (int)(newstate.getPosX() - 40 - 40) / 8;
					endx = (int)(oldstate.getPosX() - 40 - 40) / 8;
				}

				starty = (int)(oldstate.getPosY() - 40 - 40) / 8;
				endy = (int)(oldstate.getPosY() - 40 + 40) / 8;

				for (; starty < /*<=?*/ endy; starty++)
				{
					for (; startx < /*<=?*/ endx; startx++)
					{
						if (table[starty, startx] == 1 || table[starty, startx] == 3)
							return true;
					}
				}
				return false;

			}
			return false;
		}
	}
}
