using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using static GeometryFriendsAgents.RectangleShape;

namespace GeometryFriendsAgents
{
    class ActionSelectorRectangle : ActionSelector
    {
        public LevelMapRectangle levelMap;
        public Platform next_platform = null;
        public MoveInformation move;
        public Moves lastMove = Moves.NO_ACTION;
        public double tilt_height = 0;
        public bool begin_high_tilt = false;
        
        public ActionSelectorRectangle(Dictionary<CollectibleRepresentation, int> collectibleId, LearningRectangle l, LevelMapRectangle levelMap, Graph graph) : base(collectibleId, l, graph)
        {
            this.levelMap = levelMap;
        }

        public static Moves GetToPosition(double current_position, double target_position, double current_velocity, double target_velocity, MoveInformation move)
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
                        double break_point = Math.Abs(current_velocity) * current_velocity / (2 * GameInfo.RECTANGLE_ACCELERATION) + current_position;
                        if (move.moveType == MoveType.FALL && break_point < move.x * GameInfo.PIXEL_LENGTH)
                        {
                            return Moves.MOVE_LEFT;
                        }
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
                move.velocityX *= -1;
                Moves m = GetToPosition((2 * target_position - current_position), target_position, -current_velocity, -target_velocity, move);
                move.velocityX *= -1;
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

        public Moves getPhisicsMove(RectangleRepresentation rI, MoveInformation move)
        {
            RectangleShape.Shape s = RectangleShape.GetShape(rI);
            double current_position = rI.X;
            double target_position = move.x * GameInfo.PIXEL_LENGTH;
            double current_velocity = rI.VelocityX;
            double target_velocity = move.velocityX;

            if (move.moveType == MoveType.FALL || move.moveType == MoveType.NOMOVE || move.moveType == MoveType.ADJACENT)
            {
                return GetToPosition(current_position, target_position, current_velocity, target_velocity, move);
            }
            else if (move.moveType == MoveType.TILT || move.moveType == MoveType.HIGHTILT)
            {
                if (Math.Abs(current_position - target_position) > 3 * GameInfo.PIXEL_LENGTH &&
                    ((target_velocity < 0 && current_position < target_position) || (target_velocity > 0 && current_position > target_position)))
                {
                    if (target_velocity < 0 && current_position < target_position)
                    {
                        return Moves.MOVE_RIGHT;
                    }
                    else if (target_velocity > 0 && current_position > target_position)
                    {
                        return Moves.MOVE_LEFT;
                    }
                }
                if (s != RectangleShape.Shape.VERTICAL)
                {
                    if (target_velocity < 0 && current_position < target_position)
                    {
                        return Moves.MOVE_RIGHT;
                    }
                    else if (target_velocity > 0 && current_position > target_position)
                    {
                        return Moves.MOVE_LEFT;
                    }
                }
                if (move.moveType == MoveType.TILT)
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
                    // Going to edge because that is where we really want to go (also target_velocity has been calculated accordingly
                    int edge = move.velocityX > 0 ? move.landingPlatform.leftEdge : move.landingPlatform.rightEdge;
                    return GetToPosition(current_position, edge * GameInfo.PIXEL_LENGTH, current_velocity, target_velocity, move);
                }
            }
            else if (move.moveType == MoveType.MONOSIDEDROP || move.moveType == MoveType.BIGHOLEADJ)
            {
                if (Math.Abs(current_velocity) > 250)
                {
                    return Moves.NO_ACTION;
                }
                if (target_velocity > 0)
                {
                    return Moves.MOVE_RIGHT;
                }
                else
                {
                    return Moves.MOVE_LEFT;
                }
            }
            else if (move.moveType == MoveType.BIGHOLEDROP)
            {
                int distance_x;
                if (move.velocityX > 0)
                {
                    distance_x = ((int)(rI.X / GameInfo.PIXEL_LENGTH)) - move.departurePlatform.rightEdge;
                }
                else
                {
                    distance_x = ((int)(rI.X / GameInfo.PIXEL_LENGTH)) - move.departurePlatform.leftEdge;
                }

                // Remember move.velocityX stores the hole's width
                StateRectangle state = new StateRectangle(distance_x, move.departurePlatform.yTop - ((int)(rI.Y / GameInfo.PIXEL_LENGTH)),
                    RectangleAgent.DiscreetVelocity(rI.VelocityX), (int)(rI.Height / (2 * GameInfo.PIXEL_LENGTH)), move.velocityX);

                return l.ChooseMove(state, move.velocityX);
            }
            else if (move.moveType == MoveType.DROP)
            {
                if (current_position >= target_position) // Rectangle on the right
                {
                    if (current_velocity < -1 && current_position - target_position < GameInfo.PIXEL_LENGTH * 2 && current_velocity > -20)
                    {
                        return Moves.NO_ACTION;
                    }
                    // target_velocity is always = 0
                    if (current_velocity >= 0)
                    {
                        double brake_distance = current_velocity * current_velocity / (2 * GameInfo.RECTANGLE_ACCELERATION);
                        if (Math.Abs(current_position + brake_distance - target_position) <= GameInfo.ERROR * GameInfo.PIXEL_LENGTH)
                        {
                            return Moves.MOVE_RIGHT;
                        }
                        else
                        {
                            if (current_position + brake_distance > target_position)
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
                        if (0 > sup_threshold)
                        {
                            return Moves.MOVE_RIGHT;
                        }
                        else if (0 < inf_threshold)
                        {
                            return Moves.MOVE_RIGHT;
                        }
                        else if (0 <= inf_threshold + 5)
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
                    RectangleRepresentation newrI = new RectangleRepresentation((float)(2 * target_position - current_position), rI.Y, (float)-current_velocity, rI.VelocityY, rI.Height);
                    move.velocityX *= -1;
                    Moves m = getPhisicsMove(newrI, move);
                    move.velocityX *= -1;
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
            else
            {
                return Moves.NO_ACTION;
            }
        }
        
        public Moves nextActionPhisics(ref List<MoveInformation> plan, List<CollectibleRepresentation> remaining, RectangleRepresentation rI, Platform currentPlatform)
        {
            //returns the next move, a first boolean indicating whether the move will lead to an air situation (Jump or fall) and a second boolean indicating whether the ball has to rotate in the
            //same direction of the velocity or in the oposite (in general will be oposite unless the jump lands near the vertix of the parabolla)
            
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

            if(lastMove == Moves.MORPH_UP && move.moveType == MoveType.DROP)
            {
                return Moves.MORPH_UP;
            }

            Moves m = getPhisicsMove(rI, move);
            
            Platform current_platform = levelMap.RectanglePlatform(rI);
            next_platform = null;

            RectangleShape.Shape target_shape = move.shape;
            double target_height = fheight(target_shape);

            if (move.x < current_platform.leftEdge || move.x > current_platform.rightEdge)
            {
                Tuple<Platform, Platform> adjacent_platforms = levelMap.AdjacentPlatforms(currentPlatform);
                double brake_point = rI.X + Math.Abs(rI.VelocityX) * rI.VelocityX / (2 * GameInfo.RECTANGLE_ACCELERATION);
                if (Math.Abs(rI.VelocityX) <= 10)
                {
                    if (move.x * GameInfo.PIXEL_LENGTH < rI.X)
                    {
                        next_platform = adjacent_platforms.Item1;
                    }
                    else
                    {
                        next_platform = adjacent_platforms.Item2;
                    }
                }
                else {
                    if (current_platform.leftEdge * GameInfo.PIXEL_LENGTH <= brake_point &&
                        brake_point <= current_platform.rightEdge * GameInfo.PIXEL_LENGTH)
                    {
                        // break_point inside current_platfom
                        if (move.x * GameInfo.PIXEL_LENGTH < rI.X)
                        {
                            next_platform = adjacent_platforms.Item1;
                        }
                        else
                        {
                            next_platform = adjacent_platforms.Item2;
                        }
                    }
                    // break_point is at the left of current platform
                    else if(current_platform.leftEdge * GameInfo.PIXEL_LENGTH > brake_point)
                    {
                        next_platform = adjacent_platforms.Item1;
                    }
                    
                    // break_point is at the right of current_platfom
                    else
                    {
                        next_platform = adjacent_platforms.Item2;
                    }
                }

                target_shape = BestShape(current_platform, next_platform, target_shape, RectangleShape.GetShape(rI));
                target_height = fheight(target_shape);
            }

            if (move.moveType == MoveType.TILT && tilt_height == 0)
            {
                double xcenter = move.velocityX > 0 ? move.landingPlatform.leftEdge * GameInfo.PIXEL_LENGTH : move.landingPlatform.rightEdge * GameInfo.PIXEL_LENGTH;
                double ycenter = move.landingPlatform.yTop * GameInfo.PIXEL_LENGTH;
                for (double h = GameInfo.VERTICAL_RECTANGLE_HEIGHT; h >= Math.Max(GameInfo.SQUARE_HEIGHT, (move.departurePlatform.yTop - move.landingPlatform.yTop) * GameInfo.PIXEL_LENGTH * 2); h -= 4)
                {
                    bool fits = true;
                    double radius1 = h - (move.departurePlatform.yTop - move.landingPlatform.yTop) * GameInfo.PIXEL_LENGTH;
                    double width = GameInfo.RECTANGLE_AREA / h;
                    double radius2 = Math.Sqrt(width * width + radius1 * radius1);
                    double angle_difference = move.velocityX < 0 ? -Math.Atan(width / radius1) : Math.Atan(width / radius1);
                    for (double theta = Math.PI / 2; move.velocityX > 0 ? theta > 0 : theta < Math.PI;
                        theta = move.velocityX > 0 ? theta - 0.05 : theta + 0.05)
                    {
                        int x1 = (int)(xcenter + radius1 * Math.Cos(theta)) / GameInfo.PIXEL_LENGTH;
                        int y1 = (int)(ycenter - radius1 * Math.Sin(theta)) / GameInfo.PIXEL_LENGTH - 1;

                        int x2 = (int)(xcenter + radius2 * Math.Cos(theta + angle_difference)) / GameInfo.PIXEL_LENGTH;
                        int y2 = (int)(ycenter - radius1 * Math.Sin(theta + angle_difference)) / GameInfo.PIXEL_LENGTH - 1;

                        if (y1 >= move.landingPlatform.yTop)
                        {
                            break;
                        }
                        if (levelMap.levelMap[x1, y1] == LevelMap.PixelType.OBSTACLE || levelMap.levelMap[x1, y1] == LevelMap.PixelType.PLATFORM ||
                            levelMap.levelMap[x2, y2] == LevelMap.PixelType.OBSTACLE || levelMap.levelMap[x2, y2] == LevelMap.PixelType.PLATFORM)
                        {
                            fits = false;

                            break;
                        }
                    }
                    if (fits)
                    {
                        tilt_height = h;
                        break;
                    }
                }
            }
            else if (move.moveType == MoveType.BIGHOLEDROP)
            {
                int distance_x;
                if (move.velocityX > 0)
                {
                    distance_x = move.departurePlatform.rightEdge - ((int)(rI.X / GameInfo.PIXEL_LENGTH));                  
                }
                else
                {
                    distance_x = ((int)(rI.X / GameInfo.PIXEL_LENGTH)) - move.departurePlatform.leftEdge;
                }
                if (distance_x > GameInfo.MAX_DISTANCE_RECTANGLE)
                {
                    if (target_shape == RectangleShape.Shape.SQUARE && rI.Height < target_height + 5 && rI.Height > target_height - 5)
                    {

                    }
                    else if (target_height + 3 < rI.Height)
                    {
                        return Moves.MORPH_DOWN;
                    }
                    else if ((target_height == RectangleShape.fheight(RectangleShape.Shape.VERTICAL) ? target_height - 3 : target_height - 5) > rI.Height)
                    {
                        if (levelMap.RectangleCanMorphUp(rI))
                        {
                            return Moves.MORPH_UP;
                        }
                    }
                }
            }
            else if (move.moveType != MoveType.DROP || (Math.Abs(rI.X / GameInfo.PIXEL_LENGTH - move.x) <= 1 && Math.Abs(rI.VelocityX) <= 20))
            {
                // Check shape
                if (move.moveType == MoveType.TILT && (next_platform == null || next_platform.id == -1 || target_shape == RectangleShape.Shape.VERTICAL))
                {
                    target_height = tilt_height;
                }
                if (target_shape == RectangleShape.Shape.SQUARE && rI.Height < target_height + 5 && rI.Height > target_height - 5)
                {

                }
                else if (target_height + 3 < rI.Height)
                {
                    return Moves.MORPH_DOWN;
                }
                else if ((target_height == RectangleShape.fheight(RectangleShape.Shape.VERTICAL) ? target_height - 3 : target_height - 5) > rI.Height)
                {
                    if (move.moveType == MoveType.NOMOVE || move.moveType == MoveType.TILT || move.moveType == MoveType.DROP || move.moveType == MoveType.HIGHTILT)
                    {
                        if (levelMap.levelMap[(int)rI.X / GameInfo.PIXEL_LENGTH, (int)((rI.Y - 3 * rI.Height / 5) / GameInfo.PIXEL_LENGTH) - 1] != LevelMap.PixelType.OBSTACLE)
                        {
                            return Moves.MORPH_UP;
                        }
                    }
                    else if (levelMap.RectangleCanMorphUp(rI))
                    {
                        return Moves.MORPH_UP;
                    }
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
                return move_shape;
            }
            if(current_platform.shapes[(int)move_shape] && next_platform.shapes[(int)move_shape])
            {
                if (move.x > (current_platform.leftEdge + current_platform.rightEdge) / 2)
                {
                    bool canKeepShape = true;
                    Platform next= levelMap.AdjacentPlatforms(current_platform).Item2;
                    while (move.x > next.leftEdge)
                    {
                        if (!next.shapes[(int)move_shape])
                        {
                            canKeepShape = false;
                            break;
                        }
                        next = levelMap.AdjacentPlatforms(next).Item2;
                    }
                    if(canKeepShape)
                    {
                        return move_shape;
                    }
                }
                else
                {
                    bool canKeepShape = true;
                    Platform next = levelMap.AdjacentPlatforms(current_platform).Item1;
                    while (move.x < next.rightEdge)
                    {
                        if (!next.shapes[(int)move_shape])
                        {
                            canKeepShape = false;
                            break;
                        }
                        next = levelMap.AdjacentPlatforms(next).Item1;
                    }
                    if (canKeepShape)
                    {
                        return move_shape;
                    }
                }
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
