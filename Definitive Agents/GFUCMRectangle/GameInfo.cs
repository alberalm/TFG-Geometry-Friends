namespace GeometryFriendsAgents
{
    class GameInfo
    {
        public const int CIRCLE_RADIUS = 40;

        public const int VERTICAL_RECTANGLE_HEIGHT = 192;
        public const int HORIZONTAL_RECTANGLE_HEIGHT = 52;
        public const int RECTANGLE_AREA = VERTICAL_RECTANGLE_HEIGHT * HORIZONTAL_RECTANGLE_HEIGHT;
        public const int SQUARE_HEIGHT = 100;

        public const int MAX_VELOCITYX = 200;

        public const float JUMP_VELOCITYY = 437f;
        public const float FALL_VELOCITYY = 0;
        public const float GRAVITY = 299.1f;
        public const float CIRCLE_ACCELERATION = 118f;
        public const float RECTANGLE_ACCELERATION = 150f;

        public const int LEVEL_MAP_WIDTH = 160;
        public const int LEVEL_MAP_HEIGHT = 100;
        public const int LEVEL_WIDTH = 1272;
        public const int LEVEL_HEIGHT = 776;
        public const int PIXEL_LENGTH = 8;

        public const float ALPHA = 0.1f; // Learning rate
        public const float GAMMA = 0.95f; // Discount factor
        public const float EPSILON = 0.0f;

        public const int MAX_DISTANCE_CIRCLE = 25;
        public const int MAX_DISTANCE_RECTANGLE = 5;
        public const int NUM_VELOCITIES_PHISICS = 13;
        public const int NUM_VELOCITIES_QLEARNING = 7;
        public const int NUM_VELOCITIES_RECTANGLE = 10;
        public const int VELOCITY_STEP_RECTANGLE = 50;
        public const int VELOCITY_STEP_PHISICS = 15;
        public const int VELOCITY_STEP_QLEARNING = 20;
        public const bool PHYSICS = true;
        public const int TESTING_VELOCITY = 200;

        public const string Q_PATH1 = @"Q_table_";
        public const string Q_PATH_RECT = @"Q_table_R_";
        public const string Q_PATH_EXTENSION = @".csv";
        public const int TARGET_POINT_ERROR = 2; 
        public const int ERROR = 1; 

        public const double SEMI_COLLECTIBLE_HEIGHT = 32;
        public static int[] COLLECTIBLE_SIZE = { 1, 2, 3, 3, 2, 1 }; // Divided by 2
        public static int[] CIRCLE_SIZE = { 3, 4, 5, 5, 5, 5, 5, 5, 4, 3 }; // Divided by 2
        public static int[] COLLECTIBLE_INTERSECTION = { 1, 2, 3, 4, 5, 5, 4, 3, 2, 1 }; // Divided by 2
        public static RectangleShape.Shape[] SHAPES = { RectangleShape.Shape.SQUARE, RectangleShape.Shape.HORIZONTAL, RectangleShape.Shape.VERTICAL };
    }
}

