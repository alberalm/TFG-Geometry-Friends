using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class ActionSelectorRectangle : ActionSelector
    {
        public LevelMapRectangle levelMap;

        public ActionSelectorRectangle(Dictionary<CollectibleRepresentation, int> collectibleId, Learning l, LevelMapRectangle levelMap, Graph graph) : base(collectibleId, l, graph)
        {
            this.levelMap = levelMap;
        }

        protected override Moves getPhisicsMove(double current_position, double target_position, double current_velocity, double target_velocity, double brake_distance, double acceleration_distance)
        {
            throw new NotImplementedException();
        }
        
        public Tuple<Moves, Tuple<bool, bool>> nextActionPhisics(ref List<MoveInformation> plan, List<CollectibleRepresentation> remaining, RectangleRepresentation rI, Platform currentPlatform)
        {
            //returns the next move, a first boolean indicating whether the move will lead to an air situation (Jump or fall) and a second boolean indicating whether the ball has to rotate in the
            //same direction of the velocity or in the oposite (in general will be oposite unless the jump lands near the vertix of the parabolla)
            
            MoveInformation move;

            if (plan.Count > 0)
            {
                move = DiamondsCanBeCollectedFrom(levelMap.small_to_simplified[currentPlatform], remaining, (int)(rI.X / GameInfo.PIXEL_LENGTH), plan[0]);
            }
            else
            {
                move = DiamondsCanBeCollectedFrom(levelMap.small_to_simplified[currentPlatform], remaining, (int)(rI.X / GameInfo.PIXEL_LENGTH), null);
            }
            if (move != null)
            {
                
                target_position = move.x;
                target_velocity = move.velocityX;
            }
            else
            {
                if (plan.Count > 0)
                {
                    move = plan[0];
                    target_position = plan[0].x;
                    target_velocity = plan[0].velocityX;
                }
                else
                {
                    Random rnd = new Random();
                    List<Moves> possibleMoves = new List<Moves>();
                    possibleMoves.Add(Moves.MOVE_RIGHT);
                    possibleMoves.Add(Moves.MOVE_LEFT);
                    possibleMoves.Add(Moves.MORPH_DOWN);
                    possibleMoves.Add(Moves.MORPH_UP);

                    return new Tuple<Moves, Tuple<bool, bool>>(possibleMoves[rnd.Next(possibleMoves.Count)], new Tuple<bool, bool>(false, false));
                }
            }
            // Check shape
            if (move.shape == RectangleShape.Shape.SQUARE && rI.Height < RectangleShape.fheight(move.shape) + 5
                    && rI.Height > RectangleShape.fheight(move.shape) - 5)
            {

            }
            else if (RectangleShape.fheight(move.shape) + 5 < rI.Height)
            {
                return new Tuple<Moves, Tuple<bool, bool>>(Moves.MORPH_DOWN, new Tuple<bool, bool>(false, false));
            }
            else if (RectangleShape.fheight(move.shape) - 5 > rI.Height)
            {
                return new Tuple<Moves, Tuple<bool, bool>>(Moves.MORPH_UP, new Tuple<bool, bool>(false, false));
            }

            // Perform move
            if (target_position > rI.X / GameInfo.PIXEL_LENGTH)
            {
                return new Tuple<Moves, Tuple<bool, bool>>(Moves.MOVE_RIGHT, new Tuple<bool, bool>(false, false));
            }
            else
            {
                return new Tuple<Moves, Tuple<bool, bool>>(Moves.MOVE_LEFT, new Tuple<bool, bool>(false, false));
            }
        }
    }
}
