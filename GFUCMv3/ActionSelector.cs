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

        public Tuple<Moves,bool> nextActionQTable(ref List<LevelMap.MoveInformation> plan, List<CollectibleRepresentation> remaining, CircleRepresentation cI, LevelMap.Platform currentPlatform)
        {
            
            LevelMap.MoveType moveType = LevelMap.MoveType.NOMOVE;
            LevelMap.MoveInformation nextMoveInThisPlatform = DiamondsCanBeCollectedFrom(currentPlatform, remaining);
            if (nextMoveInThisPlatform != null)
            {
                target_position = nextMoveInThisPlatform.x;
                target_velocity = nextMoveInThisPlatform.velocityX;
                moveType = nextMoveInThisPlatform.moveType;
                State s = new State(((int)(cI.X / GameInfo.PIXEL_LENGTH)) - target_position, CircleAgent.DiscreetVelocity(cI.VelocityX), target_velocity);
                if (s.IsFinal())
                {
                    if (moveType == LevelMap.MoveType.JUMP)
                    {
                        return new Tuple<Moves, bool>(Moves.JUMP, true);
                    }
                    else
                    {
                        return new Tuple<Moves, bool>(Moves.NO_ACTION, true);
                    }
                }
                else
                {
                    return new Tuple<Moves, bool>(l.ChooseMove(s, ((int)(cI.X / GameInfo.PIXEL_LENGTH)) - target_position), false);
                }

            }
            else
            {
                if (plan.Count > 0)
                {
                    target_position = plan[0].x;
                    target_velocity = plan[0].velocityX;
                    moveType = plan[0].moveType;
                    State s = new State(((int)(cI.X / GameInfo.PIXEL_LENGTH)) - target_position, CircleAgent.DiscreetVelocity(cI.VelocityX), target_velocity);
                    if (s.IsFinal())
                    {
                        plan.RemoveAt(0);
                        if (moveType == LevelMap.MoveType.JUMP)
                        {
                            return new Tuple<Moves, bool>(Moves.JUMP, true);
                        }
                        else
                        {
                            return new Tuple<Moves, bool>(Moves.NO_ACTION, true);
                        }
                    }
                    else
                    {
                        return new Tuple<Moves, bool>(l.ChooseMove(s, ((int)(cI.X / GameInfo.PIXEL_LENGTH)) - target_position), false);
                    }
                }
                else
                {
                    //TODO
                    return null;
                }
            }
        }

        public Tuple<Moves, bool> nextActionPhisics(ref List<LevelMap.MoveInformation> plan, List<CollectibleRepresentation> remaining, CircleRepresentation cI, LevelMap.Platform currentPlatform)
        {
            LevelMap.MoveType moveType = LevelMap.MoveType.NOMOVE;
            LevelMap.MoveInformation nextMoveInThisPlatform = DiamondsCanBeCollectedFrom(currentPlatform, remaining);
            if (nextMoveInThisPlatform != null)
            {
                target_position = nextMoveInThisPlatform.x;
                target_velocity = nextMoveInThisPlatform.velocityX;
                moveType = nextMoveInThisPlatform.moveType;
                brake_distance = cI.VelocityX * cI.VelocityX / (2 * GameInfo.ACCELERATION);
                acceleration_distance = target_velocity * target_velocity / (2 * GameInfo.ACCELERATION);
                if (Math.Abs(target_position * GameInfo.PIXEL_LENGTH - cI.X) <= GameInfo.TARGET_POINT_ERROR * GameInfo.PIXEL_LENGTH)
                {
                    if (CircleAgent.DiscreetVelocity(cI.VelocityX) == target_velocity)
                    {
                        if (moveType == LevelMap.MoveType.JUMP)
                        {
                            return new Tuple<Moves, bool>(Moves.JUMP, true);
                        }
                        else
                        {
                            return new Tuple<Moves, bool>(Moves.NO_ACTION, true);
                        }
                    }
                    else
                    {
                        if (moveType == LevelMap.MoveType.NOMOVE)
                        {
                            return new Tuple<Moves, bool>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), false);
                        }
                        LevelMap.MoveInformation m = new LevelMap.MoveInformation(new LevelMap.Platform(-1), currentPlatform, (int)cI.X / GameInfo.PIXEL_LENGTH, 0, (int)cI.VelocityX, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);
                        levelMap.SimulateMove(cI.X, (currentPlatform.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, (int)cI.VelocityX, (int)GameInfo.JUMP_VELOCITYY, ref m);
                        if (m.landingPlatform.id == currentPlatform.id && m.diamondsCollected.Count > 0)
                        {
                            return new Tuple<Moves, bool>(Moves.JUMP, true);
                        }
                        return new Tuple<Moves, bool>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), false);
                    }
                }
                else
                {
                    return new Tuple<Moves, bool>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), false);
                }
            }
            else
            {
                if (plan.Count > 0)
                {
                    target_position = plan[0].x;
                    target_velocity = plan[0].velocityX;
                    moveType = plan[0].moveType;
                    brake_distance = cI.VelocityX * cI.VelocityX / (2 * GameInfo.ACCELERATION);
                    acceleration_distance = target_velocity * target_velocity / (2 * GameInfo.ACCELERATION);

                    if (Math.Abs(target_position * GameInfo.PIXEL_LENGTH - cI.X) <= GameInfo.TARGET_POINT_ERROR* GameInfo.PIXEL_LENGTH)
                    {
                        if (CircleAgent.DiscreetVelocity(cI.VelocityX) == target_velocity)
                        {
                            plan.RemoveAt(0);
                            if (moveType == LevelMap.MoveType.JUMP)
                            {
                                return new Tuple<Moves, bool>(Moves.JUMP, true);
                            }
                            else
                            {
                                return new Tuple<Moves, bool>(Moves.NO_ACTION, true);
                            }
                        }
                        else
                        {
                            if (moveType == LevelMap.MoveType.FALL)
                            {
                                return new Tuple<Moves, bool>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), false);
                            }
                            LevelMap.MoveInformation m = new LevelMap.MoveInformation(new LevelMap.Platform(-1), currentPlatform, (int)cI.X / GameInfo.PIXEL_LENGTH, 0, (int)cI.VelocityX, moveType, new List<int>(), new List<Tuple<float, float>>(), 10);
                            levelMap.SimulateMove(cI.X, (currentPlatform.yTop - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH) * GameInfo.PIXEL_LENGTH, (int)cI.VelocityX, (int)GameInfo.JUMP_VELOCITYY, ref m);
                            if (m.landingPlatform.id == plan[0].landingPlatform.id)
                            {
                                plan.RemoveAt(0);
                                return new Tuple<Moves, bool>(Moves.JUMP, true);
                            }
                            return new Tuple<Moves, bool>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), false);
                        }
                    }
                    else
                    {
                        return new Tuple<Moves, bool>(getPhisicsMove(cI.X, target_position * GameInfo.PIXEL_LENGTH, cI.VelocityX, target_velocity, brake_distance, acceleration_distance), false);
                    }
                }
                else
                {//TODO
                    Random rnd = new Random();
                    List<Moves> possibleMoves = new List<Moves>();
                    possibleMoves.Add(Moves.ROLL_LEFT);
                    possibleMoves.Add(Moves.ROLL_RIGHT);
                    possibleMoves.Add(Moves.NO_ACTION);
                    possibleMoves.Add(Moves.JUMP);

                    return new Tuple<Moves, bool>(possibleMoves[rnd.Next(possibleMoves.Count)], false);
                }
            }
        }
        private LevelMap.MoveInformation DiamondsCanBeCollectedFrom(LevelMap.Platform p, List<CollectibleRepresentation> remaining)
        {
            foreach (LevelMap.MoveInformation m in p.moveInfoList)
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
                                        return m;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return null;
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
                        if(current_position+ brake_distance + GameInfo.TARGET_POINT_ERROR > target_position + acceleration_distance)
                        {
                            return Moves.ROLL_LEFT;
                        }
                        else if (current_position + brake_distance  < target_position + acceleration_distance + GameInfo.TARGET_POINT_ERROR*GameInfo.PIXEL_LENGTH)
                        {
                            return Moves.ROLL_RIGHT;
                        }
                        else
                        {
                            return Moves.NO_ACTION;
                        }
                    }
                }
                else
                {
                    if (target_velocity >= 0)
                    {
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
                            return Moves.NO_ACTION;
                        }
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
                }else if (m == Moves.ROLL_RIGHT)
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
    

