using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    class ActionSelector
    {
        private const int NUM_POSSIBLE_MOVES = 2;
        private const int ACCELERATE = 0;
        private const int DEACCELERATE = 1;

        private const int DISCRETIZATION_VELOCITYX = 10;
        private const int DISCRETIZATION_DISTANCEX = 4;

        private const int MAX_DISTANCEX = 200;

        private const int NUM_STATE = 4000;
        private const int NUM_TARGET_VELOCITYX = GameInfo.MAX_VELOCITYX / (DISCRETIZATION_VELOCITYX * 2);

        private const int NUM_ROW_QMAP = NUM_STATE;
        private const int NUM_COLUMN_QMAP = NUM_POSSIBLE_MOVES * NUM_TARGET_VELOCITYX;

        private float[,] Qmap;   

        public ActionSelector()
        {
            Qmap = Utilities.ReadCsvFile(NUM_ROW_QMAP, NUM_COLUMN_QMAP, "Agents\\Qmap.csv");
        }


        public bool IsGoal(CircleRepresentation cI, int targetPointX, int targetVelocityX, bool rightMove)
        {        
            float distanceX = rightMove ? cI.X - targetPointX : targetPointX - cI.X;

            if (-DISCRETIZATION_DISTANCEX * 2 < distanceX && distanceX <= 0)
            {
                float relativeVelocityX = rightMove ? cI.VelocityX : -cI.VelocityX;

                if (targetVelocityX == 0)
                {
                    if (targetVelocityX - DISCRETIZATION_VELOCITYX <= relativeVelocityX && relativeVelocityX < targetVelocityX + DISCRETIZATION_VELOCITYX)
                    {
                        return true;
                    }
                }
                else
                {
                    if (targetVelocityX <= relativeVelocityX && relativeVelocityX < targetVelocityX + DISCRETIZATION_VELOCITYX * 2)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsGoal(RectangleRepresentation rI, int targetPointX, int targetVelocityX, bool rightMove)
        {
            float distanceX = rightMove ? rI.X - targetPointX : targetPointX - rI.X;

            if (-DISCRETIZATION_DISTANCEX * 2 < distanceX && distanceX <= 0)
            {
                float relativeVelocityX = rightMove ? rI.VelocityX : -rI.VelocityX;

                if (targetVelocityX == 0)
                {
                    if (targetVelocityX - DISCRETIZATION_VELOCITYX <= relativeVelocityX && relativeVelocityX < targetVelocityX + DISCRETIZATION_VELOCITYX)
                    {
                        return true;
                    }
                }
                else
                {
                    if (targetVelocityX <= relativeVelocityX && relativeVelocityX < targetVelocityX + DISCRETIZATION_VELOCITYX * 2)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public Moves GetCurrentAction(CircleRepresentation cI, int targetPointX, int targetVelocityX, bool rightMove)
        {
            int stateNum = GetStateNum(cI, targetPointX, rightMove);

            int currentActionNum;

            float distanceX = rightMove ? cI.X - targetPointX : targetPointX - cI.X;

            if (distanceX <= -MAX_DISTANCEX)
            {
                currentActionNum = ACCELERATE;
            }
            else if (distanceX >= MAX_DISTANCEX)
            {
                currentActionNum = DEACCELERATE;
            }
            else
            {
                currentActionNum = GetOptimalActionNum(stateNum, targetVelocityX);
            }
            
            Moves currentAction;

            if (currentActionNum == ACCELERATE)
            {
                currentAction = rightMove ? Moves.ROLL_RIGHT : Moves.ROLL_LEFT;
            }
            else
            {
                currentAction = rightMove ? Moves.ROLL_LEFT : Moves.ROLL_RIGHT;
            }

            return currentAction;
        }

        public Moves GetCurrentAction(RectangleRepresentation rI, int targetPointX, int targetVelocityX, bool rightMove)
        {

            int stateNum = GetStateNum(rI, targetPointX, rightMove);

            int currentActionNum;

            float distanceX = rightMove ? rI.X - targetPointX : targetPointX - rI.X;

            if (distanceX <= -MAX_DISTANCEX)
            {
                currentActionNum = ACCELERATE;
            }
            else if (distanceX >= MAX_DISTANCEX)
            {
                currentActionNum = DEACCELERATE;
            }
            else
            {
                currentActionNum = GetOptimalActionNum(stateNum, targetVelocityX);
            }

            Moves currentAction;

            if (currentActionNum == ACCELERATE)
            {
                currentAction = rightMove ? Moves.MOVE_RIGHT : Moves.MOVE_LEFT;
            }
            else
            {
                currentAction = rightMove ? Moves.MOVE_LEFT : Moves.MOVE_RIGHT;
            }

            return currentAction;
        }

        public int GetStateNum(CircleRepresentation cI, int targetPointX, bool rightMove)
        {
            int discretizedVelocityX = (int)((rightMove ? cI.VelocityX : -cI.VelocityX) + 200) / DISCRETIZATION_VELOCITYX;

            if (discretizedVelocityX < 0)
            {
                discretizedVelocityX = 0;
            }
            else if (GameInfo.MAX_VELOCITYX * 2 / DISCRETIZATION_VELOCITYX <= discretizedVelocityX)
            {
                discretizedVelocityX = GameInfo.MAX_VELOCITYX * 2 / DISCRETIZATION_VELOCITYX - 1;
            }

            int discretizedDistanceX = (int)((rightMove ? cI.X - targetPointX : targetPointX - cI.X) + MAX_DISTANCEX) / DISCRETIZATION_DISTANCEX;

            if (discretizedDistanceX < 0)
            {
                discretizedDistanceX = 0;
            }
            else if (MAX_DISTANCEX * 2 / DISCRETIZATION_DISTANCEX <= discretizedDistanceX)
            {
                discretizedDistanceX = MAX_DISTANCEX * 2 / DISCRETIZATION_DISTANCEX - 1;
            }

            return discretizedVelocityX + discretizedDistanceX * (GameInfo.MAX_VELOCITYX * 2 / DISCRETIZATION_VELOCITYX);          
        }

        public int GetStateNum(RectangleRepresentation rI, int targetPointX, bool rightMove)
        {
            int discretizedVelocityX = (int)((rightMove ? rI.VelocityX : -rI.VelocityX) + 200) / DISCRETIZATION_VELOCITYX;

            if (discretizedVelocityX < 0)
            {
                discretizedVelocityX = 0;
            }
            else if (GameInfo.MAX_VELOCITYX * 2 / DISCRETIZATION_VELOCITYX <= discretizedVelocityX)
            {
                discretizedVelocityX = GameInfo.MAX_VELOCITYX * 2 / DISCRETIZATION_VELOCITYX - 1;
            }

            int discretizedDistanceX = (int)((rightMove ? rI.X - targetPointX : targetPointX - rI.X) + MAX_DISTANCEX) / DISCRETIZATION_DISTANCEX;

            if (discretizedDistanceX < 0)
            {
                discretizedDistanceX = 0;
            }
            else if (MAX_DISTANCEX * 2 / DISCRETIZATION_DISTANCEX <= discretizedDistanceX)
            {
                discretizedDistanceX = MAX_DISTANCEX * 2 / DISCRETIZATION_DISTANCEX - 1;
            }

            return discretizedVelocityX + discretizedDistanceX * (GameInfo.MAX_VELOCITYX * 2 / DISCRETIZATION_VELOCITYX);
        }

        private int GetOptimalActionNum(int stateNum, int targetVelocityX)
        {
            int maxColumnNum = 0;
            float maxValue = float.MinValue;

            int from = (targetVelocityX / (DISCRETIZATION_VELOCITYX * 2)) * 2;
            int to = from + NUM_POSSIBLE_MOVES;

            for (int i = from; i < to; i++)
            {
                if (maxValue < Qmap[stateNum, i])
                {
                    maxValue = Qmap[stateNum, i];
                    maxColumnNum = i;
                }
            }

            return maxColumnNum - from;
        }
    }
}
