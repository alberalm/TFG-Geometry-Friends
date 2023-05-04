using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using GeometryFriends.AI;
using GeometryFriends.AI.Debug;

namespace GeometryFriendsAgents
{
    public abstract class Simulator
    {
        protected float X;
        protected float Y;
        protected float velX;
        protected float velY;
        protected List<Platform> platforms;
        protected List<DiamondInfo> diamonds;
        protected List<DiamondInfo> diamondsCaught;

        //for simulation
        protected float initPosX;
        protected float initPosY;
        protected float previousX;
        protected float previousY;

        protected const float initialMoveAcceleration = 10; //X acceleration when moving
        protected const float initialJumpAcceleration = -14000;//-26220 /2; //Y acceleration when jumping
        protected const float maxXVelocity = 170;//197; //maximum X velocity -> acceleration = 0; reducing max acceleration for it sometimes does not seem to reach it easily
        protected const float minYVelocity = -437; //just negative
        protected const float stepPSec = 30;
        protected const float restitCoef = 0.4f;
        protected const float gravity = 10 * stepPSec;
        protected const float radius = 40;
        protected const float diamondRadius = 10; //VERIFICAR

        //margins of error
        protected const float yMargin = 5;

        public Simulator(List<Platform> pltfms)
        {
            X = 0;
            Y = 0;
            velX = 0;
            velY = 0;
            platforms = pltfms;            
        }

        public abstract Simulator clone();

        public void setCloneSimulator(float x, float y, float vx, float vy, List<DiamondInfo> currentDiamonds, List<DiamondInfo> dCaught)
        {
            List<DiamondInfo> cD = new List<DiamondInfo>();
            foreach(DiamondInfo diamond in currentDiamonds)
            {
                cD.Add(diamond);
            }
            setSimulator(x, y, vx, vy, cD);
            diamondsCaught = new List<DiamondInfo>();
            foreach(DiamondInfo diamond in dCaught)
            {
                diamondsCaught.Add(diamond);
            }
        }

        //the current diamonds list should only contain the ones that still need to be caught at this stage
        public void setSimulator(float x, float y, float vx, float vy, List<DiamondInfo> currentDiamonds)
        {
            X = x;
            Y = y;
            velX = vx;
            velY = vy;
            diamonds = currentDiamonds;
            //this must always be reset
            diamondsCaught = new List<DiamondInfo>();
        }

       
        //CAUTION!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        public float getRectanglePositionX() { return X; }
        public float getRectanglePositionY() { return Y; }
        public float getRectangleVelocityX() { return velX; }
        public float getRectangleVelocityY() { return velY; }
        public float getRectangleHeight() { return 0; }
        //CAUTION!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!end
        //diamonds caught during simulation
        public List<DiamondInfo> getDiamondsCaught() { return diamondsCaught; }
        //uncaught collectibles -> collectibles that are left, not the ones this simulation has not caught
        public List<DiamondInfo> getUncaughtDiamonds() { return diamonds; }

        public abstract List<DebugInformation> simulate(Moves action, float time);

        protected float setStep(float i, float sec)
        {
            float seconds = sec - i;
            setInitalPos();
            return seconds;
        }

        protected void setInitalPos()
        {
            initPosY = Y;
            initPosX = X;
        }

        protected abstract void updateVelocity(Moves action);

        protected bool midAir()
        {
            if (Y + yMargin >= 720)
            {
                return false;
            }
            foreach(Platform platform in platforms)
            {
                CollisionType collision = colliding(platform, X, Y);
                if(collision == CollisionType.TOP)
                {
                    return false;
                }
            }
            return true; 
        }

        protected abstract CollisionType colliding(Platform platform, float x, float y);
        protected abstract bool colliding(Platform platform, CollisionType type);

        //TODO move to utils
        public bool lineIntersection(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {

            if ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4) == 0)
            {
                return false;
            }

            float pX = ((x1 * y2 - y1 * x2) * (x3 - x4) - (x1 - x2) * (x3 * y4 - y3 * x4)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));

            float pY = ((x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));

            if (x1 >= x2)
            {
                if (!(x2 <= pX && pX <= x1))
                {
                    return false;
                }
            }
            else
            {
                if (!(x1 <= pX && pX <= x2)) { return false; }
            }
            if (x3 >= x4)
            {
                if (!(x4 <= pX && pX <= x3)) { return false; }
            }
            else
            {
                if (!(x3 <= pX && pX <= x4)) { return false; }
            }
            if (y1 >= y2)
            {
                if (!(y2 <= pY && pY <= y1)) { return false; }
            }
            else
            {
                if (!(y1 <= pY && pY <= y2)) { return false; }
            }
            if (y3 >= y4)
            {
                if (!(y4 <= pY && pY <= y3)) { return false; }
            }
            else
            {
                if (!(y3 <= pY && pY <= y4)) { return false; }
            }
            return true;
        }

        public int sign(float a)
        {
            if(a < 0)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        //TODO move to Utils
         protected bool circleLineIntersection(float cX, float cY, float cR, float x1, float y1, float x2, float y2)
        {
            float n = Math.Abs((x2 - x1) * (y1 - cY) - (x1 - cX) * (y2 - y1));
            float d = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            float dist = n / d;
            if (dist > cR) return false;
            float d1 = (float)Math.Sqrt((cX - x1) * (cX - x1) + (cY - y1) * (cY - y1));
            if ((d1 - cR) > d) return false;
            float d2 = (float)Math.Sqrt((cX - x2) * (cX - x2) + (cY - y2) * (cY - y2));
            if ((d2 - cR) > d) return false;
            return true;
        }

        //TODO move to utils
        public bool circleRectangleCollision(float cX, float cY, float cR, float xLeft, float xRight, float yUp, float yDown)
        {
            //up, down, left, right
            if (circleLineIntersection(cX, cY, cR, xLeft, yUp, xRight, yUp) ||
               circleLineIntersection(cX, cY, cR, xLeft, yDown, xRight, yDown) ||
               circleLineIntersection(cX, cY, cR, xLeft, yUp, xLeft, yDown) ||
               circleLineIntersection(cX, cY, cR, xRight, yUp, xRight, yDown))
            {
                return true;
            }
            return false;
        }

    }
}
