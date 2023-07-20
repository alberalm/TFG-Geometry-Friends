using GeometryFriends.AI;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using static GeometryFriendsAgents.RectangleShape;

namespace GeometryFriendsAgents
{
    public class ActionSelectorRectangle : ActionSelector
    {
        public LevelMapRectangle levelMap;
        public Platform next_platform = null;
        public Moves lastMove = Moves.NO_ACTION;
        public double tilt_height = 0;
        public bool begin_high_tilt = false;
        public bool waitingForCircleToLand = false;
        public int count = -1;
        public int circle_flying_velocity = 1000;
        public bool hasFinishedReplanning = true;
        public int lastX = 0;
        public bool avoidCircle = false;
        public bool pick_up_circle = false;
        
        public ActionSelectorRectangle(Dictionary<CollectibleRepresentation, int> collectibleId, LearningRectangle l, LevelMapRectangle levelMap, Graph graph,SetupMaker setupMaker) : base(collectibleId, l, graph,setupMaker)
        {
            this.levelMap = levelMap;
        }

        protected override MoveInformation DiamondsCanBeCollectedFrom(CircleRepresentation cI, RectangleRepresentation rI, Platform p, List<CollectibleRepresentation> remaining, int agentX)
        {
            int mindistance = 4000;
            MoveInformation move = null;
            MoveInformation next_move_circle = setupMaker.planCircle.Count > 0 ? setupMaker.planCircle[0] : null;
            MoveInformation next_move_rectangle = setupMaker.planRectangle.Count > 0 ? setupMaker.planRectangle[0] : null;
            foreach (MoveInformation m in p.moveInfoList)
            {
                if (m.landingPlatform.id == p.id)
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
                                        if (tuple.Item1 == m.landingPlatform.id && tuple.Item2.Equals("r"))
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

            if (move.moveType == MoveType.FALL || move.moveType == MoveType.NOMOVE || move.moveType == MoveType.ADJACENT || move.moveType == MoveType.COOPMOVE)
            {
                return GetToPosition(current_position, target_position, current_velocity, target_velocity, move);
            }
            else if (move.moveType == MoveType.TILT || move.moveType == MoveType.HIGHTILT || move.moveType == MoveType.CIRCLETILT)
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
                if (move.moveType == MoveType.TILT || (move.moveType == MoveType.CIRCLETILT && Math.Abs(move.velocityX) == 1))
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
        
        public Moves nextActionPhisics(ref List<MoveInformation> plan, List<CollectibleRepresentation> remaining, CircleRepresentation cI,RectangleRepresentation rI, Platform currentPlatform)
        {
            MoveInformation move2 = DiamondsCanBeCollectedFrom(cI, rI, levelMap.small_to_simplified[currentPlatform], remaining, (int)(rI.X / GameInfo.PIXEL_LENGTH));
            
            if (setupMaker.currentPlatformCircle.real && (int)cI.VelocityX / 10 != circle_flying_velocity / 10)
            {
                waitingForCircleToLand = false;
                if (setupMaker.circleInAir)
                {
                    setupMaker.rectangleAgentReadyForCoop = false;
                    setupMaker.circleInAir = false;
                }
                avoidCircle = false;
            }
            
            if (!waitingForCircleToLand)
            {                
                if (move2 != null)
                {
                    move = new MoveInformation(move2);
                    target_position = move.x;
                    target_velocity = move.velocityX;
                    setupMaker.rectangleAgentReadyForCoop = false;
                    string goal = "Coger diamante(s) ";
                    foreach (int d in move.diamondsCollected)
                    {
                        goal += "D"+d.ToString() + " ";
                    }
                    if (!move.departurePlatform.real || !move.landingPlatform.real)
                    {
                        goal += "con cooperación";
                    }
                    else
                    {
                        goal += "individualmente";
                    }
                    goal += " mediante un " + move.moveType.ToString();
                    setupMaker.rectangle_immediate_goal = goal;
                }
                else
                {
                    if (plan.Count > 0)
                    {
                        move = new MoveInformation(plan[0]);
                        target_position = plan[0].x;
                        target_velocity = plan[0].velocityX;
                        if (setupMaker.actionSelectorCircle.move != null)
                        {
                            if (move.moveType == MoveType.COOPMOVE && setupMaker.actionSelectorCircle.move.departurePlatform.real && setupMaker.actionSelectorCircle.move.landingPlatform.real)
                            {
                                setupMaker.rectangle_immediate_goal = "WAIT en R" + move.departurePlatform.id.ToString();
                            }
                            else if (move.moveType == MoveType.COOPMOVE && !setupMaker.actionSelectorCircle.move.departurePlatform.real)
                            {
                                setupMaker.rectangle_immediate_goal = "Mantenerse en la plataforma R" + move.departurePlatform.id.ToString() + " para ser la plataforma de despegue del círculo";
                            }
                            else if (!setupMaker.actionSelectorCircle.move.landingPlatform.real)
                            {
                                setupMaker.rectangle_immediate_goal = "Mantenerse en la plataforma R" + move.departurePlatform.id.ToString() + " para ser la plataforma de aterrizaje del círculo";
                            }
                            else
                            {
                                setupMaker.rectangle_immediate_goal = move.moveType.ToString() + " de R" + move.departurePlatform.id.ToString() + " a R" + move.landingPlatform.id.ToString();
                                if (move.diamondsCollected.Count > 0)
                                {
                                    setupMaker.rectangle_immediate_goal += " que alcanza ";
                                    foreach (int d in move.diamondsCollected)
                                    {
                                        setupMaker.rectangle_immediate_goal += "D" + d.ToString() + " ";
                                    }
                                }
                            }
                        }
                        else
                        {
                            setupMaker.rectangle_immediate_goal = move.moveType.ToString() + " de R" + move.departurePlatform.id.ToString() + " a R" + move.landingPlatform.id.ToString();
                            if (move.diamondsCollected.Count > 0)
                            {
                                setupMaker.rectangle_immediate_goal += " que alcanza ";
                                foreach (int d in move.diamondsCollected)
                                {
                                    setupMaker.rectangle_immediate_goal += "D" + d.ToString() + " ";
                                }
                            }
                        }
                    }
                    else if (setupMaker.actionSelectorCircle.move == null)
                    {
                        Random rnd = new Random();
                        List<Moves> possibleMoves = new List<Moves>
                    {
                        Moves.MOVE_RIGHT,
                        Moves.MOVE_LEFT,
                        Moves.MORPH_DOWN,
                        Moves.MORPH_UP
                    };
                        setupMaker.rectangle_state = "No sé que hacer...";
                        return possibleMoves[rnd.Next(possibleMoves.Count)];
                    }
                    else if (move == null)
                    {
                        setupMaker.rectangle_immediate_goal = "Ayudar al círculo";
                        move = new MoveInformation(currentPlatform) { moveType = MoveType.COOPMOVE };
                    }
                    else
                    {
                        setupMaker.rectangle_immediate_goal = "Mantenerse en la plataforma R" + move.departurePlatform.id.ToString() + " sin estorbar al círculo";
                        float middle = GameInfo.PIXEL_LENGTH * (setupMaker.currentPlatformRectangle.rightEdge + setupMaker.currentPlatformRectangle.leftEdge) / 2;
                        move.x = setupMaker.circleInfo.X < middle ? setupMaker.currentPlatformRectangle.rightEdge : setupMaker.currentPlatformRectangle.leftEdge;
                        move.velocityX = 0;
                    }
                }
            }

            if (count < 100 && count >= 0)
            {
                move.x = (int)setupMaker.circleInfo.X/GameInfo.PIXEL_LENGTH;
                move.shape = RectangleShape.Shape.HORIZONTAL;
                setupMaker.rectangleAgentReadyForCoop = false;
                move.velocityX = 0;
                count++;
                setupMaker.rectangle_state = "Dejando que el círculo termine de aterrizar...";
            }
            else if (count == 100)
            {
                count = -1;
            }

            if (lastMove == Moves.MORPH_UP && move.moveType == MoveType.DROP)
            {
                return Moves.MORPH_UP;
            }
            Moves m = getPhisicsMove(rI, move);
            if (setupMaker.CircleAboveRectangle() && count == -1)
            {
                setupMaker.rectangle_state = "Transportando al círculo...";
                setupMaker.circleInAir = false;
                avoidCircle = false;
                circle_flying_velocity = 1000;
                if (waitingForCircleToLand)
                {
                    count = 0;
                    waitingForCircleToLand = false;
                }

                if (setupMaker.actionSelectorCircle.move != null && setupMaker.actionSelectorCircle.move.moveType == MoveType.ADJACENT)
                {
                    m = getPhisicsMove(rI, move);
                }
                else
                {                   
                    int w = 0;
                    if (setupMaker.actionSelectorCircle.move != null)
                    {
                        w = RectangleShape.width(RectangleShape.GetShape(new RectangleRepresentation(0, 0, 0, 0, currentPlatform.yTop * GameInfo.PIXEL_LENGTH - (setupMaker.actionSelectorCircle.move.path[0].Item2 + GameInfo.CIRCLE_RADIUS))));
                        if (setupMaker.actionSelectorCircle.move.velocityX == 0)
                        {
                            move.x = setupMaker.actionSelectorCircle.move.x;
                        }
                        else if (setupMaker.actionSelectorCircle.move.velocityX > 0)
                        {
                            move.x = setupMaker.actionSelectorCircle.move.x - w / 2 + 4;
                        }
                        else
                        {
                            move.x = setupMaker.actionSelectorCircle.move.x + w / 2 - 3;
                        }
                    }

                    move.shape = RectangleShape.Shape.HORIZONTAL;
                    move.velocityX = 0;
                    move.moveType = MoveType.COOPMOVE;
                    m = getPhisicsMove(rI, move);
                    if (Math.Abs(setupMaker.rectangleInfo.X - move.x * GameInfo.PIXEL_LENGTH) < GameInfo.PIXEL_LENGTH
                        && Math.Abs(setupMaker.rectangleInfo.VelocityX) < 20)
                    {
                        m = Moves.NO_ACTION;
                    }
                    if (Math.Abs(setupMaker.rectangleInfo.X - move.x * GameInfo.PIXEL_LENGTH) < 10 * GameInfo.PIXEL_LENGTH
                        && Math.Abs(setupMaker.rectangleInfo.VelocityX) < 75)
                    {
                        foreach (Platform small_p in setupMaker.levelMapCircle.simplified_to_small[setupMaker.actionSelectorCircle.move.departurePlatform])
                        {
                            if (Math.Abs(small_p.yTop * GameInfo.PIXEL_LENGTH - GameInfo.CIRCLE_RADIUS - setupMaker.actionSelectorCircle.move.path[0].Item2) < GameInfo.PIXEL_LENGTH)
                            {
                                foreach (RectangleShape.Shape shape in GameInfo.SHAPES)
                                {
                                    if (small_p.shapes[(int)shape])
                                    {
                                        move.shape = shape;
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        if (Math.Abs(RectangleShape.fheight(move.shape) - setupMaker.rectangleInfo.Height) < GameInfo.PIXEL_LENGTH / 2)
                        {
                            setupMaker.rectangleAgentReadyForCoop = true;
                        }
                    }
                    if (!setupMaker.circleAgentReadyForCoop)
                    {
                        if (setupMaker.actionSelectorCircle.move != null)
                        {
                            if (setupMaker.actionSelectorCircle.move.departurePlatform.id == setupMaker.actionSelectorCircle.move.landingPlatform.id)
                            {
                                PrepareForCircleLanding(true);
                                float height = currentPlatform.yTop * GameInfo.PIXEL_LENGTH - (setupMaker.actionSelectorCircle.move.path[setupMaker.actionSelectorCircle.move.path.Count - 1].Item2 + GameInfo.CIRCLE_RADIUS);
                                move.shape = RectangleShape.GetShape(new RectangleRepresentation(0, 0, 0, 0, height));
                                waitingForCircleToLand = true;
                                if (setupMaker.circleInfo.VelocityY < 0)
                                {
                                    setupMaker.circleInAir = true;
                                }
                            }
                            else
                            {
                                if (setupMaker.actionSelectorCircle.move.landingPlatform.yTop > setupMaker.currentPlatformRectangle.yTop + 1 - GameInfo.HORIZONTAL_RECTANGLE_HEIGHT / GameInfo.PIXEL_LENGTH)
                                {
                                    PrepareForCircleLanding(false);
                                    avoidCircle = true;
                                }
                                else
                                {
                                    PrepareForCircleLanding(true);
                                    waitingForCircleToLand = true;
                                    float height = currentPlatform.yTop * GameInfo.PIXEL_LENGTH - (setupMaker.actionSelectorCircle.move.path[setupMaker.actionSelectorCircle.move.path.Count - 1].Item2 + GameInfo.CIRCLE_RADIUS);
                                    move.shape = RectangleShape.GetShape(new RectangleRepresentation(0, 0, 0, 0, height));
                                    if (setupMaker.circleInfo.VelocityY < 0)
                                    {
                                        setupMaker.circleInAir = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (move.moveType == MoveType.COOPMOVE)
            {
                if ((setupMaker.circleInAir || setupMaker.currentPlatformCircle.id == -1 || avoidCircle) && setupMaker.actionSelectorCircle.move != null)
                {
                    PrepareForCircleLanding(setupMaker.actionSelectorCircle.move.landingPlatform.yTop <= setupMaker.currentPlatformRectangle.yTop + 1 -GameInfo.HORIZONTAL_RECTANGLE_HEIGHT/GameInfo.PIXEL_LENGTH);
                }// Circle wants to be above rectangle
                else if (setupMaker.planCircle.Count > 0 && !setupMaker.planCircle[0].landingPlatform.real && setupMaker.planCircle[0].departurePlatform.real)
                {
                    setupMaker.rectangle_state = "Deslizándome hasta el punto de aterrizaje del círculo";
                    if (setupMaker.actionSelectorCircle.move != null && setupMaker.actionSelectorCircle.move.distanceToObstacle > 0)
                    {
                        move.x = 0;
                    }
                    if (!setupMaker.circleInAir && (setupMaker.currentPlatformCircle.id == -1 || (!setupMaker.currentPlatformCircle.real && !setupMaker.CircleAboveRectangle())))
                    {
                        setupMaker.circleInAir = true;
                    }
                    float height = currentPlatform.yTop * GameInfo.PIXEL_LENGTH - (setupMaker.planCircle[0].path[setupMaker.planCircle[0].path.Count - 1].Item2 + GameInfo.CIRCLE_RADIUS);
                    move.shape = RectangleShape.GetShape(new RectangleRepresentation(0, 0, 0, 0, height));
                    float width = GameInfo.RECTANGLE_AREA / height;
                    float targetx = setupMaker.planCircle[0].path[setupMaker.planCircle[0].path.Count - 1].Item1;
                    float minx = targetx - width / 2;
                    float maxx = targetx + width / 2;
                    float miny = setupMaker.planCircle[0].path[setupMaker.planCircle[0].path.Count - 1].Item2 + GameInfo.CIRCLE_RADIUS;
                    float maxy = miny + height;
                    bool intersects = false;
                    foreach (Tuple<float, float> tup in setupMaker.planCircle[0].path)
                    {
                        if (tup.Item1 + GameInfo.CIRCLE_RADIUS > minx && tup.Item1 - GameInfo.CIRCLE_RADIUS < maxx && tup.Item2 + GameInfo.CIRCLE_RADIUS > miny && tup.Item2 - GameInfo.CIRCLE_RADIUS < maxy)
                        {
                            intersects = true;
                            break;
                        }
                    }
                    if (intersects)
                    {
                        bool intersects_if_left = false;
                        bool intersects_if_right = false;
                        int left_edge = GameInfo.LEVEL_MAP_WIDTH;
                        int right_edge = 0;
                        foreach(Platform small_p_rect in setupMaker.levelMapRectangle.simplified_to_small[setupMaker.currentPlatformRectangle])
                        {
                            if (small_p_rect.shapes[(int)move.shape])
                            {
                                left_edge = Math.Min(left_edge, small_p_rect.leftEdge);
                                right_edge = Math.Max(right_edge, small_p_rect.rightEdge);
                            }
                        }
                        //Check if rectangle has space in left side
                        if (left_edge + width / (2 * GameInfo.PIXEL_LENGTH) + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH <
                            targetx / GameInfo.PIXEL_LENGTH)
                        {
                            //Check if trajectory intersects when rectangle in left side
                            float minx2 = minx - (width / 2 + GameInfo.CIRCLE_RADIUS + GameInfo.PIXEL_LENGTH);
                            float maxx2 = maxx - (width / 2 + GameInfo.CIRCLE_RADIUS + GameInfo.PIXEL_LENGTH);
                            foreach (Tuple<float, float> tup in setupMaker.planCircle[0].path)
                            {
                                if (tup.Item1 + GameInfo.CIRCLE_RADIUS > minx2 && tup.Item1 - GameInfo.CIRCLE_RADIUS < maxx2 && tup.Item2 + GameInfo.CIRCLE_RADIUS > miny && tup.Item2 - GameInfo.CIRCLE_RADIUS < maxy)
                                {
                                    intersects_if_left = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            intersects_if_left = true;
                        }
                        //Check if rectangle has space in right side
                        if(right_edge - width / (2 * GameInfo.PIXEL_LENGTH) - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH >
                            targetx / GameInfo.PIXEL_LENGTH)
                        {
                            //Check if trajectory intersects when rectangle in left side
                            float minx2 = minx + (width / 2 + GameInfo.CIRCLE_RADIUS + GameInfo.PIXEL_LENGTH);
                            float maxx2 = maxx + (width / 2 + GameInfo.CIRCLE_RADIUS + GameInfo.PIXEL_LENGTH);
                            foreach (Tuple<float, float> tup in setupMaker.planCircle[0].path)
                            {
                                if (tup.Item1 + GameInfo.CIRCLE_RADIUS > minx2 && tup.Item1 - GameInfo.CIRCLE_RADIUS < maxx2 && tup.Item2 + GameInfo.CIRCLE_RADIUS > miny && tup.Item2 - GameInfo.CIRCLE_RADIUS < maxy)
                                {
                                    intersects_if_right = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            intersects_if_right = true;
                        }

                        if (setupMaker.currentPlatformCircle.yTop == setupMaker.currentPlatformRectangle.yTop) 
                        {
                            //If both agents are in the same platform the rectangle is sent (if possible) to the point where no changing is needed and the order is preserved 
                            if (setupMaker.rectangleInfo.X < setupMaker.circleInfo.X && !intersects_if_left)
                            {
                                intersects_if_right = true;
                            }
                            else if (setupMaker.rectangleInfo.X > setupMaker.circleInfo.X && !intersects_if_right)
                            {
                                intersects_if_left = true;
                            }
                        }
                        else
                        {
                            if (setupMaker.actionSelectorCircle.move != null)
                            {
                                float right = setupMaker.actionSelectorCircle.move.xlandPoint * GameInfo.PIXEL_LENGTH + width / 2 + GameInfo.CIRCLE_RADIUS;
                                float left = setupMaker.actionSelectorCircle.move.xlandPoint * GameInfo.PIXEL_LENGTH - width / 2 - GameInfo.CIRCLE_RADIUS;
                                //If agents are in different same platforms, the rectangle is sent (if possible) to the closest point
                                if (Math.Abs(setupMaker.rectangleInfo.X - right) < Math.Abs(setupMaker.rectangleInfo.X - left) && !intersects_if_right)
                                {
                                    intersects_if_left = true;
                                }
                                else if (Math.Abs(setupMaker.rectangleInfo.X - left) < Math.Abs(setupMaker.rectangleInfo.X - right) && !intersects_if_left)
                                {
                                    intersects_if_right = true;
                                }
                            }
                        }


                        if (!intersects_if_left)
                        {
                            if (setupMaker.actionSelectorCircle.move != null)
                            {
                                setupMaker.rectangle_state += " (o un poco a la izquierda)...";
                                move.x = setupMaker.actionSelectorCircle.move.xlandPoint;
                                move.velocityX = 0;
                                if (setupMaker.actionSelectorCircle.move.distanceToObstacle > 0 || (!setupMaker.circleInAir && setupMaker.currentPlatformCircle.real))
                                {
                                    move.x -= (int)(width / 2 + GameInfo.CIRCLE_RADIUS) / GameInfo.PIXEL_LENGTH;
                                }
                                if (Math.Abs(move.x - setupMaker.rectangleInfo.X / GameInfo.PIXEL_LENGTH) < 2 && Math.Abs(setupMaker.rectangleInfo.VelocityX) < 50)
                                {
                                    setupMaker.rectangleAgentReadyForCoop = true;
                                    waitingForCircleToLand = true;
                                    setupMaker.rectangle_state = "Esperando al aterrizaje del círculo...";
                                }
                            }
                        }
                        if (!intersects_if_right)
                        {
                            if (setupMaker.actionSelectorCircle.move != null)
                            {
                                setupMaker.rectangle_state += " (o un poco a la derecha)...";
                                move.x = setupMaker.actionSelectorCircle.move.xlandPoint;
                                move.velocityX = 0; 
                                if (setupMaker.actionSelectorCircle.move.distanceToObstacle > 0 || (!setupMaker.circleInAir && setupMaker.currentPlatformCircle.real))
                                {
                                    move.x += (int)(width / 2 + GameInfo.CIRCLE_RADIUS) / GameInfo.PIXEL_LENGTH;
                                }
                                if (Math.Abs(move.x - setupMaker.rectangleInfo.X / GameInfo.PIXEL_LENGTH) < 2 && Math.Abs(setupMaker.rectangleInfo.VelocityX) < 50)
                                {
                                    setupMaker.rectangleAgentReadyForCoop = true;
                                    waitingForCircleToLand = true;
                                    setupMaker.rectangle_state = "Esperando al aterrizaje del círculo...";
                                }
                            }
                        }
                        if (intersects_if_right && intersects_if_left)
                        {
                            setupMaker.rectangle_state += "...";
                            // We inverse the process of jumping to calculate from where the circle needs to jump
                            // Then we replace the current plan[0] move with it
                            move.x = (setupMaker.currentPlatformRectangle.rightEdge + setupMaker.currentPlatformRectangle.leftEdge) / 2; // Need to change this
                            Platform departure_platform = setupMaker.levelMapCircle.simplified_to_small[setupMaker.planCircle[0].departurePlatform][0];
                            Platform landing_platform = setupMaker.levelMapCircle.simplified_to_small[setupMaker.planCircle[0].landingPlatform][0];
                            foreach (Platform p in setupMaker.levelMapCircle.simplified_to_small[setupMaker.planCircle[0].landingPlatform])
                            {
                                if(p.yTop > landing_platform.yTop)
                                {
                                    landing_platform = p;
                                }
                            }
                            MoveInformation new_jump = setupMaker.levelMapCircle.moveGenerator.GenerateNewJump(
                                setupMaker.levelMapCircle.platformList,
                                departure_platform,
                                landing_platform,
                                move.x,
                                (int)setupMaker.planCircle[0].path[setupMaker.planCircle[0].path.Count - 1].Item2 / GameInfo.PIXEL_LENGTH);
                            if (new_jump != null)
                            {
                                new_jump.departurePlatform = setupMaker.levelMapCircle.small_to_simplified[new_jump.departurePlatform];
                                new_jump.landingPlatform = setupMaker.levelMapCircle.small_to_simplified[new_jump.landingPlatform];
                                setupMaker.planCircle[0] = new_jump;
                                return Moves.NO_ACTION;
                            }
                            else
                            {
                                // What can we do here?
                                setupMaker.rectangle_state = "No sé que hacer...";
                            }
                        }
                    }
                    else
                    {
                        setupMaker.rectangle_state += "...";
                        if (setupMaker.actionSelectorCircle.move != null)
                        {
                            PrepareForCircleLanding(true);
                            if (Math.Abs(move.x - setupMaker.rectangleInfo.X / GameInfo.PIXEL_LENGTH) < 2 && Math.Abs(setupMaker.rectangleInfo.VelocityX) < 50)
                            {
                                setupMaker.rectangleAgentReadyForCoop = true;
                                waitingForCircleToLand= true;
                                setupMaker.rectangle_state = "Esperando al aterrizaje del círculo...";
                            }
                        }
                    }

                    /*else if (!setupMaker.rectangleAgentReadyForCoop)
                    {
                        // Rectangle is not ready to cooperate
                        move.x = setupMaker.planCircle[0].xlandPoint;
                        move.velocityX = 0;
                        if (Math.Abs(move.x - rI.X / GameInfo.PIXEL_LENGTH) < 2 && Math.Abs(rI.VelocityX) < 50)
                        {
                            setupMaker.rectangleAgentReadyForCoop = true;
                        }
                    }
                    else if (setupMaker.currentPlatformCircle.id != setupMaker.planCircle[0].departurePlatform.id)
                    {
                        // Rectangle is ready to follow circle
                        move.x = (int)(setupMaker.circleInfo.X / GameInfo.PIXEL_LENGTH);
                    }
                    else
                    {
                        // Circle has not left departure platform
                        move.x = setupMaker.planCircle[0].xlandPoint;
                        move.velocityX = 0;
                    }*/



                    m = getPhisicsMove(rI, move);
                }
                else if (setupMaker.planCircle.Count > 0 && setupMaker.planCircle[0].landingPlatform.real && setupMaker.planCircle[0].departurePlatform.real)
                {
                    //This COOPMOVE was addeed to the plan because the rectangle has to wait
                    move.x = (int)setupMaker.rectangleInfo.X / GameInfo.PIXEL_LENGTH;
                    m = Moves.NO_ACTION;
                }
                m = getPhisicsMove(rI, move);
            }
            if (avoidCircle)
            {
                move.shape = RectangleShape.Shape.HORIZONTAL;
            }
            if(lastX != 0 && move.x == 0)
            {
                move.x = lastX;
                m = getPhisicsMove(rI, move);
            }
            if (move.x != 0)
            {
                lastX = move.x;
            }
            
            if(!(setupMaker.CircleAboveRectangle() && count == -1)&&move.moveType != MoveType.COOPMOVE)
            {
                setupMaker.rectangle_state = "Deslizándome hasta la posición objetivo...";
            }

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
                if (move.moveType == MoveType.CIRCLETILT)
                {
                    if (!setupMaker.circleAgentReadyForCircleTilt)
                    {
                        setupMaker.rectangle_state = "Esperando a que el círculo esté listo para hacer el CIRCLETILT...";                        
                        return GetToPosition(rI.X, move.velocityX > 0 ? (move.departurePlatform.leftEdge + 5) * GameInfo.PIXEL_LENGTH : (move.departurePlatform.rightEdge - 5) * GameInfo.PIXEL_LENGTH,
                                rI.VelocityX, 0, move);
                        /*if (Math.Sign(rI.X - cI.X) == Math.Sign(target_position * GameInfo.PIXEL_LENGTH - rI.X) ||
                        Math.Abs(cI.X - target_position * GameInfo.PIXEL_LENGTH) > 10 * GameInfo.PIXEL_LENGTH)
                        {
                            
                        }
                        else
                        {
                            return Moves.NO_ACTION;
                        }*/
                    }
                    else
                    {
                        setupMaker.rectangle_state = "Deslizándome hasta la posición del CIRCLETILT...";
                    }
                }
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
                else if ((target_height == RectangleShape.fheight(RectangleShape.Shape.VERTICAL) ? target_height - (move.moveType == MoveType.HIGHTILT ? 1 : 4) : target_height - 5) > rI.Height)
                {
                    if (move.moveType == MoveType.COOPMOVE || move.moveType == MoveType.NOMOVE || move.moveType == MoveType.TILT || move.moveType == MoveType.DROP || move.moveType == MoveType.HIGHTILT)
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

        private void PrepareForCircleLanding(bool pick_up_circle)
        {
            if (pick_up_circle)
            {
                setupMaker.rectangle_state = "Recalculando punto de aterrizaje del círculo...";
            }
            else
            {
                setupMaker.rectangle_state = "Esquivando al círculo...";
            }

            if (move.x == 0)
            {
                move.x = setupMaker.actionSelectorCircle.move.xlandPoint;
            }
            move.velocityX = 0;
            circle_flying_velocity = (int)setupMaker.circleInfo.VelocityX;
            this.pick_up_circle = pick_up_circle;

            setupMaker.actionSelectorCircle.move.distanceToObstacle++;
            Platform pcircle=setupMaker.levelMapCircle.CirclePlatform(setupMaker.circleInfo);
            if (setupMaker.actionSelectorCircle.move != null && (pcircle.id == -1 ||
                setupMaker.levelMapCircle.small_to_simplified[pcircle].id != setupMaker.actionSelectorCircle.move.departurePlatform.id ||
                setupMaker.circleInfo.VelocityY > 0)
                && (setupMaker.actionSelectorCircle.move.distanceToObstacle > 0 || avoidCircle))
            {
                hasFinishedReplanning = false;
                MoveInformation m2 = new MoveInformation(setupMaker.actionSelectorCircle.move);
                List<MoveInformation> l = setupMaker.levelMapCircle.SimulateMove(setupMaker.circleInfo.X, setupMaker.circleInfo.Y, setupMaker.circleInfo.VelocityX, -setupMaker.circleInfo.VelocityY, ref m2, 0.005f);
                
                foreach (MoveInformation move_l in l)
                {
                    if (setupMaker.actionSelectorCircle.move.landingPlatform.id == setupMaker.levelMapCircle.small_to_simplified[move_l.landingPlatform].id &&
                        Math.Abs(move_l.path[move_l.path.Count - 1].Item2 - setupMaker.actionSelectorCircle.move.path[setupMaker.actionSelectorCircle.move.path.Count - 1].Item2) < 2 * GameInfo.PIXEL_LENGTH)
                    {                       
                        if (pick_up_circle)
                        {
                            move.x = move_l.xlandPoint;
                        }
                        else
                        {
                            float width = GameInfo.VERTICAL_RECTANGLE_HEIGHT;
                            float targetx = move_l.xlandPoint * GameInfo.PIXEL_LENGTH;
                            float minx = targetx - width / 2;
                            float maxx = targetx + width / 2;
                            float miny = setupMaker.actionSelectorCircle.move.path[setupMaker.actionSelectorCircle.move.path.Count - 1].Item2 + GameInfo.CIRCLE_RADIUS;
                            float maxy = miny + GameInfo.HORIZONTAL_RECTANGLE_HEIGHT;
                            bool intersects_if_left = false;
                            bool intersects_if_right = false;
                            float left_point = targetx - (width / 2 + GameInfo.CIRCLE_RADIUS + GameInfo.PIXEL_LENGTH);
                            float right_point = targetx + (width / 2 + GameInfo.CIRCLE_RADIUS + GameInfo.PIXEL_LENGTH);
                            //Check if rectangle has space in left side
                            if (setupMaker.currentPlatformRectangle.leftEdge + width / (2 * GameInfo.PIXEL_LENGTH) + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH <
                                targetx / GameInfo.PIXEL_LENGTH)
                            {
                                //Check if trajectory intersects when rectangle in left side
                                float minx2 = minx - (width / 2 + GameInfo.CIRCLE_RADIUS + GameInfo.PIXEL_LENGTH);
                                float maxx2 = maxx - (width / 2 + GameInfo.CIRCLE_RADIUS + GameInfo.PIXEL_LENGTH);
                                
                                foreach (Tuple<float, float> tup in setupMaker.actionSelectorCircle.move.path)
                                {
                                    if (tup.Item1 + GameInfo.CIRCLE_RADIUS > minx2 && tup.Item1 - GameInfo.CIRCLE_RADIUS < maxx2 && tup.Item2 + GameInfo.CIRCLE_RADIUS > miny && tup.Item2 - GameInfo.CIRCLE_RADIUS < maxy)
                                    {
                                        intersects_if_left = true;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                intersects_if_left = true;
                            }
                            //Check if rectangle has space in right side
                            if (setupMaker.currentPlatformRectangle.rightEdge - width / (2 * GameInfo.PIXEL_LENGTH) - GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH >
                                targetx / GameInfo.PIXEL_LENGTH)
                            {
                                //Check if trajectory intersects when rectangle in left side
                                float minx2 = minx + (width / 2 + GameInfo.CIRCLE_RADIUS + GameInfo.PIXEL_LENGTH);
                                float maxx2 = maxx + (width / 2 + GameInfo.CIRCLE_RADIUS + GameInfo.PIXEL_LENGTH);
                                foreach (Tuple<float, float> tup in setupMaker.actionSelectorCircle.move.path)
                                {
                                    if (tup.Item1 + GameInfo.CIRCLE_RADIUS > minx2 && tup.Item1 - GameInfo.CIRCLE_RADIUS < maxx2 && tup.Item2 + GameInfo.CIRCLE_RADIUS > miny && tup.Item2 - GameInfo.CIRCLE_RADIUS < maxy)
                                    {
                                        intersects_if_right = true;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                intersects_if_right = true;
                            }
                            if (!intersects_if_left && (Math.Abs(left_point - setupMaker.rectangleInfo.X) <=  Math.Abs(right_point - setupMaker.rectangleInfo.X) || intersects_if_right))
                            {
                                move.x = (int)left_point / GameInfo.PIXEL_LENGTH;
                            }
                            else if(!intersects_if_right && (Math.Abs(left_point - setupMaker.rectangleInfo.X) > Math.Abs(right_point - setupMaker.rectangleInfo.X) || intersects_if_left))
                            {
                                move.x = (int)right_point / GameInfo.PIXEL_LENGTH;
                            }
                        }
                        setupMaker.actionSelectorCircle.move.distanceToObstacle = -5; // < 0 means it has already been simulated
                        break;
                    }
                }                
            }
            
            if(move.x > setupMaker.currentPlatformRectangle.rightEdge)
            {
                move.x = setupMaker.currentPlatformRectangle.rightEdge - 1;
            }
            else if (move.x < setupMaker.currentPlatformRectangle.leftEdge)
            {
                move.x = setupMaker.currentPlatformRectangle.leftEdge + 1;
            }
            
            hasFinishedReplanning = true;
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
                    while (next.id != -1 && move.x > next.leftEdge)
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
                    while (next.id != 1 && move.x < next.rightEdge)
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
