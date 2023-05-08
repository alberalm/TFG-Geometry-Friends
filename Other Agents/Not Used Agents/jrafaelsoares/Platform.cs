using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public class Platform
    {
        private float posX;
        private float posY;
        private float width;
        private float height;
        private float left;
        private float right;
        private float top;
        private float bottom;
        private PlatformType type;
        private List<int> diamondsOn;

        public Platform(float x, float y, float w, float h, PlatformType t)
        {
            posX = x;
            posY = y;
            width = w;
            height = h;
            left = x - w / 2;
            right = x + w / 2;
            top = y - h / 2;
            bottom = y + h / 2;
            type = t;
            diamondsOn = new List<int>();
        }

        public float getX()
        {
            return posX;
        }

        public float getY()
        {
            return posY;
        }

        public float getWidth()
        {
            return width;
        }

        public float getHeight()
        {
            return height;
        }

        public float getLeft()
        {
            return left;
        }

        public float getRight()
        {
            return right;
        }

        public float getTop()
        {
            return top;
        }

        public float getBottom()
        {
            return bottom;
        }

        public List<int> getDiamondsOn()
        {
            return diamondsOn;
        }

        public void addDiamondOn(int diamond)
        {
            diamondsOn.Add(diamond);
        }

        public PlatformType getType()
        {
            return type;
        }
    }
}
