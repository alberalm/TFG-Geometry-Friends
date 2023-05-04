using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    public class State
    {
        private float positionX;
        private float positionY;
        private float velocityX;
        private float velocityY;
        private float height;
        private float circleVelocityRadius;

        //For game simulator
        private List<CollectibleRepresentation> caughtCollectibles;
        private List<CollectibleRepresentation> uncaughtCollectibles;

        //For Circle Simulator
        private List<DiamondInfo> caughtDiamonds;
        private List<DiamondInfo> uncaughtDiamonds;

        //Circle Simulator States
        public State(float cPosX, float cPosY, float cVelX, float cVelY, float cH, float cVelRad, List<DiamondInfo> cC, List<DiamondInfo> uC)
        {
            positionX = cPosX;
            positionY = cPosY;
            velocityX = cVelX;
            velocityY = cVelY;
            height = cH;
            circleVelocityRadius = cVelRad;
            caughtDiamonds = cC;
            uncaughtDiamonds = uC;
        }

        //Game Simulator states
        public State(float cPosX, float cPosY, float cVelX, float cVelY, float cH, float cVelRad, List<CollectibleRepresentation> cC, List<CollectibleRepresentation> uC)
        {
            positionX = cPosX;
            positionY = cPosY;
            velocityX = cVelX;
            velocityY = cVelY;
            height = cH;
            circleVelocityRadius = cVelRad;
            caughtCollectibles = cC;
            uncaughtCollectibles = uC;
        }

        public float getPosX()
        {
            return positionX;
        }

        public float getPosY()
        {
            return positionY;
        }

        public float getVelX()
        {
            return velocityX;
        }

        public float getVelY()
        {
            return velocityY;
        }

        public float getHeight()
        {
            return height;
        }

        /*********************************************************/
        /**************** Game Simulator Methods *****************/
        /*********************************************************/

        public int getNumberCaughtCollectibles()
        {
            return caughtCollectibles.Count;
        }

        public int getNumberUncaughtCollectibles()
        {
            return uncaughtCollectibles.Count;
        }

        public List<CollectibleRepresentation> getCaughtCollectibles()
        {
            return caughtCollectibles;
        }

        public List<CollectibleRepresentation> getUncaughtCollectibles()
        {
            return uncaughtCollectibles;
        }

        public void addCaughtCollectibles(List<CollectibleRepresentation> collectibles)
        {
            //actualizes all the collectibles that were caught without duplicating
            foreach (CollectibleRepresentation collectible in collectibles)
            {
                if (!caughtCollectibles.Contains(collectible))
                {
                    caughtCollectibles.Add(collectible);
                }
            }
        }

        /***********************************************************/
        /**************** General Simulator Methods ****************/
        /***********************************************************/

        public int getNumberCaughtDiamonds()
        {
            return caughtDiamonds.Count;
        }

        public int getNumberUncaughtDiamonds()
        {
            return uncaughtDiamonds.Count;
        }
        

        public List<DiamondInfo> getCaughtDiamonds()
        {
            return caughtDiamonds;
        }

        public List<DiamondInfo> getUncaughtDiamonds()
        {
            return uncaughtDiamonds;
        }

        public void addCaughtDiamonds(List<DiamondInfo> diamonds)
        {
            //actualizes all the collectibles that were caught without duplicating
            foreach(DiamondInfo collectible in diamonds)
            {
                if (!caughtDiamonds.Contains(collectible))
                {
                    caughtDiamonds.Add(collectible);
                }
            }
        }

       
    }
}
