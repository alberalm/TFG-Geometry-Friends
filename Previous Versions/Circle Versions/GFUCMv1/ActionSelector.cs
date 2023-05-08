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
        public ActionSelector(Dictionary<CollectibleRepresentation,int> collectibleId)
        {
            this.collectibleId = collectibleId;
        }

        public Tuple<Moves,bool> nextAction(List<LevelMap.MoveInformation> plan, List<CollectibleRepresentation> remaining, CircleRepresentation cI, LevelMap.Platform p)
        {
            if (plan.Count > 0)
            {
                LevelMap.MoveInformation nextMoveToAnotherPlatform = plan[0];
                LevelMap.MoveInformation nextMoveInThisPlatform = DiamondsCanBeCollectedFrom(nextMoveToAnotherPlatform.departurePlatform, remaining);
                if (nextMoveInThisPlatform!=null)
                {
                    //Remaining collectibles in this platform
                    return new Tuple<Moves, bool>(GoToPositionWithVelocity((int)(cI.X/GameInfo.PIXEL_LENGTH), (int) cI.VelocityX, nextMoveInThisPlatform.x, nextMoveInThisPlatform.velocityX, nextMoveInThisPlatform.moveType==LevelMap.MoveType.JUMP), false);
                }
                else{
                    //We have to move To the next platform
                    return new Tuple<Moves, bool>(GoToPositionWithVelocity((int)(cI.X / GameInfo.PIXEL_LENGTH), (int)cI.VelocityX, nextMoveToAnotherPlatform.x, nextMoveToAnotherPlatform.velocityX, nextMoveToAnotherPlatform.moveType == LevelMap.MoveType.JUMP),true);

                }
            }
            LevelMap.MoveInformation nextMove = DiamondsCanBeCollectedFrom(p, remaining);
            return new Tuple<Moves, bool>(GoToPositionWithVelocity((int)(cI.X / GameInfo.PIXEL_LENGTH), (int)cI.VelocityX, nextMove.x, nextMove.velocityX, nextMove.moveType == LevelMap.MoveType.JUMP),false);


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
