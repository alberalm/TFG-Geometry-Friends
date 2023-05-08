using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GeometryFriends.AI.Perceptions.Information;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI;
using GeometryFriends.AI.Debug;
using System.Diagnostics;

namespace GeometryFriendsAgents
{
    class CircleCoopAgent
    {
        private CoopRules coopRules;
        private CircleSingleplayer circleAgent;
        private int state;

        public CircleCoopAgent(Rectangle area, CollectibleRepresentation[] diamonds, ObstacleRepresentation[] platforms, ObstacleRepresentation[] rectanglePlatforms, ObstacleRepresentation[] circlePlatforms, CircleSingleplayer circleSingleplayer)
        {
            state = 0; // Getting singleplayer diamonds
            coopRules = new CoopRules(area, diamonds, platforms, rectanglePlatforms, circlePlatforms);
            circleAgent = circleSingleplayer;
        }

        public void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
            //Splits the diamonds into each category
            coopRules.ApplyRules(cI, rI);
            circleAgent.Setup(nI, rI, cI, oI, rPI, cPI, coopRules.getCircleDiamonds(), area, timeLimit);
        }

        public void SensorsUpdated(int nC, RectangleRepresentation rI, CircleRepresentation cI, CollectibleRepresentation[] colI)
        {
            switch (state)
            {
                case 0:
                    CollectibleRepresentation[] singleplayerDiamonds = coopRules.updateCircleDiamonds(colI);

                    if (singleplayerDiamonds.Count() == 0)
                    {
                        state = 1; // Coop State
                    }

                    circleAgent.SensorsUpdated(nC, rI, cI, singleplayerDiamonds);
                    break;
            }
            
        }

        public void ActionSimulatorUpdated(ActionSimulator updatedSimulator)
        {
            circleAgent.ActionSimulatorUpdated(updatedSimulator);
        }

        public Moves GetAction()
        {
            switch (state)
            {
                case 0:
                    return circleAgent.GetAction();
            }

            return Moves.NO_ACTION;
        }

        public void Update(TimeSpan elapsedGameTime)
        {
            //All the special things will be implemented here
            circleAgent.Update(elapsedGameTime);
        }

        public DebugInformation[] GetDebugInformation()
        {
            return circleAgent.GetDebugInformation();
        }

    }
}
