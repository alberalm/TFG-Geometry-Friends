using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class ActionSelector
    {
        public Dictionary<CollectibleRepresentation, int> collectibleId;
        public Learning l;
        public int target_position = 0;
        public int target_velocity = 0;
        public float brake_distance = 0;
        public float acceleration_distance = 0;
        public Graph graph;
        public LevelMap levelMap;

        public ActionSelector(Dictionary<CollectibleRepresentation,int> collectibleId, Learning l, LevelMap levelMap, Graph graph)
        {
            this.collectibleId = collectibleId;
            this.l = l;
            this.levelMap = levelMap;
            this.graph = graph;
        }
       
        public Tuple<Moves, Tuple<bool, bool>> nextActionQTable(ref List<MoveInformation> plan, List<CollectibleRepresentation> remaining, CircleRepresentation cI, Platform currentPlatform)
        {
            //returns the next move, a first boolean indicating whether the move will lead to an air situation (Jump or fall) and a second boolean indicating whether the ball has to rotate in the
            //same direction of the velocity or in the oposite (in general will be oposite unless the jump lands near the vertix of the parabolla)
            MoveType moveType = MoveType.NOMOVE;
            MoveInformation nextMoveInThisPlatform;
            if  (plan.Count > 0)
            {
                nextMoveInThisPlatform = DiamondsCanBeCollectedFrom(currentPlatform, remaining, (int)(cI.X / GameInfo.PIXEL_LENGTH),  plan[0]);
            }
            else
            {
                nextMoveInThisPlatform = DiamondsCanBeCollectedFrom(currentPlatform, remaining, (int)(cI.X / GameInfo.PIXEL_LENGTH), null);
            }
            if (nextMoveInThisPlatform != null)
            {
                target_position = nextMoveInThisPlatform.x;
                target_velocity = nextMoveInThisPlatform.velocityX;
                moveType = nextMoveInThisPlatform.moveType;
                brake_distance = cI.VelocityX * cI.VelocityX / (2 * GameInfo.ACCELERATION);//Not needed, just for visual debug
                acceleration_distance = target_velocity * target_velocity / (2 * GameInfo.ACCELERATION);//Not needed, just for visual debug
                State s = new State(((int)(cI.X / GameInfo.PIXEL_LENGTH)) - target_position, CircleAgent.DiscreetVelocity(cI.VelocityX, GameInfo.VELOCITY_STEP_QLEARNING), target_velocity);

                if (Math.Abs(target_position * GameInfo.PIXEL_LENGTH - cI.X) <= GameInfo.TARGET_POINT_ERROR * GameInfo.PIXEL_LENGTH)
                {
                    if (CircleAgent.DiscreetVelocity(cI.VelocityX, GameInfo.VELOCITY_STEP_QLEARNING) == target_velocity)
                    {
                        if (moveType == MoveType.JUMP)
                        {
                            return new Tuple<Moves, Tuple<bool, bool>>(Moves.JUMP, new Tuple<bool, bool>(true, JumpNeedsAngularMomentum(nextMoveInThisPlatform)));
                        }
                        else
                        {
                            return new Tuple<Moves, Tuple<bool, bool>>(Moves.NO_ACTION, new Tuple<bool, bool>(true, false));
                        }
                    }
                    else
                    {
                        if (moveType == MoveType.NOMOVE)
                        {
                            return new Tuple<Moves, Tuple<bool, bool>>(l.ChooseMove(s, ((int)(cI.X / GameInfo.PIXEL_LENGTH)) - target_position), new Tuple<bool, bool>(false, false));
                        }
                        MoveInformation m = new MoveInformation(new Platform(-1), currentPlatform, (int)cI.X / GameInfo.PIXEL_LENGTH, 0, (int)cI.VelocityX, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);
                        levelMap.SimulateMove(cI.X, (currentPlatform.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, (int)cI.VelocityX, (int)GameInfo.JUMP_VELOCITYY, ref m);
                        if (m.landingPlatform.id == currentPlatform.id && LevelMap.Contained(nextMoveInThisPlatform.diamondsCollected, m.diamondsCollected))
                        {
                            return new Tuple<Moves, Tuple<bool, bool>>(Moves.JUMP, new Tuple<bool, bool>(true, JumpNeedsAngularMomentum(m)));
                        }
                        return new Tuple<Moves, Tuple<bool, bool>>(l.ChooseMove(s, ((int)(cI.X / GameInfo.PIXEL_LENGTH)) - target_position), new Tuple<bool, bool>(false, false));
                    }
                }
                else
                {
                    return new Tuple<Moves, Tuple<bool, bool>>(l.ChooseMove(s, ((int)(cI.X / GameInfo.PIXEL_LENGTH)) - target_position), new Tuple<bool, bool>(false, false));
                }
            }
            else
            {
                if (plan.Count > 0)
                {
                    MoveInformation aux_move = plan[0];
                    target_position = plan[0].x;
                    target_velocity = plan[0].velocityX;
                    moveType = plan[0].moveType;
                    brake_distance = cI.VelocityX * cI.VelocityX / (2 * GameInfo.ACCELERATION);//Not needed, just for visual debug
                    acceleration_distance = target_velocity * target_velocity / (2 * GameInfo.ACCELERATION);//Not needed, just for visual debug
                    State s = new State(((int)(cI.X / GameInfo.PIXEL_LENGTH)) - target_position, CircleAgent.DiscreetVelocity(cI.VelocityX, GameInfo.VELOCITY_STEP_QLEARNING), target_velocity);
                    if (Math.Abs(target_position * GameInfo.PIXEL_LENGTH - cI.X) <= GameInfo.TARGET_POINT_ERROR * GameInfo.PIXEL_LENGTH)
                    {
                        if (CircleAgent.DiscreetVelocity(cI.VelocityX, GameInfo.VELOCITY_STEP_QLEARNING) == target_velocity)
                        {

                            plan.RemoveAt(0);
                            if (moveType == MoveType.JUMP)
                            {
                                return new Tuple<Moves, Tuple<bool, bool>>(Moves.JUMP, new Tuple<bool, bool>(true, JumpNeedsAngularMomentum(aux_move)));
                            }
                            else
                            {
                                return new Tuple<Moves, Tuple<bool, bool>>(Moves.NO_ACTION, new Tuple<bool, bool>(true, false));
                            }
                        }
                        else
                        {
                            if (moveType == MoveType.FALL)
                            {
                                return new Tuple<Moves, Tuple<bool, bool>>(l.ChooseMove(s, ((int)(cI.X / GameInfo.PIXEL_LENGTH)) - target_position), new Tuple<bool, bool>(false, false));
                            }
                            MoveInformation m = new MoveInformation(new Platform(-1), currentPlatform, (int)cI.X / GameInfo.PIXEL_LENGTH, 0, (int)cI.VelocityX, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);
                            levelMap.SimulateMove(cI.X, (currentPlatform.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, (int)cI.VelocityX, (int)GameInfo.JUMP_VELOCITYY, ref m);
                            if (m.landingPlatform.id == plan[0].landingPlatform.id && LevelMap.Contained(aux_move.diamondsCollected, m.diamondsCollected))
                            {
                                plan.RemoveAt(0);
                                return new Tuple<Moves, Tuple<bool, bool>>(Moves.JUMP, new Tuple<bool, bool>(true, JumpNeedsAngularMomentum(m)));
                            }
                            return new Tuple<Moves, Tuple<bool, bool>>(l.ChooseMove(s, ((int)(cI.X / GameInfo.PIXEL_LENGTH)) - target_position), new Tuple<bool, bool>(false, false));
                        }
                    }
                    else
                    {
                        return new Tuple<Moves, Tuple<bool, bool>>(l.ChooseMove(s, ((int)(cI.X / GameInfo.PIXEL_LENGTH)) - target_position), new Tuple<bool, bool>(false, false));
                    }
                }
                else
                {
                    Random rnd = new Random();
                    List<Moves> possibleMoves = new List<Moves>();
                    possibleMoves.Add(Moves.ROLL_LEFT);
                    possibleMoves.Add(Moves.ROLL_RIGHT);
                    possibleMoves.Add(Moves.NO_ACTION);
                    possibleMoves.Add(Moves.JUMP);

                    return new Tuple<Moves, Tuple<bool, bool>>(possibleMoves[rnd.Next(possibleMoves.Count)], new Tuple<bool, bool>(false, false));
                }
            }
        }

        public Tuple<Moves, Tuple<bool, bool>> nextActionPhisics(ref List<MoveInformation> plan, List<CollectibleRepresentation> remaining, CircleRepresentation cI, Platform currentPlatform)
        {
            //returns the next move, a first boolean indicating whether the move will lead to an air situation (Jump or fall) and a second boolean indicating whether the ball has to rotate in the
            //same direction of the velocity or in the oposite (in general will be oposite unless the jump lands near the vertix of the parabolla)
            MoveType moveType = MoveType.NOMOVE;
            MoveInformation nextMoveInThisPlatform;
            if (plan.Count > 0)
            {
                nextMoveInThisPlatform = DiamondsCanBeCollectedFrom(currentPlatform, remaining, (int)(cI.X / GameInfo.PIXEL_LENGTH), plan[0]);
            }
            else
            {
                nextMoveInThisPlatform = DiamondsCanBeCollectedFrom(currentPlatform, remaining, (int)(cI.X / GameInfo.PIXEL_LENGTH), null);
            }
            if (nextMoveInThisPlatform != null)
            {
                target_position = nextMoveInThisPlatform.x;
                target_velocity = nextMoveInThisPlatform.velocityX;
                moveType = nextMoveInThisPlatform.moveType;
                brake_distance = cI.VelocityX * cI.VelocityX / (2 * GameInfo.ACCELERATION);
                acceleration_distance = target_velocity * target_velocity / (2 * GameInfo.ACCELERATION);
                if (Math.Abs(target_position * GameInfo.PIXEL_LENGTH - cI.X) <= GameInfo.TARGET_POINT_ERROR * GameInfo.PIXEL_LENGTH)
                {
                    if (CircleAgent.DiscreetVelocity(cI.VelocityX,GameInfo.VELOCITY_STEP_PHISICS) == target_velocity)
                    {
                        if (moveType == MoveType.JUMP)
                        {
                            return new Tuple<Moves, Tuple<bool,bool>>(Moves.JUMP, new Tuple<bool, bool>(true, JumpNeedsAngularMomentum(nextMoveInThisPlatform)));
                        }
                        else
                        {
                            return new Tuple<Moves, Tuple<bool, bool>>(Moves.NO_ACTION, new Tuple<bool, bool>(true,false));
                        }
                    }
                    else
                    {
                        if (moveType == MoveType.NOMOVE)
                        {
                            return new Tuple<Moves, Tuple<bool, bool>>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), new Tuple<bool, bool>(false,false));
                        }
                        MoveInformation m = new MoveInformation(new Platform(-1), currentPlatform, (int)cI.X / GameInfo.PIXEL_LENGTH, 0, (int)cI.VelocityX, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);
                        levelMap.SimulateMove(cI.X, (currentPlatform.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, (int)cI.VelocityX, (int)GameInfo.JUMP_VELOCITYY, ref m);
                        if (m.landingPlatform.id == currentPlatform.id && LevelMap.Contained(nextMoveInThisPlatform.diamondsCollected,m.diamondsCollected))
                        {
                            return new Tuple<Moves, Tuple<bool, bool>>(Moves.JUMP, new Tuple<bool, bool>(true, JumpNeedsAngularMomentum(m)));
                        }
                        return new Tuple<Moves, Tuple<bool, bool>>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), new Tuple<bool, bool>(false, false));
                    }
                }
                else
                {
                    return new Tuple<Moves, Tuple<bool, bool>>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), new Tuple<bool, bool>(false, false));
                }
            }
            else
            {
                if (plan.Count > 0)
                {
                    MoveInformation aux_move = plan[0];
                    target_position = plan[0].x;
                    target_velocity = plan[0].velocityX;
                    moveType = plan[0].moveType;
                    brake_distance = cI.VelocityX * cI.VelocityX / (2 * GameInfo.ACCELERATION);
                    acceleration_distance = target_velocity * target_velocity / (2 * GameInfo.ACCELERATION);

                    if (Math.Abs(target_position * GameInfo.PIXEL_LENGTH - cI.X) <= GameInfo.TARGET_POINT_ERROR* GameInfo.PIXEL_LENGTH)
                    {
                        if (CircleAgent.DiscreetVelocity(cI.VelocityX,GameInfo.VELOCITY_STEP_PHISICS) == target_velocity)
                        {
                            
                            plan.RemoveAt(0);
                            if (moveType == MoveType.JUMP)
                            {
                                return new Tuple<Moves, Tuple<bool, bool>>(Moves.JUMP, new Tuple<bool, bool>(true, JumpNeedsAngularMomentum(aux_move)));
                            }
                            else
                            {
                                return new Tuple<Moves, Tuple<bool, bool>>(Moves.NO_ACTION, new Tuple<bool, bool>(true,false));
                            }
                        }
                        else
                        {
                            if (moveType == MoveType.FALL)
                            {
                                return new Tuple<Moves, Tuple<bool, bool>>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), new Tuple<bool, bool>(false, false));
                            }
                            MoveInformation m = new MoveInformation(new Platform(-1), currentPlatform, (int)cI.X / GameInfo.PIXEL_LENGTH, 0, (int)cI.VelocityX, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);
                            levelMap.SimulateMove(cI.X, (currentPlatform.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, (int)cI.VelocityX, (int)GameInfo.JUMP_VELOCITYY, ref m);
                            if (m.landingPlatform.id == plan[0].landingPlatform.id && LevelMap.Contained(aux_move.diamondsCollected, m.diamondsCollected))//CUIDADO, IGUAL ES DEMASIADO RESTRICTIVO
                            {
                                plan.RemoveAt(0);
                                return new Tuple<Moves, Tuple<bool, bool>>(Moves.JUMP, new Tuple<bool, bool>(true, JumpNeedsAngularMomentum(m)));
                            }
                            return new Tuple<Moves, Tuple<bool, bool>>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), new Tuple<bool, bool>(false, false));
                        }
                    }
                    else
                    {
                        return new Tuple<Moves, Tuple<bool, bool>>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), new Tuple<bool, bool>(false, false));
                    }
                }
                else
                { 
                    Random rnd = new Random();
                    List<Moves> possibleMoves = new List<Moves>();
                    possibleMoves.Add(Moves.ROLL_LEFT);
                    possibleMoves.Add(Moves.ROLL_RIGHT);
                    possibleMoves.Add(Moves.NO_ACTION);
                    possibleMoves.Add(Moves.JUMP);

                    return new Tuple<Moves, Tuple<bool, bool>>(possibleMoves[rnd.Next(possibleMoves.Count)], new Tuple<bool, bool> (false, false));
                }
            }
        }

        private bool JumpNeedsAngularMomentum(MoveInformation m)
        {
            return Math.Abs(m.landingPlatform.yTop+ GameInfo.JUMP_VELOCITYY* GameInfo.JUMP_VELOCITYY /(2*GameInfo.GRAVITY*GameInfo.PIXEL_LENGTH) - m.departurePlatform.yTop)<= 5;
        }

        private MoveInformation DiamondsCanBeCollectedFrom(Platform p, List<CollectibleRepresentation> remaining, int circleX, MoveInformation next_move)
        {
            int mindistance = 4000;
            MoveInformation move = null;
            foreach (MoveInformation m in p.moveInfoList)
            {
                if (m.landingPlatform.id == p.id){
                    foreach(int d in m.diamondsCollected)
                    {
                        if (CollectiblesIds(remaining).Contains(d))
                        {
                            foreach (Graph.Diamond diamond in graph.collectibles)
                            {
                                if  (diamond.id == d)
                                {
                                    if (diamond.isAbovePlatform == p.id)
                                    {
                                        if (Math.Abs(m.x - circleX) < mindistance && (next_move == null || !next_move.diamondsCollected.Contains(d)))
                                        {
                                            move = m;
                                            mindistance = Math.Abs(m.x - circleX);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return move;
        }

        private List<int> CollectiblesIds(List<CollectibleRepresentation> colI)
        {
            List<int> l = new List<int>();
            foreach(CollectibleRepresentation c in colI)
            {
                l.Add(collectibleId[c]);
            }
            return l;
        }

        Moves getPhisicsMove(double current_position, double target_position, double current_velocity, double target_velocity, double brake_distance, double acceleration_distance)
        {
            if (current_position >= target_position)//Circle on the right
            {
                if (current_velocity >= 0)
                {
                    if (target_velocity >= 0)
                    {
                        return Moves.ROLL_LEFT;
                    }
                    else
                    {
                        if(Math.Abs(current_position + brake_distance- target_position - acceleration_distance)<= GameInfo.ERROR)
                        {
                            return Moves.ROLL_RIGHT;
                        }
                        else
                        {
                            if(current_position + brake_distance > target_position + acceleration_distance)
                            {
                                return Moves.ROLL_LEFT;
                            }
                            else
                            {
                                return Moves.ROLL_RIGHT;
                            }
                        }
                        /*
                        if(current_position+ brake_distance + GameInfo.TARGET_POINT_ERROR > target_position + acceleration_distance)
                        {
                            return Moves.ROLL_LEFT;
                        }
                        else if (current_position + brake_distance < target_position + acceleration_distance + GameInfo.TARGET_POINT_ERROR * GameInfo.PIXEL_LENGTH)
                        {
                            return Moves.ROLL_RIGHT;
                        }
                        else
                        {
                            return Moves.ROLL_LEFT;
                        }*/
                    }
                }
                else
                {
                    if (target_velocity >= 0)
                    {
                        if(Math.Abs(current_position - brake_distance- target_position + acceleration_distance) <= GameInfo.ERROR * GameInfo.PIXEL_LENGTH)
                        {
                            return Moves.ROLL_RIGHT;
                        }
                        else
                        {
                            if (current_position - brake_distance > target_position - acceleration_distance)
                            {
                                return Moves.ROLL_LEFT;
                            }
                            else
                            {
                                return Moves.ROLL_RIGHT;
                            }
                        }
                        /*
                        if (current_position - brake_distance  > target_position - acceleration_distance + GameInfo.TARGET_POINT_ERROR * GameInfo.PIXEL_LENGTH)
                        {
                            return Moves.ROLL_LEFT;
                        }
                        else if (current_position - brake_distance + GameInfo.TARGET_POINT_ERROR < target_position - acceleration_distance)
                        {
                            return Moves.ROLL_RIGHT;
                        }
                        else
                        {
                            return Moves.ROLL_RIGHT;
                        }*/
                    }
                    else
                    {
                        double sup_threshold = current_velocity * current_velocity + 2 * (current_position - target_position) * GameInfo.ACCELERATION;
                        double inf_threshold = current_velocity * current_velocity - 2 * (current_position - target_position) * GameInfo.ACCELERATION;
                        double square_target = target_velocity * target_velocity;
                        if (square_target > sup_threshold)
                        {
                            return Moves.ROLL_RIGHT;
                        }
                        else if(square_target < inf_threshold)
                        {
                            return Moves.ROLL_RIGHT;
                        }
                        else if(square_target <= inf_threshold + 5)
                        {
                            return Moves.ROLL_RIGHT;
                        }
                        else
                        {
                            return Moves.ROLL_LEFT;
                        }
                    }
                }
            }
            else
            {
                Moves m= getPhisicsMove(2 * target_position - current_position, target_position, -current_velocity, -target_velocity, brake_distance, acceleration_distance);
                if (m == Moves.ROLL_LEFT)
                {
                    return Moves.ROLL_RIGHT;
                }
                else if (m == Moves.ROLL_RIGHT)
                {
                    return Moves.ROLL_LEFT;
                }
                else
                {
                    return Moves.NO_ACTION;
                }
            }
        }

        Moves GoToPosition(int currentx, int targetx, bool jump)
        {
            if (Math.Abs(currentx - targetx) <= 1 && jump)
            {
                return Moves.JUMP;
            }
            else if (currentx < targetx)
            {
                return Moves.ROLL_RIGHT;
            }
            else
            {
                return Moves.ROLL_LEFT;
            }
        }

        Moves GoToPositionWithVelocity(int currentx, int currentvx, int targetx, int targetvx, bool jump)
        {
            if (Math.Abs(currentx - targetx) <= 1 && jump)
            {
                if (currentvx * targetvx < 0)
                {
                    if (currentvx > 0)
                    {
                        return Moves.ROLL_RIGHT;
                    }
                    else
                    {
                        return Moves.ROLL_LEFT;
                    }
                }
                return Moves.JUMP;
            }
            else if (currentx < targetx)
            {
                return Moves.ROLL_RIGHT;
            }
            else
            {
                return Moves.ROLL_LEFT;
            }
        }
        
      }
} 
    

