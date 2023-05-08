using System;

namespace GeometryFriendsAgents
{
	class RectangleValidator : Validator
	{

		int topy;
		int topx;
		bool emptyBottom = false;

        public override Boolean validate(CircleNode newnode, CircleNode oldnode, InfoDomain domain, Graph graph) { return false; }

		public override Boolean validate(RectangleNode newnode, RectangleNode oldnode, InfoDomain domain, Graph graph)
		{
			State newstate = newnode.getState();
			State oldstate = oldnode.getState();

            // The 3rd and 4th action were already verified
			if (newnode.getState().getAction() == 3 || newnode.getState().getAction() == 4)
				return true;

			if (outsideOfMap(newstate))
				return false;
			else
			{
				if (isVerticalMovement(oldstate, newstate) || newstate.getAction()==2) //se foi gap nao fazer isto
				{
					if (thereIsWallBetweenV(newstate, oldstate, domain))
						return false;
					else
						return true;
				}
				else
				{
					if (thereIsWallBetweenH(newstate, oldstate, domain))
					{
						if (emptyBottom)
						{
							oldnode._busyActions[3] = false;
                            emptyBottom = false;
                            if (newstate.getVelocityX() == 0)
                                oldnode._busyMorphLeft = false;
                            else
                                oldnode._busyMorphRight = false;
                            //may need to put a turning point.
						}
						if (canPassOver(newstate,oldstate,domain))
						{
							oldnode._busyActions[4] = false;
                            if (newstate.getVelocityX() == 0)
                                oldnode._busyTiltLeft = false;
                            else
                                oldnode._busyTiltRight = false;
                            //may need to put a turning point.
						}

						if ((!oldnode._busyActions[3] || !oldnode._busyActions[4]))
							if( !graph.NonBusyNodes.Contains(oldnode))
							graph.NonBusyNodes.Add(oldnode);
 
						return false;
					}
					else
					{
						return true;
					}

				}
			}
		}

		protected override Boolean outsideOfMap(State newstate)
		{
			int halfheight = newstate.getSizeOfAgent() / 2;
			int halfwidth = getWidth(newstate.getSizeOfAgent()) / 2;
			return !(((newstate.getPosX() - halfwidth) >= 40) && ((newstate.getPosX() + halfwidth) <= 1240) && ((newstate.getPosY() - halfheight) >= 40) && ((newstate.getPosY() + halfheight) <= 760));
		}

		protected override Boolean isVerticalMovement(State oldstate, State newstate)
		{
			int oldpy = (int) oldstate.getPosY();
			int newpy = (int) newstate.getPosY();
            int oldsize = oldstate.getSizeOfAgent();
            int newsize = newstate.getSizeOfAgent();
            int dify = Math.Abs(newpy - oldpy);
            int difsize = Math.Abs(newsize - oldsize);

            if (dify > difsize/2)
                return true;
            else
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
			int midy;
			int iterx;

			bool walltop = false;
			bool wallbottom = false;

			int halfheight = newstate.getSizeOfAgent()/2;
            int oldhalfwidth = getWidth(oldstate.getSizeOfAgent())/2;
			int halfwidth = getWidth(newstate.getSizeOfAgent())/2;

			int[,] table = domain.Table;

			if (inewpx > ioldpx)
			{
				startx = (int)(oldstate.getPosX() - 40 + oldhalfwidth) / 8;
				endx = (int)(newstate.getPosX() - 40 + halfwidth) / 8; ;
			}
			else
			{
				startx = (int)(newstate.getPosX() - 40 - halfwidth) / 8;
				endx = (int)(oldstate.getPosX() - 40 - oldhalfwidth) / 8;
			}

			starty = (int)(oldstate.getPosY() - 40 - halfheight) / 8;
			endy = (int)(oldstate.getPosY() - 40 + halfheight) / 8;

			midy = endy - 6;
			
			for (; starty < /*<=?*/ midy; starty++)
			{
				iterx = startx;
				for (; iterx < /*<=?*/ endx; iterx++)
				{
					if (table[starty, iterx] == 1 || table[starty, iterx] == 2)
					{
						topy = starty;
						topx = iterx;
						walltop = true;
						break;
					}       
				}
				if (walltop)
					break;
			}

			starty = midy;

			for (; starty < /*<=?*/ endy; starty++)
			{
				iterx = startx;
				for (; iterx < /*<=?*/ endx; iterx++)
				{
					if (table[starty, iterx] == 1 || table[starty, iterx] == 2)
					{
						if (!walltop)
						{
							topy = starty;
							topx = iterx;
						}
						wallbottom = true;
						break;
					}
				}
				if (wallbottom)
					break;
			}

			if (!walltop && !wallbottom)
				return false;
			else
			{
				if (walltop && !wallbottom)
					emptyBottom = true;
				return true;
			}
		   
		}

		private int getWidth(int size)
		{
			return (96 * 96) / size;
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
			int iterx;

            int halfheight = newstate.getSizeOfAgent() / 2;
            int halfwidth = getWidth(newstate.getSizeOfAgent()) / 2;

			if (newstate.getAction() == 2)
			{
                float newx = newstate.getPosX();
                float collectibley = 720;
                for(int i = oldstate.sizeOfCaughtCollectible()*2; i<newstate.sizeOfCaughtCollectible()*2;i+=2)
                    if(newstate.getCaughtCollectible(i + 1) < collectibley)
                    {
                        collectibley = newstate.getCaughtCollectible(i + 1);
                        newx = newstate.getCaughtCollectible(i);
                    }

                starty = (int)(collectibley + 32 - 40) / 8;
                if (starty < 0)
                    starty = 0;
                int height = (int) (newstate.CurrentPlatform.maxHeight() - collectibley - 32);
                halfwidth = getWidth(height)/2;
                endy = (int)(newstate.getPosY() - 40 + oldstate.getSizeOfAgent() / 2) / 8;
                startx = (int)(newstate.getPosX() - 40 - halfwidth) / 8;
                endx = (int)(newstate.getPosX() - 40 + halfwidth) / 8;
                //
                newstate.point.Size = (int) (newstate.CurrentPlatform.maxHeight() - collectibley - 32);
                newstate.setSizeOfAgent(newstate.point.Size);
                newstate.setPosY(newstate.CurrentPlatform.maxHeight() - newstate.getSizeOfAgent() / 2);
                //
                newstate.point.x = newx;
			}
            else
            {
                if (inewpy > ioldpy)
                {
                    starty = (int)(oldstate.getPosY() - 40 - halfheight) / 8;
                    endy = (int)(newstate.getPosY() - 40 + halfheight) / 8;
                }
                else
                {
                    starty = (int)(newstate.getPosY() - 40 - halfheight) / 8;
                    endy = (int)(oldstate.getPosY() - 40 + halfheight) / 8;
                }
                startx = (int)(newstate.getPosX() - 40 - halfwidth) / 8;
                endx = (int)(newstate.getPosX() - 40 + halfwidth) / 8;
            }

			int[,] table = domain.Table;

			for (; starty < /*<=?*/ endy; starty++)
			{
				iterx = startx;
				for (; iterx < /*<=?*/ endx; iterx++)
				{
					if (table[starty, iterx] == 1 || table[starty, iterx] == 2)
					{
						return true;
					}
				}
			}

			return false;
        }

		protected Boolean canPassOver(State newstate, State oldstate, InfoDomain domain)
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
			int iterx;

            if (topy <= ((newstate.CurrentPlatform.maxHeight()-40)/8) - 12)
                return false;

			int halfheight = newstate.getSizeOfAgent() / 2;
			int halfwidth = getWidth(newstate.getSizeOfAgent()) / 2;

			int[,] table = domain.Table;

			startx = topx - 12; // 12 = NORMALSIZE / 8
			endx = topx + 12;


			starty = topy - 13;
			endy = topy;

			if (starty < 0 || endy >= 90 || startx < 0 || endx >= 150)
				return false;

			for (; starty < /*<=?*/ endy; starty++)
			{
				iterx = startx;
				for (; iterx < /*<=?*/ endx; iterx++)
				{
					if (table[starty, iterx] == 1 || table[starty, iterx] == 2)
					{
						return false;
					}
				}
			}

			return true;


		}

	}
}
