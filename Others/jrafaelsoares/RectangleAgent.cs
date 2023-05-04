using GeometryFriends;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Communication;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;
using GeometryFriends.XNAStub;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace GeometryFriendsAgents
{
    public class RectangleAgent : AbstractRectangleAgent
    {
        //agent implementation specificiation
        private bool implementedAgent;
        private string agentName = "RRTRectangle2017";

        //game type
        private RectangleSingleplayer singlePlayer;
        private RectangleCoopAgent multiPlayer;
        private int gameMode;

        //for tests
        private bool cutplan = true;
        private bool testing = true;
        private bool timer = true;

        public RectangleAgent()
        {
            //Change flag if agent is not to be used
            implementedAgent = true;
        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                     MAIN SETUP                                       ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        //implements abstract rectangle interface: used to setup the initial information so that the agent has basic knowledge about the level
        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
            singlePlayer = new RectangleSingleplayer(cutplan, testing, timer);

            //check if it is a single or multiplayer level
            if (cI.X < 0 || cI.Y < 0)
            {
                //if the circle has negative position then this is a single player level
                gameMode = 0;
                singlePlayer.Setup(nI, rI, cI, oI, rPI, cPI, colI, area, timeLimit);
            }
            else
            {
                gameMode = 1;
                multiPlayer = new RectangleCoopAgent(area, colI, oI, rPI, cPI, singlePlayer);
                multiPlayer.Setup(nI, rI, cI, oI, rPI, cPI, area, timeLimit);
            }
        }

        //implements abstract rectangle interface: registers updates from the agent's sensors that it is up to date with the latest environment information
        /*WARNING: this method is called independently from the agent update - Update(TimeSpan elapsedGameTime) - so care should be taken when using complex 
         * structures that are modified in both (e.g. see operation on the "remaining" collection)      
         */
        public override void SensorsUpdated(int nC, RectangleRepresentation rI, CircleRepresentation cI, CollectibleRepresentation[] colI)
        {
            if (gameMode == 0)
            {
                singlePlayer.SensorsUpdated(nC, rI, cI, colI);
            }
            else
            {
                multiPlayer.SensorsUpdated(nC, rI, cI, colI);
            }
        }

        //implements abstract circle interface: provides the circle agent with a simulator to make predictions about the future level state
        public override void ActionSimulatorUpdated(ActionSimulator updatedSimulator)
        {
            if (gameMode == 0)
            {
                singlePlayer.ActionSimulatorUpdated(updatedSimulator);
            }
            else
            {
                multiPlayer.ActionSimulatorUpdated(updatedSimulator);
            }
        }

        //implements abstract rectangle interface: signals if the agent is actually implemented or not
        public override bool ImplementedAgent()
        {
            return implementedAgent;
        }

        //implements abstract rectangle interface: provides the name of the agent to the agents manager in GeometryFriends
        public override string AgentName()
        {
            return agentName;
        }

        //implements abstract rectangle interface: GeometryFriends agents manager gets the current action intended to be actuated in the enviroment for this agent
        public override Moves GetAction()
        {
            if (gameMode == 0)
            {
                return singlePlayer.GetAction();
            }
            else
            {
                return multiPlayer.GetAction();
            }
        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                     UPDATE                                           ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        //implements abstract rectangle interface: updates the agent state logic and predictions
        public override void Update(TimeSpan elapsedGameTime)
        {
            if (gameMode == 0)
            {
                singlePlayer.Update(elapsedGameTime);
            }
            else
            {
                multiPlayer.Update(elapsedGameTime);
            }
        }

        //implements abstract circle interface: signals the agent the end of the current level
        public override void EndGame(int collectiblesCaught, int timeElapsed)
        {
            Log.LogInformation("CIRCLE - Collectibles caught = " + collectiblesCaught + ", Time elapsed - " + timeElapsed);
        }

        //implements abstract circle interface: gets the debug information that is to be visually represented by the agents manager
        public override DebugInformation[] GetDebugInformation()
        {
            if (gameMode == 0)
            {
                return singlePlayer.GetDebugInformation();
            }
            else
            {
                return multiPlayer.GetDebugInformation();
            }
        }
    }
}