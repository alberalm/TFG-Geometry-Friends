﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class GameInfo
    {
        public const int CIRCLE_RADIUS = 40;

        //public const int SQUARE_RADIUS = 50; //71
        //public const int RECTANGLE_AREA = 10000;

        //public const int SQUARE_HEIGHT = 100;
        //public const int VERTICAL_RECTANGLE_HEIGHT = 200;
        //public const int HORIZONTAL_RECTANGLE_HEIGHT = 50;

        public const int MAX_VELOCITYX = 200;
        public const int MAX_VELOCITYY = 20;

        public const float JUMP_VELOCITYY = 437f;
        public const float FALL_VELOCITYY = 0;
        public const float GRAVITY = 299.1f;
        public const float ACCELERATION = 118f;

        public const int LEVEL_MAP_WIDTH = 160;
        public const int LEVEL_MAP_HEIGHT = 100;
        public const int LEVEL_WIDTH = 1272;
        public const int LEVEL_HEIGHT = 776;
        public const int PIXEL_LENGTH = 8;
        public const float RESTITUTION = 0*75f * (JUMP_VELOCITYY - GRAVITY) / (JUMP_VELOCITYY - 0.72f * GRAVITY);

        public const float ALPHA = 0.1f;//Learning rate
        public const float GAMMA = 0.95f;//Discount factor
        public const float EPSILON = 0.0f;

        public const int MAX_DISTANCE = 25;
        public const int NUM_VELOCITIES_PHISICS = 13;
        public const int NUM_VELOCITIES_QLEARNING = 7;
        public const int VELOCITY_STEP_PHISICS = 15;
        public const int VELOCITY_STEP_QLEARNING = 20;
        public const bool PHYSICS = true;

        public const string Q_PATH1 = @"Q_table_";
        public const string Q_PATH2 = @".csv";
        public const int TARGET_POINT_ERROR = 2;//Cambiado
        public const int ERROR = 1;//Cambiado
    }
}

