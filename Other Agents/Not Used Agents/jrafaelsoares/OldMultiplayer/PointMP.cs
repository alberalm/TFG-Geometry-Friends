using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    public class PointMP
    {
        private float posX;
        private float posY;
        private float velX;
        private float height;
        private float posXPartner;
        private float posYPartner;
        private Moves action;
        private List<CollectibleRepresentation> uncaughtCollectibles;
        private int timesPassed;
        private NodeMP node;

        public PointMP(float pX, float pY, float vX, float h, float pXp, float pYp, Moves a, List<CollectibleRepresentation> uC, NodeMP n)
        {
            posX = pX;
            posY = pY;
            velX = vX;
            height = h;
            posXPartner = pXp;
            posYPartner = pYp;
            action = a;
            uncaughtCollectibles = uC;
            timesPassed = 0;
            node = n;
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

        public float getPartnerX()
        {
            return posXPartner;
        }

        public float getPartnerY()
        {
            return posYPartner;
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

        public NodeMP getNode()
        {
            return node;
        }
    }
}
