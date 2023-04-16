using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public class Platform : IComparable<Platform>
    {
        public int id;
        public int yTop;
        public int leftEdge;
        public int rightEdge;
        public bool[] shapes = { false, false, false };
        public List<MoveInformation> moveInfoList;
        public bool real = true;

        public Platform()
        {
        }

        public Platform(Platform other)
        {
            this.id = other.id;
            this.yTop = other.yTop;
            this.leftEdge = other.leftEdge;
            this.rightEdge = other.rightEdge;
            this.shapes = other.shapes; 
            this.moveInfoList = new List<MoveInformation> (other.moveInfoList);
            this.real = other.real;
        }

        public Platform(int id, int yTop, int leftEdge, int rightEdge, List<MoveInformation> moveInfoList)
        {
            this.id = id;
            this.yTop = yTop;
            this.leftEdge = leftEdge;
            this.rightEdge = rightEdge;
            this.moveInfoList = moveInfoList;
        }
        public Platform(int id, int yTop, int leftEdge, int rightEdge, List<MoveInformation> moveInfoList, RectangleShape.Shape s)
        {
            this.id = id;
            this.yTop = yTop;
            this.leftEdge = leftEdge;
            this.rightEdge = rightEdge;
            this.moveInfoList = moveInfoList;
            shapes[(int)s] = true;
        }

        public Platform(int id)
        {
            this.id = id;
            this.yTop = 0;
            this.leftEdge = 0;
            this.rightEdge = 0;
            this.moveInfoList = new List<MoveInformation>();
        }

        public void CombineShapes(bool[] others)
        {
            for (int i=0; i<shapes.Length; i++)
            {
                shapes[i] = shapes[i] || others[i];
            }
        }

        public bool ShapesAreEqual(Platform other)
        {
            foreach (RectangleShape.Shape s in GameInfo.SHAPES)
            {
                if((!shapes[(int)s] && other.shapes[(int)s]) || (shapes[(int)s] && !other.shapes[(int)s]))
                {
                    return false;
                }
            }
            return true;
        }

        public List<int> ReachableCollectiblesLandingInThisPlatformWithoutCooperation()
        {
            List<int> rc = new List<int>();
            foreach (MoveInformation m in moveInfoList)
            {
                if (m.departurePlatform.id == m.landingPlatform.id && m.departurePlatform.real)
                {
                    foreach (int d in m.diamondsCollected)
                    {
                        if (!rc.Contains(d))
                        {
                            rc.Add(d);
                        }
                    }
                }
            }
            return rc;
        }

        public List<int> ReachableCollectiblesLandingInThisPlatformWithCooperation()
        {
            List<int> rc = new List<int>();
            foreach (MoveInformation m in moveInfoList)
            {
                if (m.departurePlatform.id == m.landingPlatform.id && !m.departurePlatform.real)
                {
                    foreach (int d in m.diamondsCollected)
                    {
                        if (!rc.Contains(d))
                        {
                            rc.Add(d);
                        }
                    }
                }
            }
            return rc;
        }

        public List<int> ReachableCollectiblesLandingInOtherPlatform()
        {
            List<int> rc = new List<int>();
            foreach (MoveInformation m in moveInfoList)
            {
                if (m.departurePlatform.id != m.landingPlatform.id)
                {
                    foreach (int d in m.diamondsCollected)
                    {
                        if (!rc.Contains(d))
                        {
                            rc.Add(d);
                        }
                    }
                }
            }
            return rc;
        }

        // Returns 1 if this is less than other, -1 if other is less than this, 0 if equal
        public int CompareTo(Platform other)
        {
            if (this.yTop == other.yTop)
            {
                return this.leftEdge.CompareTo(other.leftEdge);
            }
            return yTop.CompareTo(other.yTop);
        }
    }
}
