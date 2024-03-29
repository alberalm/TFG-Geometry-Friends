﻿using GeometryFriends;
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
using System.Runtime.Remoting;

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
        public List<CollectibleRepresentation> remaining;

        //Representation of level
        public SetupMaker setupMaker;

        //Execution
        public Platform currentPlatformCircle;
        bool flag = false;
        private double t = 0;
        private double t_0 = 0;
        private double t_recovery = 0;
        private bool finished_changing = true;

        //Debug
        private DebugInformation[] debugInfo = null;
        private List<DebugInformation> newDebugInfo;

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
            
        }

        //implements abstract circle interface: used to setup the initial information so that the agent has basic knowledge about the level
        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
            
        }

        private void UpdateDraw()
        {
            newDebugInfo.Clear();
            newDebugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());
            setupMaker.ExplainabilitySystem(ref newDebugInfo);
            debugInfo = newDebugInfo.ToArray();
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
        }

        private void RandomActionWithLessJump()
        {
            /*
             Circle Actions
             ROLL_LEFT = 1      
             ROLL_RIGHT = 2
             JUMP = 3
             GROW = 4
            */
            int r = rnd.Next(20);
            if(r < 9)
            {
                currentAction = Moves.ROLL_RIGHT;
            }
            else if(r < 18)
            {
                currentAction = Moves.ROLL_LEFT;
            }
            else
            {
                currentAction = Moves.JUMP;
            }
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

            setupMaker.currentPlatformCircle = setupMaker.levelMapCircle.CirclePlatform(setupMaker.circleInfo);
            setupMaker.currentPlatformRectangle = setupMaker.levelMapRectangle.PlatformBelowRectangle(setupMaker.rectangleInfo);
            UpdateDraw();

            if (Math.Abs(setupMaker.circleInfo.X - setupMaker.lastCircleInfo.X) <= 5 && Math.Abs(setupMaker.circleInfo.Y - setupMaker.lastCircleInfo.Y) <= 5)
            {
                setupMaker.timesStuckCircle++;
            }
            else
            {
                setupMaker.lastCircleInfo = setupMaker.circleInfo;
                setupMaker.timesStuckCircle = 0;
            }

            if (t_recovery > 0 || (setupMaker.timesStuckCircle > 70 && setupMaker.timesStuckRectangle > 70 &&
                Math.Abs(setupMaker.circleInfo.X- setupMaker.rectangleInfo.X) < GameInfo.CIRCLE_RADIUS + GameInfo.VERTICAL_RECTANGLE_HEIGHT / 2 + 2 * GameInfo.PIXEL_LENGTH &&
                Math.Abs(setupMaker.circleInfo.Y - setupMaker.rectangleInfo.Y) < GameInfo.CIRCLE_RADIUS + GameInfo.VERTICAL_RECTANGLE_HEIGHT / 2 + 2 * GameInfo.PIXEL_LENGTH))
            {
                t_recovery += elapsedGameTime.TotalMilliseconds;
                if (setupMaker.timesStuckCircle > 70 && t_recovery > 300 + setupMaker.numStuck * 100)
                {
                    RandomActionWithLessJump();
                    t_recovery = 1;
                }
                else if (t_recovery > 300 + setupMaker.numStuck * 100)
                {
                    t_recovery = 0;
                    setupMaker.numStuck++;
                }
                setupMaker.circle_state = "Sistema de recuperación...";
                return;
            }
            
            t_0 += elapsedGameTime.TotalMilliseconds;
            t += elapsedGameTime.TotalMilliseconds;
            if (t < 100)
            {
                return;
            }
            currentPlatformCircle = setupMaker.levelMapCircle.CirclePlatform(setupMaker.circleInfo);
            
            if (!setupMaker.levelMapCircle.AtBorder(setupMaker.circleInfo, currentPlatformCircle, ref currentAction, setupMaker.planCircle))
            {
                if (currentPlatformCircle.id == -1 && setupMaker.CircleAboveRectangle())
                {
                    setupMaker.circle_state = "Manteniendo el equilibrio encima del rectángulo...";
                    Platform rectangle_platform = setupMaker.levelMapRectangle.RectanglePlatform(setupMaker.rectangleInfo);
                    foreach(int id in setupMaker.levelMapCircle.small_circle_to_small_rectangle.Keys)
                    {
                        if(setupMaker.levelMapCircle.small_circle_to_small_rectangle[id].id == rectangle_platform.id)
                        {
                            currentPlatformCircle = setupMaker.levelMapCircle.platformList[id];
                            break;
                        }
                    }
                }
                if (currentPlatformCircle.id == -1) // Ball is in the air
                {
                    setupMaker.circle_state = "Volando...";
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
                    if (setupMaker.planCircle.Count == 0 || ((currentPlatformCircle.real || setupMaker.CircleAboveRectangle()) &&
                            setupMaker.planCircle[0].departurePlatform.id != setupMaker.levelMapCircle.small_to_simplified[currentPlatformCircle].id)
                            || (setupMaker.planCircle[0].moveType == MoveType.COOPMOVE && setupMaker.planRectangle[0].moveType == MoveType.COOPMOVE)) //CIRCLE IN LAST PLATFORM
                    {
                        // TODO: Add logic with failed move
                        setupMaker.Replanning();
                    }
                    setupMaker.UpdateChanging();
                    if (setupMaker.changing)
                    {
                        setupMaker.circle_state = "Intercambiando posición con el rectángulo...";
                        if (setupMaker.rectangleInfo.X > setupMaker.circleInfo.X)
                        {
                            if (setupMaker.circleInfo.VelocityX > 100)
                            {
                                currentAction = Moves.NO_ACTION;
                            }
                            else
                            {
                                currentAction = Moves.ROLL_RIGHT;
                            }
                        }
                        else
                        {
                            if (setupMaker.circleInfo.VelocityX < -100)
                            {
                                currentAction = Moves.NO_ACTION;
                            }
                            else
                            {
                                currentAction = Moves.ROLL_LEFT;
                            }
                        }
                        finished_changing = false;
                        return;
                    }
                    else if(!finished_changing)
                    {
                        if(Math.Abs(setupMaker.rectangleInfo.X - setupMaker.circleInfo.X) < GameInfo.VERTICAL_RECTANGLE_HEIGHT/2 + GameInfo.CIRCLE_RADIUS)
                        {
                            return;
                        }
                        else
                        {
                            finished_changing = true;
                        }
                    }
                    Tuple<Moves, Tuple<bool, bool>> tup;
                    if (GameInfo.PHYSICS)
                    {
                        tup = setupMaker.actionSelectorCircle.nextActionPhisics(ref setupMaker.planCircle, remaining, setupMaker.circleInfo, setupMaker.rectangleInfo, currentPlatformCircle);
                    }
                    else
                    {
                        //tup = setupMaker.actionSelectorCircle.nextActionQTable(ref setupMaker.planCircle, remaining, setupMaker.circleInfo, setupMaker.rectangleInfo, currentPlatformCircle);
                    }
                    currentAction = tup.Item1;
                    if (tup.Item2.Item1)
                    {
                        t = 0;
                    }
                    flag = tup.Item2.Item2;
                }
            }
            else {
                setupMaker.circle_state = "Evitando caer por un borde...";
            }
            if(currentAction == Moves.JUMP)
            {
                setupMaker.circleAgentReadyForCoop = false;
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
                        setupMaker.circleAgent = this;
                    }
                }
            }
        }
    }
}

