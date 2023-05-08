using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    public class Point
    {
        private float posX;
        private float posY;
        private float velX;
        private float height;
        private Moves action;
        private List<DiamondInfo> uncaughtDiamonds;
        private List<CollectibleRepresentation> uncaughtCollectibles;

        private int timesPassed;
        //For general Simulator
        public Point(float pX, float pY, float vX, float h, Moves a, List<DiamondInfo> uC)
        {
            posX = pX;
            posY = pY;
            velX = vX;
            height = h;
            action = a;
            uncaughtDiamonds = uC;
            timesPassed = 0;
        }

        //For Game Simulator
        public Point(float pX, float pY, float vX, float h, Moves a, List<CollectibleRepresentation> uC)
        {
            posX = pX;
            posY = pY;
            velX = vX;
            height = h;
            action = a;
            uncaughtCollectibles = uC;
            timesPassed = 0;
        }

        public float getPosX()
        {
            return posX;
        }

        public float getPosY()
        {
            return posY;
        }

        //only use to clean a plan
        public void setY(float y)
        {
            posY = y;
        }

        public float getVelX()
        {
            return velX;
        }

        public float getHeight()
        {
            return height;
        }

        public Moves getAction()
        {
            return action;
        }

        public List<DiamondInfo> getUncaughtDiamonds()
        {
            return uncaughtDiamonds;
        }

        public List<CollectibleRepresentation> getUncaughtColl()
        {
            return uncaughtCollectibles;
        }
        public void passedThrough()
        {
            timesPassed++;
        }

        public void resetTimesPassed()
        {
            timesPassed = 0;
        }

        public int getTimesPassed()
        {
            return timesPassed;
        }
    }
}
