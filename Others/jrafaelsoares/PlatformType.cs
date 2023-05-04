using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public enum PlatformType
    {
        /*Types:
         *      All - catch all diamonds
         *      FirstPossible - get path to one diamond from which it is possible to return
         *      Return - get path from a position to the current one
         *      Coop - get cooperative diamonds
         */
        Black = 0,
        Yellow = 1,
        Green= 2,
    }
}
