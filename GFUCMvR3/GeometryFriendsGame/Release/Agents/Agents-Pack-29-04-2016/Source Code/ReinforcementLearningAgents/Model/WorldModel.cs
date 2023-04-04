using GeometryFriends.AI;
using GeometryFriendsAgents.GameStates;
using GeometryFriendsAgents.Managers;
using GeometryFriendsAgents.WorldObjects;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GeometryFriendsAgents.Model {
    abstract class WorldModel {

        public const int COLLECTIBLE_HEIGHT = 27;

        protected Platform startPlatform;
        protected GraphLink link;

        protected DateTime lastLinkTime = DateTime.Now;

        public GraphLink ClosestLink {
            get {
                if (link == null) {
                    link = GetClosestLink();
                }

                return link;
            }
        }

        protected PlatformManager platformManager;

        protected float _agentXPosition;

        public float AgentXPosition {
            get { return _agentXPosition; }
            set { _agentXPosition = value; }
        }

        protected float _agentYPosition;

        public float AgentYPosition {
            get { return _agentYPosition; }
            set { _agentYPosition = value; }
        }

        protected float _agentXSpeed;

        public float AgentXSpeed {
            get { return _agentXSpeed; }
            set { _agentXSpeed = value; }
        }

        protected float _agentYSpeed;

        public float AgentYSpeed {
            get { return _agentYSpeed; }
            set { _agentYSpeed = value; }
        }

        protected Rectangle _area;

        public Rectangle Area {
            get { return _area; }
            set { _area = value; }
        }

        public List<Platform> Platforms {
            get { return platformManager.GetPlatforms(); }
        }

        public abstract GameState GetGameState();


        private double timeLimit;

        public double TimeLimit {
            get { return timeLimit; }
            set { timeLimit = value; }
        }

        public int NumberCollectiblesCaught {
            get { return NumberCollectiblesGlobal - NumberCollectiblesRemaining; }
        }

        private int timeElapsed;
        public int TimeElapsed {
            get { return timeElapsed; }
            set { timeElapsed = value; }
        }

        // -1 to avoid getting points for just finishing the level
        public double EndStateValue {
            get { return (100 * NumberCollectiblesCaught) + (timeLimit / TimeElapsed) - 1; }
        }

        public int NumberCollectiblesRemaining { get; set; }

        public int NumberCollectiblesGlobal { get; set; }

        private List<Collectible> remainingCollectibles;

        public List<Collectible> RemainingCollectibles {
            set { remainingCollectibles = value; }
        }

        protected Collectible GetClosestCollectible() {
            List<Collectible> possilbeCollectibles = new List<Collectible>(AgentPlatform.Collectibles);
            foreach (Collectible collectible in possilbeCollectibles) {
                collectible.Distance = calculateDistance(collectible);
            }
            possilbeCollectibles.Sort();
            return possilbeCollectibles[0];
        }

        private double calculateDistance(Collectible collectible) {
            return Math.Sqrt(Math.Pow(AgentXPosition - collectible.xPos, 2) + (Math.Pow(AgentYPosition - collectible.yPos, 2)));
        }

        internal void DistributeCollectbles() {
            platformManager.DistributeCollectibles(remainingCollectibles);
        }

        public Platform AgentPlatform {
            get {
                return platformManager.FindAgentPlatform(_agentXPosition, _agentYPosition);
            }
        }

        internal void InitPlatforms(float[] oI, float[] aI, int numberGeneric, int numberAgent, float[] agentInfo) {
            platformManager.AddPlatforms(oI, aI, numberGeneric, numberAgent, agentInfo);
        }

        internal void RefreshPlatforms(float[] agentInfo) {
            platformManager.RefreshPlatforms(agentInfo);
        }

        internal void EndGame(int collectiblesCaught, int timeElapsed, Dictionary<int, List<IntraPlatformPlayedGameStateInfo>> playedStates) {
            TimeElapsed = timeElapsed;
            NumberCollectiblesRemaining = NumberCollectiblesGlobal - collectiblesCaught;
            foreach (int platformId in playedStates.Keys) {
                Platform p = platformManager.GetPlatform(platformId);
                int numberOfStates = playedStates[platformId].Count;
                p.StartTime = playedStates[platformId][0].PlayedTime;
                if (p.PercentageCollectiblesCaught == 100) {
                    p.EndTime = playedStates[platformId][numberOfStates - 1].PlayedTime;
                } else {
                    p.EndTime = p.StartTime.AddSeconds(timeElapsed);
                }


            }
        }

        internal void EndGame(int collectiblesCaught, int timeElapsed, Dictionary<int, List<IntraPlatformPlayedGameStateInfo>> intraPlatformStates, Dictionary<int, List<InterPlatformPlayedGameStateInfo>> interPlatformStates) {
            TimeElapsed = timeElapsed;
            NumberCollectiblesRemaining = NumberCollectiblesGlobal - collectiblesCaught;
            foreach (int platformId in intraPlatformStates.Keys) {
                Platform p = platformManager.GetPlatform(platformId);
                int numberOfStates = intraPlatformStates[platformId].Count;
                p.StartTime = intraPlatformStates[platformId][0].PlayedTime;
                if (p.PercentageCollectiblesCaught == 100) {
                    p.EndTime = intraPlatformStates[platformId][numberOfStates - 1].PlayedTime;
                } else {
                    p.EndTime = p.StartTime.AddSeconds(timeElapsed);
                }
            }
            foreach (int link in interPlatformStates.Keys) {
                int numberOfStates = interPlatformStates[link].Count;
                GraphLink myLink = GetGraph().GetGraphLink(link);
                myLink.StartTime = interPlatformStates[link][0].PlayedTime;
                myLink.EndTime = interPlatformStates[link][0].PlayedTime.AddSeconds(timeElapsed);
            }

        }

        internal Platform GetPlatform(int platformId) {
            return platformManager.GetPlatform(platformId);
        }

        public abstract Moves FindMoveTowardsClosestCollectible();

        public Graph GetGraph() {
            return platformManager.GetGraph();
        }

        internal abstract bool isOnPlatform(Platform p, Collectible c);

        internal GraphLink GetClosestLink() {
            List<GraphLink> links = platformManager.GetGraph().GetNextLinks(AgentPlatform);

            double distance = double.MaxValue;
            GraphLink link = null;

            foreach (GraphLink l in links) {
                double linkDistance = Utils.Utils.CalculateL2(AgentXPosition, AgentYPosition, l.FromXPos, l.FromNode.Platform.Top);
                if (linkDistance < distance) {
                    distance = linkDistance;
                    link = l;
                }
            }
            return link;
        }

        public abstract bool WillHaveMoves();

        public void VerifyPlatform() {
            Platform lastPlatform = AgentPlatform;
            if (lastPlatform != null) {
                if (startPlatform != null && !lastPlatform.Equals(startPlatform)) {
                    if (link != null && link.Equals(ClosestLink)) {
                        SaveLink();
                    }
                    startPlatform = null;
                    link = null;
                } else {
                    startPlatform = lastPlatform;
                }
                if ((DateTime.Now - lastLinkTime).TotalSeconds >= 0.5) {
                    SetClosestLink();
                }
            }
        }

        protected abstract void SaveLink();

        public void SetClosestLink() {
            link = GetClosestLink();
        }
        
    }
}
