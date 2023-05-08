using GeometryFriendsAgents.WorldObjects;

namespace GeometryFriendsAgents.GameStates {
    abstract class GameState {

        protected float _xPos;

        public float xPos {
            get { return _xPos; }
        }

        protected float _yPos;

        public float yPos {
            get { return _yPos; }
        }
        
        public abstract string GetStateId();
           

        public abstract double GetStateValue();

        public abstract double GetDistanceCollectible();

        public abstract int GetPercentageCollectibleCaught();

        public abstract Collectible GetClosestCollectible();

        public abstract int GetNumberCollectiblesCaught();
    }
}
