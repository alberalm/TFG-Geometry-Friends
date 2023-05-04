using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using GeometryFriends.AI;

namespace GeometryFriendsAgents
{
    public class CircleController
    {
        CirclePID cPID;
        private float jumpMarginPos = 20;
        private float jump0Margin = 10;
        private float rollMarginPos = 50;
        private float jumpMarginV = 75;
        private float PIDSpace = 200;
        private Stopwatch jumpTime;
        private float gameSpeed;
        public bool jumping = false;
        public bool jumpReached = false;
        public bool rollReached = false;
        private float vel0Margin = 50;

        public CircleController(float gSpeed)
        {
            cPID = new CirclePID();
            jumpTime = new Stopwatch();
            gameSpeed = gSpeed;
        }

        public void resetPID()
        {
            cPID.resetPID();
        }

        public Moves computeAction(Point point, float velX, float x, float y, float timestep, float a)
        {
            //the action JUMP need to be treated more carefully
            if (point.getAction() == Moves.JUMP)
            {
                if (jumpReached)
                {
                    jumpTime.Restart();
                    jumpReached = false;
                    //jumping = true;
                }
                else if (jumping && jumpTime.ElapsedMilliseconds * 0.001f * gameSpeed > 1.5f)
                {
                    jumping = false;
                    return Moves.JUMP;
                }
                else if (stateReachedJump(point, velX, x, y))
                {
                    jumpReached = true;
                    jumping = true;
                    return Moves.JUMP;
                }
            }

            //if the point has been reached 
            else if (point.getAction() != Moves.JUMP && stateReachedRoll(point, velX, x, y))
            {
                rollReached = true;
                return point.getAction();
            }

            //see if the agent needs to go left or right
            //if the agent is at the points right, and the desired velocity also goes right
            if (x < point.getPosX() && point.getVelX() >= 0)
            {
                //if it is goint to fast, slow down
                if (velX > point.getVelX() && velX > vel0Margin)
                {
                    return Moves.ROLL_LEFT;
                }

                float tempPos = x;
                float tempVel = velX;
                float auxVel = velX;
                //simulate going left if it is possible to achieve the desired velocity
                if(a == 0)
                {
                    return Moves.ROLL_RIGHT;
                }
                while (tempPos < point.getPosX())
                {
                    //make sure it goes to the right even when currently sliding left
                    //we are assuming the acceleration is constant - TODO - verify this
                    tempVel = tempVel + Math.Abs(a);// * timestep;
                    tempPos = tempPos + auxVel + (tempVel - auxVel) / 2;
                    auxVel = tempVel;
                }
                //now check if the velocity is the same or higher than the desired one. If it is, then it is possible to reach it within the given distance
                if (tempVel >= point.getVelX())
                {
                    //go right
                    return Moves.ROLL_RIGHT;
                }
                else
                {
                    return Moves.ROLL_LEFT;
                }
            }
            //if the agent is at the points right, and the desired velocity goes left
            else if (x < point.getPosX() && point.getVelX() <= 0)
            {
                return Moves.ROLL_RIGHT;
            }

            //if the agent is at the points left, and the desired velocity also goes left
            else if (x > point.getPosX() && point.getVelX() <= 0)
            {
                //if it is goint to fast, slow down
                if (velX < point.getVelX() && velX < -vel0Margin)
                {
                    return Moves.ROLL_RIGHT;
                }

                float tempPos = x;
                float tempVel = velX;
                float auxVel = velX;
                //simulate going left if it is possible to achieve the desired velocity
                if (a == 0)
                {
                    return Moves.ROLL_LEFT;
                }
                while (tempPos > point.getPosX())
                {
                    //make sure it goes to the left even when currently sliding right
                    //we are assuming the acceleration is constant - TODO - verify this
                    tempVel = tempVel + (Math.Abs(a) * -1);// * timestep;
                    tempPos = tempPos + auxVel + (tempVel - auxVel) / 2;
                    auxVel = tempVel;
                }
                //now check if the velocity is the same or higher than the desired one. If it is, then it is possible to reach it within the given distance
                if (tempVel <= point.getVelX())
                {
                    //go right
                    return Moves.ROLL_LEFT;
                }
                else
                {
                    return Moves.ROLL_RIGHT;
                }
            }
            //if the agent is at the points left, and the desired velocity goes right
            else if (x > point.getPosX() && point.getVelX() >= 0)
            {
                return Moves.ROLL_LEFT;
            }
            else
            {
                //Shouldn't reach here
                return point.getAction();
            }
        }

        //method taken from previous RRT solution
        public Moves computeAction(Point point, float velX, float x, float y, float timestep)
        {
            //the action JUMP need to be treated more carefully
            if (point.getAction() == Moves.JUMP)
            {
                if (jumpReached)
                {
                    jumpTime.Restart();
                    jumpReached = false;
                    //jumping = true;
                }
                else if (jumping && jumpTime.ElapsedMilliseconds * 0.001f * gameSpeed > 1.5f)
                {
                    jumping = false;
                    return Moves.JUMP;
                }
                else if (stateReachedJump(point, velX, x, y))
                {
                    jumpReached = true;
                    jumping = true;
                    return Moves.JUMP;
                }
            }

            //if the point has been reached 
            else if (point.getAction() != Moves.JUMP && stateReachedRoll(point, velX, x, y))
            {
                rollReached = true;
                return point.getAction();
            }

            if (point.getPosX() < x)
            {
                if (x - point.getPosX() < PIDSpace)
                {
                    if (Math.Abs(velX) > 25)
                        return cPID.calculateAction(x, Math.Abs(velX), point.getVelX(), 0, timestep); // 0.1 value for step is a placeholder, must be replaced with the real time value
                    else
                        return Moves.ROLL_LEFT;
                }
                else
                {
                    return Moves.ROLL_LEFT;
                }
            }
            else
            {
                if (point.getPosX() - x < PIDSpace)
                {
                    if (Math.Abs(velX) > 25)
                        return cPID.calculateAction(x, velX, point.getVelX(), 1, timestep); // 0.1 value for step is a placeholder, must be replaced with the real time value
                    else
                        return Moves.ROLL_RIGHT;
                }
                else
                {
                    return Moves.ROLL_RIGHT;
                }
            }
        }

        private bool stateReachedJump(Point point, float velX, float x, float y)
        {
            //return true when the agent is very close to the jumping point and going in the same direction
            if (Math.Abs(point.getPosX() - x) <= jumpMarginPos && Math.Abs(point.getPosY() - y) <= jumpMarginPos &&
                //((point.getVelX() >= jump0Margin ^ velX < jump0Margin) || 
                ((Math.Abs(point.getVelX() - velX) <= jumpMarginV) ||
                ((Math.Abs(point.getVelX()) <= jump0Margin &&
                Math.Abs(velX) <= jump0Margin) && (Math.Abs(point.getVelX()) >= 0 && Math.Abs(velX) >= 0))))
            {
                return true;
            }
            return false;
        }

        private bool stateReachedRoll(Point point, float velX, float x, float y)
        {
            //return true when the agent is close to the point 
            if (Math.Abs(point.getPosX() - x) <= rollMarginPos && Math.Abs(point.getPosY() - y) <= rollMarginPos)
            {
                return true;
            }
            return false;
        }
    }
}