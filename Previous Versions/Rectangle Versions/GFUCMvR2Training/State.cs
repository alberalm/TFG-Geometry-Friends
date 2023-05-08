using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public class State
    {
        public int distance_x;
        public int distance_y;
        public int current_velocity_x;
        public int height; // Discretized in units of 2 pixels (our pixels).
        public int hole_width;
        //Width of bigholedrops can be 14 or 15 pixels (our pixels). Rigth now we assume it's 15

        public State(int distance_x, int distance_y, int current_velocity_x, int height, int hole_width = 15)
        {
            if  (hole_width >= 0)
            {
                this.hole_width = hole_width;
                this.distance_x = distance_x;
                this.current_velocity_x = current_velocity_x;
            }
            else
            {
                this.hole_width = -hole_width;
                this.distance_x = -distance_x;
                this.current_velocity_x = -current_velocity_x;
            }
            this.distance_y = distance_y;
            this.height = height;
        }

        public bool IsFinal()
        {
            return distance_y < 0;
        }

        public double Reward()
        {
            if (IsFinal())
            {
                return 100;
            }
            else
            {
                return -1;
            }
        }
        
        public override string ToString()
        {
            return distance_x.ToString() + ";" + distance_y.ToString() + ";"
                + current_velocity_x.ToString() + ";" + height.ToString() + ";"
                + hole_width.ToString();
        }
    }
}
