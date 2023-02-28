﻿//A

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
    /// A circle agent implementation for the GeometryFriends game that demonstrates prediction and history keeping capabilities.
    /// </summary>
    public class CircleAgent : AbstractCircleAgent
    {
        //agent implementation specificiation
        private bool implementedAgent;
        private string agentName = "UCMAgent";

        //auxiliary variables for agent action
        private Moves currentAction;
        private List<Moves> possibleMoves;
        private long lastMoveTime;
        private Random rnd;

        //predictor of actions for the circle
        private ActionSimulator predictor = null;

        //private int debugCircleSize = 20;

        //debug agent predictions and history keeping
        private List<CollectibleRepresentation> caughtCollectibles;
        private List<CollectibleRepresentation> uncaughtCollectibles;
        private object remainingInfoLock = new Object();
        private List<CollectibleRepresentation> remaining;

        //Sensors Information and level state
        private CountInformation numbersInfo;
        private RectangleRepresentation rectangleInfo;
        private CircleRepresentation circleInfo;
        private ObstacleRepresentation[] obstaclesInfo;
        private ObstacleRepresentation[] rectanglePlatformsInfo;
        private ObstacleRepresentation[] circlePlatformsInfo;
        private CollectibleRepresentation[] collectiblesInfo;

        private Dictionary<CollectibleRepresentation, int> collectibleId;

        private int nCollectiblesLeft;

        private List<AgentMessage> messages;

        //Area of the game screen
        private Rectangle area;

        //Representation of level
        LevelMap levelMap;

        //Planning
        Graph graph;
        private List<LevelMap.MoveInformation> plan;

        //Execution
        ActionSelector actionSelector;
        LevelMap.MoveInformation currentJump;
        LevelMap.Platform currentPlatform;

        //Debug
        private DebugInformation[] debugInfo = null;
        private List<DebugInformation> newDebugInfo;
        private List<CircleRepresentation> trajectory;
        private List<LevelMap.MoveInformation> fullPlan;
        private Dictionary<int, CircleRepresentation> velocityPoints;
        public static bool random = false;


        private List<TimeSpan> elapsed;


        //Learning
        private Learning l;
        int target_position = GameInfo.LEVEL_MAP_WIDTH / 2;
        int target_velocity = GameInfo.LEARNING_VELOCITY;
        int update_table_counter = 0;
        int reset_counter = 0;
        int update_counter = 0;
        bool jump = false;

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
            //possibleMoves.Add(Moves.JUMP);                

            //history keeping
            uncaughtCollectibles = new List<CollectibleRepresentation>();
            caughtCollectibles = new List<CollectibleRepresentation>();
            remaining = new List<CollectibleRepresentation>();

            //messages exchange
            messages = new List<AgentMessage>();

            levelMap = new LevelMap();

            //Debug
            newDebugInfo = new List<DebugInformation>();
            trajectory = new List<CircleRepresentation>();
            velocityPoints = new Dictionary<int, CircleRepresentation>();
            elapsed = new List<TimeSpan>();
            l = new Learning();
        }

        //implements abstract circle interface: used to setup the initial information so that the agent has basic knowledge about the level
        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
            numbersInfo = nI;
            nCollectiblesLeft = nI.CollectiblesCount;
            rectangleInfo = rI;
            circleInfo = cI;
            obstaclesInfo = oI;
            rectanglePlatformsInfo = rPI;
            circlePlatformsInfo = cPI;
            collectiblesInfo = colI;
            uncaughtCollectibles = new List<CollectibleRepresentation>(collectiblesInfo);
            collectibleId = new Dictionary<CollectibleRepresentation, int>();
            for (int i = 0; i < colI.Length; i++)
            {
                collectibleId[colI[i]] = i;
            }
            actionSelector = new ActionSelector(collectibleId, l);
            this.area = area;
            levelMap.CreateLevelMap(colI, oI, rPI);

            graph = new Graph(levelMap.GetPlatforms(), colI);

            plan = graph.SearchAlgorithm(levelMap.PlatformBelowCircle(cI).id, colI);
            fullPlan = new List<LevelMap.MoveInformation>(plan);
            //InitialDraw();

            //send a message to the rectangle informing that the circle setup is complete and show how to pass an attachment: a pen object
            messages.Add(new AgentMessage("Setup complete, testing to send an object as an attachment.", new Pen(Color.AliceBlue)));

            //DebugSensorsInfo();
        }
        private void InitialDraw()
        {
            levelMap.DrawLevelMap(ref newDebugInfo);
            levelMap.DrawConnections(ref newDebugInfo);
            PlanDebug();
        }

        private void UpdateDraw()
        {
            newDebugInfo.Clear();
            newDebugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());
            //InitialDraw();
            CircleDraw();
            debugInfo = newDebugInfo.ToArray();
        }

        private void PlanDebug()
        {
            int step = 1;
            foreach (LevelMap.MoveInformation m in fullPlan)
            {
                foreach (Tuple<float, float> tup in m.path)
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(tup.Item1, tup.Item2), 2, GeometryFriends.XNAStub.Color.Red));
                }
                newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(m.path[m.path.Count / 2].Item1, m.path[m.path.Count / 2].Item2), step.ToString(), GeometryFriends.XNAStub.Color.Black));
                step++;
            }
        }

        private void CircleDraw()
        {
            /*//Circle Silhouette
            int[] CIRCLE_SIZE = new int[] { 3, 4, 5, 5, 5, 5, 5, 5, 4, 3 };//Divided by 2
            for (int i = -GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i < GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH; i++)
            {
                for (int j = -CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j < CIRCLE_SIZE[i + GameInfo.CIRCLE_RADIUS / GameInfo.PIXEL_LENGTH]; j++)
                {
                    int x = (int)(circleInfo.X / GameInfo.PIXEL_LENGTH);
                    int y = (int)(circleInfo.Y / GameInfo.PIXEL_LENGTH);
                    DebugInformation di = DebugInformationFactory.CreateRectangleDebugInfo(new PointF((x + i) * GameInfo.PIXEL_LENGTH, (y + j) * GameInfo.PIXEL_LENGTH), new Size(GameInfo.PIXEL_LENGTH, GameInfo.PIXEL_LENGTH), GeometryFriends.XNAStub.Color.YellowGreen);
                    newDebugInfo.Add(di);
                }
            }

            //Circle trajectory
            trajectory.Add(circleInfo);
            for (int i = Math.Max(0, trajectory.Count - 200); i < trajectory.Count; i++)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(trajectory[i].X, trajectory[i].Y), 4, GeometryFriends.XNAStub.Color.Orange));
            }
           */
            //Circle velocity
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(circleInfo.X, circleInfo.Y), new PointF(circleInfo.X + circleInfo.VelocityX, circleInfo.Y), GeometryFriends.XNAStub.Color.Red));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(circleInfo.X, circleInfo.Y), new PointF(circleInfo.X, circleInfo.Y + circleInfo.VelocityY), GeometryFriends.XNAStub.Color.Blue));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X + 20, circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X - 20, circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X + 40, circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X - 20, circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));


            //Circle velocity
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(target_position * GameInfo.PIXEL_LENGTH, circleInfo.Y), 5, GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF((target_position + GameInfo.MAX_DISTANCE) * GameInfo.PIXEL_LENGTH, circleInfo.Y), 5, GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF((target_position - GameInfo.MAX_DISTANCE) * GameInfo.PIXEL_LENGTH, circleInfo.Y), 5, GeometryFriends.XNAStub.Color.Black));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 100), "Velocidad: " + circleInfo.VelocityX, GeometryFriends.XNAStub.Color.Orange));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 150), "Velocidad objetivo: " + target_velocity, GeometryFriends.XNAStub.Color.Orange));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 200), "Velocidad relativa: " + Math.Abs(circleInfo.VelocityX - target_velocity), GeometryFriends.XNAStub.Color.Orange));
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 250), "Distancia: " + Math.Abs(circleInfo.X / GameInfo.PIXEL_LENGTH - target_position), GeometryFriends.XNAStub.Color.Orange));

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
            VisualDebug.DrawParabola(ref newDebugInfo, target_position * GameInfo.PIXEL_LENGTH, 700, target_velocity, GameInfo.JUMP_VELOCITYY, GeometryFriends.XNAStub.Color.Purple);

            if (random)
            {
                newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(900, 250), 20, GeometryFriends.XNAStub.Color.Orange));
            }
            /*
            for(int i =-200; i <= 200; i += 20)
            {
                if (Math.Abs(circleInfo.VelocityX - i) <= 5 && !velocityPoints.ContainsKey(i))
                {
                    velocityPoints[i]=circleInfo;
                }
            }
            foreach(var ci in velocityPoints)
            {
                
                 newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(ci.Value.X, ci.Value.Y), 5, GeometryFriends.XNAStub.Color.Green));
                 newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(100, ci.Value.Y - ci.Key-50), "X=" + ci.Value.X + " VX= " + ci.Value.VelocityX, GeometryFriends.XNAStub.Color.Red));
                
            }

            */
        }

        //implements abstract circle interface: registers updates from the agent's sensors that it is up to date with the latest environment information
        /*WARNING: this method is called independently from the agent update - Update(TimeSpan elapsedGameTime) - so care should be taken when using complex 
         * structures that are modified in both (e.g. see operation on the "remaining" collection)      
         */
        public override void SensorsUpdated(int nC, RectangleRepresentation rI, CircleRepresentation cI, CollectibleRepresentation[] colI)
        {
            nCollectiblesLeft = nC;

            rectangleInfo = rI;
            circleInfo = cI;
            collectiblesInfo = colI;
            lock (remaining)
            {
                remaining = new List<CollectibleRepresentation>(collectiblesInfo);
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

        public static int DiscreetVelocity(float velocity)
        {
            if (velocity >= 0)
            {
                return ((int)((velocity + 10) / 20)) * 20;
            }
            else
            {
                return -DiscreetVelocity(-velocity);
            }
        }

        //implements abstract circle interface: updates the agent state logic and predictions
        public override void Update(TimeSpan elapsedGameTime)
        {
            if (update_table_counter != GameInfo.UPDATE_TABLE_MS / (10 * GameInfo.SIMULATION_SPEED) - 1)
            {
                update_table_counter++;
            }
            else if (target_velocity != 0)
            {
                State s = new State(((int)(circleInfo.X / GameInfo.PIXEL_LENGTH)) - target_position, CircleAgent.DiscreetVelocity(circleInfo.VelocityX), target_velocity);
                l.UpdateTable(s);
                l.SaveFile();
                update_table_counter = 0;
            }
            if (update_counter != GameInfo.UPDATE_FRECUENCY_MS / (10 * GameInfo.SIMULATION_SPEED) - 1)
            {
                update_counter++;
                return;
            }

            update_counter = 0;

            UpdateDraw();

            if (!jump)
            {
                State s = new State(((int)(circleInfo.X / GameInfo.PIXEL_LENGTH)) - target_position, CircleAgent.DiscreetVelocity(circleInfo.VelocityX), target_velocity);
                if (s.IsFinal())
                {
                    l.UpdateTable(s);
                    l.SaveFile();
                    if (target_velocity == 0)
                    {
                        target_velocity = GameInfo.LEARNING_VELOCITY;
                        target_position = GameInfo.LEVEL_MAP_WIDTH / 2;
                    }
                    else{
                        currentAction = Moves.JUMP;
                        jump = true;
                        reset_counter = 0;
                    }
                }
                else 
                {
                    currentAction = l.ChooseMove(s, ((int)(circleInfo.X / GameInfo.PIXEL_LENGTH)) - target_position);
                } 
            }
            else
            { 
                if (reset_counter < GameInfo.TIMEJUMPING_MS / GameInfo.UPDATE_FRECUENCY_MS)
                {
                    currentAction = Moves.JUMP;
                }
                else
                {
                    jump = false; 
                    if(target_velocity == 0) {
                        target_velocity = GameInfo.LEARNING_VELOCITY;
                        target_position = GameInfo.LEVEL_MAP_WIDTH / 2;
                    }
                    else {
                        target_velocity = 0;
                        target_position = 16 + rnd.Next(GameInfo.LEVEL_MAP_WIDTH - 32);
                    }
                }
                reset_counter++;
            }

            /*
            t_0 += elapsedGameTime.TotalMilliseconds;
            t += elapsedGameTime.TotalMilliseconds;
            if (t < 100)
            {
                return;
            }

            currentPlatform = levelMap.CirclePlatform(circleInfo);

            if (currentPlatform.id == -1) // Ball is in the air
            {
                if (circleInfo.VelocityX > 0)
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
                if (plan[0].departurePlatform != currentPlatform)
                {

                    plan = graph.SearchAlgorithm(levelMap.PlatformBelowCircle(circleInfo).id,collectiblesInfo);
                    fullPlan = new List<LevelMap.MoveInformation>(plan);
                }

                Tuple<Moves, bool> tup = actionSelector.nextAction(ref plan,remaining,circleInfo,currentPlatform);
                currentAction = tup.Item1;
                if (tup.Item2)
                {
                    t = 0;
                }

            }*/
        }

        //typically used console debugging used in previous implementations of GeometryFriends
        protected void DebugSensorsInfo()
        {
            Log.LogInformation("Circle Agent - " + numbersInfo.ToString());

            Log.LogInformation("Circle Agent - " + rectangleInfo.ToString());

            Log.LogInformation("Circle Agent - " + circleInfo.ToString());

            foreach (ObstacleRepresentation i in obstaclesInfo)
            {
                Log.LogInformation("Circle Agent - " + i.ToString("Obstacle"));
            }

            foreach (ObstacleRepresentation i in rectanglePlatformsInfo)
            {
                Log.LogInformation("Circle Agent - " + i.ToString("Rectangle Platform"));
            }

            foreach (ObstacleRepresentation i in circlePlatformsInfo)
            {
                Log.LogInformation("Circle Agent - " + i.ToString("Circle Platform"));
            }

            foreach (CollectibleRepresentation i in collectiblesInfo)
            {
                Log.LogInformation("Circle Agent - " + i.ToString());
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
        public override List<GeometryFriends.AI.Communication.AgentMessage> GetAgentMessages()
        {
            List<AgentMessage> toSent = new List<AgentMessage>(messages);
            messages.Clear();
            return toSent;
        }

        //implememts abstract agent interface: receives messages from the rectangle agent
        public override void HandleAgentMessages(List<GeometryFriends.AI.Communication.AgentMessage> newMessages)
        {
            foreach (AgentMessage item in newMessages)
            {
                Log.LogInformation("Circle: received message from rectangle: " + item.Message);
                if (item.Attachment != null)
                {
                    Log.LogInformation("Received message has attachment: " + item.Attachment.ToString());
                    if (item.Attachment.GetType() == typeof(Pen))
                    {
                        Log.LogInformation("The attachment is a pen, let's see its color: " + ((Pen)item.Attachment).Color.ToString());
                    }
                }
            }
        }
    }
}
