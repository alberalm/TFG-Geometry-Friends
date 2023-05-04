using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using GeometryFriends.AI;

namespace GeometryFriendsAgents
{
    public class RectangleController
    {
        private float morphMarginPos = 20;
        private float morphFallMargin = 20;
        private float morph0Margin = 10;
        private float morphLowMargin = 1;
        private float morphYMargin = 10;
        private float slideMarginPos = 10;
        private float slideMarginPosX = 20;
        private float vel0Margin = 25;
        private float morphMarginV = 75;
        private float PIDSpace = 200;
        private float aMargin = 50;
        private float farMargin = 150;
        private Stopwatch morphTime;
        private float gameSpeed;
        private Utils utils;
        public bool morphing = false;
        public bool morphReached = false;
        public bool slideReached = false;

        public RectangleController(float gSpeed, Utils u)
        {
            morphTime = new Stopwatch();
            gameSpeed = gSpeed;
            utils = u;
        }
        
        public Moves computeSlide(Point point, float velX, float x, float y, float timestep, float a)
        {
            //if the point has been reached 

            if (stateReachedSlide(point, velX, x, y))
            {
                slideReached = true;
                return point.getAction();
            }

            //see if the agent needs to go left or right
            //if the agent is at the points left, and the desired velocity goes right
            if (x < point.getPosX() && point.getVelX() >= 0)
            {
                //if it is goint to fast, when close slow down, else keep going
                if (velX > point.getVelX() && velX > vel0Margin)
                {
                    if(Math.Abs(x - point.getPosX()) < farMargin)
                    {
                        return Moves.MOVE_LEFT;
                    }
                    else
                    {
                        return Moves.MOVE_RIGHT;
                    }
                }

                if (a == 0)
                {
                    return Moves.MOVE_RIGHT;
                }

                float tempPos = x;
                float tempVel = velX;
                float auxVel = velX;
                //simulate going left if it is possible to achieve the desired velocity
                while (tempPos - aMargin < point.getPosX())
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
                    return Moves.MOVE_RIGHT;
                }
                else
                {
                    return Moves.MOVE_LEFT;
                }
            }
            //if the agent is at the points left, and the desired velocity goes right
            else if (x < point.getPosX() && point.getVelX() <= 0)
            {
                return Moves.MOVE_RIGHT;
            }

            //if the agent is at the points right, and the desired velocity goes left
            else if (x > point.getPosX() && point.getVelX() <= 0)
            {
                //if it is goint too fast, when close, slow down, else, keep going
                if (velX > point.getVelX() && velX > vel0Margin)
                {
                    if (Math.Abs(x - point.getPosX()) < farMargin)
                    {
                        return Moves.MOVE_RIGHT;
                    }
                    else
                    {
                        return Moves.MOVE_LEFT;
                    }
                }

                float tempPos = x;
                float tempVel = velX;
                float auxVel = velX;

                if (a == 0)
                {
                    return Moves.MOVE_RIGHT;
                }
                //simulate going left if it is possible to achieve the desired velocity
                while (tempPos + aMargin > point.getPosX())
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
                    return Moves.MOVE_LEFT;
                }
                else
                {
                    return Moves.MOVE_RIGHT;
                }
            }
            //if the agent is at the points left, and the desired velocity goes right
            else if (x > point.getPosX() && point.getVelX() >= 0)
            {
                return Moves.MOVE_LEFT;
            }
            else
            {
                //Shouldn't reach here
                return point.getAction();
            }
        }

        public Moves computeAction(Point point, float velX, float velY, float x, float y, float timestep, float a)
        {
            if (point.getAction() == Moves.MORPH_UP || point.getAction() == Moves.MORPH_DOWN)
            {
                //if the agent is not falling and reached the desired height
                if (Math.Abs(velY) <= morphFallMargin && Math.Abs(point.getPosY() - y) <= morphLowMargin)
                {
                    //make sure the agent is at the right x position
                    if (stateReachedSlide(point, velX, x, y))
                    {
                        morphReached = true;
                        morphing = true;
                        return Moves.NO_ACTION;
                    }
                    else
                    {
                        computeSlide(point, velX, x, y, timestep, a);
                    }

                }
                //if the agent is not falling and hasn't reached the desired height
                else if (Math.Abs(velY) <= morphFallMargin && Math.Abs(point.getPosY() - y) > morphLowMargin)
                {
                    //if it is too hight 
                    if(y > point.getPosY())
                    {
                        return Moves.MORPH_UP;
                    }
                    //if it is too low
                    if(y < point.getPosY())
                    {
                        //if the point is too low but in the air, then the agent must likely needs to path through a narrow space
                        Platform plat = utils.onPlatform(point.getPosX(), point.getPosY(), 25, 10);
                        if(plat == null)
                        {
                            return point.getAction();
                        }
                        return Moves.MORPH_DOWN;
                    }
                    //should not reach here
                    return point.getAction();
                }
                //if the agent is falling
                //TODO - this might make impossible the resolution of some levels
                else
                {
                    return point.getAction();
                }
            }

            //if the point has been reached 
            else if (point.getAction() != Moves.MORPH_DOWN && point.getAction() != Moves.MORPH_UP && stateReachedSlide(point, velX, x, y))
            {
                slideReached = true;
                return point.getAction();
            }

            ////if the point has almost been reached in X but not in Y
            //if (point.getAction() != Moves.MORPH_DOWN && point.getAction() != Moves.MORPH_UP && stateReachedSlideX(point, velX, x, y))
            //{
            //    //if the agent is too low, morph-up
            //    if (y > point.getPosY())
            //    {
            //        return Moves.MORPH_UP;
            //    }
            //    if (y < point.getPosY())
            //    {
            //        return Moves.MORPH_DOWN;
            //    }
            //}

            return computeSlide(point, velX, x, y, timestep, a);
        }

        

        private bool stateReachedMorph(Point point, float velX, float x, float y)
        {
            //return true when the agent is very close to the jumping point and going in the same direction
            if (Math.Abs(point.getPosX() - x) <= morphMarginPos && Math.Abs(point.getPosY() - y) <= morphMarginPos &&
                ((Math.Abs(point.getVelX() - velX) <= morphMarginV) ||
                ((Math.Abs(point.getVelX()) <= morph0Margin &&
                Math.Abs(velX) <= morph0Margin) && (Math.Abs(point.getVelX()) >= 0 && Math.Abs(velX) >= 0))) &&
                Math.Abs(point.getPosY() - y) <= morphYMargin)
            {
                return true;
            }
            return false;
        }

        private bool stateReachedSlide(Point point, float velX, float x, float y)
        {
            //return true when the agent is close to the point 
            if (Math.Abs(point.getPosX() - x) <= slideMarginPos && Math.Abs(point.getPosY() - y) <= slideMarginPos)
            {
                return true;
            }
            return false;
        }

        private bool stateReachedSlideX(Point point, float velX, float x, float y)
        {
            //return true when the agent is close to the point 
            if (Math.Abs(point.getPosX() - x) <= slideMarginPosX)
            {
                return true;
            }
            return false;
        }
    }
}
