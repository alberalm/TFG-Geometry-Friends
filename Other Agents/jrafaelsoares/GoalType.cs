using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public enum GoalType
    {
        /*Types:
         *      All - catch all diamonds
         *      FirstPossible - get path to one diamond from which it is possible to return
         *      Return - get path from a position to the current one
         *      Coop - get cooperative diamonds
         */
        All = 0,
        FirstPossible = 1,
        Return = 2,
        Coop = 3,
        HighestSingle = 4
    }
}
