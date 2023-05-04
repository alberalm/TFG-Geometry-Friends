
namespace GeometryFriendsAgents
{
    public class RectangleWalkingPoint
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

        private float distance_to_morph;

        public float distanceToMorph
        {
            get { return distance_to_morph; }
            set { distance_to_morph = value; }
        }

        private float error_size;

        public float errorSize
        {
            get { return error_size; }
            set { error_size = value; }
        }

        private float min_velocity;

        public float minVelocity
        {
            get { return min_velocity; }
            set { min_velocity = value; }
        }

        private bool y_condition;

        public bool yCondition
        {
            get { return y_condition; }
            set { y_condition = value; }
        }
        

        public RectangleWalkingPoint(float x, float y, float v, int numberOfCollectibles, Obstacles.Obstacle obs, float distance_to_morph, float error_size, float min_velocity, bool ycondition)
        {
            XCoordinate = x;
            YCoordinate = y;
            Velocity = v;
            Collectibles = numberOfCollectibles;
            Obs = obs;
            distanceToMorph = distance_to_morph;
            errorSize = error_size;
            minVelocity = min_velocity;
            yCondition = ycondition;
        }
    }
}
