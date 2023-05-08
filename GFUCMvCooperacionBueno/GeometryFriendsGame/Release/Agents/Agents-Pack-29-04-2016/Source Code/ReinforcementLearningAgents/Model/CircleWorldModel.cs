using GeometryFriends.AI;
using GeometryFriendsAgents.GameStates;
using GeometryFriendsAgents.Managers;
using GeometryFriendsAgents.Model;
using GeometryFriendsAgents.WorldObjects;
using System;

namespace GeometryFriendsAgents {
    class CircleWorldModel : WorldModel {

        public CircleWorldModel(CircleAgent agent)
            : base() {
            platformManager = new CirclePlatformManager(this);
            this.agent = agent;
        }

        private CircleAgent agent;

        private float _agentRadius;

        public float AgentRadius {
            get { return _agentRadius; }
            set { _agentRadius = value; }
        }

        public const int DEFAULT_RADIUS = 40;

        public override GameState GetGameState() {
            if (ClosestLink == null) {
                return new CircleGameState(AgentXPosition, AgentYPosition, AgentXSpeed, AgentPlatform, this);
            } else {
                return new CircleGameState(AgentXPosition, AgentYPosition, AgentXSpeed, AgentPlatform, this, ClosestLink);
            }
        }

        public override Moves FindMoveTowardsClosestCollectible() {

            if (AgentPlatform.PercentageCollectiblesCaught < 100) {
                Collectible closest = GetClosestCollectible();
                bool jump = false;

                if (closest.yPos < AgentYPosition - 3 * DEFAULT_RADIUS) {
                    jump = true;
                }
                

                if (AgentXPosition < closest.xPos - 3* DEFAULT_RADIUS) {
                    return Moves.ROLL_RIGHT;
                } else if (AgentXPosition > closest.xPos + 3* DEFAULT_RADIUS) {
                    return Moves.ROLL_LEFT;
                } else {
                    if (!jump || (jump && (AgentXSpeed == 0 || AgentXSpeed/(closest.xPos-AgentXPosition) < 0))) {
                        if (AgentXPosition < closest.xPos) {
                            return Moves.ROLL_RIGHT;
                        } else {
                            return Moves.ROLL_LEFT;
                        }
                    }
                    return Moves.JUMP;
                }
            } else {
                
                double realDistance = ClosestLink.FromXPos - AgentXPosition;
                double minDistance = Math.Abs(realDistance);

                if (minDistance < Math.Abs(_agentXSpeed / 35) * CircleWorldModel.DEFAULT_RADIUS && ClosestLink.DeltaY <= 0 && _agentXSpeed / realDistance > 0) {
                    if (ClosestLink.FromXPos <= ClosestLink.ToNode.Platform.Left && _agentXSpeed / 20 > 5) {
                        return Moves.JUMP;
                    }
                    if (ClosestLink.FromXPos >= ClosestLink.ToNode.Platform.Right && _agentXSpeed / 20 < -5) {
                        return Moves.JUMP;
                    }
                }


                if (realDistance < 0) {
                    return Moves.ROLL_LEFT;
                } else {
                    return Moves.ROLL_RIGHT;
                }

            }
        }

        internal override bool isOnPlatform(GeometryFriendsAgents.WorldObjects.Platform p, GeometryFriendsAgents.WorldObjects.Collectible c) {
            if (c.yPos > p.Top - 2 * DEFAULT_RADIUS - WorldModel.COLLECTIBLE_HEIGHT) {
                return c.xPos >= p.Left - DEFAULT_RADIUS && c.xPos <= p.Right + DEFAULT_RADIUS;
            } else {
                return c.xPos >= p.Left - 3 * DEFAULT_RADIUS && c.xPos <= p.Right + 3 * DEFAULT_RADIUS;
            }
        }

        public override bool WillHaveMoves() {
            Platform agentPlatform = AgentPlatform; //prevent changes in the platform between it's usage between the two conditions below
            if (agentPlatform != null)
            {
                if (agentPlatform.PercentageCollectiblesCaught < 100)
                {
                    return true;
                } else if (ClosestLink != null) {
                    return true;
                }
            }

            return false;
        }

        public void SetJumpStart() {
            startPlatform = AgentPlatform;
        }

        protected override void SaveLink() {
            agent.LearningCenter.CloseLink(link, DateTime.Now);
        }

        //public override void VerifyPlatform() {
        //    if (AgentPlatform != null) {
        //        if (AgentPlatform != startPlatform && startPlatform != null) {
        //            if (link != null) {
        //                SaveLink();
        //            }
        //            startPlatform = null;
        //            link = null;
        //        } else {
        //            startPlatform = AgentPlatform;
        //        }
        //        if ((DateTime.Now - lastLinkTime).TotalSeconds >= 0.5) {
        //            SetClosestLink();
        //        }
        //    }
        //}
    }
}
