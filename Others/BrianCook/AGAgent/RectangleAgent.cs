using GeometryFriends.AI;
using GeometryFriends.AI.Communication;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GeometryFriendsAgents
{
    public class RectangleAgent : AbstractRectangleAgent
    {
        string agentName = "DoNothing";
        List<AgentMessage> noMessages = new List<AgentMessage>();

        public RectangleAgent()
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
            return true;
        }

        public override string AgentName()
        {
            return agentName;
        }

        public override Moves GetAction()
        {
            return Moves.NO_ACTION;
        }

        public override void Update(TimeSpan elapsedGameTime)
        {
        }

        public override void EndGame(int collectiblesCaught, int timeElapsed)
        {
        }

        public override List<AgentMessage> GetAgentMessages()
        {
            return noMessages;
        }

        public override void HandleAgentMessages(List<AgentMessage> newMessages)
        {
        }
    }
}