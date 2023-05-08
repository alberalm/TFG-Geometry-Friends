
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public class RectangleShape
    {
        public enum Shape
        {
            SQUARE, HORIZONTAL, VERTICAL
        }

        public static int height(Shape shape) // Check if 100/8 and 50/8 are problematic
        {
            switch (shape)
            {
                case Shape.SQUARE:
                    return GameInfo.SQUARE_HEIGHT / GameInfo.PIXEL_LENGTH;
                case Shape.HORIZONTAL:
                    return GameInfo.HORIZONTAL_RECTANGLE_HEIGHT / GameInfo.PIXEL_LENGTH;
                case Shape.VERTICAL:
                    return GameInfo.VERTICAL_RECTANGLE_HEIGHT / GameInfo.PIXEL_LENGTH;
            }
            return 0;
        }

        public static int width(Shape shape)
        {
            switch (shape)
            {
                case Shape.SQUARE:
                    return GameInfo.SQUARE_HEIGHT / GameInfo.PIXEL_LENGTH;
                case Shape.HORIZONTAL:
                    return GameInfo.VERTICAL_RECTANGLE_HEIGHT / GameInfo.PIXEL_LENGTH;
                case Shape.VERTICAL:
                    return GameInfo.HORIZONTAL_RECTANGLE_HEIGHT / GameInfo.PIXEL_LENGTH;
            }
            return 0;
        }

        public static float fheight(Shape shape)
        {
            switch (shape)
            {
                case Shape.SQUARE:
                    return GameInfo.SQUARE_HEIGHT;
                case Shape.HORIZONTAL:
                    return GameInfo.HORIZONTAL_RECTANGLE_HEIGHT;
                case Shape.VERTICAL:
                    return GameInfo.VERTICAL_RECTANGLE_HEIGHT;
            }
            return 0;
        }

        public static float fwidth(Shape shape)
        {
            switch (shape)
            {
                case Shape.SQUARE:
                    return GameInfo.SQUARE_HEIGHT;
                case Shape.HORIZONTAL:
                    return GameInfo.VERTICAL_RECTANGLE_HEIGHT;
                case Shape.VERTICAL:
                    return GameInfo.HORIZONTAL_RECTANGLE_HEIGHT;
            }
            return 0;
        }

        // Returns 1 is first is better, -1 if second is better, 0 if same
        public static int CompareShapes(Shape first, Shape second)
        {
            if(first == second)
            {
                return 0;
            }
            if(first == Shape.HORIZONTAL)
            {
                return 1;
            }
            else if (second == Shape.HORIZONTAL)
            {
                return -1;
            }
            else if(first == Shape.SQUARE)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        public static Shape GetShape(RectangleRepresentation rI)
        {
            if(rI.Height > (GameInfo.SQUARE_HEIGHT + GameInfo.VERTICAL_RECTANGLE_HEIGHT) / 2)
            {
                return Shape.VERTICAL;
            }
            else if (rI.Height < (GameInfo.SQUARE_HEIGHT + GameInfo.HORIZONTAL_RECTANGLE_HEIGHT) / 2)
            {
                return Shape.HORIZONTAL;
            }
            else
            {
                return Shape.SQUARE;
            }
        }
    }
}
