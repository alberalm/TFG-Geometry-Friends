using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using static GeometryFriendsAgents.RectangleShape;

namespace GeometryFriendsAgents
{
    class ActionSelectorRectangle : ActionSelector
    {
        public LevelMapRectangle levelMap;
        public Platform next_platform = null;
        public MoveInformation move;

        public ActionSelectorRectangle(Dictionary<CollectibleRepresentation, int> collectibleId, Learning l, LevelMapRectangle levelMap, Graph graph) : base(collectibleId, l, graph)
        {
            this.levelMap = levelMap;
        }

        public Moves getPhisicsMove(double current_position, double target_position, double current_velocity, double target_velocity, MoveInformation move)
        {

            if (move.moveType == MoveType.FALL || move.moveType == MoveType.NOMOVE || move.moveType == MoveType.ADJACENT || move.moveType == MoveType.DROP)
            {
                if (current_position >= target_position) // Rectangle on the right
                {
                    // target_velocity is always <= 0
                    if (current_velocity >= 0)
                    {
                        double brake_distance = current_velocity * current_velocity / (2 * GameInfo.RECTANGLE_ACCELERATION);
                        double acceleration_distance = target_velocity * target_velocity / (2 * GameInfo.RECTANGLE_ACCELERATION);
                        if (Math.Abs(current_position + brake_distance - target_position - acceleration_distance) <= GameInfo.ERROR * GameInfo.PIXEL_LENGTH)
                        {
                            return Moves.MOVE_RIGHT;
                        }
                        else
                        {
                            if (current_position + brake_distance > target_position + acceleration_distance)
                            {
                                return Moves.MOVE_LEFT;
                            }
                            else
                            {
                                return Moves.MOVE_RIGHT;
                            }
                        }
                    }
                    else
                    {
                        double sup_threshold = current_velocity * current_velocity + 2 * (current_position - target_position) * GameInfo.RECTANGLE_ACCELERATION;
                        double inf_threshold = current_velocity * current_velocity - 2 * (current_position - target_position) * GameInfo.RECTANGLE_ACCELERATION;
                        double square_target = target_velocity * target_velocity;
                        if (square_target > sup_threshold)
                        {
                            return Moves.MOVE_RIGHT;
                        }
                        else if (square_target < inf_threshold)
                        {
                            return Moves.MOVE_RIGHT;
                        }
                        else if (square_target <= inf_threshold + 5)
                        {
                            return Moves.MOVE_RIGHT;
                        }
                        else
                        {
                            return Moves.MOVE_LEFT;
                        }
                    }
                }
                else
                {
                    Moves m = getPhisicsMove(2 * target_position - current_position, target_position, -current_velocity, -target_velocity, move);
                    if (m == Moves.MOVE_LEFT)
                    {
                        return Moves.MOVE_RIGHT;
                    }
                    else if (m == Moves.MOVE_RIGHT)
                    {
                        return Moves.MOVE_LEFT;
                    }
                    else
                    {
                        return Moves.NO_ACTION;
                    }
                }
            }
            else if (move.moveType == MoveType.TILT || move.moveType == MoveType.MONOSIDEDROP)
            {
                if (target_velocity > 0)
                {
                    return Moves.MOVE_RIGHT;
                }
                else
                {
                    return Moves.MOVE_LEFT;
                }
            }
            else
            {
                return Moves.NO_ACTION;
            }
        }
        
        public Moves nextActionPhisics(ref List<MoveInformation> plan, List<CollectibleRepresentation> remaining, RectangleRepresentation rI, Platform currentPlatform)
        {
            //returns the next move, a first boolean indicating whether the move will lead to an air situation (Jump or fall) and a second boolean indicating whether the ball has to rotate in the
            //same direction of the velocity or in the oposite (in general will be oposite unless the jump lands near the vertix of the parabolla)
            if (Math.Abs(rI.X - 1143) <= 2)
            {
                int a = 0;
            }
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
                    List<Moves> possibleMoves = new List<Moves>
                    {
                        Moves.MOVE_RIGHT,
                        Moves.MOVE_LEFT,
                        Moves.MORPH_DOWN,
                        Moves.MORPH_UP
                    };
                    return possibleMoves[rnd.Next(possibleMoves.Count)];
                }
            }

            Moves m = getPhisicsMove(rI.X, move.x * GameInfo.PIXEL_LENGTH, rI.VelocityX, move.velocityX, move);
            
            Platform current_platform = levelMap.RectanglePlatform(rI);
            next_platform = null;

            RectangleShape.Shape target_shape = move.shape;
            if (move.x < current_platform.leftEdge || move.x > current_platform.rightEdge)
            {
                if (rI.VelocityX >= 0)
                {
                    RectangleRepresentation rI2 = new RectangleRepresentation(GameInfo.PIXEL_LENGTH * (currentPlatform.rightEdge + 2), rI.Y, rI.VelocityX, rI.VelocityY, rI.Height);
                    next_platform = levelMap.RectanglePlatform(rI2);
                }
                else
                {
                    RectangleRepresentation rI2 = new RectangleRepresentation(GameInfo.PIXEL_LENGTH * (currentPlatform.leftEdge - 2), rI.Y, rI.VelocityX, rI.VelocityY, rI.Height);
                    next_platform = levelMap.RectanglePlatform(rI2);
                }
                target_shape = BestShape(current_platform, next_platform, target_shape, RectangleShape.GetShape(rI));
            }
            
            // Check shape
            if (target_shape == RectangleShape.Shape.SQUARE && rI.Height < RectangleShape.fheight(target_shape) + 5
                    && rI.Height > RectangleShape.fheight(target_shape) - 5)
            {

            }
            else if (RectangleShape.fheight(target_shape) + 5 < rI.Height)
            {
                return Moves.MORPH_DOWN;
            }
            else if (RectangleShape.fheight(target_shape) - 5 > rI.Height)
            {
                if (levelMap.levelMap[(int) rI.X / GameInfo.PIXEL_LENGTH, (int)((rI.Y-rI.Height)/GameInfo.PIXEL_LENGTH) - 1] != LevelMap.PixelType.OBSTACLE)
                {
                    return Moves.MORPH_UP;
                }
                
            }
            return m;
        }

        public RectangleShape.Shape BestShape(Platform current_platform, Platform next_platform, RectangleShape.Shape move_shape, RectangleShape.Shape current_shape)
        {
            if (next_platform == null)
            {
                return move_shape;
            }
            if(next_platform.id == -1)
            {
                return current_shape;
            }
            if (next_platform.shapes[(int)current_shape])
            {
                return current_shape;
            }
            if (current_platform.shapes[(int)RectangleShape.Shape.SQUARE] && next_platform.shapes[(int)RectangleShape.Shape.SQUARE])
            {
                return RectangleShape.Shape.SQUARE;
            }
            if (current_platform.shapes[(int)RectangleShape.Shape.HORIZONTAL] && next_platform.shapes[(int)RectangleShape.Shape.HORIZONTAL])
            {
                return RectangleShape.Shape.HORIZONTAL;
            }
            return RectangleShape.Shape.VERTICAL;
        }
    }
}
