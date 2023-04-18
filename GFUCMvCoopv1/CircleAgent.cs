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
using System.IO;

namespace GeometryFriendsAgents
{
    /// <summary>
    /// A circle agent implementation for the GeometryFriends game that demonstrates prediction and history keeping capabilities.
    /// </summary>
    public class CircleAgent : AbstractCircleAgent
    {
        //agent implementation specificiation
        private bool implementedAgent;
        private string agentName = "UCMAgent";

        //auxiliary variables for agent action
        public Moves currentAction;
        private List<Moves> possibleMoves;
        private long lastMoveTime;
        private Random rnd;

        //predictor of actions for the circle
        private ActionSimulator predictor = null;
        
        private List<AgentMessage> messages;

        //debug agent predictions and history keeping
        private List<CollectibleRepresentation> remaining;

        //Area of the game screen
        private Rectangle area;

        //Representation of level
        public SetupMaker setupMaker;

        //Execution
        Platform currentPlatformCircle;
        bool flag = false;
        private double t = 0;
        private double t_0 = 0;

        //Debug
        private DebugInformation[] debugInfo = null;
        private List<DebugInformation> newDebugInfo;
        private List<CircleRepresentation> trajectory;

        public CircleAgent()
        {
            //Change flag if agent is not to be used
            implementedAgent = true;

            //setup for action updates
            lastMoveTime = DateTime.Now.Second;
            currentAction = Moves.NO_ACTION;
            rnd = new Random();

            //prepare the possible moves  
            possibleMoves = new List<Moves>();
            possibleMoves.Add(Moves.ROLL_LEFT);
            possibleMoves.Add(Moves.ROLL_RIGHT);
            possibleMoves.Add(Moves.JUMP);
      
            //history keeping
            remaining = new List<CollectibleRepresentation>();

            //messages exchange
            messages = new List<AgentMessage>();

            //Debug
            newDebugInfo = new List<DebugInformation>();
            trajectory = new List<CircleRepresentation>();
        }

        //implements abstract circle interface: used to setup the initial information so that the agent has basic knowledge about the level
        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
            
        }

        private void InitialDraw()
        {
            newDebugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());
            setupMaker.levelMapCircle.DrawLevelMap(ref newDebugInfo);
            setupMaker.levelMapCircle.DrawConnections(ref newDebugInfo);
            //levelMapCircle.DrawConnectionsVertex(ref newDebugInfo);
            PlanDebug();
        }

        private void UpdateDraw()
        {
            newDebugInfo.Clear();
            newDebugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());
            InitialDraw();
            CircleDraw();
            debugInfo = newDebugInfo.ToArray();
        }

        private void PlanDebug()
        {
            int step = 1;
            foreach (MoveInformation m in setupMaker.fullPlanCircle)
            {
                foreach (Tuple<float, float> tup in m.path)
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 2, GeometryFriends.XNAStub.Color.Red));
                }
                if (m.path.Count > 0)
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(m.path[m.path.Count / 2].Item1, m.path[m.path.Count / 2].Item2), step.ToString(), GeometryFriends.XNAStub.Color.Black));
                }
                step++;
            }
        }

        private void CircleDraw()
        {
            //Circle Silhouette
            int[] CIRCLE_SIZE = new int[] { 3, 4, 5, 5, 5, 5, 5, 5, 4, 3 };//Divided by 2
            for (int i = -GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i < GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i++)
            {
                for (int j = -CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j < CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j++)
                {
                    int x = (int)(setupMaker.circleInfo.X / GameInfo.PIXEL_LENGTH);
                    int y = (int)(setupMaker.circleInfo.Y / GameInfo.PIXEL_LENGTH);
                    DebugInformation di = DebugInformationFactory.CreateRectangleDebugInfo(new PointF((x + i) * GameInfo.PIXEL_LENGTH, (y + j) * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.YellowGreen);
                    newDebugInfo.Add(di);
                }
            }

            //Circle trajectory
            trajectory.Add(setupMaker.circleInfo);
            for (int i = Math.Max(0, trajectory.Count - 200); i < trajectory.Count; i++)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(trajectory[i].X, trajectory[i].Y), 4, GeometryFriends.XNAStub.Color.Orange));
            }

            //Circle velocity
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(setupMaker.circleInfo.X, setupMaker.circleInfo.Y), new PointF(  setupMaker.circleInfo.X + setupMaker.circleInfo.VelocityX, setupMaker.circleInfo.Y), GeometryFriends.XNAStub.Color.Red));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(setupMaker.circleInfo.X, setupMaker.circleInfo.Y), new PointF(  setupMaker.circleInfo.X, setupMaker.circleInfo.Y + setupMaker.circleInfo.VelocityY), GeometryFriends.XNAStub.Color.Blue));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(setupMaker.circleInfo.X + 20, setupMaker.circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(setupMaker.circleInfo.X - 20, setupMaker.circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(setupMaker.circleInfo.X + 40, setupMaker.circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(setupMaker.circleInfo.X - 20, setupMaker.circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            
            //Circle velocity
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(setupMaker.actionSelectorCircle.target_position * GameInfo.PIXEL_LENGTH, setupMaker.circleInfo.Y), 5, GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 100), "Velocidad: " + setupMaker.circleInfo.VelocityX, GeometryFriends.XNAStub.Color.Orange));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 150), "Velocidad objetivo: " + setupMaker.actionSelectorCircle.target_velocity, GeometryFriends.XNAStub.Color.Orange));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 200), "Distancia: " + Math.Abs(setupMaker.circleInfo.X/ GameInfo.PIXEL_LENGTH - setupMaker.actionSelectorCircle.target_position), GeometryFriends.XNAStub.Color.Orange));

            if (setupMaker.actionSelectorCircle.target_velocity > 0)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(setupMaker.actionSelectorCircle.target_position * GameInfo.PIXEL_LENGTH- setupMaker.actionSelectorCircle.acceleration_distance, setupMaker.circleInfo.Y), 5, GeometryFriends.XNAStub.Color.Pink));
            }
            else
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(setupMaker.actionSelectorCircle.target_position * GameInfo.PIXEL_LENGTH + setupMaker.actionSelectorCircle.acceleration_distance, setupMaker.circleInfo.Y), 5, GeometryFriends.XNAStub.Color.Pink));
            }
            if (setupMaker.circleInfo.VelocityX > 0)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(setupMaker.circleInfo.X + setupMaker.actionSelectorCircle.brake_distance, setupMaker.circleInfo.Y), 5, GeometryFriends.XNAStub.Color.Gray));
            }
            else {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(setupMaker.circleInfo.X - setupMaker.actionSelectorCircle.brake_distance, setupMaker.circleInfo.Y), 5, GeometryFriends.XNAStub.Color.Gray));
            }

            //Platform
            setupMaker.currentPlatformCircle = setupMaker.levelMapCircle.CirclePlatform(setupMaker.circleInfo);
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(setupMaker.circleInfo.X, setupMaker.circleInfo.Y), setupMaker.currentPlatformCircle.id.ToString(), GeometryFriends.XNAStub.Color.Black));

            //Current Action
            if (currentAction == Moves.NO_ACTION)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(600, 300), 10, GeometryFriends.XNAStub.Color.Blue));
            }
            else if (currentAction == Moves.ROLL_LEFT)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(550, 300), 10, GeometryFriends.XNAStub.Color.Green));
            }
            else if (currentAction == Moves.ROLL_RIGHT)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(650, 300), 10, GeometryFriends.XNAStub.Color.Purple));
            }
            else if (currentAction == Moves.JUMP)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(600, 250), 10, GeometryFriends.XNAStub.Color.Yellow));
            }
        }

        //implements abstract circle interface: registers updates from the agent's sensors that it is up to date with the latest environment information
        /*WARNING: this method is called independently from the agent update - Update(TimeSpan elapsedGameTime) - so care should be taken when using complex 
         * structures that are modified in both (e.g. see operation on the "remaining" collection)      
         */
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

            //DebugSensorsInfo();
        }

        //implements abstract circle interface: provides the circle agent with a simulator to make predictions about the future level state
        public override void ActionSimulatorUpdated(ActionSimulator updatedSimulator)
        {
            predictor = updatedSimulator;
        }

        //implements abstract circle interface: signals if the agent is actually implemented or not
        public override bool ImplementedAgent()
        {
            return implementedAgent;
        }

        //implements abstract circle interface: provides the name of the agent to the agents manager in GeometryFriends
        public override string AgentName()
        {
            return agentName;
        }

        //simple algorithm for choosing a random action for the circle agent
        private void RandomAction()
        {
            /*
             Circle Actions
             ROLL_LEFT = 1      
             ROLL_RIGHT = 2
             JUMP = 3
             GROW = 4
            */
            currentAction = possibleMoves[rnd.Next(possibleMoves.Count)];
            
            //send a message to the rectangle agent telling what action it chose
            messages.Add(new AgentMessage("Going to :" + currentAction));
        }

        //implements abstract circle interface: GeometryFriends agents manager gets the current action intended to be actuated in the enviroment for this agent
        public override Moves GetAction()
        {
            return currentAction;
        }
        
        public static int DiscreetVelocity(float velocity, int velocity_step)
        {
            if (velocity >= 0)
            {
                return ((int)((velocity + velocity_step / 2) / velocity_step)) * velocity_step;
            }
            else
            {
                return -DiscreetVelocity(-velocity, velocity_step);
            }
        }
        
        //implements abstract circle interface: updates the agent state logic and predictions
        public override void Update(TimeSpan elapsedGameTime)
        {
            if (setupMaker == null)
            {
                return;
            }
            UpdateDraw();
            t_0 += elapsedGameTime.TotalMilliseconds;
            t += elapsedGameTime.TotalMilliseconds;
            if (t < 100)
            {
                return;
            }
            currentPlatformCircle = setupMaker.levelMapCircle.CirclePlatform(setupMaker.circleInfo);
            if (!setupMaker.levelMapCircle.AtBorder(setupMaker.circleInfo, currentPlatformCircle, ref currentAction, setupMaker.planCircle))
            {
                if (currentPlatformCircle.id == -1) // Ball is in the air
                {
                    if (!flag)
                    {
                        if (setupMaker.circleInfo.VelocityX > 0)
                        {
                            currentAction = Moves.ROLL_LEFT;
                        }
                        else
                        {
                            currentAction = Moves.ROLL_RIGHT;
                        }
                    }
                    else
                    {
                        if (setupMaker.circleInfo.VelocityX > 0)
                        {
                            currentAction = Moves.ROLL_RIGHT;
                        }
                        else
                        {
                            currentAction = Moves.ROLL_LEFT;
                        }
                    }
                }
                else
                {
                    if (setupMaker.planCircle.Count == 0 || setupMaker.planCircle[0].departurePlatform.id != setupMaker.levelMapCircle.small_to_simplified[currentPlatformCircle].id) //CIRCLE IN LAST PLATFORM
                    {
                        // TODO: Add logic with failed move
                        setupMaker.Replanning();
                    }
                    Tuple<Moves, Tuple<bool, bool>> tup;
                    if (GameInfo.PHYSICS)
                    {
                        tup = setupMaker.actionSelectorCircle.nextActionPhisics(ref setupMaker.planCircle, remaining, setupMaker.circleInfo, currentPlatformCircle);
                    }
                    else
                    {
                        tup = setupMaker.actionSelectorCircle.nextActionQTable(ref setupMaker.planCircle, remaining, setupMaker.circleInfo, currentPlatformCircle);
                    }
                    currentAction = tup.Item1;
                    if (tup.Item2.Item1)
                    {
                        t = 0;
                    }
                    flag = tup.Item2.Item2;
                }
            }
        }

        //implements abstract circle interface: signals the agent the end of the current level
        public override void EndGame(int collectiblesCaught, int timeElapsed)
        {
            Log.LogInformation("CIRCLE - Collectibles caught = " + collectiblesCaught + ", Time elapsed - " + timeElapsed);
        }

        //implements abstract circle interface: gets the debug information that is to be visually represented by the agents manager
        public override DebugInformation[] GetDebugInformation()
        {
            return debugInfo;
        }

        //implememts abstract agent interface: send messages to the rectangle agent
        public override List<AgentMessage> GetAgentMessages()
        {
            List<AgentMessage> toSent = new List<AgentMessage>(messages);
            messages.Clear();
            return toSent;
        }

        //implememts abstract agent interface: receives messages from the rectangle agent
        public override void HandleAgentMessages(List<AgentMessage> newMessages)
        {
            foreach (AgentMessage item in newMessages)
            {
                if (item.Attachment != null)
                {
                    if (item.Message.CompareTo("Rectangle setup completed") == 0)
                    {
                        setupMaker = (SetupMaker) item.Attachment;
                    }
                }
            }
        }
    }
}

