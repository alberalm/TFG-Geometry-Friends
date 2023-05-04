using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class RectangleSTP
    {
        /******STP variables ******/
        private float skillYmargin = 70;
        private float skillXmargin = 70;
        private float skillVelMargin = 10;
        private float skillMorphBias = 0.5f;

        private float agentMaxReach = 100;
        private float morphBias = 0.3f; //TODO test
        private Utils utils;
        private Random rnd;
        private ObstacleRepresentation[] platforms;
        private Rectangle area;

        public RectangleSTP(Utils utls, Random rand, ObstacleRepresentation[] pltfrms, Rectangle a)
        {
            utils = utls;
            rnd = rand;
            platforms = pltfrms;
            area = a;
        }

        public Moves skillHigherPlatform(Node node, DiamondInfo dInfo)
        {
            State agent = node.getState();
            Platform platform = dInfo.getPlatform();
            //TODO check if agent can get to the platform
            //if (Math.Abs(agent.getPosY() - (platform.getY() - platform.getHeight() / 2)) < circleMaxJump)
            //{
            //    //if agent is under the platform - roll to one of the sides
            //    if (agent.getPosX() >= platform.getX() - platform.getWidth() / 2 && agent.getPosX() <= platform.getX() + platform.getWidth() / 2)
            //    {
            //        //TODO - check the there isn't a wall
            //        if (node.getRemainingMoves().Exists(x => x == Moves.ROLL_LEFT))
            //        {
            //            return Moves.ROLL_LEFT;
            //        }
            //        else if (node.getRemainingMoves().Exists(x => x == Moves.ROLL_RIGHT))
            //        {
            //            return Moves.ROLL_RIGHT;
            //        }
            //        else
            //        {
            //            node.noRemainingSTPActions();
            //            return Moves.NO_ACTION;
            //        }
            //    }
            //    else
            //    {
            //        //TODO - check if there isn't another platform in the way of the jump
            //        //if the agent is at the platform's left, roll right or jump
            //        if (agent.getPosX() < platform.getX())
            //        {
            //            if (node.getRemainingMoves().Exists(x => x == Moves.ROLL_RIGHT))
            //            {
            //                return Moves.ROLL_RIGHT;
            //            }
            //            else if (node.getRemainingMoves().Exists(x => x == Moves.JUMP))
            //            {
            //                return Moves.JUMP;
            //            }
            //            else
            //            {
            //                node.noRemainingSTPActions();
            //                return Moves.NO_ACTION;
            //            }
            //        }
            //        //if the agent is at the platform's right, roll left or jump
            //        else
            //        {
            //            if (node.getRemainingMoves().Exists(x => x == Moves.ROLL_LEFT))
            //            {
            //                return Moves.ROLL_LEFT;
            //            }
            //            else if (node.getRemainingMoves().Exists(x => x == Moves.JUMP))
            //            {
            //                return Moves.JUMP;
            //            }
            //            else
            //            {
            //                node.noRemainingSTPActions();
            //                return Moves.NO_ACTION;
            //            }
            //        }

            //    }
            //}
            //else
            //{
                return utils.randomAction(node, rnd, morphBias);
            //}

        }

        public Moves skillLowerPlatform(Node node, DiamondInfo dInfo)
        {
            State agent = node.getState();
            Platform platform = dInfo.getPlatform();

            //if the diamond cant be reached from its platform then this skill does not apply
            if (Math.Abs(dInfo.getY() - (dInfo.getPlatform().getY() - dInfo.getPlatform().getHeight() / 2)) > agentMaxReach)
            {
                //TODO - create a new skill
                return utils.randomAction(node, rnd, morphBias);
            }

            //check if the agent is in between walls
            bool leftWall = false;
            bool rightWall = false;
            Platform agentPlatform = utils.onPlatform(agent.getPosX(), agent.getPosY(), 50, 10);
            if (agentPlatform != null && utils.obstacleBetween(agent.getPosX(), agentPlatform.getX() - agentPlatform.getWidth() / 2, agentPlatform))
            {
                leftWall = true;
            }
            if (agentPlatform != null && utils.obstacleBetween(agent.getPosX(), agentPlatform.getX() + agentPlatform.getWidth() / 2, agentPlatform))
            {
                rightWall = true;
            }

            //if the agent is completely stuck
            //TODO make a new skill to differentiate the cases when the agent is completely stuck from the ones when it only has one wall
            if (leftWall || rightWall)
            {
                //TODO - create a new skill
                return utils.randomAction(node, rnd, morphBias);
            }

            //roll to the one of the sides
            if (node.getRemainingMoves().Exists(x => x == Moves.MOVE_LEFT))
            {
                return Moves.MOVE_LEFT;
            }
            else if (node.getRemainingMoves().Exists(x => x == Moves.MOVE_RIGHT))
            {
                return Moves.MOVE_RIGHT;
            }
            else
            {
                node.noRemainingSTPActions();
                return Moves.NO_ACTION;
            }

        }

        public Moves skillCatchDiamond(Node node, DiamondInfo dInfo)
        {

            //if the agent is below the diamond at a very low velocity then morph up
            if (Math.Abs(dInfo.getX() - node.getState().getPosX()) <= skillXmargin &&
                    Math.Abs(node.getState().getVelX()) < skillVelMargin)
            {
                if (node.getRemainingMoves().Exists(x => x == Moves.MORPH_UP))
                {
                    return Moves.MORPH_UP;
                }
                else
                {
                    node.noRemainingSTPActions();
                    return Moves.NO_ACTION;
                }
            }
            //if the diamond is at the agents left
            else if (dInfo.getX() < node.getState().getPosX())// && Math.Abs(dInfo.getX() - node.getState().getPosX()) > skillXmargin)
            {
                //if it is at the same level - go left
                if (Math.Abs(dInfo.getY() - node.getState().getPosY()) < skillYmargin)
                {
                    if (node.getRemainingMoves().Exists(x => x == Moves.MOVE_LEFT))
                    {
                        return Moves.MOVE_LEFT;
                    }
                    else
                    {
                        node.noRemainingSTPActions();
                        return Moves.NO_ACTION;
                    }
                }
                //if not, then it will be necessary to jump at some point
                else
                {

                    if ((rnd.NextDouble() > skillMorphBias ||
                            node.getState().getVelX() > 0) && node.getRemainingMoves().Exists(x => x == Moves.MOVE_LEFT))
                    {
                        return Moves.MOVE_LEFT;
                    }
                    else
                    {
                        //make sure that it wont try the action jump if moving right 
                        if (node.getRemainingMoves().Exists(x => x == Moves.MORPH_UP) && node.getState().getVelX() < 0)
                        {
                            return Moves.MORPH_UP;
                        }
                        else
                        {
                            node.noRemainingSTPActions();
                            return Moves.NO_ACTION;
                        }
                    }
                }
            }
            //if the diamond is at the agents right
            if (dInfo.getX() > node.getState().getPosX())// && Math.Abs(dInfo.getX() - node.getState().getPosX()) > skillXmargin)
            {
                //if it is at the same level - go right
                if (Math.Abs(dInfo.getY() - node.getState().getPosY()) < skillYmargin)
                {
                    if (node.getRemainingMoves().Exists(x => x == Moves.MOVE_RIGHT))
                    {
                        return Moves.MOVE_RIGHT;
                    }
                    else
                    {
                        return Moves.NO_ACTION;
                    }
                }
                //if not, then it will be necessary to jump at some point
                else
                {
                    if ((rnd.NextDouble() > skillMorphBias ||
                        node.getState().getVelX() < 0) && node.getRemainingMoves().Exists(x => x == Moves.MOVE_RIGHT))
                    {
                        return Moves.MOVE_RIGHT;
                    }
                    else
                    {
                        //make sure that it wont try the action jump if moving left
                        if (node.getRemainingMoves().Exists(x => x == Moves.MORPH_UP) && node.getState().getVelX() > 0)
                        {
                            return Moves.MORPH_UP;
                        }
                        else
                        {
                            node.noRemainingSTPActions();
                            return Moves.NO_ACTION;
                        }
                    }
                }
            }

            return Moves.NO_ACTION;
        }
    }
}
