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
        private List<CollectibleRepresentation> uncaughtCollectibles;
        private int timesPassed;

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
