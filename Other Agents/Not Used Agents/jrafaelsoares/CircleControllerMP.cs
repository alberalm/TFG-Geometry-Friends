using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using GeometryFriends.AI;

namespace GeometryFriendsAgents
{
    public class CircleControllerMP
    {
        private float jumpMarginPos = 20;
        private float jump0Margin = 10;
        private float rollMarginPos = 50;
        private float jumpMarginV = 75;
        private float PIDSpace = 200;
        private float skillVelMargin = 10;
        private float diamondXMargin = 100;
        private float jumpToRectangleMargin = 75;
        private float platformRectangleMargin = 200;
        private float radius = 40;
        private float circleMaxJump = 400;
        private Stopwatch jumpTime;
        private float gameSpeed;
        public bool jumping = false;
        public bool jumpReached = false;
        public bool rollReached = false;
        private float vel0Margin = 75;
        private Utils utils;
        private float onRectanglePositionMargin = 25;
        private float rectanglePositionMargin = 100;

        public CircleControllerMP(float gSpeed, Utils u)
        {
            jumpTime = new Stopwatch();
            gameSpeed = gSpeed;
            utils = u;
        }

        private Moves jumpToRectangle(float x, float y, float velX, float rectX, float rectY, float rectWidth, float a)
        {
            float rectangleLeftSide = rectX - rectWidth / 2 - 50;
            float rectangleRightSide = rectX + rectWidth / 2 + 50;

            //if far from rectangle - roll to its side
            if (x + radius < rectangleLeftSide)
            {
                return computeSlide(x, velX, rectangleLeftSide, jumpToRectangleMargin, a);
            }
            else if (x - radius > rectangleRightSide)
            {
                return computeSlide(x, velX, rectangleRightSide, -jumpToRectangleMargin, a);
            }
            //if at left
            else if (x + radius > rectangleLeftSide && x + radius < rectangleRightSide)
            {
                //if at right velocity
                if (velX < jumpToRectangleMargin && velX > 0)
                {
                    return Moves.JUMP;
                }
                else
                {
                    return computeSlide(x, velX, rectangleLeftSide, jumpToRectangleMargin, a);
                }
            }
            //if at right
            else if (x - radius < rectangleRightSide && x - radius > rectangleLeftSide)
            {
                //if at right velocity
                if (velX > -jumpToRectangleMargin && velX < 0)
                {
                    return Moves.JUMP;
                }
                else
                {
                    return computeSlide(x, velX, rectangleRightSide, -jumpToRectangleMargin, a);
                }
            }

            return Moves.NO_ACTION;
        }

        private Moves keepRectangleVel(float velX, float rectVelX)
        {
            //if going faster to the right
            if(velX > rectVelX)
            {
                return Moves.ROLL_LEFT;
            }
            //if going faster to the left
            if(velX < rectVelX)
            {
                return Moves.ROLL_RIGHT;
            }
            return Moves.NO_ACTION;
        }

        public Moves catchFromRectangle(float x, float y, float velX, float velY, float rectX, float rectY, float rectVelX, float rectHeight, float rectWidth, float diamondX, float diamondY, float a)
        {
            //if circle is midair
            if(velY > vel0Margin)
            {
                return Moves.NO_ACTION;
            }
            //if circle is on rectangle
            if (utils.onRectangle(x, y + radius, rectX, rectY, rectHeight, rectWidth, 25))
            {
                //if it is in position and at height
                if (diamondX - onRectanglePositionMargin < x && diamondX + onRectanglePositionMargin > x && y - (diamondY + 35) <= circleMaxJump)
                {
                    return Moves.JUMP;
                }
                else
                {
                    //match rectangle velocity
                    return keepRectangleVel(velX, rectVelX);
                }
            }

            //check if rectangle is in position
            if (Math.Abs(diamondX - rectX) < diamondXMargin && Math.Abs(rectVelX) < skillVelMargin)
            {
                return jumpToRectangle(x, y, velX, rectX, rectY, rectWidth, a);
            }
            else
            {
                //wait
                return Moves.NO_ACTION;
            }
        }

        public Moves catchFromRectangleOnYellowPlatform(float x, float y, float velX, float velY, float rectX, float rectY, float rectVelX, float rectHeight, float rectWidth, DiamondInfo diamond, Platform platform, float a)
        {
            //if circle is midair
            if (velY > vel0Margin)
            {
                return Moves.NO_ACTION;
            }

            //if circle is on rectangle
            if (utils.onRectangle(x, y + radius, rectX, rectY, rectHeight, rectWidth, 25))
            {
                //if it is in position and at height
                if (diamond.getX() - onRectanglePositionMargin < x && diamond.getX() + onRectanglePositionMargin > x && y - (diamond.getY() + 35) <= circleMaxJump)
                {
                    return Moves.JUMP;
                }
                else
                {
                    //match rectangle velocity
                    return keepRectangleVel(velX, rectVelX);
                }
            }

            float platformLeftSide = platform.getX() - platform.getWidth() / 2;
            float platformRightSide = platform.getX() + platform.getWidth() / 2;

            //check if rectangle is in position
            if (x < diamond.getX() && rectX > platformRightSide && rectX - platformRightSide < platformRectangleMargin && Math.Abs(rectVelX) < skillVelMargin)
            {
                return Moves.ROLL_RIGHT;
            }
            else if (x > diamond.getX() && rectX < platformLeftSide && platformLeftSide - rectX < platformRectangleMargin && Math.Abs(rectVelX) < skillVelMargin)
            {
                return Moves.ROLL_LEFT;
            }
            else
            {
                //wait
                return Moves.NO_ACTION;
            }
        }

        public Moves computeSlide(float x, float velX, float desiredX, float desiredVelX, float a)
        {
            
            //see if the agent needs to go left or right
            //if the agent is at the points right, and the desired velocity also goes right
            if (x < desiredX && desiredVelX >= 0)
            {
                //if it is goint to fast, slow down
                if (velX > desiredVelX && velX > vel0Margin)
                {
                    return Moves.ROLL_LEFT;
                }

                float tempPos = x;
                float tempVel = velX;
                float auxVel = velX;
                //simulate going left if it is possible to achieve the desired velocity
                if (a == 0)
                {
                    return Moves.ROLL_RIGHT;
                }
                while (tempPos < desiredX)
                {
                    //make sure it goes to the right even when currently sliding left
                    //we are assuming the acceleration is constant - TODO - verify this
                    tempVel = tempVel + Math.Abs(a);// * timestep;
                    tempPos = tempPos + auxVel + (tempVel - auxVel) / 2;
                    auxVel = tempVel;
                }
                //now check if the velocity is the same or higher than the desired one. If it is, then it is possible to reach it within the given distance
                if (tempVel >= desiredVelX)
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
            else if (x < desiredX && desiredVelX <= 0)
            {
                return Moves.ROLL_RIGHT;
            }

            //if the agent is at the points left, and the desired velocity also goes left
            else if (x > desiredX && desiredVelX <= 0)
            {
                //if it is goint to fast, slow down
                if (velX < desiredVelX && velX < -vel0Margin)
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
                while (tempPos > desiredX)
                {
                    //make sure it goes to the left even when currently sliding right
                    //we are assuming the acceleration is constant - TODO - verify this
                    tempVel = tempVel + (Math.Abs(a) * -1);// * timestep;
                    tempPos = tempPos + auxVel + (tempVel - auxVel) / 2;
                    auxVel = tempVel;
                }
                //now check if the velocity is the same or higher than the desired one. If it is, then it is possible to reach it within the given distance
                if (tempVel <= desiredVelX)
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
            else if (x > desiredX && desiredX >= 0)
            {
                return Moves.ROLL_LEFT;
            }
            return Moves.NO_ACTION;
        }

        public Moves computeAction(PointMP point, float velX, float x, float y, float timestep, float a)
        {
            //the action JUMP need to be treated more carefully
            if (point.getAction() == Moves.JUMP)
            {
                if (stateReachedJump(point, velX, x, y))
                {
                    rollReached = true;
                    jumping = true;
                    return Moves.JUMP;
                }
            }
            if (jumping && jumpTime.ElapsedMilliseconds * 0.001f * gameSpeed > 1.5f)
            {
                jumping = false;
                return Moves.NO_ACTION;
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

        private bool stateReachedJump(PointMP point, float velX, float x, float y)
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

        private bool stateReachedRoll(PointMP point, float velX, float x, float y)
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