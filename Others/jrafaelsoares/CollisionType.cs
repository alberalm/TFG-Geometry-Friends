using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public enum CollisionType
    {
        /*Types:
         *      All - catch all diamonds
         *      FirstPossible - get path to one diamond from which it is possible to return
         *      Return - get path from a position to the current one
         *      Coop - get cooperative diamonds
         */
        TOP = 0,
        BOTTOM = 1,
        LEFT = 2,
        RIGHT = 3,
        NONE = 4
    }
}
