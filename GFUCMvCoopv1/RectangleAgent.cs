using GeometryFriends;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Communication;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GeometryFriendsAgents
{
    /// <summary>
    /// A rectangle agent implementation for the GeometryFriends game that demonstrates simple random action selection.
    /// </summary>
    public class RectangleAgent : AbstractRectangleAgent
    {
        //agent implementation specificiation
        private bool implementedAgent;
        private string agentName = "UCMAgent";

        //auxiliary variables for agent action
        public Moves currentAction;
        private List<Moves> possibleMoves;
        private long lastMoveTime;
        private Random rnd;
        private int update_counter = 0;
        private int timesStuck = 0;

        //Sensors Information
        private RectangleRepresentation lastRectangleInfo;

        //predictor of actions for the circle
        private ActionSimulator predictor = null;

        private List<AgentMessage> messages;

        //debug agent predictions and history keeping
        private List<CollectibleRepresentation> remaining;

        //Area of the game screen
        protected Rectangle area;

        //Representation of level
        public SetupMaker setupMaker;

        //Execution
        Platform currentPlatformRectangle;
        private bool hasFinishedDrop = true;
        private bool hasFinishedTilt = true;
        private double t = 0;
        private double t_0 = 0;

        //Debug
        private DebugInformation[] debugInfo = null;
        private List<DebugInformation> newDebugInfo;
        private List<CircleRepresentation> trajectory;

        public RectangleAgent()
        {
            //Change flag if agent is not to be used
            implementedAgent = true;

            lastMoveTime = DateTime.Now.Second;
            currentAction = Moves.NO_ACTION;
            rnd = new Random();

            //prepare the possible moves  
            possibleMoves = new List<Moves>();
            possibleMoves.Add(Moves.MOVE_LEFT);
            possibleMoves.Add(Moves.MOVE_RIGHT);
            possibleMoves.Add(Moves.MORPH_UP);
            possibleMoves.Add(Moves.MORPH_DOWN);

            //messages exchange
            messages = new List<AgentMessage>();

            //history keepin
            remaining = new List<CollectibleRepresentation>();

            //Debug
            newDebugInfo = new List<DebugInformation>();
            trajectory = new List<CircleRepresentation>();

        }

        //implements abstract rectangle interface: used to setup the initial information so that the agent has basic knowledge about the level
        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
            setupMaker = new SetupMaker(nI, rI, cI, oI, rPI, cPI, colI);
            setupMaker.rectangleAgent = this;
            setupMaker.SetUp();
            this.area = area;
            messages.Add(new AgentMessage("Rectangle setup completed", setupMaker));
        }

        private void InitialDraw()
        {
            //setupMaker.levelMapRectangle.DrawLevelMap(ref newDebugInfo);
            setupMaker.levelMapRectangle.DrawConnections(ref newDebugInfo);
            //levelMapCircle.DrawConnectionsVertex(ref newDebugInfo);            
            PlanDebug();
        }

        private void UpdateDraw()
        {
            //newDebugInfo.Clear();
            //newDebugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());
            //InitialDraw();
            //RectangleDraw();
            debugInfo = newDebugInfo.ToArray();
        }

        private void PlanDebug()
        {
            int step = 1;
            foreach (MoveInformation m in setupMaker.fullPlanRectangle)
            {
                foreach (Tuple<float, float> tup in m.path)
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 2, GeometryFriends.XNAStub.Color.Red));
                }
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(m.path[m.path.Count / 2].Item1, m.path[m.path.Count / 2].Item2), step.ToString(), GeometryFriends.XNAStub.Color.Black));
                step++;
            }
        }

        private void RectangleDraw()
        {
            /*//Rectangle trajectory
            trajectory.Add(circleInfo);
            for (int i = Math.Max(0, trajectory.Count - 200); i < trajectory.Count; i++)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(trajectory[i].X, trajectory[i].Y), 4, GeometryFriends.XNAStub.Color.Orange));
            }*/

            //Rectangle velocity
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(setupMaker.rectangleInfo.X, setupMaker.rectangleInfo.Y), new PointF(setupMaker.rectangleInfo.X + setupMaker.rectangleInfo.VelocityX, setupMaker.rectangleInfo.Y), GeometryFriends.XNAStub.Color.Red));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(setupMaker.rectangleInfo.X, setupMaker.rectangleInfo.Y), new PointF(setupMaker.rectangleInfo.X, setupMaker.rectangleInfo.Y + setupMaker.rectangleInfo.VelocityY), GeometryFriends.XNAStub.Color.Blue));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(setupMaker.rectangleInfo.X + 20, setupMaker.rectangleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(setupMaker.rectangleInfo.X - 20, setupMaker.rectangleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(setupMaker.circleInfo.X + 40, setupMaker.rectangleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(setupMaker.rectangleInfo.X - 20, setupMaker.rectangleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));

            //Rectangle dimensions
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 100), "Velocidad: " + setupMaker.rectangleInfo.VelocityX, GeometryFriends.XNAStub.Color.Orange));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 150), "Altura: " + setupMaker.rectangleInfo.Height, GeometryFriends.XNAStub.Color.Orange));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 200), "X: " + setupMaker.rectangleInfo.X, GeometryFriends.XNAStub.Color.Orange));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 250), "Y: " + setupMaker.rectangleInfo.Y, GeometryFriends.XNAStub.Color.Orange));
            //newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 200), "Anchura: " + rectangleInfo., GeometryFriends.XNAStub.Color.Orange));

            //newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 150), "Velocidad objetivo: " + actionSelector.target_velocity, GeometryFriends.XNAStub.Color.Orange));
            //newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 200), "Distancia: " + Math.Abs(circleInfo.X / GameInfo.PIXEL_LENGTH - actionSelector.target_position), GeometryFriends.XNAStub.Color.Orange));

            //Platform
            //currentPlatform = levelMapCircle.RectanglePlatform(rectangleInfo);
            //newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(rectangleInfo.X, rectangleInfo.Y), currentPlatform.id.ToString(), GeometryFriends.XNAStub.Color.Black));

            //Current Action
            if (currentAction == Moves.NO_ACTION)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(600, 300), 10, GeometryFriends.XNAStub.Color.Blue));
            }
            else if (currentAction == Moves.MOVE_LEFT)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(550, 300), 10, GeometryFriends.XNAStub.Color.Green));
            }
            else if (currentAction == Moves.MOVE_RIGHT)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(650, 300), 10, GeometryFriends.XNAStub.Color.Purple));
            }
            else if (currentAction == Moves.MORPH_UP)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(600, 250), 10, GeometryFriends.XNAStub.Color.Yellow));
            }
            else if (currentAction == Moves.MORPH_DOWN)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(600, 350), 10, GeometryFriends.XNAStub.Color.Red));
            }
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(setupMaker.rectangleInfo.X, setupMaker.rectangleInfo.Y), setupMaker.levelMapRectangle.RectanglePlatform(setupMaker.rectangleInfo).id.ToString(), GeometryFriends.XNAStub.Color.Black));
            if (setupMaker.actionSelectorRectangle.next_platform == null)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 400), "Next platform: null", GeometryFriends.XNAStub.Color.Orange));
            }
            else
            {
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 400), "Next platform: " + setupMaker.actionSelectorRectangle.next_platform.id.ToString(), GeometryFriends.XNAStub.Color.Orange));
            }
            
            if (setupMaker.actionSelectorRectangle.move == null)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 500), "Next move: null", GeometryFriends.XNAStub.Color.Orange));
            }
            else
            {
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 500), "Next move: -Departure" + setupMaker.actionSelectorRectangle.move.departurePlatform.id.ToString()
                    +" -Tipo" + setupMaker.actionSelectorRectangle.move.moveType, GeometryFriends.XNAStub.Color.Orange));
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(setupMaker.actionSelectorRectangle.move.x*GameInfo.PIXEL_LENGTH, setupMaker.actionSelectorRectangle.move.departurePlatform.yTop * GameInfo.PIXEL_LENGTH), 10, GeometryFriends.XNAStub.Color.Purple));
            }

        }

        //implements abstract rectangle interface: registers updates from the agent's sensors that it is up to date with the latest environment information
        public override void SensorsUpdated(int nC, RectangleRepresentation rI, CircleRepresentation cI, CollectibleRepresentation[] colI)
        {
            setupMaker.nCollectiblesLeft = nC;
            setupMaker.rectangleInfo = rI;
            setupMaker.circleInfo = cI;
            setupMaker.collectiblesInfo = colI;
            lock (remaining)
            {
                remaining = new List<CollectibleRepresentation>(setupMaker.collectiblesInfo);
            }
        }

        //implements abstract circle interface: provides the circle agent with a simulator to make predictions about the future level state
        public override void ActionSimulatorUpdated(ActionSimulator updatedSimulator)
        {
            predictor = updatedSimulator;
        }

        //implements abstract rectangle interface: signals if the agent is actually implemented or not
        public override bool ImplementedAgent()
        {
            return implementedAgent;
        }

        //implements abstract rectangle interface: provides the name of the agent to the agents manager in GeometryFriends
        public override string AgentName()
        {
            return agentName;
        }

        //simple algorithm for choosing a random action for the rectangle agent
        private void RandomAction()
        {
            currentAction = possibleMoves[rnd.Next(possibleMoves.Count)];
        }

        private void WeightRandomAction()
        {
            int a = rnd.Next(6);
            if(a == 0 || a == 1)
            {
                currentAction = Moves.MOVE_LEFT;
            }
            else if (a == 2 || a == 3)
            {
                currentAction = Moves.MOVE_RIGHT;
            }
            else if(a == 4)
            {
                currentAction = Moves.MORPH_UP;
            }
            else
            {
                currentAction = Moves.MORPH_DOWN;
            }
        }

        public static int DiscreetVelocity(float velocity)
        {
            int velocity_step = 10;
            if (velocity >= 0)
            {
                return ((int)((velocity + velocity_step / 2) / velocity_step)) * velocity_step;
            }
            else
            {
                return -DiscreetVelocity(-velocity);
            }
        }

        //implements abstract rectangle interface: GeometryFriends agents manager gets the current action intended to be actuated in the enviroment for this agent
        public override Moves GetAction()
        {
            return currentAction;
        }

        public void Update2(TimeSpan elapsedGameTime)
        {
            UpdateDraw();
            t += elapsedGameTime.TotalMilliseconds;
            if(t < 1000)
            {
                currentAction = Moves.MORPH_DOWN;
                return;
            }
            if(t < 3000)
            {
                currentAction = Moves.MOVE_LEFT;
                return;
            }
            if (setupMaker.levelMapRectangle.RectanglePlatform(setupMaker.rectangleInfo).id != -1 && setupMaker.rectangleInfo.VelocityX < GameInfo.TESTING_VELOCITY)
            {
                currentAction = Moves.MOVE_RIGHT;
            }
            else if(setupMaker.levelMapRectangle.RectanglePlatform(setupMaker.rectangleInfo).id == -1)
            {
                currentAction = Moves.NO_ACTION;
            }
            else
            {
                currentAction = Moves.NO_ACTION;
            }
        }

        //implements abstract rectangle interface: updates the agent state logic and predictions
        public override void Update(TimeSpan elapsedGameTime)
        {
            if(setupMaker.levelMapRectangle == null)
            {
                return;
            }
            if (Math.Abs(setupMaker.rectangleInfo.X - lastRectangleInfo.X) <= 5 && Math.Abs(setupMaker.rectangleInfo.Y - lastRectangleInfo.Y) <= 5)
            {
                timesStuck++;
            }
            else
            {
                lastRectangleInfo = setupMaker.rectangleInfo;
                timesStuck = 0;
            }
            
            UpdateDraw();

            /*t += elapsedGameTime.TotalMilliseconds;
            
            if (t < 100)
            {
                return;
            }
            t = 0;*/

            if (t_0 > 0 || timesStuck > 30)
            {
                t_0 += elapsedGameTime.TotalMilliseconds;
                if (timesStuck > 30 && t_0 > 200)
                {
                    RandomAction();
                    t_0 = 1;
                }
                else if(t_0 > 200)
                {
                    t_0 = 0;
                }
                return;
            }

            if (update_counter != 4 && setupMaker.actionSelectorRectangle.move != null && setupMaker.actionSelectorRectangle.move.moveType == MoveType.BIGHOLEDROP)
            {
                update_counter++;
                return;
            }
            update_counter = 0;

            currentPlatformRectangle = setupMaker.levelMapRectangle.RectanglePlatform(setupMaker.rectangleInfo);
            
            //Become horozintal asap when move=drop
            if (!hasFinishedDrop && setupMaker.planRectangle.Count > 0 && !setupMaker.planRectangle[0].landingPlatform.real)
            {
                if (setupMaker.rectangleInfo.Height < GameInfo.HORIZONTAL_RECTANGLE_HEIGHT + 5)
                {
                    hasFinishedDrop = true;
                }
                else
                {
                    currentAction = setupMaker.levelMapRectangle.RectangleCanMorphDown(setupMaker.rectangleInfo) ? Moves.MORPH_DOWN : Moves.NO_ACTION;
                    return;
                }
            }

            //Mantain action while tilting and check if tilt has finished
            if (setupMaker.actionSelectorRectangle.move != null)
            {
                int edge = setupMaker.actionSelectorRectangle.move.velocityX > 0 ? setupMaker.actionSelectorRectangle.move.landingPlatform.leftEdge : setupMaker.actionSelectorRectangle.move.landingPlatform.rightEdge;
                if (!hasFinishedTilt)
                {
                    if ((setupMaker.actionSelectorRectangle.move.moveType == MoveType.TILT && 
                        Math.Abs(setupMaker.rectangleInfo.X - edge * GameInfo.PIXEL_LENGTH) > 4 * GameInfo.PIXEL_LENGTH) ||
                        (setupMaker.actionSelectorRectangle.move.moveType == MoveType.HIGHTILT &&
                        Math.Abs(setupMaker.rectangleInfo.X - edge * GameInfo.PIXEL_LENGTH) > 10 * GameInfo.PIXEL_LENGTH) ||
                        (setupMaker.actionSelectorRectangle.move.moveType == MoveType.CIRCLETILT &&
                        Math.Abs(setupMaker.rectangleInfo.X - edge * GameInfo.PIXEL_LENGTH) > 16 * GameInfo.PIXEL_LENGTH))
                    {
                        hasFinishedTilt = true;
                        setupMaker.actionSelectorRectangle.tilt_height = 0;
                    }
                    else
                    {
                        return;
                    }
                }
                
                if ((setupMaker.actionSelectorRectangle.move.moveType == MoveType.TILT && Math.Abs(setupMaker.rectangleInfo.X - edge * GameInfo.PIXEL_LENGTH) < 3 * GameInfo.PIXEL_LENGTH ) ||
                    (setupMaker.actionSelectorRectangle.move.moveType == MoveType.HIGHTILT && Math.Sign(setupMaker.rectangleInfo.VelocityX) == Math.Sign(setupMaker.actionSelectorRectangle.move.velocityX)
                    && Math.Abs(setupMaker.rectangleInfo.X - edge * GameInfo.PIXEL_LENGTH) < 7 * GameInfo.PIXEL_LENGTH) ||
                    (setupMaker.actionSelectorRectangle.move.moveType == MoveType.CIRCLETILT && Math.Sign(setupMaker.rectangleInfo.VelocityX) == Math.Sign(setupMaker.actionSelectorRectangle.move.velocityX)
                    && Math.Sign(setupMaker.circleInfo.X - setupMaker.rectangleInfo.X) == Math.Sign(edge * GameInfo.PIXEL_LENGTH - setupMaker.circleInfo.X) &&
                    Math.Abs(setupMaker.rectangleInfo.X - edge * GameInfo.PIXEL_LENGTH) < 15 * GameInfo.PIXEL_LENGTH))
                {
                    if (setupMaker.actionSelectorRectangle.move.velocityX > 0)
                    {
                        currentAction = Moves.MOVE_RIGHT;
                    }
                    else
                    {
                        currentAction = Moves.MOVE_LEFT;
                    }
                    hasFinishedTilt = false;
                    return;
                }
            }

            // Normal behaviour
            if ((setupMaker.actionSelectorRectangle.move != null && ((setupMaker.actionSelectorRectangle.move.moveType == MoveType.FALL &&
                Math.Abs(setupMaker.actionSelectorRectangle.move.x * GameInfo.PIXEL_LENGTH - setupMaker.rectangleInfo.X) <= 2 * GameInfo.PIXEL_LENGTH)
                || setupMaker.actionSelectorRectangle.move.moveType == MoveType.BIGHOLEDROP))
                || !setupMaker.levelMapRectangle.AtBorder(setupMaker.rectangleInfo, currentPlatformRectangle, ref currentAction, setupMaker.planRectangle))
            {
                if (currentPlatformRectangle.id == -1) // Rectangle is in the air
                {
                    if (setupMaker.actionSelectorRectangle.move != null)
                    {
                        // Become horozintal asap when move=drop
                        if (setupMaker.actionSelectorRectangle.move.moveType == MoveType.DROP && setupMaker.levelMapRectangle.RectangleCanMorphDown(setupMaker.rectangleInfo))
                        {
                            currentAction = Moves.MORPH_DOWN;
                            hasFinishedDrop = false;
                        }
                        else if (setupMaker.actionSelectorRectangle.move.moveType == MoveType.MONOSIDEDROP || setupMaker.actionSelectorRectangle.move.moveType == MoveType.BIGHOLEADJ)
                        {
                            if (setupMaker.rectangleInfo.Y / GameInfo.PIXEL_LENGTH < setupMaker.actionSelectorRectangle.move.departurePlatform.yTop)
                            {
                                if (setupMaker.actionSelectorRectangle.move.x > setupMaker.actionSelectorRectangle.move.departurePlatform.rightEdge)
                                {
                                    currentAction = Moves.MOVE_RIGHT;
                                }
                                else
                                {
                                    currentAction = Moves.MOVE_LEFT;
                                }
                            }
                            else if (setupMaker.levelMapRectangle.RectangleCanMorphDown(setupMaker.rectangleInfo))
                            {
                                currentAction = Moves.MORPH_DOWN;
                                hasFinishedDrop = false;
                            }
                            else
                            {
                                currentAction = Moves.NO_ACTION;
                            }
                        }
                        else if (setupMaker.actionSelectorRectangle.move.moveType == MoveType.BIGHOLEDROP)
                        {
                            int distance_x;
                            if (setupMaker.actionSelectorRectangle.move.velocityX > 0)
                            {
                                distance_x = ((int)(setupMaker.rectangleInfo.X / GameInfo.PIXEL_LENGTH)) - setupMaker.actionSelectorRectangle.move.departurePlatform.rightEdge;
                            }
                            else
                            {
                                distance_x = ((int)(setupMaker.rectangleInfo.X / GameInfo.PIXEL_LENGTH)) - setupMaker.actionSelectorRectangle.move.departurePlatform.leftEdge;
                            }
                            // Remember move.velocityX stores the hole's width
                            StateRectangle state = new StateRectangle(distance_x, setupMaker.actionSelectorRectangle.move.departurePlatform.yTop - ((int)(setupMaker.rectangleInfo.Y / GameInfo.PIXEL_LENGTH)),
                                RectangleAgent.DiscreetVelocity(setupMaker.rectangleInfo.VelocityX), (int)(setupMaker.rectangleInfo.Height / (2 * GameInfo.PIXEL_LENGTH)), setupMaker.actionSelectorRectangle.move.velocityX);

                            currentAction = setupMaker.lRectangle.ChooseMove(state, setupMaker.actionSelectorRectangle.move.velocityX);
                        }
                        else if (setupMaker.actionSelectorRectangle.move.moveType == MoveType.FALL)
                        {
                            currentAction = setupMaker.actionSelectorRectangle.move.moveDuringFlight;
                            /*if(actionSelector.move.moveDuringFlight != Moves.NO_ACTION)
                            {
                                currentAction = actionSelector.move.moveDuringFlight;
                            }
                            else
                            {
                                int xmidpoint = (actionSelector.move.landingPlatform.leftEdge + actionSelector.move.landingPlatform.rightEdge) / 2;
                                MoveInformation m_left = new MoveInformation(actionSelector.move);
                                m_left.moveDuringFlight = Moves.MOVE_LEFT;
                                m_left.landingPlatform = new Platform(-1);
                                levelMapCircle.SimulateMove(rectangleInfo.X, rectangleInfo.Y, rectangleInfo.VelocityX, rectangleInfo.VelocityY, ref m_left, RectangleShape.GetShape(rectangleInfo));
                                MoveInformation m_right = new MoveInformation(actionSelector.move);
                                m_right.moveDuringFlight = Moves.MOVE_RIGHT;
                                m_right.landingPlatform = new Platform(-1);
                                levelMapCircle.SimulateMove(rectangleInfo.X, rectangleInfo.Y, rectangleInfo.VelocityX, rectangleInfo.VelocityY, ref m_right, RectangleShape.GetShape(rectangleInfo));
                                currentAction = Moves.NO_ACTION;
                                if (m_left.landingPlatform.id >= 0 &&
                                    levelMapCircle.small_to_simplified[m_left.landingPlatform].id == actionSelector.move.landingPlatform.id)
                                {
                                    if (Math.Abs(m_left.xlandPoint - xmidpoint) < Math.Abs(actionSelector.move.xlandPoint-xmidpoint))
                                    {
                                        currentAction = Moves.MOVE_LEFT;
                                    }
                                }
                                if(m_right.landingPlatform.id >= 0 &&
                                    levelMapCircle.small_to_simplified[m_right.landingPlatform].id == actionSelector.move.landingPlatform.id)
                                {
                                    if ((currentAction == Moves.NO_ACTION &&
                                        Math.Abs(m_right.xlandPoint - xmidpoint) < Math.Abs(actionSelector.move.xlandPoint - xmidpoint))
                                        || (currentAction == Moves.MOVE_LEFT &&
                                        Math.Abs(m_right.xlandPoint - xmidpoint) < Math.Abs(m_left.xlandPoint - xmidpoint)))
                                    {
                                        currentAction = Moves.MOVE_RIGHT;
                                    }
                                }
                            }*/
                        }
                        else
                        {
                            currentAction = Moves.NO_ACTION;
                        }
                    }
                    else
                    {
                        // TODO

                    }
                }
                else
                {
                    // Check if height is really what we are told -> Generates up and down movement in falls
                    if (Math.Abs((setupMaker.rectangleInfo.Height + 2 * setupMaker.rectangleInfo.Y) / GameInfo.PIXEL_LENGTH - currentPlatformRectangle.yTop * 2) > 4)
                    {
                        if (setupMaker.rectangleInfo.Height > GameInfo.SQUARE_HEIGHT)
                        {
                            currentAction = currentAction == Moves.MORPH_UP ? Moves.MORPH_DOWN : Moves.MORPH_UP;
                        }
                        else
                        {
                            currentAction = currentAction == Moves.MORPH_DOWN ? Moves.MORPH_UP : Moves.MORPH_DOWN;
                        }
                        return;
                    }
                    if (setupMaker.levelMapRectangle.HitsCeiling(setupMaker.rectangleInfo,currentPlatformRectangle))
                    {
                        currentAction = Moves.MORPH_DOWN;
                        return;
                    }

                    if (setupMaker.planRectangle.Count == 0 || setupMaker.planRectangle[0].departurePlatform.id != setupMaker.levelMapRectangle.small_to_simplified[currentPlatformRectangle].id) //RECTANGLE IN LAST PLATFORM
                    {
                        // TODO: Add logic with failed move
                        setupMaker.Replanning();
                    }
                    currentAction = setupMaker.actionSelectorRectangle.nextActionPhisics(ref setupMaker.planRectangle, remaining, setupMaker.circleInfo, setupMaker.rectangleInfo, currentPlatformRectangle);
                    setupMaker.actionSelectorRectangle.lastMove = currentAction;
                }
            }
        }

        //implements abstract rectangle interface: signals the agent the end of the current level
        public override void EndGame(int collectiblesCaught, int timeElapsed)
        {
            Log.LogInformation("RECTANGLE - Collectibles caught = " + collectiblesCaught + ", Time elapsed - " + timeElapsed);
        }

        //implememts abstract agent interface: send messages to the circle agent
        public override List<GeometryFriends.AI.Communication.AgentMessage> GetAgentMessages()
        {
            List<AgentMessage> toSent = new List<AgentMessage>(messages);
            messages.Clear();
            return toSent;
        }

        //implements abstract circle interface: gets the debug information that is to be visually represented by the agents manager
        public override DebugInformation[] GetDebugInformation()
        {
            return debugInfo;
        }

        //implememts abstract agent interface: receives messages from the circle agent
        public override void HandleAgentMessages(List<GeometryFriends.AI.Communication.AgentMessage> newMessages)
        {
            foreach (AgentMessage item in newMessages)
            {
                if (item.Attachment != null)
                {
                    if (item.Attachment.GetType() == typeof(Pen))
                    {
                        Log.LogInformation("The attachment is a pen, let's see its color: " + ((Pen)item.Attachment).Color.ToString());
                    }
                }
            }
        }
    }
}