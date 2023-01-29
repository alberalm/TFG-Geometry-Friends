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
        public const float ACCELERATION = 120f;

        public const int LEVEL_MAP_WIDTH = 160;
        public const int LEVEL_MAP_HEIGHT = 100;
        public const int LEVEL_WIDTH = 1272;
        public const int LEVEL_HEIGHT = 776;
        public const int PIXEL_LENGTH = 8;
        public const float RESTITUTION = 0*75f * (JUMP_VELOCITYY - GRAVITY) / (JUMP_VELOCITYY - 0.72f * GRAVITY);

        public const float ALPHA = 0.1f;//Learning rate
        public const float GAMMA = 0.95f;//Discount factor
        public const float EPSILON = 0f;

        public const int MAX_DISTANCE = 25;
        public const int NUM_VELOCITIES = 10;
        public const int VELOCITY_STEP = 20;

        public const int LEARNING_VELOCITY = 100;
        public const string Q_PATH = @"Q_table_100.csv";

        public const int UPDATE_FRECUENCY_MS = 150;
        public const int TIMEJUMPING_MS = 250;
        public const int TIMEROLLING_MS = 5500;
        public const int SIMULATION_SPEED = 1;
        public const int UPDATE_TABLE_MS = 60000;
        public const int TARGET_POINT_ERROR = 1;
    }
}
