using GeometryFriends.AI;
using GeometryFriendsAgents.GameStates;
using GeometryFriendsAgents.Managers;
using GeometryFriendsAgents.Model;
using GeometryFriendsAgents.WorldObjects;
using System;

namespace GeometryFriendsAgents {
    class SquareWorldModel : WorldModel {

        private RectangleAgent agent;

        public SquareWorldModel(RectangleAgent agent)
            : base() {
            platformManager = new SquarePlatformManager(this);
            this.agent = agent;
        }

        public const int SQUARE_AREA = 100 * 100;
        public const int DEFAULT_HEIGHT = 100;
        public const int MIN_HEIGHT = 52;

        private float _height;

        public float Height {
            get { return _height; }
            set { _height = value; }
        }

        public override GameState GetGameState() {
            if (ClosestLink == null) {
                return new SquareGameState(AgentXPosition, AgentYPosition, AgentXSpeed, AgentPlatform, this);
            } else {
                return new SquareGameState(AgentXPosition, AgentYPosition, AgentXSpeed, AgentPlatform, this, ClosestLink);
            }
        }

        public override Moves FindMoveTowardsClosestCollectible() {

            if (AgentPlatform != null) {
                if (AgentPlatform.PercentageCollectiblesCaught < 100) {
                    Collectible closest = GetClosestCollectible();

                    if (BelowClosestCollectible(closest)) {
                        return Moves.MORPH_UP;
                    } else if (_height > DEFAULT_HEIGHT) {
                        return Moves.MORPH_DOWN;
                    } else if (AgentXPosition < closest.xPos - DEFAULT_HEIGHT / 4) {
                        return Moves.MOVE_RIGHT;
                    } else {
                        return Moves.MOVE_LEFT;
                    }
                } else {

                    double realDistance = ClosestLink.FromXPos - AgentXPosition;
                    double minDistance = Math.Abs(realDistance);

                    if (minDistance < MIN_HEIGHT / 2f && ClosestLink.DeltaY > 0 && _agentXSpeed / realDistance > 0) {
                        if (ClosestLink.FromNode.Platform.Top < ClosestLink.ToNode.Platform.Top) {
                            return Moves.MORPH_UP;
                        } else {
                            return Moves.MORPH_DOWN;
                        }
                    }
                    if (realDistance < 0) {
                        return Moves.MOVE_LEFT;
                    } else {
                        return Moves.MOVE_RIGHT;
                    }
                }
            } else {
                if (ClosestLink != null) {
                    double realDistance = ClosestLink.FromXPos - AgentXPosition;
                    double minDistance = Math.Abs(realDistance);

                    if (minDistance < DEFAULT_HEIGHT && ClosestLink.DeltaY < 0 && _agentXSpeed / realDistance > 0) {
                        if (ClosestLink.DeltaY < 0) {
                            return Moves.MORPH_UP;
                        } else {
                            return Moves.MORPH_DOWN;
                        }
                    }
                    if (realDistance < 0) {
                        return Moves.MOVE_LEFT;
                    } else {
                        return Moves.MOVE_RIGHT;
                    }
                }

                return Moves.MOVE_LEFT;
            }
        }

        private bool BelowClosestCollectible(Collectible closest) {
            return closest.xPos < AgentXPosition + SquareWorldModel.DEFAULT_HEIGHT / 2 && closest.xPos > AgentXPosition - SquareWorldModel.DEFAULT_HEIGHT / 2;
        }

        internal override bool isOnPlatform(Platform p, Collectible c) {
            if (c.yPos > p.Top - 2 * DEFAULT_HEIGHT - WorldModel.COLLECTIBLE_HEIGHT) {
                return c.xPos >= p.Left - DEFAULT_HEIGHT / 2 && c.xPos <= p.Right + DEFAULT_HEIGHT / 2;
            } else {
                return false;
            }
        }

        public override bool WillHaveMoves() {
            if (AgentPlatform != null) {
                if (AgentPlatform.PercentageCollectiblesCaught < 100) {
                    return true;
                } else if (ClosestLink != null) {
                    return true;
                }
            }
            return false;
        }

        //public override void VerifyPlatform() {
        //    if (AgentPlatform != null) {
        //        if (startPlatform != null && AgentPlatform.Equals(startPlatform)) {
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

        protected override void SaveLink() {
            agent.LearningCenter.CloseLink(link, DateTime.Now);
        }
    }
}
