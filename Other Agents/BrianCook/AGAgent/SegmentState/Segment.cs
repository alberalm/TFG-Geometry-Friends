using System;
using System.Drawing;

namespace GeometryFriendsAgents
{
    [Serializable]
    public class Segment 
    {
        public PointF from;
        public PointF to;

        public float y { get { return from.Y; } }
        public float width { get { return to.X - from.X; } }
        public PointF midpoint { get { return new PointF(from.X + (to.X - from.X) / 2, to.Y); } }

        public Segment()
        {
        }

        public Segment(PointF from, PointF to)
        {
            this.from = from;
            this.to = to;
        }

        public override string ToString()
        {
            return string.Format("[X={0}..{1} Y={2}]", (int)from.X, (int)to.X, (int)from.Y);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (int)from.X;
                result = (result * 397) ^ (int)from.Y;
                result = (result * 397) ^ (int)to.X;
                result = (result * 397) ^ (int)to.Y;
                return result;
            }
        }

        public bool IsNull { get { return from.X == 0 && to.X == 0; } }
    }
}
