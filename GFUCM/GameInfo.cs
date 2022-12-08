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

        public const int LEVEL_MAP_WIDTH = 160;
        public const int LEVEL_MAP_HEIGHT = 100;
        public const int LEVEL_WIDTH = 1272;
        public const int LEVEL_HEIGHT = 776;
        public const int PIXEL_LENGTH = 8;
        public const float VERTICAL_COLLISION_COEFFICIENT = (0.82f * GRAVITY - 0.8f * JUMP_VELOCITYY) / (JUMP_VELOCITYY - 0.8f * GRAVITY);
    }
}
