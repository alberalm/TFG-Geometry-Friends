using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public enum RRTTypes
    {
        //Original
        Original = 0,

        //State selection
        BGT = 1,
        Bias = 2,
        BGTBias = 3,
        BGTAreaBias = 4,
        AreaBias = 5,

        //Action selection
        STP = 6
    }
}
