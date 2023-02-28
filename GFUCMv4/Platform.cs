﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class Platform : IComparable<Platform>
    {
        public int id;
        public int yTop;
        public int leftEdge;
        public int rightEdge;
        public List<MoveInformation> moveInfoList;

        public Platform()
        {
        }

        public Platform(int id, int yTop, int leftEdge, int rightEdge, List<MoveInformation> moveInfoList)
        {
            this.id = id;
            this.yTop = yTop;
            this.leftEdge = leftEdge;
            this.rightEdge = rightEdge;
            this.moveInfoList = moveInfoList;
        }

        public Platform(int id)
        {
            this.id = id;
            this.yTop = 0;
            this.leftEdge = 0;
            this.rightEdge = 0;
            this.moveInfoList = null;
        }

        public List<int> ReachableCollectiblesLandingInThisPlatform()
        {
            List<int> rc = new List<int>();
            foreach (MoveInformation m in moveInfoList)
            {
                if (m.departurePlatform.id == m.landingPlatform.id)
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