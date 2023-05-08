using GeometryFriends.AI;
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
			this.Kp = 5; // experimental tuning lead to this values
			this.Ki = 3;
			this.Kd = 3;
			this.errorAccumulation = 0;
			this.lastErrorCalculation = 0;
		}

		public Moves calculateAction(float currentX, float currentVelocity, float desiredVelocity, int direction, float step)
		{
			timestep = step;											// set the time that has passed between the last calculation and this
			float error = desiredVelocity - currentVelocity;			// calculate error
			float integralValue = integralCalculation(error);			// calculate the integral part
			float derivativeValue = derivativeCalculation(error);		// calculate the derivative part
			float proportionalValue = proportionalCalculation(error);	// calculate the proportional part

			float outDecision = proportionalValue + derivativeValue + integralValue;
			lastErrorCalculation = error;
			errorAccumulation += error;

			if ( outDecision > 0) // this means it is above the expected value, as such we need to compensate by using the opposite action
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
			else if( outDecision < 0) // this means it is under the expected value, as such we need to compensate by using the speed up action
			{
				if(direction == 0)
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
				if(direction == 0)
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
			return Kd * ( (error - lastErrorCalculation)/timestep );
		}
		public float proportionalCalculation(float error)
		{
			return Kp * error;
		}

	}

}