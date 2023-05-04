using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using GeometryFriends.AI;

namespace GeometryFriendsAgents
{
    public class RectangleControllerMP
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
        private float farMargin = 200;
        private float circleMaxJump = 400;
        private Stopwatch morphTime;
        private float gameSpeed;
        public bool morphing = false;
        public bool morphReached = false;
        public bool slideReached = false;
        private float radius = 40;
        private float onRectanglePositionMargin = 25;
        private float jumpToRectangleMargin = 75;
        private float platformRectangleMargin = 200;
        private float maxVelWithCircle = 50;
        private float diamondXMargin = 100;
        private float skillVelMargin = 10;
        private float minRectangleHeight = 52;
        private Utils utils;

        public RectangleControllerMP(float gSpeed, Utils u)
        {
            morphTime = new Stopwatch();
            gameSpeed = gSpeed;
            utils = u;
        }

        public Moves slideWithCircle(float x, float desiredX, float velX)
        {
            //if at left
            if(x < desiredX - onRectanglePositionMargin)
            {
                if(velX > maxVelWithCircle)
                {
                    return Moves.MOVE_LEFT;
                }
                else
                {
                    return Moves.MOVE_RIGHT;
                }
            }
            //if at right
            if (x > desiredX + onRectanglePositionMargin)
            {
                if (velX < -maxVelWithCircle)
                {
                    return Moves.MOVE_RIGHT;
                }
                else
                {
                    return Moves.MOVE_LEFT;
                }
            }
            return Moves.NO_ACTION;
        }

        public Moves catchFromRectangle(float x, float y, float velX, float velY, float rectHeight, float rectWidth, float circleX, float circleY, float circleVelX, float diamondX, float diamondY, float a)
        {
            //if midair
            if (velY > vel0Margin)
            {
                return Moves.NO_ACTION;
            }
            //if circle is on rectangle
            if (utils.onRectangle(circleX, circleY + radius, x, y, rectHeight, rectWidth, 25))
            {
                //if it is in position and at height
                if (diamondX - onRectanglePositionMargin < x && diamondX + onRectanglePositionMargin > x)
                {
                    //if at height
                    if(circleY - diamondY <= circleMaxJump)
                    {
                        return Moves.NO_ACTION;
                    }
                    //if not, morph up
                    else
                    {
                        return Moves.MORPH_UP;
                    }
                    
                }
                else
                {
                    //go to position
                    return slideWithCircle(x, diamondX, velX);
                }
            }

            //morph down to the max
            if(rectHeight > minRectangleHeight)
            {
                return Moves.MORPH_DOWN;
            }
            //check if rectangle is in position
            if (Math.Abs(diamondX - x) < diamondXMargin && Math.Abs(velX) < skillVelMargin)
            {
                //wait
                return Moves.NO_ACTION;
            }
            else
            {
                //go to position
                return computeFastSlide(x, diamondX, velX);
            }
        }

        public Moves computeFastSlide (float x, float desiredX, float velX)
        {
            if (desiredX - x >= 200)
            {
                return Moves.MOVE_RIGHT;
            }
            if (desiredX - x <= -200)
            {
                return Moves.MOVE_LEFT;
            }
            if (desiredX > x)
            {
                if (velX > 100)
                {
                    return Moves.MOVE_LEFT;
                }
                else
                {
                    return Moves.MOVE_RIGHT;
                }
            }
            else
            {
                if (velX < -100)
                {
                    return Moves.MOVE_RIGHT;
                }
                else
                {
                    return Moves.MOVE_LEFT;
                }
            }
        }

        public Moves catchFromRectangleOnYellowPlatform(float x, float y, float velX, float velY, float rectHeight, float rectWidth, float circleX, float circleY, float circleVelX, DiamondInfo diamond, Platform platform, float a)
        {
            //if circle is midair
            if (velY > vel0Margin)
            {
                return Moves.NO_ACTION;
            }

            //if circle is on rectangle
            if (utils.onRectangle(circleX, circleY + radius, x, y, rectHeight, rectWidth, 25))
            {
                //if it is in position and at height
                if (diamond.getX() - onRectanglePositionMargin < x && diamond.getX() + onRectanglePositionMargin > x)
                {
                    //if at height
                    if (circleY - diamond.getY() <= circleMaxJump)
                    {
                        return Moves.NO_ACTION;
                    }
                    //if not, morph up
                    else
                    {
                        return Moves.MORPH_UP;
                    }
                }
                else
                {
                    //go to position
                    return slideWithCircle(x, diamond.getX(), velX);
                }
            }

            float platformLeftSide = platform.getX() - platform.getWidth() / 2;
            float platformRightSide = platform.getX() + platform.getWidth() / 2;

            //morph down to the max
            if (rectHeight > minRectangleHeight)
            {
                return Moves.MORPH_DOWN;
            }

            //check if rectangle is in position
            if ((circleX < diamond.getX() && x > platformRightSide && x - platformRightSide < platformRectangleMargin && Math.Abs(velX) < skillVelMargin) ||
                (circleX > diamond.getX() && x < platformLeftSide && platformLeftSide - x < platformRectangleMargin && Math.Abs(velX) < skillVelMargin))
            {
                //if so, wait
                return Moves.NO_ACTION;
            }
            //if not, go to position
            if (circleX < diamond.getX())
            {
                return computeFastSlide(x, platformRightSide + platformRectangleMargin, velX);
            }
            else
            {
                return computeFastSlide(x, platformLeftSide - platformRectangleMargin, velX);
            }
            return Moves.NO_ACTION;
        }

        public Moves computeSlide(PointMP point, float velX, float x, float y, float timestep, float a)
        {
            //if the point has been reached 
            if (stateReachedSlide(point, velX, x, y))
            {
                slideReached = true;
                return point.getAction();
            }

            //see if the agent needs to go left or right
            //if the agent is at the points right, and the desired velocity also goes right
            if (x < point.getPosX() && point.getVelX() >= 0)
            {
                //if it is goint to fast, when close slow down, else keep going
                if (velX > point.getVelX() && velX > vel0Margin)
                {
                    if (Math.Abs(x - point.getPosX()) < farMargin)
                    {
                        return Moves.MOVE_LEFT;
                    }
                    else
                    {
                        return Moves.MOVE_RIGHT;
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
            //if the agent is at the points right, and the desired velocity goes left
            else if (x < point.getPosX() && point.getVelX() <= 0)
            {
                return Moves.MOVE_RIGHT;
            }

            //if the agent is at the points left, and the desired velocity also goes left
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

        public Moves computeAction(PointMP point, float velX, float velY, float x, float y, float timestep, float a)
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
                    if(y < point.getPosY())
                    {
                        return Moves.MORPH_DOWN;
                    }
                    //should not reach here
                    return point.getAction();
                }
                //if the agent is falling
                //TODO - this might make impossible the resolution of some levels
                else
                {
                    return Moves.NO_ACTION;
                }
            }

            //if the point has been reached 
            if (point.getAction() != Moves.MORPH_DOWN && point.getAction() != Moves.MORPH_UP && stateReachedSlide(point, velX, x, y))
            {
                slideReached = true;
                return point.getAction();
            }
            ////if the point has almost been reached in X but not in Y
            //if (point.getAction() != Moves.MORPH_DOWN && point.getAction() != Moves.MORPH_UP && stateReachedSlideX(point, velX, x, y))
            //{
            //    //if the agent is too low, morph-up
            //    if(y > point.getPosY())
            //    {
            //        return Moves.MORPH_UP;
            //    }
            //    if(y < point.getPosY())
            //    {
            //        return Moves.MORPH_DOWN;
            //    }
            //}

            return computeSlide(point, velX, x, y, timestep, a);
        }

        

        private bool stateReachedMorph(PointMP point, float velX, float x, float y)
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

        private bool stateReachedSlide(PointMP point, float velX, float x, float y)
        {
            //return true when the agent is close to the point 
            if (Math.Abs(point.getPosX() - x) <= slideMarginPos && Math.Abs(point.getPosY() - y) <= slideMarginPos)
            {
                return true;
            }
            return false;
        }

        private bool stateReachedSlideX(PointMP point, float velX, float x, float y)
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
