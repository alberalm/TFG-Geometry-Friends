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
        public ActionSelector(Dictionary<CollectibleRepresentation,int> collectibleId, Learning l)
        {
            this.collectibleId = collectibleId;
            this.l = l;
        }

        public Tuple<Moves,bool> nextAction(ref List<LevelMap.MoveInformation> plan, List<CollectibleRepresentation> remaining, CircleRepresentation cI, LevelMap.Platform currentPlatform)
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

        private LevelMap.MoveInformation DiamondsCanBeCollectedFrom(LevelMap.Platform p, List<CollectibleRepresentation> remaining)
        {
            foreach (LevelMap.MoveInformation m in p.moveInfoList)
            {
                if (m.landingPlatform.id == p.id){
                    foreach(int d in m.diamondsCollected)
                    {
                        if (CollectiblesIds(remaining).Contains(d))
                        {
                            return m;
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
    

