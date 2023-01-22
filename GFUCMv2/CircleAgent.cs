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

        //Learning

        private int target_velocity=0;
        private int target_position=GameInfo.LEVEL_MAP_WIDTH/2;
        private double t = 0;
        private double t_0 = 0;
        private Learning l; 

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
            for (int i=0; i < colI.Length; i++)
            {
                collectibleId[colI[i]] = i;
            }
            actionSelector = new ActionSelector(collectibleId);
            this.area = area;
            levelMap.CreateLevelMap(colI,oI, rPI);

            graph = new Graph(levelMap.GetPlatforms(), colI);
            
            plan = graph.SearchAlgorithm(levelMap.PlatformBelowCircle(cI).id);
            fullPlan = new List<LevelMap.MoveInformation>(plan);
            //InitialDraw();

            //send a message to the rectangle informing that the circle setup is complete and show how to pass an attachment: a pen object
            messages.Add(new AgentMessage("Setup complete, testing to send an object as an attachment.", new Pen(Color.AliceBlue)));
            
            //DebugSensorsInfo();
        }
        private void InitialDraw()
        {
            newDebugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());
            levelMap.DrawLevelMap(ref newDebugInfo);
            levelMap.DrawConnections(ref newDebugInfo);
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
            //Circle Silhouette
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
           
            //Circle velocity
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(circleInfo.X, circleInfo.Y), new PointF(circleInfo.X + circleInfo.VelocityX, circleInfo.Y), GeometryFriends.XNAStub.Color.Red));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(circleInfo.X, circleInfo.Y), new PointF(circleInfo.X, circleInfo.Y + circleInfo.VelocityY), GeometryFriends.XNAStub.Color.Blue));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X + 20, circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X - 20, circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X + 40, circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X - 20, circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            State s =new State(((int)(circleInfo.X / GameInfo.PIXEL_LENGTH)) - target_position, DiscreetVelocity(circleInfo.VelocityX), 0);
            if (s.Reward()>0)
            {
                if (s.IsFinal())
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(500, 500), 2, GeometryFriends.XNAStub.Color.Green));
                }
                else
                {
                    newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(500, 500), 2, GeometryFriends.XNAStub.Color.Yellow));
                }
            }
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(target_position*GameInfo.PIXEL_LENGTH, circleInfo.Y), 2, GeometryFriends.XNAStub.Color.Orange));
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
        
        private int DiscreetVelocity(float velocity)
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
            /*t += elapsedGameTime.TotalSeconds;
            t_0+= elapsedGameTime.TotalMilliseconds;
            UpdateDraw();
            if (t > 1)
            {
                State s = new State(((int)(circleInfo.X / GameInfo.PIXEL_LENGTH)) - target_position, DiscreetVelocity(circleInfo.VelocityX), 0);

                if (s.IsFinal())
                {
                    l.UpdateTable(s);
                    l.SaveFile();
                    t = 0;
                    currentAction = Moves.JUMP;
                    target_position = 25 + rnd.Next(GameInfo.LEVEL_MAP_WIDTH - 50);
                }
                else
                {
                    if (t_0 < 100)
                    {
                        return;
                    }
                    t_0 = 0;

                    currentAction = l.ChooseMove(s, ((int)(circleInfo.X / GameInfo.PIXEL_LENGTH)) - target_position);
                }
                if (t > 10)
                {
                    l.UpdateTable(s);
                    l.SaveFile();
                    t = 0;
                }
            }

            */
            //Every second one new action is choosen
            /*if (lastMoveTime == 60)
                lastMoveTime = 0;

            if ((lastMoveTime) <= (DateTime.Now.Second) && (lastMoveTime < 60))
            {
                if (!(DateTime.Now.Second == 59))
                {
                    RandomAction();
                    lastMoveTime = lastMoveTime + 1;
                    //DebugSensorsInfo();                    
                }
                else
                    lastMoveTime = 60;
            }
            */



            //Previusly worked
            UpdateDraw();
            currentPlatform = levelMap.CirclePlatform(circleInfo);
            if (currentPlatform == null || currentPlatform.id != 2)
            {
                currentPlatform = levelMap.CirclePlatform(circleInfo);
            }
            if (currentPlatform.id == -1)
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
                
                Tuple<Moves,bool> tup = actionSelector.nextAction(plan, remaining, circleInfo, currentPlatform);
                currentAction = tup.Item1;
                if (currentAction == Moves.JUMP && tup.Item2)
                {

                    currentJump = plan[0];
                    plan.RemoveAt(0);
                    //Log.LogInformation("Plan pop", true);
                }
                if (currentAction == Moves.ROLL_RIGHT && circleInfo.X<200)
                {
                    int z = 0;
                }
             
                
            }
            
            /*
            //check if any collectible was caught
            lock (remaining)
            {
                if (remaining.Count > 0)
                {
                    List<CollectibleRepresentation> toRemove = new List<CollectibleRepresentation>();
                    foreach (CollectibleRepresentation item in uncaughtCollectibles)
                    {
                        if (!remaining.Contains(item))
                        {
                            caughtCollectibles.Add(item);
                            toRemove.Add(item);
                        }
                    }
                    foreach (CollectibleRepresentation item in toRemove)
                    {
                        uncaughtCollectibles.Remove(item);
                    }
                }
            }
            */


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
            l.SaveFile();
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

