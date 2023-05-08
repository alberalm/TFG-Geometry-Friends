using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    [Serializable]
    public class SegmentState
    {
        public Segment segment;
        public float x;
        public float xVelocity;
        public int[] remainingCollectibles;
        public float y { get { return segment != null ? segment.from.Y : 0; } }
        public PointF Location { get { return new PointF(x, y); } }

        public SegmentState(Segment segment, float x, float xVelocity, int[] remainingCollectibles)
        {
            this.segment = segment;
            this.x = x;
            this.xVelocity = xVelocity;
            this.remainingCollectibles = remainingCollectibles;
            Debug.Assert(remainingCollectibles != null);
        }

        public SegmentState Copy()
        {
            return new SegmentState(segment, x, xVelocity, remainingCollectibles);
        }

        public override string ToString()
        {
            if (remainingCollectibles != null)
                return string.Format("(x:{0} y:{1} xVel:{2} need:{3})", x, y, xVelocity, string.Join(",", remainingCollectibles));
            else
                return string.Format("(x:{0} y:{1} xVel:{2} need:NONE)", x, y, xVelocity);
        }

        public bool Equals(SegmentState other)
        {
            if (ReferenceEquals(null, other)) return false;

            if (other.segment != segment)
                return false;

            if (other.remainingCollectibles.Length != remainingCollectibles.Length)
                return false;

            for (int i = 0; i < remainingCollectibles.Length; i++)
                if (other.remainingCollectibles[i] != remainingCollectibles[i])
                    return false;

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SegmentState);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = segment != null ? segment.GetHashCode() : 777;
                for (int i = 0; i < remainingCollectibles.Length; i++)
                    result = (result * 397) ^ remainingCollectibles[i];
                return result;
            }
        }

        public void RemoveCollectible(int collectible)
        {
            var list = new List<int>(remainingCollectibles);
            list.Remove(collectible);
            remainingCollectibles = list.ToArray();
        }

        public void RemoveCollectibles(int[] collectibles)
        {
            if (collectibles == null || collectibles.Length == 0)
                return;

            var list = new List<int>(remainingCollectibles);
            foreach (var collectible in collectibles)
                list.Remove(collectible);
            remainingCollectibles = list.ToArray();
        }
    }
}
