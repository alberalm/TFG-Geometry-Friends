// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 4.0.30319.1
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------
using System.Collections;


namespace GeometryFriendsAgents
{
	public class State
	{
		private float _velocityX;
		private float _velocityY;
		private float _posX;
		private float _posY;
		private int _error;			// Precision for equals method
		private int _action;
		private ArrayList _collectibles;
		private ArrayList _collectiblesCaught;
		private int _size; // Radius for circle, height for rectangle
		private Obstacles.Obstacle _currentPlatform;
		private Point _point;
		private ArrayList _reachableObstacles;
		private bool[] _visitedPlatforms;
		private bool _performedSpecialMoveRight;
		private bool _performedSpecialMoveLeft;

		public bool[] VisitedPlatforms
		{
			get { return _visitedPlatforms; }
			set { _visitedPlatforms = value; }
		}

		public ArrayList ReachableObstacles
		{
			get { return _reachableObstacles; }
			set { _reachableObstacles = value; }
		}

		public bool SpecialMoveR
		{
			get { return _performedSpecialMoveRight; }
			set { _performedSpecialMoveRight = value; }
		}

		public bool SpecialMoveL
		{
			get { return _performedSpecialMoveLeft; }
			set { _performedSpecialMoveLeft = value; }
		}


		public Point point
		{
			get { return _point; }
			set { _point = value; }
		}
		

		public Obstacles.Obstacle CurrentPlatform
		{
			get { return _currentPlatform; }
            set
            {
                this.point.Platform = value;
                _currentPlatform = value;
            }
		}


		public State (float velX, float velY, float posX, float posY, int action, ArrayList collectibles, ArrayList collectiblesCaughtByPreviousState)
		{
			this._velocityX = velX;
			this._velocityY = velY;
			this._posX = posX;
			this._posY = posY;
			this.setError(5);
			this._action = action;
			this._collectibles = collectibles;
			this._collectiblesCaught = collectiblesCaughtByPreviousState;
			this.point = new Point(posX,posY);
			this.SpecialMoveL = false;
			this.SpecialMoveR = false;
			this.ReachableObstacles = null;
		}

		public void setCollectibles(ArrayList col)
		{
			this._collectibles = col;
		}

		public void caughtCollectible(float posx, float poxy)
		{
			this._collectiblesCaught.Add(posx);
			this._collectiblesCaught.Add(poxy);
		}

		public ArrayList getAllCaughtCollectibles()
		{
			return (ArrayList) this._collectiblesCaught.Clone();
		}

		public float getCaughtCollectible(int i)
		{
			return (float)this._collectiblesCaught[i];
		}

		public int sizeOfCaughtCollectible()
		{
			return this._collectiblesCaught.Count / 2;
		}

		public int getSizeOfAgent()
		{
			return _size;
		}

		public void setSizeOfAgent(int s)
		{
            this.point.Size = s;
			this._size = s;
		}

		public ArrayList getCollectibles()
		{
			ArrayList giveawayList = (ArrayList) this._collectibles.Clone();
			return giveawayList;
		}

		public int numberOfCollectibles()
		{
			return this._collectibles.Count / 2;
		}

		public void setAction(int a)
		{
			this._action = a;
		}

		public int getAction()
		{
			return this._action;
		}

		public void setError(int x)
		{
			this._error = x;
		}

		public int getError()
		{
			return this._error;
		}

		public float getVelocityX()
		{
			return this._velocityX;
		}

        public void setVelocityX(float velocityX)
        {
            this._velocityX = velocityX;
        }

		public float getVelocityY()
		{
			return this._velocityY;
		}

		public float getPosX()
		{
			return this._posX;
		}

		public void setPosX(float x)
		{
			this._posX = x;
		}

		public float getPosY()
		{
			return this._posY;
		}

		public void setPosY(float y)
		{
			this._posY = y;
		}

		// equals -> checks position (with a error parameter)& velocity (for now only checks is the velocities are in the same direction
		public override bool Equals (object obj)
		{
			if( obj == null)
				return false;

			State s = obj as State;
			if( (System.Object)s == null)
				return false;

		//	if( this.isPositive(this._velocityX) == s.isPositive(s.getVelocityX()))

			if ((this.getPosX() + this.getError()) > s.getPosX() && (this.getPosX() - this.getError() < s.getPosX()))
			{
                if (this.getVelocityX() == s.getVelocityX())
				{
                    if (this.getAction() == s.getAction())
                    //if (this.getAction() != 2 || (this.getAction() == 2 && s.getAction() != 2) || (this.getAction() == 2 && s.getAction() != 2))
					{
						if (this.CurrentPlatform.getID() == s.CurrentPlatform.getID())
						{
							if (this.sizeOfCaughtCollectible() == s.sizeOfCaughtCollectible())
							{
								int a = 0;
								foreach (float i in this._collectiblesCaught)
								{
									if (!i.Equals(s._collectiblesCaught[a]))
										return false;
									a++;
								}
								return true;
							}
							else
								return false;
						}
						else
							return false;
					}
                    else
                        return false;
				}
                else
                    return false;
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}

		private bool isPositive(float x)
		{
			if(x > 0)
				return true;
			else
				return false;
		}

	}
}
