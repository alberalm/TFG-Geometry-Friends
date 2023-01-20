using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class State
    {
        public int distance;
        public int current_velocity;
        public int target_velocity;

        public State(int distance, int current_velocity, int target_velocity)
        {
            this.distance = distance;
            this.current_velocity = current_velocity;
            this.target_velocity = target_velocity;
        }

        public State(string state)
        {
            string[] array = state.Split(',');
            
            distance = int.Parse(array[0]);
            current_velocity = int.Parse(array[1]);
            target_velocity = int.Parse(array[2]);
        }


        public bool IsFinal()
        {
            return Math.Abs(distance) <= 2 && Math.Abs(current_velocity - target_velocity) <= 2;
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
        public string toString()
        {
            return distance.ToString() + "," + current_velocity.ToString() + "," + target_velocity.ToString();
        }
    }
}
