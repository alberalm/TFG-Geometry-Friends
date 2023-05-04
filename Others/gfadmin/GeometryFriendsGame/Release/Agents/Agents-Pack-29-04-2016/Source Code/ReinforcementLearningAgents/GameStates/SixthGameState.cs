using GeometryFriendsAgents.Model;
using GeometryFriendsAgents.WorldObjects;
using System;

namespace GeometryFriendsAgents.GameStates {
    abstract class SixthGameState : GameState {

        protected float _xSpeed;

        public float xSpeed {
            get { return _xSpeed; }
        }

        protected Platform _agentPlatform;

        public Platform AgentPlatform {
            get { return _agentPlatform; }
        }

        private GraphLink _closestLink;

        public GraphLink ClosestLink {
            get { return _closestLink; }
        }

        public SixthGameState(float xPos, float yPos, float xSpeed, Platform agentPlatform, GraphLink closestLink) {
            _xPos = xPos;
            _yPos = yPos;
            _xSpeed = xSpeed;
            if (agentPlatform != null) {
                _agentPlatform = agentPlatform.Clone();
            } else {
                _agentPlatform = null;
            }
            _closestLink = closestLink.Clone();
        }

        public SixthGameState(float xPos, float yPos, float xSpeed, Platform agentPlatform) {
            _xPos = xPos;
            _yPos = yPos;
            _xSpeed = xSpeed;
            if (agentPlatform != null) {
                _agentPlatform = agentPlatform.Clone();
            } else {
                _agentPlatform = null;
            }
            _closestLink = null;
        }

        protected int CalculatePercentageCollectiblesCaught() {
            return AgentPlatform.PercentageCollectiblesCaught;
        }

        protected int GetClosestCollectiblePosition() {
            Collectible closestCollectible = GetClosestCollectible();
            if (closestCollectible != null) {
                return CalculatePercentage(closestCollectible.xPos);
            } else {
                return -1; //no collectible in platform
            }
        }

        public override Collectible GetClosestCollectible() {
            double distance = double.MaxValue;
            Collectible closestCollectible = null;
            foreach (Collectible c in AgentPlatform.Collectibles) {
                if (Utils.Utils.CalculateL2(xPos,yPos,c.xPos,c.yPos) < distance) {
                    distance = Utils.Utils.CalculateL2(xPos, yPos, c.xPos, c.yPos);
                    closestCollectible = c;
                }
            }
            return closestCollectible;
        }

        

        protected int CalculatePercentage(float xPosition) {
            int percentage = (int)(((xPosition - AgentPlatform.Left) / AgentPlatform.Width) * 100);
            return percentage;
        }


        public override double GetStateValue() {
            double value = AgentPlatform.PercentageCollectiblesCaught * 100 + 50 * (1 - ((float)Math.Abs(CalculatePercentage(xPos) - GetClosestCollectiblePosition())) / 100.0);
            return value;
        }


        public override double GetDistanceCollectible() {
            Collectible c = GetClosestCollectible();

            return Utils.Utils.CalculateL2(xPos, yPos, c.xPos, c.yPos);
        }


        public override int GetPercentageCollectibleCaught() {
            return AgentPlatform.PercentageCollectiblesCaught;
        }

        public override string GetStateId() {
            throw new NotImplementedException();
        }

        public override int GetNumberCollectiblesCaught() {
            return AgentPlatform.NumberCollectiblesCaught;
        }
    }
}
