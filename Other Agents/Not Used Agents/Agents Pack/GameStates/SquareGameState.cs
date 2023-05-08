using GeometryFriendsAgents.Model;
using GeometryFriendsAgents.WorldObjects;
using System;

namespace GeometryFriendsAgents.GameStates {
    class SquareGameState : SixthGameState {

        private SquareWorldModel _model;

        public SquareGameState(float xPos, float yPos, float xSpeed, Platform agentPlatform, SquareWorldModel model, GraphLink closestLink)
            : base(xPos, yPos, xSpeed, agentPlatform, closestLink) {
            _model = model;
        }

        public SquareGameState(float xPos, float yPos, float xSpeed, Platform agentPlatform, SquareWorldModel model)
            : base(xPos, yPos, xSpeed, agentPlatform) {
            _model = model;
        }

        public override string GetStateId() {
            if(AgentPlatform == null) {
                return InLinkStateId();
            } else if (AgentPlatform.PercentageCollectiblesCaught < 100) {
                return GetAvailableCollectiblesStateId();
            } else {
                return GetNoCollectiblesStateId();
            }
        }

        private string InLinkStateId() {
            string s = "";

            GraphLink l = null;

            if (l == null) {
                return "In no link";
            }

            int height = (int)(_model.Height - SquareWorldModel.MIN_HEIGHT) / 5;
            int speed = ((int)xSpeed / 20);

            s += "/Speed: " + speed;
            s += "/Height: " + height;

            if (l.DeltaY != 0) {
                int jump = (int)l.DeltaY / 25;
                s += "/Vertical Jump: " + jump;
            } else {
                int unitVector;
                int distance;
                if (l.DeltaX < 0) {
                    unitVector = -1;
                    distance = (int)(l.ToNode.Platform.Right - _model.AgentXPosition) / 60;
                } else {
                    unitVector = 1;
                    distance = (int)(l.ToNode.Platform.Left - _model.AgentXPosition) / 60;
                }
                s += "/UnitVector = " + unitVector;
                s += "/Distance = " + distance;
            }

            return s;
        }

        private string GetAvailableCollectiblesStateId() {
            int agentPosition = CalculatePercentage(xPos);

            Collectible c = GetClosestCollectible();

            int closestCollectiblePosition = CalculatePercentage(c.xPos);

            int unitVector;

            if (agentPosition < closestCollectiblePosition) {
                unitVector = 1;
            } else {
                unitVector = -1;
            }

            int height = (int)(_model.Height - SquareWorldModel.MIN_HEIGHT) / 5;
            int speed = ((int)xSpeed / 20);

            string s = "";

            if (agentPosition < 20 || agentPosition > 80) {
                s += "Pos:NearEnd";
            } else {
                s += "Pos:Middle";
            }

            s += "/Speed:" + speed;

            s += "/Height:" + height;


            int numberCollectibles = AgentPlatform.Collectibles.Count;

            s += "/MoreColls:";
            if (numberCollectibles > 1) {
                s += "1";
            } else {
                s += "0";
            }

            s += "/UnitVector:" + unitVector.ToString();

            if (unitVector == 1) {
                if (closestCollectiblePosition > 80) {
                    s += "/Col:NearEnd";
                    if (AgentPlatform.RightWall) {
                        s += "/End:Wall";
                    }
                }

            } else {
                if (closestCollectiblePosition < 20) {
                    s += "/Col:NearEnd";
                    if (AgentPlatform.LeftWall) {
                        s += "/Wall";
                    }
                }
            }

            if (c.yPos <= AgentPlatform.Top - SquareWorldModel.DEFAULT_HEIGHT - WorldModel.COLLECTIBLE_HEIGHT) {
                s += "/Col:OnPlatform";
            } else {
                s += "/Col:MorphUp";
                if (Math.Abs(agentPosition - closestCollectiblePosition) <= 20) {
                    s += "/Below";
                } else {
                    s += "/NotBelow";
                }
            }



            return s;
        }


        private string GetNoCollectiblesStateId() {
            String s = "";

            double realDistance = ClosestLink.FromXPos - xPos;
            double minDistance = Math.Abs(realDistance);

            int speed = ((int)xSpeed / 20);

            s += "Speed:" + speed;

            int vectorToClosestLinkPoint;
            if (realDistance < 0) {
                vectorToClosestLinkPoint = -1;
            } else {
                vectorToClosestLinkPoint = 1;
            }

            s += "/VectorToLP:" + vectorToClosestLinkPoint;

            int pos = (int)minDistance / 120;
            s += "/Pos:" + pos;

            if (ClosestLink.DeltaY == 0) {
                if (ClosestLink.DeltaX > 0) {
                    if (ClosestLink.FromNode.Platform.RightWall) {
                        s += "/Horizontal Below";
                    } else {
                        s += "/Horizontal Normal";
                    }
                } else if (ClosestLink.FromNode.Platform.LeftWall) {
                    s += "/Horizontal Below";
                } else {
                    s += "/Horizontal Normal";
                }
            } else if (ClosestLink.DeltaY < 0) {
                s += "/Vertical Up";
            } else {
                s += "/Vertical Down";
            }
            return s;
        }
    }
}
