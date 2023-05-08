
namespace GeometryFriendsAgents
{
    public class CircleWalkingPoint
    {
        private float _x;
		private float _y;
		private float _v;
        private int _collectibles;
        private Obstacles.Obstacle _obs;

		public float XCoordinate
		{
			get { return _x; }
			set { _x = value; }
		}

		public float YCoordinate
		{
			get { return _y; }
			set { _y = value; }
		}

		public float Velocity
		{
			get { return _v; }
			set { _v = value; }
		}

        public int Collectibles
        {
            get { return _collectibles; }
            set { _collectibles = value; }
        }

        public Obstacles.Obstacle Obs
        {
            get { return _obs; }
            set { _obs = value; }
        }

		public CircleWalkingPoint(float x, float y, float v, int numberOfCollectibles, Obstacles.Obstacle obs)
		{
			XCoordinate = x;
			YCoordinate = y;
			Velocity = v;
            Collectibles = numberOfCollectibles;
            Obs = obs;
		}
    }
}
