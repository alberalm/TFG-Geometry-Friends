
using GeometryFriends.AI;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Drawing;

namespace GeometryFriendsAgents
{
    /// <summary>
    /// Dummy circle agent to prevent game craches since there was no implementation of a subgoalA* circle agent
    /// </summary>
    public class CircleAgent : AbstractCircleAgent
    {
        public CircleAgent()
        {
        }

        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
        }
        
        public override void SensorsUpdated(int nC, RectangleRepresentation rI, CircleRepresentation cI, CollectibleRepresentation[] colI)
        {
        }


        public override bool ImplementedAgent()
        {
            return false;
        }

        //Manager gets this action from agent
        public override Moves GetAction()
        {
            return Moves.NO_ACTION; //does nothing
        }

        public override void Update(TimeSpan elapsedGameTime)
        {
        }

        public override void EndGame(int collectiblesCaught, int timeElapsed)
        {
        }

        public override string AgentName()
        {
            return "Dummy SuboalAI Circle";
        }
    }
}


