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
        private Moves currentAction;
        private List<Moves> possibleMoves;
        private long lastMoveTime;
        private Random rnd;
        //predictor of actions for the circle

        private ActionSimulator predictor = null;
        //Sensors Information

        private CountInformation numbersInfo;
        private RectangleRepresentation rectangleInfo;
        private CircleRepresentation circleInfo;
        private ObstacleRepresentation[] obstaclesInfo;
        private ObstacleRepresentation[] rectanglePlatformsInfo;
        private ObstacleRepresentation[] circlePlatformsInfo;
        private CollectibleRepresentation[] collectiblesInfo;

        private int nCollectiblesLeft;

        private List<AgentMessage> messages;

        //Area of the game screen
        protected Rectangle area;

        private Dictionary<CollectibleRepresentation, int> collectibleId;

        //Representation of level
        LevelMapRectangle levelMap;

        //Planning
        Graph graph;
        private List<MoveInformation> plan;

        //Execution
        ActionSelector actionSelector;
        Platform currentPlatform;
        bool flag = false;

        //Debug
        private DebugInformation[] debugInfo = null;
        private List<DebugInformation> newDebugInfo;
        private List<CircleRepresentation> trajectory;
        private List<MoveInformation> fullPlan;

        //Learning
        private double t = 0;
        private double t_0 = 0;
        private Learning l;

        //debug agent predictions and history keeping
        private List<CollectibleRepresentation> remaining;

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
            
            levelMap = new LevelMapRectangle();

            //Debug
            newDebugInfo = new List<DebugInformation>();
            trajectory = new List<CircleRepresentation>();

            l = new Learning();
        }

        //implements abstract rectangle interface: used to setup the initial information so that the agent has basic knowledge about the level
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
            this.area = area;
            collectibleId = new Dictionary<CollectibleRepresentation, int>();

            
            for (int i = 0; i < colI.Length; i++)
            {
                collectibleId[colI[i]] = i;
            }

            levelMap.CreateLevelMap(colI, oI, cPI);

            graph = new Graph(levelMap.simplified_platforms, colI);

            plan = graph.SearchAlgorithm(levelMap.PlatformBelowRectangle(rI).id, colI, null);
            fullPlan = new List<MoveInformation>(plan);

            //actionSelector = new ActionSelector(collectibleId, l, levelMap, graph);

            //InitialDraw();

            //send a message to the rectangle informing that the circle setup is complete and show how to pass an attachment: a pen object
            //messages.Add(new AgentMessage("Setup complete, testing to send an object as an attachment.", new Pen(Color.AliceBlue)));
            
        }

        private void InitialDraw()
        {
            levelMap.DrawLevelMap(ref newDebugInfo);
            levelMap.DrawConnections(ref newDebugInfo);
            levelMap.DrawConnectionsVertex(ref newDebugInfo);
            PlanDebug();
        }

        private void UpdateDraw()
        {
            newDebugInfo.Clear();
            newDebugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());
            InitialDraw();
            RectangleDraw();
            debugInfo = newDebugInfo.ToArray();
        }

        private void PlanDebug()
        {
            int step = 1;
            foreach (MoveInformation m in fullPlan)
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
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(rectangleInfo.X, rectangleInfo.Y), new PointF(rectangleInfo.X + rectangleInfo.VelocityX, rectangleInfo.Y), GeometryFriends.XNAStub.Color.Red));
            newDebugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(new PointF(rectangleInfo.X, rectangleInfo.Y), new PointF(rectangleInfo.X, rectangleInfo.Y + rectangleInfo.VelocityY), GeometryFriends.XNAStub.Color.Blue));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(rectangleInfo.X + 20, rectangleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(rectangleInfo.X - 20, rectangleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(circleInfo.X + 40, rectangleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));
            newDebugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(rectangleInfo.X - 20, rectangleInfo.Y), 2, GeometryFriends.XNAStub.Color.Silver));

            //Rectangle velocity
            
            newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 100), "Velocidad: " + rectangleInfo.VelocityX, GeometryFriends.XNAStub.Color.Orange));
            //newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 150), "Velocidad objetivo: " + actionSelector.target_velocity, GeometryFriends.XNAStub.Color.Orange));
            //newDebugInfo.Add(DebugInformationFactory.CreateTextDebugInfo(new PointF(600, 200), "Distancia: " + Math.Abs(circleInfo.X / GameInfo.PIXEL_LENGTH - actionSelector.target_position), GeometryFriends.XNAStub.Color.Orange));

            //Platform
            //currentPlatform = levelMap.RectanglePlatform(rectangleInfo);
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
        }

        //implements abstract rectangle interface: registers updates from the agent's sensors that it is up to date with the latest environment information
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
            /*
             Rectangle Actions
             MOVE_LEFT = 5
             MOVE_RIGHT = 6
             MORPH_UP = 7
             MORPH_DOWN = 8
            */

            currentAction = possibleMoves[rnd.Next(possibleMoves.Count)];

            //send a message to the circle agent telling what action it chose
            messages.Add(new AgentMessage("Going to :" + currentAction));
        }

        //implements abstract rectangle interface: GeometryFriends agents manager gets the current action intended to be actuated in the enviroment for this agent
        public override Moves GetAction()
        {
            return currentAction;
        }

        //implements abstract rectangle interface: updates the agent state logic and predictions
        public override void Update(TimeSpan elapsedGameTime)
        {
            t += elapsedGameTime.TotalMilliseconds;

            if(t < 1000)
            {
                currentAction = Moves.NO_ACTION;
            }
            else if (t <= 2500)
            {
                currentAction = Moves.MORPH_UP;
            }
            else
            {
                UpdateDraw();
                if (GameInfo.TESTING_VELOCITY > 0)
                {
                    if (rectangleInfo.VelocityX > GameInfo.TESTING_VELOCITY)
                    {
                        currentAction = Moves.NO_ACTION;
                    }
                    else
                    {
                        currentAction = Moves.MOVE_RIGHT;
                    }
                }
                else
                {
                    if (rectangleInfo.VelocityX < GameInfo.TESTING_VELOCITY)
                    {
                        currentAction = Moves.NO_ACTION;
                    }
                    else
                    {
                        currentAction = Moves.MOVE_LEFT;
                    }
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
                Log.LogInformation("Rectangle: received message from circle: " + item.Message);
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