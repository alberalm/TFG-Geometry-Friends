using GeometryFriendsAgents.Model;
using GeometryFriendsAgents.WorldObjects;
using System;

namespace GeometryFriendsAgents.GameStates {
    class CircleGameState : SixthGameState {

        private CircleWorldModel _model;

        public CircleGameState(float xPos, float yPos, float xSpeed, Platform agentPlatform, CircleWorldModel model, GraphLink closestLink)
            : base(xPos, yPos, xSpeed, agentPlatform, closestLink) {
            _model = model;
        }

        public CircleGameState(float xPos, float yPos, float xSpeed, Platform agentPlatform, CircleWorldModel model)
            : base(xPos, yPos, xSpeed, agentPlatform) {
            _model = model;
        }

        public override string GetStateId() {
            if (AgentPlatform.PercentageCollectiblesCaught < 100) {
                return GetAvailableCollectiblesStateId();
            } else {
                return GetNoCollectiblesStateId();
            }
        }

        private string GetNoCollectiblesStateId() {
            string s = "";
            
            float agentPosition = xPos;            

            int leftPos = (int)(agentPosition - AgentPlatform.Left) / CircleWorldModel.DEFAULT_RADIUS;
            int rightPos = (int)(AgentPlatform.Right - agentPosition) / CircleWorldModel.DEFAULT_RADIUS;

            s += "/-Pos:" + leftPos;
            s += "/+Pos:" + rightPos;

            int speed = ((int)xSpeed / 20);

            s += "/Speed:" + speed.ToString();


            
            double realDistance = ClosestLink.FromXPos - xPos;
            double minDistance = Math.Abs(realDistance);

            int vectorToClosestJumpPoint = (int)(realDistance / CircleWorldModel.DEFAULT_RADIUS);

            s += "/VectorToLP:" + vectorToClosestJumpPoint;

            int vectorX;
            if (ClosestLink.DeltaX == 0) {
                vectorX = 0;
            } else {
                vectorX = (int)ClosestLink.DeltaX / (CircleWorldModel.DEFAULT_RADIUS);
            }

            int vectorY;

            if (ClosestLink.DeltaY == 0) {
                vectorY = 0;
            } else {
                vectorY = (int)ClosestLink.DeltaY / (CircleWorldModel.DEFAULT_RADIUS);
            }


            s += "/JumpVector:[" + vectorX + "/" + vectorY + "]";

            int landingSize = (int)ClosestLink.ToNode.Platform.Width / (CircleWorldModel.DEFAULT_RADIUS);

            s += "/LandingSize:" + landingSize;

            return s;
        }

        private string GetAvailableCollectiblesStateId() {
            float agentPosition = xPos;

            Collectible c = GetClosestCollectible();

            float closestCollectiblePosition = c.xPos;            

            string s = "";

            int leftPos = (int)(agentPosition - AgentPlatform.Left) / CircleWorldModel.DEFAULT_RADIUS;
            int rightPos = (int)(AgentPlatform.Right - agentPosition) / CircleWorldModel.DEFAULT_RADIUS;

            s += "/-Pos:" + leftPos;
            s += "/+Pos:" + rightPos;


            int speed = ((int)xSpeed / 20);

            s += "/Speed:" + speed.ToString();


            int numberCollectibles = AgentPlatform.Collectibles.Count;

            s += "/Colls:" + numberCollectibles;


            int colXVector = (int)((c.xPos - xPos) / (CircleWorldModel.DEFAULT_RADIUS));
            int colYVector = (int)((c.yPos - yPos) / (CircleWorldModel.DEFAULT_RADIUS));

            s += "/ColVector:[" + colXVector + "/" + colYVector + "]";

            if (colXVector >= 1) {
                if (closestCollectiblePosition > AgentPlatform.Right - 5*CircleWorldModel.DEFAULT_RADIUS) {
                    if (AgentPlatform.RightWall) {
                        s += "/End:-Wall";
                    }
                }

            } else {
                if (closestCollectiblePosition < AgentPlatform.Left + 5*CircleWorldModel.DEFAULT_RADIUS) {
                    if (AgentPlatform.LeftWall) {
                        s += "/+Wall";
                    }
                }
            }

            return s;
        }

    }
}
