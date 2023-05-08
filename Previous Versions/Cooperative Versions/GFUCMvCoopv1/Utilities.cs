using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class Utilities
    {
        public static bool Contained<T>(List<T> l1, List<T> l2) //returns if l1 is contained in l2
        {
            foreach (T e in l1)
            {
                if (!l2.Contains(e))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
