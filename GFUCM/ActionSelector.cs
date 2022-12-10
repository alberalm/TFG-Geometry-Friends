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

        public Moves nextAction(List<LevelMap.MoveInformation> plan, List<CollectibleRepresentation> remaining, CircleRepresentation cI, LevelMap.Platform p)
        {
            if (plan.Count > 0)
            {
                LevelMap.MoveInformation nextMoveToAnotherPlatform = plan[0];
                LevelMap.MoveInformation nextMoveInThisPlatform = DiamondsCanBeCollectedFrom(nextMoveToAnotherPlatform.departurePlatform, remaining);
                if (nextMoveInThisPlatform!=null)
                {
                    //Remaining collectibles in this platform
                    return GoToPosition((int)cI.X/GameInfo.PIXEL_LENGTH, nextMoveInThisPlatform.x,nextMoveInThisPlatform.moveType==LevelMap.MoveType.JUMP);
                }
                else{
                    //We have to move To the next platform
                    return GoToPosition((int)cI.X / GameInfo.PIXEL_LENGTH, nextMoveToAnotherPlatform.x, nextMoveToAnotherPlatform.moveType == LevelMap.MoveType.JUMP);
                }
            }
            LevelMap.MoveInformation nextMove = DiamondsCanBeCollectedFrom(p, remaining);
            return GoToPosition((int)cI.X / GameInfo.PIXEL_LENGTH, nextMove.x, nextMove.moveType == LevelMap.MoveType.JUMP);
            
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

        /*Moves GoToWithVelocity(int currentx, int currentvx, int targetx, int targetvx, bool jump)
        {
            if(Math.Abs(currentx-targetx)<=1 && Math.Abs(currentvx - targetvx) <= 1 && jump)
            {
                return Moves.JUMP;
            }
            else if(Math.Abs(currentvx - targetvx) <= 1 )
        }
        */
        
    }
}
