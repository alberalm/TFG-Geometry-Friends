/***************************************************/
/*     Controller from previous RRT solution       */
/***************************************************/

using GeometryFriends.AI;
using System;

namespace GeometryFriendsAgents
{
    public class CirclePID
    {
        private float Kp; // proportional gain
        private float Ki; // integral gain
        private float Kd; // derivative gain

        private float errorAccumulation; // in order to not having to store all the values and have to sum over them, we simply use the accumulative value of the error
        private float lastErrorCalculation;
        private float timestep;

        public CirclePID()
        {
            this.Kp = 2000; // experimental tuning lead to this values
            this.Ki = 25;
            this.Kd = 0;
            this.errorAccumulation = 0;
            this.lastErrorCalculation = 0;
        }

        public void resetPID()
        {
            this.errorAccumulation = 0;
            this.lastErrorCalculation = 0;
        }

        public Moves calculateAction(float currentX, float desiredX, float currentVelocity, float desiredVelocity, float step)
        {
            //TODO
            //if the agent is going in the right direction 
            //rolling right
            if (currentX < desiredX && currentVelocity > 0 && desiredVelocity > 0) 
            {
                //if it has enough space in between in case of a jump
                if (Math.Abs(currentX - desiredX) >= Math.Abs(currentVelocity - desiredVelocity)/2)
                {
                    if (currentVelocity > desiredVelocity)
                    {
                        return Moves.ROLL_LEFT;
                    }
                    else if (currentVelocity < desiredVelocity)
                    {
                        return Moves.ROLL_RIGHT;
                    }
                    else
                    {
                        return Moves.NO_ACTION;
                    }
                }
                else
                {
                    return Moves.ROLL_LEFT;
                }
                
            }
            //rolling left
            if (currentX > desiredX && currentVelocity < 0 && desiredVelocity < 0)
            {
                if (Math.Abs(currentX - desiredX) >= Math.Abs(currentVelocity - desiredVelocity)/2)
                {
                    if (currentVelocity < desiredVelocity)
                    {
                        return Moves.ROLL_RIGHT;
                    }
                    else if (currentVelocity > desiredVelocity)
                    {
                        return Moves.ROLL_LEFT;
                    }
                    else
                    {
                        return Moves.NO_ACTION;
                    }
                }
                else
                {
                    return Moves.ROLL_RIGHT;
                }
                
            }
            //if the agent is going towards the point but with the opposite direction
            //rolling right
            if (currentX < desiredX && currentVelocity > 0 && desiredVelocity < 0)
            {
                return Moves.ROLL_RIGHT;
            }
            //rolling left
            if (currentX > desiredX && currentVelocity < 0 && desiredVelocity > 0)
            {
                return Moves.ROLL_LEFT;
            }
            //if the agent is going away from the point in the opposite direction
            //rolling right
            if (currentX > desiredX && currentVelocity > 0 && desiredVelocity < 0)
            {
                if (Math.Abs(currentX - desiredX) >= Math.Abs(desiredVelocity/2))
                {
                    return Moves.ROLL_LEFT;
                }
                else
                {
                    return Moves.ROLL_RIGHT;
                }
            }
            //rolling left
            if (currentX < desiredX && currentVelocity < 0 && desiredVelocity > 0)
            {
                if (Math.Abs(currentX - desiredX) >= Math.Abs(desiredVelocity/2))
                {
                    return Moves.ROLL_RIGHT;
                }
                else
                {
                    return Moves.ROLL_LEFT;
                }
            }
            //if the agent is rolling away from the point at the same direction
            //rolling right
            if(currentX > desiredX && currentVelocity > 0 && desiredVelocity > 0)
            {
                return Moves.ROLL_LEFT;
            }
            //rolling left
            if (currentX < desiredX && currentVelocity < 0 && desiredVelocity < 0)
            {
                return Moves.ROLL_RIGHT;
            }

            return Moves.NO_ACTION;
        }

        public Moves calculateAction(float currentX, float currentVelocity, float desiredVelocity, int direction, float step)
        {
            timestep = step;                                            // set the time that has passed between the last calculation and this
            float error = desiredVelocity - currentVelocity;            // calculate error
            float integralValue = integralCalculation(error);           // calculate the integral part
            float derivativeValue = derivativeCalculation(error);       // calculate the derivative part
            float proportionalValue = proportionalCalculation(error);   // calculate the proportional part

            float outDecision = proportionalValue + derivativeValue + integralValue;
            lastErrorCalculation = error;
            errorAccumulation += error;

            if (outDecision > 0) // this means it is above the expected value, as such we need to compensate by using the opposite action
            {
                if (direction == 0)
                {
                    return Moves.ROLL_RIGHT;
                }
                else
                {
                    return Moves.ROLL_LEFT;
                }
            }
            else if (outDecision < 0) // this means it is under the expected value, as such we need to compensate by using the speed up action
            {
                if (direction == 0)
                {
                    return Moves.ROLL_LEFT;
                }
                else
                {
                    return Moves.ROLL_RIGHT;
                }
            }
            else // outDecision == 0 | in case of a 0 value we are at the perfect velocity, as such the best would be to to no action at all, since this isnt possible we simply slow it down by returning the opposite action of the direction
            {
                if (direction == 0)
                {
                    return Moves.ROLL_LEFT;
                }
                else
                {
                    return Moves.ROLL_RIGHT;
                }
            }
        }

        public float integralCalculation(float error)
        {
            return Ki * (errorAccumulation + error) * timestep;
        }
        public float derivativeCalculation(float error)
        {
            return Kd * ((error - lastErrorCalculation) / timestep);
        }
        public float proportionalCalculation(float error)
        {
            return Kp * error; 
        }

    }

}