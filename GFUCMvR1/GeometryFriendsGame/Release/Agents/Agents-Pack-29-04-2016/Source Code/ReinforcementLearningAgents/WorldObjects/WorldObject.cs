
namespace GeometryFriendsAgents.WorldObjects {
   abstract class WorldObject {

        protected int _xPos;

        public int xPos {
            get { return _xPos; }
        }

        protected int _yPos;

        public int yPos {
            get { return _yPos; }
            set { _yPos = value; }
        }

    }
}
