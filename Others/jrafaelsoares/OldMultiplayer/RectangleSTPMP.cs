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
    class RectangleSTPMP
    {
        /******STP variables ******/
        private float skillYmargin = 70;
        private float skillXmargin = 70;
        private float skillVelMargin = 10;
        private float skillMorphBias = 0.5f;
        private float onRectanglePositionMargin = 100;
        private float rectanglePositionMargin = 100;

        private float radius;
        private float agentMaxReach = 100;
        private float rectangleMaxHeight = 193;
        private float morphBias = 0.3f; //TODO test
        private Utils utils;
        private Random rnd;
        private ObstacleRepresentation[] platforms;
        private Rectangle area;

        public RectangleSTPMP(Utils utls, Random rand, ObstacleRepresentation[] pltfrms, Rectangle a, float rad)
        {
            utils = utls;
            rnd = rand;
            platforms = pltfrms;
            area = a;
            radius = rad;
        }

        private Moves trySlideLeft(NodeMP node, List<Moves[]> possibleMoves)
        {
            if (possibleMoves.Exists(x => x[0] == Moves.MOVE_LEFT) || possibleMoves.Exists(x => x[1] == Moves.MOVE_LEFT))
            {
                return Moves.MOVE_LEFT;
            }
            else
            {
                node.noRemainingSTPActions();
                return utils.randomAction(node, possibleMoves, rnd, morphBias);
            }
        }

        private Moves trySlideRight(NodeMP node, List<Moves[]> possibleMoves)
        {
            if (possibleMoves.Exists(x => x[0] == Moves.MOVE_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.MOVE_RIGHT))
            {
                return Moves.MOVE_RIGHT;
            }
            else
            {
                node.noRemainingSTPActions();
                return utils.randomAction(node, possibleMoves, rnd, morphBias);
            }
        }

        private Moves trySlideLeftAndMorphDown(NodeMP node, List<Moves[]> possibleMoves)
        {
            if (possibleMoves.Exists(x => x[0] == Moves.MOVE_LEFT) || possibleMoves.Exists(x => x[1] == Moves.MOVE_LEFT))
            {
                return Moves.MOVE_LEFT;
            }
            else if (possibleMoves.Exists(x => x[0] == Moves.MORPH_DOWN) || possibleMoves.Exists(x => x[1] == Moves.MORPH_DOWN))
            {
                return Moves.MORPH_DOWN;
            }
            else
            {
                node.noRemainingSTPActions();
                return utils.randomAction(node, possibleMoves, rnd, morphBias);
            }
        }

        private Moves trySlideRightAndMorphDown(NodeMP node, List<Moves[]> possibleMoves)
        {
            if (possibleMoves.Exists(x => x[0] == Moves.MOVE_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.MOVE_RIGHT))
            {
                return Moves.MOVE_RIGHT;
            }
            else if (possibleMoves.Exists(x => x[0] == Moves.MORPH_DOWN) || possibleMoves.Exists(x => x[1] == Moves.MORPH_DOWN))
            {
                return Moves.MORPH_DOWN;
            }
            else
            {
                node.noRemainingSTPActions();
                return utils.randomAction(node, possibleMoves, rnd, morphBias);
            }
        }

        private Moves tryMorphUp(NodeMP node, List<Moves[]> possibleMoves)
        {
            if (possibleMoves.Exists(x => x[0] == Moves.MORPH_UP) || possibleMoves.Exists(x => x[1] == Moves.MORPH_UP))
            {
                return Moves.MORPH_UP;
            }
            else
            {
                node.noRemainingSTPActions();
                return utils.randomAction(node, possibleMoves, rnd, morphBias);
            }
        }

        private Moves tryNoAction(NodeMP node, List<Moves[]> possibleMoves, int type)
        {
            if (type == 0)
            {
                if (possibleMoves.Exists(x => x[1] == Moves.NO_ACTION))
                {
                    return Moves.NO_ACTION;
                }
                else
                {
                    node.noRemainingSTPActions();
                    return utils.randomAction(node, possibleMoves, rnd, morphBias);
                }
            }
            else
            {
                if (possibleMoves.Exists(x => x[0] == Moves.NO_ACTION))
                {
                    return Moves.NO_ACTION;
                }
                else
                {
                    node.noRemainingSTPActions();
                    return utils.randomAction(node, possibleMoves, rnd, morphBias);
                }
            }
        }


        //use when diamond needs to be caught by the circle using the rectangle as a platform
        public Moves skillCoopHighDiamond(NodeMP node, DiamondInfo dInfo, List<Moves[]> possibleMoves, int type)
        {
            StateMP state = node.getState();
            //if circle is on rectangle
            if (utils.onRectangle(state.getPosX() + radius, state.getPosY(), state.getPartnerX(), state.getPartnerY(), state.getHeight(), utils.getRectangleWidth(state.getHeight()), 25))
            {
                //if it is in position
                if (dInfo.getX() > state.getPosX() - onRectanglePositionMargin || dInfo.getX() < state.getPosX() + onRectanglePositionMargin)
                {
                    //morph up (it might be necessary for the circle be able to reach the diamond)
                    return tryMorphUp(node, possibleMoves);
                }
                else
                {
                    //slide to the diamond side
                    if (state.getPosX() < dInfo.getX())
                    {
                        return trySlideRight(node, possibleMoves);
                    }
                    else
                    {
                        return trySlideLeft(node, possibleMoves);
                    }
                }
            }
            //if not
            else
            {
                //if rectangle is in position
                if (dInfo.getX() > state.getPartnerX() - rectanglePositionMargin || dInfo.getX() < state.getPartnerX() + rectanglePositionMargin)
                {
                    //wait for the circle
                    return tryNoAction(node, possibleMoves, type);
                }
                else
                {
                    //go to position
                    if (state.getPosX() < dInfo.getX())
                    {
                        return trySlideRightAndMorphDown(node, possibleMoves);
                    }
                    else
                    {
                        return trySlideLeftAndMorphDown(node, possibleMoves);
                    }
                }
            }
        }

        //use when diamond is between two close platforms - only the rectangle can get it
        public Moves skillClosePlatforms(NodeMP node, DiamondInfo dInfo, List<Moves[]> possibleMoves, int type)
        {
            Platform dPlatform = dInfo.getPlatform();
            Platform agentPlatform = utils.onPlatform(node.getState().getPosX(), node.getState().getPosY(), 50, 10);


            StateMP agent = node.getState();
            //check if the rectangle is on the platform - if so, morph_down and go in that direction
            if (agentPlatform != null && dPlatform != null && agentPlatform.getY() == dPlatform.getY())
            {
                //morph down or go to the one of the sides
                if (dInfo.getX() < agent.getPosX() && (possibleMoves.Exists(x => x[0] == Moves.MOVE_LEFT) || possibleMoves.Exists(x => x[1] == Moves.MOVE_LEFT)))
                {
                    return Moves.MOVE_LEFT;
                }
                else if (dInfo.getX() > agent.getPosX() && (possibleMoves.Exists(x => x[0] == Moves.MOVE_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.MOVE_RIGHT)))
                {
                    return Moves.MOVE_RIGHT;
                    
                }
                else if (possibleMoves.Exists(x => x[0] == Moves.MORPH_DOWN) || possibleMoves.Exists(x => x[1] == Moves.MORPH_DOWN))
                {
                    return Moves.MORPH_DOWN;
                    
                }
                
                else
                {
                    node.noRemainingSTPActions();
                    return utils.randomActionRectangle(node, possibleMoves, rnd, morphBias);
                   
                }
            }
            //if below, check if it is possible for the rectangle to get up from there
            else if (agentPlatform != null && dPlatform != null && agentPlatform.getY() > dPlatform.getY() &&
                (agentPlatform.getY() - agentPlatform.getHeight() / 2) - (dPlatform.getY() - dPlatform.getHeight() / 2) < rectangleMaxHeight)
            {
                //if so, go to the diamond direction
                if (dInfo.getX() < agent.getPosX() && (possibleMoves.Exists(x => x[0] == Moves.MOVE_LEFT) || possibleMoves.Exists(x => x[1] == Moves.MOVE_LEFT)))
                {
                    return Moves.MOVE_LEFT;
                }
                else if (dInfo.getX() > agent.getPosX() && (possibleMoves.Exists(x => x[0] == Moves.MOVE_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.MOVE_RIGHT)))
                {
                    return Moves.MOVE_RIGHT;
                }
                else
                {
                    node.noRemainingSTPActions();
                    return utils.randomActionRectangle(node, possibleMoves, rnd, morphBias);
                }
            }
            else
            {
                node.noRemainingSTPActions();
                return utils.randomActionRectangle(node, possibleMoves, rnd, morphBias);
            }
        }

        public Moves skillHigherPlatform(NodeMP node, DiamondInfo dInfo, List<Moves[]> possibleMoves)
        {
            StateMP agent = node.getState();
            Platform platform = dInfo.getPlatform();
            //TODO check if agent can get to the platform
            
            return utils.randomActionRectangle(node, possibleMoves, rnd, morphBias);

        }
        
        public Moves skillLowerPlatform(NodeMP node, DiamondInfo dInfo, List<Moves[]> possibleMoves)
        {
            StateMP agent = node.getState();
            Platform platform = dInfo.getPlatform();

            //if the diamond cant be reached from its platform then this skill does not apply
            if (Math.Abs(dInfo.getY() - (dInfo.getPlatform().getY() - dInfo.getPlatform().getHeight() / 2)) > agentMaxReach)
            {
                //TODO - create a new skill
                return utils.randomActionRectangle(node, possibleMoves, rnd, morphBias);
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
                return utils.randomActionRectangle(node, possibleMoves, rnd, morphBias);
            }

            //roll to the one of the sides
            if (possibleMoves.Exists(x => x[0] == Moves.MOVE_LEFT) || possibleMoves.Exists(x => x[1] == Moves.MOVE_LEFT))
            {
                return Moves.MOVE_LEFT;
            }
            else if (possibleMoves.Exists(x => x[0] == Moves.MOVE_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.MOVE_RIGHT))
            {
                return Moves.MOVE_RIGHT;
            }
            else
            {
                node.noRemainingSTPActions();
                return utils.randomActionRectangle(node, possibleMoves, rnd, morphBias);
            }

        }

        public Moves skillCatchDiamond(NodeMP node, DiamondInfo dInfo, List<Moves[]> possibleMoves)
        {

            //if the agent is below the diamond at a very low velocity then morph up
            if (Math.Abs(dInfo.getX() - node.getState().getPosX()) <= skillXmargin &&
                    Math.Abs(node.getState().getVelX()) < skillVelMargin)
            {
                if (possibleMoves.Exists(x => x[0] == Moves.MORPH_UP) || possibleMoves.Exists(x => x[1] == Moves.MORPH_UP))
                {
                    return Moves.MORPH_UP;
                }
                else
                {
                    node.noRemainingSTPActions();
                    return utils.randomActionRectangle(node, possibleMoves, rnd, morphBias);
                }
            }
            //if the diamond is at the agents left
            else if (dInfo.getX() < node.getState().getPosX())// && Math.Abs(dInfo.getX() - node.getState().getPosX()) > skillXmargin)
            {
                //if it is at the same level - go left
                if (Math.Abs(dInfo.getY() - node.getState().getPosY()) < skillYmargin)
                {
                    if (possibleMoves.Exists(x => x[0] == Moves.MOVE_LEFT) || possibleMoves.Exists(x => x[1] == Moves.MOVE_LEFT))
                    {
                        return Moves.MOVE_LEFT;
                    }
                    else
                    {
                        node.noRemainingSTPActions();
                        return utils.randomActionRectangle(node, possibleMoves, rnd, morphBias);
                    }
                }
                //if not, then it will be necessary to jump at some point
                else
                {

                    if ((rnd.NextDouble() > skillMorphBias ||
                            node.getState().getVelX() > 0) && (possibleMoves.Exists(x => x[0] == Moves.MOVE_LEFT) || possibleMoves.Exists(x => x[1] == Moves.MOVE_LEFT)))
                    {
                        return Moves.MOVE_LEFT;
                    }
                    else
                    {
                        //make sure that it wont try the action jump if moving right 
                        if ((possibleMoves.Exists(x => x[0] == Moves.MORPH_UP) || possibleMoves.Exists(x => x[1] == Moves.MORPH_UP)) && node.getState().getVelX() < 0)
                        {
                            return Moves.MORPH_UP;
                        }
                        else
                        {
                            node.noRemainingSTPActions();
                            return utils.randomActionRectangle(node, possibleMoves, rnd, morphBias);
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
                    if (possibleMoves.Exists(x => x[0] == Moves.MOVE_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.MOVE_RIGHT))
                    {
                        return Moves.MOVE_RIGHT;
                    }
                    else
                    {
                        return utils.randomActionRectangle(node, possibleMoves, rnd, morphBias);
                    }
                }
                //if not, then it will be necessary to jump at some point
                else
                {
                    if ((rnd.NextDouble() > skillMorphBias ||
                        node.getState().getVelX() < 0) && (possibleMoves.Exists(x => x[0] == Moves.MOVE_RIGHT) || possibleMoves.Exists(x => x[1] == Moves.MOVE_RIGHT)))
                    {
                        return Moves.MOVE_RIGHT;
                    }
                    else
                    {
                        //make sure that it wont try the action jump if moving left
                        if ((possibleMoves.Exists(x => x[0] == Moves.MORPH_UP) || possibleMoves.Exists(x => x[1] == Moves.MORPH_UP)) && node.getState().getVelX() > 0)
                        {
                            return Moves.MORPH_UP;
                        }
                        else
                        {
                            node.noRemainingSTPActions();
                            return utils.randomActionRectangle(node, possibleMoves, rnd, morphBias);
                        }
                    }
                }
            }

            return utils.randomActionRectangle(node, possibleMoves, rnd, morphBias);
        }

        
    }
}
