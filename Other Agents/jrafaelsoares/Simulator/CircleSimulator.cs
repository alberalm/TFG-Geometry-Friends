using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using GeometryFriends.AI.Debug;
using GeometryFriends.AI;

namespace GeometryFriendsAgents
{
    class CircleSimulator : Simulator
    {
        private bool jumped = false; //careful, it is only jumping (max) one time per jump simulation

        public CircleSimulator(List<Platform> pltfms) : base(pltfms)
        {
            //
        }

        public override Simulator clone()
        {
            CircleSimulator clonedSimulator = new CircleSimulator(platforms);
            clonedSimulator.setCloneSimulator(X, Y, velX, velY, diamonds, diamondsCaught);
            return clonedSimulator;
        }

        public override List<DebugInformation> simulate(Moves action, float time)
        {
            List<DebugInformation> debug = new List<DebugInformation>();

            initPosX = X;
            initPosY = Y;
            previousX = X;
            previousY = Y;
            float sec = time;
            jumped = false;
            float i;
            bool collided = false;

            //step simulation
            for (i = 1 / stepPSec; i <= sec; i = i + 1 / stepPSec)
            {
                if (velY < minYVelocity)
                {
                    velY = minYVelocity;
                }

                collided = false;
                updateVelocity(action);
                X = initPosX + velX * i;
                Y = initPosY + velY * i + (gravity * i * i) / 2;

                //for debug
                if (X < 950 && X - previousX < 0)
                {
                    bool toBreak = true;
                }

                //check collision with floor, ceiling and walls
                //floor
                if (Y > 720)
                {
                    velY = (-velY + gravity * i) * restitCoef;  //Vt = V0 - gt
                    Y = 720;
                    sec = setStep(i, sec);
                    i = 1 / stepPSec;
                    collided = true;
                }
                //ceiling
                else if (Y < 80)
                {
                    velY = (-velY + gravity * i) * restitCoef;
                    Y = 80;
                    sec = setStep(i, sec);
                    i = 1 / stepPSec;
                    collided = true;
                }
                //right wall
                if (X > 1200)
                {
                    velY = (velY + gravity * i) * restitCoef;
                    velX = -velX * restitCoef;
                    X = 1200;
                    if (collided)
                    {
                        setInitalPos();
                    }
                    else
                    {
                        sec = setStep(i, sec);
                        i = 1 / stepPSec;
                        collided = true;
                    }
                }
                //left wall
                else if (X < 80)
                {
                    velY = (velY + gravity * i) * restitCoef;
                    velX = -velX * restitCoef;
                    X = 80;
                    if (collided)
                    {
                        setInitalPos();
                    }
                    else
                    {
                        sec = setStep(i, sec);
                        i = 1 / stepPSec;
                        collided = true;
                    }
                }
                //check other platforms
                foreach (Platform platform in platforms)
                {
                    CollisionType collision = colliding(platform, X, Y);
                    //check if inside the platform if so there was a collision
                    if (collision == CollisionType.NONE && 
                        ((X+radius > platform.getLeft() && X + radius < platform.getRight()) ||
                         (X - radius > platform.getLeft() && X - radius < platform.getRight())) &&
                          ((Y + radius > platform.getTop() && Y + radius < platform.getBottom()) ||
                         (Y - radius > platform.getTop() && Y - radius < platform.getBottom())))
                    {
                        
                        if (previousY + radius <= platform.getTop())
                        {
                            collision = CollisionType.TOP;
                        }
                        //bottom?
                        else if (previousY - radius >= platform.getBottom())
                        {
                            collision = CollisionType.BOTTOM;
                        }
                        //left?
                        else if (previousX + radius <= platform.getLeft())
                        {
                            collision = CollisionType.LEFT;
                        }
                        //right?
                        else if (previousX - radius >= platform.getRight())
                        {
                            collision = CollisionType.RIGHT;
                        }
                        else
                        {
                            //testing
                            float xDist = Math.Min(Math.Abs(previousX - platform.getLeft()), Math.Abs(previousX - platform.getRight()));
                            float yDist = Math.Min(Math.Abs(previousY - platform.getTop()), Math.Abs(previousY - platform.getBottom()));

                            if(xDist < yDist)
                            {
                                if(Math.Abs(previousX - platform.getLeft()) < Math.Abs(previousX - platform.getRight()))
                                {
                                    collision = CollisionType.LEFT;
                                }
                                else
                                {
                                    collision = CollisionType.RIGHT;
                                }
                            }
                            else
                            {
                                if (Math.Abs(previousY - platform.getTop()) < Math.Abs(previousY - platform.getBottom()))
                                {
                                    collision = CollisionType.TOP;
                                }
                                else
                                {
                                    collision = CollisionType.BOTTOM;
                                }
                            }
                        }
                    }
                    if (collision != CollisionType.NONE)
                    {
                        switch (collision)
                        {
                            case CollisionType.TOP:
                                //check if it is actually outside the platform
                                if (X < platform.getLeft() || X > platform.getRight())
                                {
                                    //see it as falling
                                    break;
                                }
                                velY = (-velY + gravity * i) * restitCoef;  //Vt = V0 - gt
                                Y = platform.getTop() - radius;
                                if (collided)
                                {
                                    setInitalPos();
                                }
                                else
                                {
                                    sec = setStep(i, sec);
                                    i = 1 / stepPSec;
                                    collided = true;
                                }
                                break;
                            case CollisionType.BOTTOM:
                                velY = (-velY + gravity * i) * restitCoef;
                                Y = platform.getBottom() + radius;
                                if (collided)
                                {
                                    setInitalPos();
                                }
                                else
                                {
                                    sec = setStep(i, sec);
                                    i = 1 / stepPSec;
                                    collided = true;
                                }
                                break;
                            case CollisionType.LEFT:
                                velY = (velY + gravity * i) * restitCoef;
                                velX = -velX * restitCoef;
                                X = platform.getLeft() - radius;
                                if (collided)
                                {
                                    setInitalPos();
                                }
                                else
                                {
                                    sec = setStep(i, sec);
                                    i = 1 / stepPSec;
                                    collided = true;
                                }
                                break;
                            case CollisionType.RIGHT:
                                velY = (velY + gravity * i) * restitCoef;
                                velX = -velX * restitCoef;
                                X = platform.getRight() + radius;
                                if (collided)
                                {
                                    setInitalPos();
                                }
                                else
                                {
                                    sec = setStep(i, sec);
                                    i = 1 / stepPSec;
                                    collided = true;
                                }
                                break;
                        }
                    }
                }

                List<DiamondInfo> diamondsToRemove = new List<DiamondInfo>();
                //check if a diamond was caught
                foreach (DiamondInfo diamond in diamonds)
                {
                    float leftX = diamond.getX() - diamondRadius;
                    float rightX = diamond.getX() + diamondRadius;
                    float topY = diamond.getY() - diamondRadius;
                    float bottomY = diamond.getY() + diamondRadius;

                    if (circleDiamondCollision((float)Math.Round(X), (float)Math.Round(Y), radius, (float)Math.Round(diamond.getX()), (float)Math.Round(diamond.getY()), (float)Math.Round(leftX), (float)Math.Round(rightX), (float)Math.Round(topY), (float)Math.Round(bottomY)))
                    {
                        debug.Add(GeometryFriends.AI.Debug.DebugInformationFactory.CreateCircleDebugInfo(new PointF(diamond.getX(), diamond.getY()), 10, new GeometryFriends.XNAStub.Color(255, 0, 0)));
                        //add diamond to the list of caught diamonds
                        diamondsCaught.Add(diamond);
                        //remove diamond of the uncaught diamonds list
                        diamondsToRemove.Add(diamond);
                    }
                }

                //only remove diamonds from list after the previous foreach 
                if (diamondsToRemove.Count != 0)
                {
                    foreach (DiamondInfo diamond in diamondsToRemove)
                    {
                        diamonds.Remove(diamond);
                    }
                }

                debug.Add(GeometryFriends.AI.Debug.DebugInformationFactory.CreateCircleDebugInfo(new PointF(X, Y), 2, new GeometryFriends.XNAStub.Color(255, 255, 0)));
                previousX = X;
                previousY = Y;
            }
            //there is a need to update the Y velocity at the end of the simulation
            velY = velY + gravity * i;
            return debug;
        }

        protected override void updateVelocity(Moves action)
        {
            //update initial X or Y velocity
            switch (action)
            {
                case Moves.ROLL_LEFT:
                    rollLeft();
                    break;
                case Moves.ROLL_RIGHT:
                    rollRight();
                    break;
                case Moves.JUMP:
                    jump();
                    break;
                default:
                    throw new Exception("Must have valid action");
            }
        }

        protected void rollLeft()
        {
            if (velX > -maxXVelocity && !midAir())
            {
                velX = velX - initialMoveAcceleration;// * 1 / stepPSec;
            }
            else if (velX <= -maxXVelocity && !midAir())
            {
                velX = -maxXVelocity;
            }
        }

        protected void rollRight()
        {
            if (velX < maxXVelocity && !midAir())
            {
                velX = velX + initialMoveAcceleration;// * 1/stepPSec;
            }
            else if (velX >= maxXVelocity && !midAir())
            {
                velX = maxXVelocity;
            }
        }

        protected void jump()
        {
            if (!midAir() && !jumped)
            {
                //the acceleration must be negative
                velY = velY + initialJumpAcceleration * 1 / stepPSec;
                jumped = true;
            }
        }

        protected override CollisionType colliding(Platform platform, float x, float y)
        {
            float left = platform.getLeft();
            float right = platform.getRight();
            float top = platform.getTop();
            float bottom = platform.getBottom();

            //is the agent inside the platform?
            if (circleLineIntersection(X, Y, radius, left, top, left, bottom) ||
                circleLineIntersection(X, Y, radius, left, top, right, top) ||
                circleLineIntersection(X, Y, radius, right, top, right, bottom) ||
                circleLineIntersection(X, Y, radius, right, bottom, left, bottom))
            {
                //if so, where is it colliding? not dealing with corners yet (eg top right corner)
                //top?
                if (previousY + radius <= top + 1)
                {
                    return CollisionType.TOP;
                }
                //bottom?
                else if (previousY - radius >= bottom)
                {
                    return CollisionType.BOTTOM;
                }
                //left?
                else if (previousX + radius <= left)
                {
                    return CollisionType.LEFT;
                }
                //right?
                else if (previousX - radius >= right)
                {
                    return CollisionType.RIGHT;
                }
            }

            return CollisionType.NONE;
        }

        protected override bool colliding(Platform platform, CollisionType type)
        {
            float left = platform.getLeft();
            float right = platform.getRight();
            float top = platform.getTop();
            float bottom = platform.getBottom();
            if (circleLineIntersection(X, Y, radius, left, top, left, bottom) ||
                circleLineIntersection(X, Y, radius, left, top, right, top) ||
                circleLineIntersection(X, Y, radius, right, top, right, bottom) ||
                circleLineIntersection(X, Y, radius, right, bottom, left, bottom))
            {
                switch (type)
                {
                    case CollisionType.BOTTOM:
                        if (velY <= 0 && previousY + radius < top || previousY - radius > bottom)
                        {
                            return true;
                        }
                        return false;
                    case CollisionType.TOP:
                        if (velY >= 0 && previousY + radius < top || previousY - radius > bottom)
                        {
                            return true;
                        }
                        else if (Math.Abs(velY) < 10 && previousX + radius >= left && previousX - radius <= right)
                        {
                            return true;
                        }
                        return false;
                    case CollisionType.LEFT:
                        if (velX > 0 && previousX + radius < left || previousX - radius > right)
                        {
                            return true;
                        }
                        return false;
                    case CollisionType.RIGHT:
                        if (velX < 0 && previousX + radius < left || previousX - radius > right)
                        {
                            return true;
                        }
                        return false;
                }
            }
            return false;
        }

        public bool circleDiamondCollision(float cX, float cY, float cR, float dX, float dY, float xLeft, float xRight, float yUp, float yDown)
        {
            //up, down, left, right
            if (circleLineIntersection(cX, cY, cR, xLeft, dY, dX, yUp) ||
               circleLineIntersection(cX, cY, cR, xLeft, dY, dX, yDown) ||
               circleLineIntersection(cX, cY, cR, dX, yUp, xRight, dY) ||
               circleLineIntersection(cX, cY, cR, dX, yDown, xRight, dY))
            {
                return true;
            }
            return false;
        }


        public float getCirclePositionX() { return X; }
        public float getCirclePositionY() { return Y; }
        public float getCircleVelocityX() { return velX; }
        public float getCircleVelocityY() { return velY; }
        //velocity radius -> not really necessary
        public float getCircleVelocityRadius() { return 0; }
    }
}
