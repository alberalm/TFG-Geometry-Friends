// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 4.0.30319.1
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------
namespace GeometryFriendsAgents
{
	abstract public class Actions
	{
		private int _numberOfActions;

		public Actions ()
		{
			_numberOfActions = 2;
		}

		public Actions (int n)
		{
			_numberOfActions = n;
		}

		abstract public int getRandomAction();
		public int getNrActions()
		{
			return this._numberOfActions;
		}


	}
}
