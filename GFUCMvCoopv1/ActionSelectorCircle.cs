using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public class ActionSelectorCircle : ActionSelector
    {
        public LevelMapCircle levelMap;

        public ActionSelectorCircle(Dictionary<CollectibleRepresentation, int> collectibleId, LearningCircle l, LevelMapCircle levelMap, Graph graph, SetupMaker setupMaker) : base(collectibleId, l, graph, setupMaker)
        {
            this.levelMap = levelMap;
        }

        protected override MoveInformation DiamondsCanBeCollectedFrom(CircleRepresentation cI, RectangleRepresentation rI, Platform p, List<CollectibleRepresentation> remaining, int agentX)
        {
            int mindistance = 4000;
            MoveInformation move = null;
            MoveInformation next_move_circle = setupMaker.planCircle.Count > 0 ? setupMaker.planCircle[0] : null;
            MoveInformation next_move_rectangle = setupMaker.planRectangle.Count > 0 ? setupMaker.planRectangle[0] : null;
            if (!levelMap.small_to_simplified.ContainsKey(p))
            {
                int a =  0;
            }
            foreach (MoveInformation m in levelMap.small_to_simplified[p].moveInfoList)
            {
                if (m.landingPlatform.id == levelMap.small_to_simplified[p].id)
                {
                    foreach (int d in m.diamondsCollected)
                    {
                        if (CollectiblesIds(remaining).Contains(d))
                        {
                            foreach (Graph.Diamond diamond in graph.collectibles)
                            {
                                if (diamond.id == d)
                                {
                                    foreach (Tuple<int, string> tuple in diamond.isAbovePlatform)
                                    {
                                        if (tuple.Item1 == m.departurePlatform.id && (tuple.Item2.Equals("c") || tuple.Item2.Equals("cr")))
                                        {
                                            if (Math.Abs(m.x - agentX) < mindistance && (next_move_circle == null || (!next_move_circle.diamondsCollected.Contains(d) && !next_move_rectangle.diamondsCollected.Contains(d))))
                                            {
                                                move = m;
                                                mindistance = Math.Abs(m.x - agentX);
                                            }
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

        /*public Tuple<Moves, Tuple<bool, bool>> nextActionQTable(ref List<MoveInformation> plan, List<CollectibleRepresentation> remaining, CircleRepresentation cI, RectangleRepresentation rI, Platform currentPlatform)
        {
            //returns the next move, a first boolean indicating whether the move will lead to an air situation (Jump or fall) and a second boolean indicating whether the ball has to rotate in the
            //same direction of the velocity or in the oposite (in general will be oposite unless the jump lands near the vertix of the parabolla)
            MoveType moveType = MoveType.NOMOVE;
            MoveInformation nextMoveInThisPlatform;
            int min_distance = GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH;
            nextMoveInThisPlatform = DiamondsCanBeCollectedFrom(cI,rI,currentPlatform, remaining, (int)(cI.X / GameInfo.PIXEL_LENGTH));
            
            if (nextMoveInThisPlatform != null)
            {
                target_position = nextMoveInThisPlatform.x;
                target_velocity = nextMoveInThisPlatform.velocityX;
                moveType = nextMoveInThisPlatform.moveType;
                brake_distance = cI.VelocityX * cI.VelocityX / (2 * GameInfo.CIRCLE_ACCELERATION);//Not needed, just for visual debug
                acceleration_distance = target_velocity * target_velocity / (2 * GameInfo.CIRCLE_ACCELERATION);//Not needed, just for visual debug
                StateCircle s = new StateCircle(((int)(cI.X / GameInfo.PIXEL_LENGTH)) - target_position, CircleAgent.DiscreetVelocity(cI.VelocityX, GameInfo.VELOCITY_STEP_QLEARNING), target_velocity);

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
                        if (m.landingPlatform.id == currentPlatform.id && Utilities.Contained(nextMoveInThisPlatform.diamondsCollected, m.diamondsCollected))
                        {
                            if (nextMoveInThisPlatform.DistanceToRollingEdge() < min_distance || m.DistanceToRollingEdge() >= min_distance)
                            {
                                return new Tuple<Moves, Tuple<bool, bool>>(Moves.JUMP, new Tuple<bool, bool>(true, JumpNeedsAngularMomentum(m)));
                            }
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
                    brake_distance = cI.VelocityX * cI.VelocityX / (2 * GameInfo.CIRCLE_ACCELERATION);//Not needed, just for visual debug
                    acceleration_distance = target_velocity * target_velocity / (2 * GameInfo.CIRCLE_ACCELERATION);//Not needed, just for visual debug
                    StateCircle s = new StateCircle(((int)(cI.X / GameInfo.PIXEL_LENGTH)) - target_position, CircleAgent.DiscreetVelocity(cI.VelocityX, GameInfo.VELOCITY_STEP_QLEARNING), target_velocity);
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
                            if (m.landingPlatform.id == plan[0].landingPlatform.id && Utilities.Contained(aux_move.diamondsCollected, m.diamondsCollected))
                            {
                                if (nextMoveInThisPlatform.DistanceToRollingEdge() < min_distance || m.DistanceToRollingEdge() >= min_distance)
                                {
                                    plan.RemoveAt(0);
                                    return new Tuple<Moves, Tuple<bool, bool>>(Moves.JUMP, new Tuple<bool, bool>(true, JumpNeedsAngularMomentum(m)));
                                }
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
        }*/

        public Tuple<Moves, Tuple<bool, bool>> nextActionPhisics(ref List<MoveInformation> plan, List<CollectibleRepresentation> remaining, CircleRepresentation cI, RectangleRepresentation rI,Platform currentPlatform)
        {
            //returns the next move, a first boolean indicating whether the move will lead to an air situation (Jump or fall) and a second boolean indicating whether the ball has to rotate in the
            //same direction of the velocity or in the oposite (in general will be oposite unless the jump lands near the vertix of the parabolla)
            
            MoveType moveType = MoveType.NOMOVE;
            MoveInformation nextMoveInThisPlatform;
            int min_distance = 3 * GameInfo.CIRCLE_RADIUS / (GameInfo.PIXEL_LENGTH * 5);
            nextMoveInThisPlatform = DiamondsCanBeCollectedFrom(cI, rI, currentPlatform, remaining, (int)(cI.X / GameInfo.PIXEL_LENGTH));
            
            if(plan.Count == 0 || plan[0].moveType != MoveType.COOPMOVE || setupMaker.planRectangle[0].moveType != MoveType.CIRCLETILT)
            {
                setupMaker.circleAgentReadyForCircleTilt = false;
            }
            
            if (nextMoveInThisPlatform != null)
            {
                move = nextMoveInThisPlatform;
            }
            else if (plan.Count > 0)
            {
                move = plan[0];
            }
            if (move != null)
            {
                target_position = move.x;
            }

            if (setupMaker.CircleAboveRectangle())
            {
                setupMaker.circleAgentReadyForCoop = true;
                brake_distance = (cI.VelocityX - rI.VelocityX) * (cI.VelocityX - rI.VelocityX) / (2 * GameInfo.CIRCLE_ACCELERATION);
                
                if (!setupMaker.rectangleAgentReadyForCoop)
                {
                    return new Tuple<Moves, Tuple<bool, bool>>(getPhisicsMove(cI.X, rI.X, cI.VelocityX - rI.VelocityX, 0, brake_distance, 0), new Tuple<bool, bool>(false, false));
                }
            }

            if (nextMoveInThisPlatform != null)
            {
                if (!setupMaker.CircleAboveRectangle())
                {
                    setupMaker.circleAgentReadyForCoop = false;
                }
                target_position = nextMoveInThisPlatform.x;
                target_velocity = nextMoveInThisPlatform.velocityX;
                moveType = nextMoveInThisPlatform.moveType;
                brake_distance = cI.VelocityX * cI.VelocityX / (2 * GameInfo.CIRCLE_ACCELERATION);
                acceleration_distance = target_velocity * target_velocity / (2 * GameInfo.CIRCLE_ACCELERATION);
                if (Math.Abs(target_position * GameInfo.PIXEL_LENGTH - cI.X) <= GameInfo.TARGET_POINT_ERROR * GameInfo.PIXEL_LENGTH)
                {
                    if (CircleAgent.DiscreetVelocity(cI.VelocityX, GameInfo.VELOCITY_STEP_PHISICS) == target_velocity)
                    {
                        if (moveType == MoveType.JUMP)
                        {
                            setupMaker.circleAgentReadyForCoop = false;
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
                            return new Tuple<Moves, Tuple<bool, bool>>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), new Tuple<bool, bool>(false, false));
                        }
                        
                        MoveInformation m = new MoveInformation(new Platform(-1), currentPlatform, (int)cI.X / GameInfo.PIXEL_LENGTH, 0, (int)cI.VelocityX, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);
                        
                        List<MoveInformation> moves = levelMap.SimulateMove(cI.X, (currentPlatform.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, (int)cI.VelocityX, (int)GameInfo.JUMP_VELOCITYY, ref m);
                        foreach (MoveInformation move in moves)
                        {
                            if (move.landingPlatform.id == currentPlatform.id && Utilities.Contained(nextMoveInThisPlatform.diamondsCollected, move.diamondsCollected))
                            {
                                if (nextMoveInThisPlatform.DistanceToRollingEdge() < min_distance || move.DistanceToRollingEdge() >= min_distance)
                                {
                                    return new Tuple<Moves, Tuple<bool, bool>>(Moves.JUMP, new Tuple<bool, bool>(true, JumpNeedsAngularMomentum(move)));
                                }
                            }

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
                    brake_distance = cI.VelocityX * cI.VelocityX / (2 * GameInfo.CIRCLE_ACCELERATION);
                    acceleration_distance = target_velocity * target_velocity / (2 * GameInfo.CIRCLE_ACCELERATION);

                    if (plan[0].moveType == MoveType.COOPMOVE && setupMaker.planRectangle[0].moveType == MoveType.CIRCLETILT)
                    {
                        target_position = setupMaker.planRectangle[0].x;
                        target_position += target_position * GameInfo.PIXEL_LENGTH > cI.X ? 2 : -2;
                        target_velocity = 0;
                        acceleration_distance = target_velocity * target_velocity / (2 * GameInfo.CIRCLE_ACCELERATION);
                        if (Math.Sign(rI.X - cI.X) == Math.Sign(target_position * GameInfo.PIXEL_LENGTH - rI.X) &&
                            Math.Abs(rI.X - cI.X) < 25 * GameInfo.PIXEL_LENGTH)
                        {
                            return new Tuple<Moves, Tuple<bool, bool>>(Moves.JUMP, new Tuple<bool, bool>(true, false));
                        }
                        if (Math.Abs(target_position * GameInfo.PIXEL_LENGTH - cI.X) <= 5*GameInfo.PIXEL_LENGTH && Math.Abs(cI.VelocityX) < 30)
                        {
                            setupMaker.circleAgentReadyForCircleTilt = true;
                        }
                        return new Tuple<Moves, Tuple<bool, bool>>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), new Tuple<bool, bool>(false, false));
                    }
                    
                    if (!plan[0].landingPlatform.real && setupMaker.planRectangle[0].moveType == MoveType.COOPMOVE)
                    {
                        if (!setupMaker.rectangleAgentReadyForCoop)
                        {
                            /*float real_target = target_position * GameInfo.PIXEL_LENGTH + (target_velocity < 0 ? 1 : -1) * acceleration_distance;
                            target_position = (int)(real_target / GameInfo.PIXEL_LENGTH);
                            return new Tuple<Moves, Tuple<bool, bool>>(getPhisicsMove(cI.X, real_target,
                                cI.VelocityX, 0, brake_distance, acceleration_distance), new Tuple<bool, bool>(false, false));*/
                            return new Tuple<Moves, Tuple<bool, bool>>(Moves.NO_ACTION, new Tuple<bool, bool>(false, false));
                        }
                    }

                    if (Math.Abs(target_position * GameInfo.PIXEL_LENGTH - cI.X) <= GameInfo.TARGET_POINT_ERROR * GameInfo.PIXEL_LENGTH)
                    {
                        if (CircleAgent.DiscreetVelocity(cI.VelocityX, GameInfo.VELOCITY_STEP_PHISICS) == target_velocity)
                        {
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
                                return new Tuple<Moves, Tuple<bool, bool>>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), new Tuple<bool, bool>(false, false));
                            }
                            MoveInformation m = new MoveInformation(new Platform(-1), currentPlatform, (int)cI.X / GameInfo.PIXEL_LENGTH, 0, (int)cI.VelocityX, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);
                            List<MoveInformation> moves=levelMap.SimulateMove(cI.X, (currentPlatform.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, (int)cI.VelocityX, (int)GameInfo.JUMP_VELOCITYY, ref m);
                            foreach (MoveInformation move in moves)
                            {
                                if (move.landingPlatform.id == plan[0].landingPlatform.id && Utilities.Contained(aux_move.diamondsCollected, move.diamondsCollected))//CUIDADO, IGUAL ES DEMASIADO RESTRICTIVO
                                {
                                    if (aux_move.DistanceToRollingEdge() < min_distance || move.DistanceToRollingEdge() >= min_distance)
                                    {
                                        plan.RemoveAt(0);
                                        return new Tuple<Moves, Tuple<bool, bool>>(Moves.JUMP, new Tuple<bool, bool>(true, JumpNeedsAngularMomentum(move)));
                                    }
                                }
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

                    //return new Tuple<Moves, Tuple<bool, bool>>(possibleMoves[rnd.Next(possibleMoves.Count)], new Tuple<bool, bool>(false, false));
                    return new Tuple<Moves, Tuple<bool, bool>>(Moves.NO_ACTION, new Tuple<bool, bool>(false, false));
                }
            }
        }

        private bool JumpNeedsAngularMomentum(MoveInformation m)
        {
            return Math.Abs(m.landingPlatform.yTop + GameInfo.JUMP_VELOCITYY * GameInfo.JUMP_VELOCITYY / (2 * GameInfo.GRAVITY * GameInfo.PIXEL_LENGTH) - m.departurePlatform.yTop) <= 5;
        }

        public Moves getPhisicsMove(double current_position, double target_position, double current_velocity, double target_velocity, double brake_distance, double acceleration_distance)
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
                        if (Math.Abs(current_position + brake_distance - target_position - acceleration_distance) <= GameInfo.ERROR * GameInfo.PIXEL_LENGTH)//CHANGED (* GameInfo.PIXEL_LENGTH ADDED)
                        {
                            return Moves.ROLL_RIGHT;
                        }
                        else
                        {
                            if (current_position + brake_distance > target_position + acceleration_distance)
                            {
                                return Moves.ROLL_LEFT;
                            }
                            else
                            {
                                return Moves.ROLL_RIGHT;
                            }
                        }
                    }
                }
                else
                {
                    if (target_velocity >= 0)
                    {
                        if (Math.Abs(current_position - brake_distance - target_position + acceleration_distance) <= GameInfo.ERROR * GameInfo.PIXEL_LENGTH)
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
                    }
                    else
                    {
                        double sup_threshold = current_velocity * current_velocity + 2 * (current_position - target_position) * GameInfo.CIRCLE_ACCELERATION;
                        double inf_threshold = current_velocity * current_velocity - 2 * (current_position - target_position) * GameInfo.CIRCLE_ACCELERATION;
                        double square_target = target_velocity * target_velocity;
                        if (square_target > sup_threshold)
                        {
                            return Moves.ROLL_RIGHT;
                        }
                        else if (square_target < inf_threshold)
                        {
                            return Moves.ROLL_RIGHT;
                        }
                        else if (square_target <= inf_threshold + 5)
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
                Moves m = getPhisicsMove(2 * target_position - current_position, target_position, -current_velocity, -target_velocity, brake_distance, acceleration_distance);
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
    }
}
