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
    class CircleSTPMP
    {
        /******STP variables ******/
        private float skillYmargin = 70;
        private float skillXmargin = 70;
        private float skillVelMargin = 10;
        private float skillJumpBias = 0.5f;
        private float diamondXMargin = 100;
        private float onRectanglePositionMargin = 100;
        private float rectanglePositionMargin = 100;

        private float platformYMargin = 10;
        private float circleMaxJump = 400;
        private float rectangleMaxHeight = 193;
        private float jumpBias = 0.3f; //TODO test
        private float radius;
        private Utils utils;
        private Random rnd;
        private ObstacleRepresentation[] platforms;
        private Rectangle area;

        public CircleSTPMP(Utils utls, Random rand, ObstacleRepresentation[] pltfrms, Rectangle a, float rad)
        {
            utils = utls;
            rnd = rand;
            platforms = pltfrms;
            area = a;
            radius = rad;
        }

        private Moves tryLeftAndJump(NodeMP node, List<Moves[]> possibleMoves)
        {
            if (possibleMoves.Exists(x => x[0] == Moves.ROLL_LEFT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_LEFT))
            {
                return Moves.ROLL_LEFT;
            }
            else if (possibleMoves.Exists(x => x[0] == Moves.JUMP) || possibleMoves.Exists(x => x[1] == Moves.JUMP))
            {
                return Moves.JUMP;
            }
            else
            {
                node.noRemainingSTPActions();
                return utils.randomAction(node, possibleMoves, rnd, jumpBias);
            }
        }

        private Moves tryRightAndJump(NodeMP node, List<Moves[]> possibleMoves)
        {
            if (possibleMoves.Exists(x => x[0] == Moves.ROLL_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_RIGHT))
            {
                return Moves.ROLL_RIGHT;
            }
            else if (possibleMoves.Exists(x => x[0] == Moves.JUMP) || possibleMoves.Exists(x => x[1] == Moves.JUMP))
            {
                return Moves.JUMP;
            }
            else
            {
                node.noRemainingSTPActions();
                return utils.randomAction(node, possibleMoves, rnd, jumpBias);
            }
        }

        private Moves tryJumping(NodeMP node, List<Moves[]> possibleMoves)
        {
            if (possibleMoves.Exists(x => x[0] == Moves.JUMP) || possibleMoves.Exists(x => x[1] == Moves.JUMP))
            {
                return Moves.JUMP;
            }
            else
            {
                node.noRemainingSTPActions();
                return utils.randomAction(node, possibleMoves, rnd, jumpBias);
            }
        }

        private Moves tryNoAction(NodeMP node, List<Moves[]> possibleMoves, int type)
        {
            if(type == 0)
            {
                if (possibleMoves.Exists(x => x[0] == Moves.NO_ACTION))
                {
                    return Moves.NO_ACTION;
                }
                else
                {
                    node.noRemainingSTPActions();
                    return utils.randomAction(node, possibleMoves, rnd, jumpBias);
                }
            }
            else
            {
                if (possibleMoves.Exists(x => x[1] == Moves.NO_ACTION))
                {
                    return Moves.NO_ACTION;
                }
                else
                {
                    node.noRemainingSTPActions();
                    return utils.randomAction(node, possibleMoves, rnd, jumpBias);
                }
            }
        }

        ////use when diamond needs to be caught by the circle using the rectangle as a platform
        //public Moves skillCoopHighDiamond(NodeMP node, DiamondInfo dInfo, List<Moves[]> possibleMoves, int type)
        //{
        //    StateMP state = node.getState();
        //    //if circle is on rectangle
        //    if (utils.onRectangle(state.getPosX() + radius, state.getPosY(), state.getPartnerX(), state.getPartnerY(), state.getHeight(), utils.getRectangleWidth(state.getHeight()), 25))
        //    {
        //        //if it is in position
        //        if(dInfo.getX() > state.getPosX() - onRectanglePositionMargin || dInfo.getX() < state.getPosX() + onRectanglePositionMargin)
        //        {
        //            //JUMP
        //            return tryJumping(node, possibleMoves);
        //        }
        //        else
        //        {
        //            //wait until it is
        //            return tryNoAction(node, possibleMoves, type);
        //        }
        //    }
        //    //if not
        //    else
        //    {
        //        //if rectangle is in position
        //        if (dInfo.getX() > state.getPartnerX() - rectanglePositionMargin || dInfo.getX() < state.getPartnerX() + rectanglePositionMargin)
        //        {
        //            //go to the rectangle side and jump
        //            if(state.getPosX() < dInfo.getX())
        //            {
        //                return tryRightAndJump(node, possibleMoves);
        //            }
        //            else
        //            {
        //                return tryLeftAndJump(node, possibleMoves);
        //            }
                    
        //        }
        //        else
        //        {
        //            //wait until it is
        //            return tryNoAction(node, possibleMoves, type);
        //        }
        //    }
        //}

        //use when diamond is between two close platforms - only the rectangle can get it
        public Moves skillClosePlatforms(NodeMP node, DiamondInfo dInfo, List<Moves[]> possibleMoves, int type)
        {
            Platform dPlatform = dInfo.getPlatform();
            Platform agentPlatform = utils.onPlatform(node.getState().getPosX(), node.getState().getPosY(), 50, platformYMargin);

            StateMP agent = node.getState();
            //check if the circle is above or on the platform - if so, try to get down
            if (agentPlatform != null && dPlatform != null && agentPlatform.getY() <= dPlatform.getY())
            {
                //check if the agent is in between walls
                bool leftWall = false;
                bool rightWall = false;

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
                    return utils.randomAction(node, possibleMoves, rnd, jumpBias);
                }

                //roll to the one of the sides
                if (possibleMoves.Exists(x => x[0] == Moves.ROLL_LEFT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_LEFT))
                {
                    return Moves.ROLL_LEFT;
                }
                else if (possibleMoves.Exists(x => x[0] == Moves.ROLL_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_RIGHT))
                {
                    return Moves.ROLL_RIGHT;
                }
                else
                {
                    node.noRemainingSTPActions();
                    return utils.randomAction(node, possibleMoves, rnd, jumpBias);
                }
            }
            //if below, check if it is possible for the rectangle to get up from there
            else if (agentPlatform != null && dPlatform != null && agentPlatform.getY() > dPlatform.getY() &&
                (agentPlatform.getY() - agentPlatform.getHeight() / 2) - (dPlatform.getY() - dPlatform.getHeight() / 2) < rectangleMaxHeight)
            {
                //if so, go to the diamond direction
                if (dInfo.getX() < agent.getPosX() && (possibleMoves.Exists(x => x[0] == Moves.ROLL_LEFT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_LEFT)))
                {
                    return Moves.ROLL_LEFT;
                }
                else if (dInfo.getX() > agent.getPosX() && (possibleMoves.Exists(x => x[0] == Moves.ROLL_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_RIGHT)))
                {
                    return Moves.ROLL_RIGHT;
                }
                else
                {
                    node.noRemainingSTPActions();
                    return utils.randomAction(node, possibleMoves, rnd, jumpBias);
                }
            }
            else
            {
                node.noRemainingSTPActions();
                return utils.randomAction(node, possibleMoves, rnd, jumpBias);
            }
        }

        public Moves skillHigherPlatform(NodeMP node, DiamondInfo dInfo, List<Moves[]> possibleMoves)
        {
            StateMP agent = node.getState();
            Platform platform = dInfo.getPlatform();
            //check if agent can jump to the platform
            if (Math.Abs(agent.getPosY() - (platform.getY() - platform.getHeight() / 2)) < circleMaxJump)
            {
                //if agent is under the platform - roll to one of the sides
                if (agent.getPosX() >= platform.getX() - platform.getWidth() / 2 && agent.getPosX() <= platform.getX() + platform.getWidth() / 2)
                {
                    //TODO - check the there isn't a wall
                    if (possibleMoves.Exists(x => x[0] == Moves.ROLL_LEFT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_LEFT))
                    {
                        return Moves.ROLL_LEFT;
                    }
                    else if (possibleMoves.Exists(x => x[0] == Moves.ROLL_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_RIGHT))
                    {
                        return Moves.ROLL_RIGHT;
                    }
                    else
                    {
                        node.noRemainingSTPActions();
                        return utils.randomAction(node, possibleMoves, rnd, jumpBias);
                    }
                }
                else
                {
                    //TODO - check if there isn't another platform in the way of the jump
                    //if the agent is at the platform's left, roll right or jump
                    if (agent.getPosX() < platform.getX())
                    {
                        if (possibleMoves.Exists(x => x[0] == Moves.ROLL_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_RIGHT))
                        {
                            return Moves.ROLL_RIGHT;
                        }
                        else if (possibleMoves.Exists(x => x[0] == Moves.JUMP) || possibleMoves.Exists(x => x[1] == Moves.JUMP))
                        {
                            return Moves.JUMP;
                        }
                        else
                        {
                            node.noRemainingSTPActions();
                            return utils.randomAction(node, possibleMoves, rnd, jumpBias);
                        }
                    }
                    //if the agent is at the platform's right, roll left or jump
                    else
                    {
                        if (possibleMoves.Exists(x => x[0] == Moves.ROLL_LEFT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_LEFT))
                        {
                            return Moves.ROLL_LEFT;
                        }
                        else if (possibleMoves.Exists(x => x[0] == Moves.JUMP) || possibleMoves.Exists(x => x[1] == Moves.JUMP))
                        {
                            return Moves.JUMP;
                        }
                        else
                        {
                            node.noRemainingSTPActions();
                            return utils.randomAction(node, possibleMoves, rnd, jumpBias);
                        }
                    }

                }
            }
            else
            {
                return utils.randomAction(node, possibleMoves, rnd, jumpBias);
            }

        }

        public Moves skillLowerPlatform(NodeMP node, DiamondInfo dInfo, List<Moves[]> possibleMoves)
        {
            StateMP agent = node.getState();
            Platform platform = dInfo.getPlatform();

            //if the diamond cant be reached from its platform then this skill does not apply
            if (Math.Abs(dInfo.getY() - (dInfo.getPlatform().getY() - dInfo.getPlatform().getHeight() / 2)) > circleMaxJump)
            {
                //TODO - create a new skill
                return utils.randomAction(node, possibleMoves, rnd, jumpBias);
            }

            //check if the agent is in between walls
            bool leftWall = false;
            bool rightWall = false;
            Platform agentPlatform = utils.onPlatform(agent.getPosX(), agent.getPosY(), 50, platformYMargin);
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
                return utils.randomAction(node, possibleMoves, rnd, jumpBias);
            }

            //roll to the one of the sides
            if (possibleMoves.Exists(x => x[0] == Moves.ROLL_LEFT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_LEFT))
            {
                return Moves.ROLL_LEFT;
            }
            else if (possibleMoves.Exists(x => x[0] == Moves.ROLL_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_RIGHT))
            {
                return Moves.ROLL_RIGHT;
            }
            else
            {
                node.noRemainingSTPActions();
                return utils.randomAction(node, possibleMoves, rnd, jumpBias);
            }

        }

        public Moves skillCatchDiamond(NodeMP node, DiamondInfo dInfo, List<Moves[]> possibleMoves)
        {

            //if the agent is below the diamond at a very low velocity then jump
            if (Math.Abs(dInfo.getX() - node.getState().getPosX()) <= skillXmargin &&
                    Math.Abs(node.getState().getVelX()) < skillVelMargin)
            {
                if (possibleMoves.Exists(x => x[0] == Moves.JUMP) || possibleMoves.Exists(x => x[1] == Moves.JUMP))
                {
                    return Moves.JUMP;
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
                    if (possibleMoves.Exists(x => x[0] == Moves.ROLL_LEFT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_LEFT))
                    {
                        return Moves.ROLL_LEFT;
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

                    if ((rnd.NextDouble() > skillJumpBias ||
                            node.getState().getVelX() > 0) && (possibleMoves.Exists(x => x[0] == Moves.ROLL_LEFT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_LEFT)))
                    {
                        return Moves.ROLL_LEFT;
                    }
                    else
                    {
                        //make sure that it wont try the action jump if moving right 
                        if ((possibleMoves.Exists(x => x[0] == Moves.JUMP) || possibleMoves.Exists(x => x[1] == Moves.JUMP)) && node.getState().getVelX() < 0)
                        {
                            return Moves.JUMP;
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
                    if (possibleMoves.Exists(x => x[0] == Moves.ROLL_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_RIGHT))
                    {
                        return Moves.ROLL_RIGHT;
                    }
                    else
                    {
                        return Moves.NO_ACTION;
                    }
                }
                //if not, then it will be necessary to jump at some point
                else
                {
                    if ((rnd.NextDouble() > skillJumpBias ||
                        node.getState().getVelX() < 0) && (possibleMoves.Exists(x => x[0] == Moves.ROLL_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_RIGHT)))
                    {
                        return Moves.ROLL_RIGHT;
                    }
                    else
                    {
                        //make sure that it wont try the action jump if moving left
                        if ((possibleMoves.Exists(x => x[0] == Moves.JUMP) || possibleMoves.Exists(x => x[1] == Moves.JUMP)) && node.getState().getVelX() > 0)
                        {
                            return Moves.JUMP;
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

        //only call when circle is on the same platform as the diamond
        public Moves skillCoopHighDiamond(NodeMP node, DiamondInfo dInfo, List<Moves[]> possibleMoves, int type)
        {
            //TODO
            StateMP state = node.getState();
            Platform platform = dInfo.getPlatform();

            //check if rectangle is on that platform
            float rectX = state.getPartnerX();
            float rectY = state.getPartnerY();
            float circleX = state.getPosX();
            float rectVelX = state.getPartnerVelX();
            Platform rectPlatform = utils.onPlatform(rectX, rectY, 50, platformYMargin);

            if (rectPlatform.getX() == platform.getX() && rectPlatform.getY() == platform.getY())
            {
                //check if rectangle is in position
                if (Math.Abs(dInfo.getX() - rectX) < diamondXMargin && Math.Abs(rectVelX) < skillVelMargin)
                {
                    //go to rectangle and jump if it is
                    //when circle is at the left, go right
                    if (circleX < rectX)
                    {
                        if ((rnd.NextDouble() > skillJumpBias ||
                        node.getState().getVelX() < 0) && (possibleMoves.Exists(x => x[0] == Moves.ROLL_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_RIGHT)))
                        {
                            return Moves.ROLL_RIGHT;
                        }
                        else
                        {
                            //make sure that it wont try the action jump if moving left
                            if ((possibleMoves.Exists(x => x[0] == Moves.JUMP) || possibleMoves.Exists(x => x[1] == Moves.JUMP)) && node.getState().getVelX() > 0)
                            {
                                return Moves.JUMP;
                            }
                            else
                            {
                                node.noRemainingSTPActions();
                                return Moves.NO_ACTION;
                            }
                        }
                    }
                    //when circle is at the right, go left
                    else
                    {
                        if ((rnd.NextDouble() > skillJumpBias ||
                            node.getState().getVelX() > 0) && (possibleMoves.Exists(x => x[0] == Moves.ROLL_LEFT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_LEFT)))
                        {
                            return Moves.ROLL_LEFT;
                        }
                        else
                        {
                            //make sure that it wont try the action jump if moving right
                            if ((possibleMoves.Exists(x => x[0] == Moves.JUMP) || possibleMoves.Exists(x => x[1] == Moves.JUMP)) && node.getState().getVelX() < 0)
                            {
                                return Moves.JUMP;
                            }
                            else
                            {
                                node.noRemainingSTPActions();
                                return Moves.NO_ACTION;
                            }
                        }
                    }
                }
                //keep still if it is not
                else
                {
                    if (possibleMoves.Exists(x => x[type] == Moves.NO_ACTION))
                    {
                        return Moves.NO_ACTION;
                    }
                    else
                    {
                        node.noRemainingSTPActions();
                        return Moves.NO_ACTION;
                    }
                }
            }
            //when not, check if rectangle is on a platform above 
            if (rectY < platform.getY())
            {
                //wait for the rectangle come down
                if (possibleMoves.Exists(x => x[type] == Moves.NO_ACTION))
                {
                    return Moves.NO_ACTION;
                }
                else
                {
                    node.noRemainingSTPActions();
                    return Moves.NO_ACTION;
                }
            }
            //if below, check if the platform the rectangle is on is high enough to get to the current
            if ((rectPlatform.getY() - rectPlatform.getHeight() / 2) - (platform.getY() - platform.getHeight() / 2) >= rectangleMaxHeight / 2)
            {
                //if not, go help it up
                if (circleX < rectX)
                {
                    if ((rnd.NextDouble() > skillJumpBias ||
                    node.getState().getVelX() < 0) && (possibleMoves.Exists(x => x[0] == Moves.ROLL_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_RIGHT)))
                    {
                        return Moves.ROLL_RIGHT;
                    }
                    else
                    {
                        node.noRemainingSTPActions();
                        return Moves.NO_ACTION;
                    }
                }
                //when circle is at the right, go left
                else
                {
                    if ((rnd.NextDouble() > skillJumpBias ||
                        node.getState().getVelX() > 0) && (possibleMoves.Exists(x => x[0] == Moves.ROLL_LEFT) || possibleMoves.Exists(x => x[1] == Moves.ROLL_LEFT)))
                    {
                        return Moves.ROLL_LEFT;
                    }
                    else
                    {
                        node.noRemainingSTPActions();
                        return Moves.NO_ACTION;
                    }
                }
            }
            //if it is, wait 
            else
            {
                //wait for the rectangle come down
                if (possibleMoves.Exists(x => x[type] == Moves.NO_ACTION))
                {
                    return Moves.NO_ACTION;
                }
                else
                {
                    node.noRemainingSTPActions();
                    return Moves.NO_ACTION;
                }
            }
        }
    }
}
