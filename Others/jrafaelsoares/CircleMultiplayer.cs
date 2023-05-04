using GeometryFriends;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Communication;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;
using GeometryFriends.XNAStub;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace GeometryFriendsAgents
{
    public class CircleMultiplayer
    {
        //auxiliary variables for agent action
        private Moves currentAction;
        private List<Moves> possibleMoves;
        private Random rnd;

        //predictor of actions for the circle
        private ActionSimulator predictor = null;
        private DebugInformation[] debugInfo = null;
        private DebugInformation[] debugText = null;
        private int debugCircleSize = 20;
        private bool getDebugInfo = false;
        private bool debugTree = true;

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
        private Stopwatch gameTime;
        private Stopwatch searchTime;
        private float gSpeed;
        private Utils utils;

        private int nCollectiblesLeft;

        //RRT tree
        private RRTUtils RRT;
        private RRTUtilsMP RRTMP;
        private Tree T;
        private Tree TReturn;
        private TreeMP TMP;
        private int iterationsS = 150;
        private int iterationsSControl = 1;
        private int iterationsRecover = 50;
        private int iterationsFirst = 150;
        private int iterationsReturn = 50;
        private int iterationsHighest = 1;
        private int iterationsMP = 10;
        //plan
        private int remainingActions;
        //if a new plan is needed
        private bool planRRT = true;
        private bool newPlan = true;
        //simulator
        //time of simulation
        private float actionTime = 2.1f;
        private float actionTimeMargin = 0.5f;
        private float jumpTimeMargin = 1.0f;
        private float simTime = 2.0f;
        private float simTimeFinish = 0.1f;
        //control
        private CircleController controller;
        private CircleControllerMP controllerMP;
        private PathPlan pathPlan;
        private PathPlan originalPlan;
        private PathPlanMP pathPlanMP;
        private PathPlanMP originalPlanMP;
        private float pointTimeMargin = 10;
        private float pointTimeMarginMP = 15; //more time to let the human player get there
        private bool justJumped = false;
        private Moves lastMove = Moves.NO_ACTION;
        private bool controlling = false;
        private Stopwatch timestep;

        private float acceleration;
        private float velVar = 0;
        private float previousVelX = 0;
        private float previousAcceleration = 0;
        //rectangle info - area
        private float rectangleArea;
        //circle info
        private float previousCirclePosX;
        private float previousCirclePosY;
        private int type = 0;  //0 -> circle  1 -> rectangle
        private bool jumpPerformed = false;
        //level info
        private bool hasStarted = false;
        private bool firstAction = true;
        private bool firstAction2;
        private bool firstAction3;
        private bool lastAction = false;
        private List<DiamondInfo> Diamonds;
        private List<Platform> Platforms;
        private Platform ground;
        private int[,] levelLayout;
        private int gameMode; //0 -> singleplayer  1 -> multiplayer
        private GoalType goalMode;
        private float diamondRadius = 30;
        private float circleMaxJump = 400;
        private float rectangleMaxHeight = 193;

        //correction
        private bool correctRRT = false;
        private float correctVelYMargin = 20.0f;
        private float correctVelXMargin = 5.0f;
        private int VLODmaxPoints = 2;
        private PathPlan previousPlan;
        private PathPlanMP previousPlanMP;

        //Area of the game screen
        private Rectangle area;

        //debug
        private float circleMaxHeight;
        private float circleInitialY;
        private bool initialY = true;

        //for tests
        private bool cutplan;
        private bool written;
        private bool testing;
        private bool timer;
        private int i = 0;

        public CircleMultiplayer(bool cp, bool test, bool timr)
        {
            //setup for action updates
            currentAction = Moves.NO_ACTION;
            rnd = new Random();

            //prepare the possible moves  
            possibleMoves = new List<Moves>();
            possibleMoves.Add(Moves.ROLL_LEFT);
            possibleMoves.Add(Moves.ROLL_RIGHT);
            possibleMoves.Add(Moves.JUMP);           

            //history keeping
            uncaughtCollectibles = new List<CollectibleRepresentation>();
            caughtCollectibles = new List<CollectibleRepresentation>();
            remaining = new List<CollectibleRepresentation>();

            //test flags
            cutplan = cp;
            written = false;
            testing = test;
            timer = timr;
    }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                     MAIN SETUP                                       ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        //implements abstract circle interface: used to setup the initial information so that the agent has basic knowledge about the level
        public void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
            ground = new Platform(0, area.Bottom, 0, 0, PlatformType.Black);
            utils = new Utils(ground, circleInfo.Radius, area);
            numbersInfo = nI;
            nCollectiblesLeft = nI.CollectiblesCount;
            rectangleInfo = rI;
            circleInfo = cI;
            obstaclesInfo = utils.joinObstacles(oI, rPI);
            rectanglePlatformsInfo = rPI;
            circlePlatformsInfo = cPI;
            collectiblesInfo = colI;
            uncaughtCollectibles = new List<CollectibleRepresentation>(collectiblesInfo);
            this.area = area;
            gSpeed = 1.0f;
            goalMode = GoalType.FirstPossible;

            //setup level layout
            levelLayout = utils.getLevelLayout(obstaclesInfo, area);

            //calculates de area of the rectangle since only the info of the height is available
            rectangleArea = utils.setRectangleArea(rectangleInfo.Height);

            //gets the initial position of the circle to test is the game has started or is still at the menu
            previousCirclePosX = circleInfo.X;
            previousCirclePosY = circleInfo.Y;

            Platforms = utils.setupPlatforms(obstaclesInfo, cPI, rPI);
            Diamonds = utils.setupDiamonds(collectiblesInfo, levelLayout);

            /*************FINAL*************/
            RRT = new RRTUtils(actionTime, simTime, simTimeFinish, getPossibleMoves(), type, area, collectiblesInfo.Length, RRTTypes.BGT, RRTTypes.STP, obstaclesInfo, gSpeed, Diamonds, Platforms, utils, true, true);
            RRTMP = new RRTUtilsMP(actionTime, simTime, simTimeFinish, getPossibleMovesMP(), type, area, collectiblesInfo.Length, RRTTypes.BGT, RRTTypes.STP, obstaclesInfo, gSpeed, Diamonds, Platforms, utils, true, true);
            RRTMP.setRadius(circleInfo.Radius);
            pathPlanMP = new PathPlanMP(cutplan, colI.GetLength(0), null, utils);
            controllerMP = new CircleControllerMP(gSpeed, utils);

            RRT.setRadius(circleInfo.Radius);
            pathPlan = new PathPlan(cutplan, colI.GetLength(0), utils);
            controller = new CircleController(gSpeed);

            timestep = new Stopwatch();
            timestep.Start();
        }

        //implements abstract circle interface: registers updates from the agent's sensors that it is up to date with the latest environment information
        /*WARNING: this method is called independently from the agent update - Update(TimeSpan elapsedGameTime) - so care should be taken when using complex 
         * structures that are modified in both (e.g. see operation on the "remaining" collection)      
         */
        public void SensorsUpdated(int nC, RectangleRepresentation rI, CircleRepresentation cI, CollectibleRepresentation[] colI)
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
        public void ActionSimulatorUpdated(ActionSimulator updatedSimulator)
        {
            predictor = updatedSimulator;
        }

        //implements abstract circle interface: GeometryFriends agents manager gets the current action intended to be actuated in the enviroment for this agent
        public Moves GetAction()
        {
            return currentAction;
        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                     UPDATE                                           ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        //implements abstract circle interface: updates the agent state logic and predictions
        public void Update(TimeSpan elapsedGameTime)
        {
            //check if the game as started so the plan can be executed
            if (!hasStarted)
            {
                checkIfGameStarted();
            }

            if (hasStarted)
            {
                if (hasStarted)
                {
                    MultiPUpdate(elapsedGameTime);
                }
            }

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
                            //update diamond information
                            foreach (DiamondInfo diamond in Diamonds)
                            {
                                if (Math.Round(item.X) == Math.Round(diamond.getX()) &&
                                   Math.Round(item.Y) == Math.Round(diamond.getY()))
                                {
                                    diamond.setCaught();
                                }
                            }
                        }
                    }
                    foreach (CollectibleRepresentation item in toRemove)
                    {
                        uncaughtCollectibles.Remove(item);
                    }
                }
            }

            //tests - write time to file when game over
            if (uncaughtCollectibles.Count == 0 && testing)
            {
                utils.writeTimeToFile(2, 0, searchTime, gSpeed);
            }

            timestep.Restart();
        }

        public void MultiPUpdate(TimeSpan elapsedGameTime)
        {
            Platform partnerPlatform = utils.onPlatform(rectangleInfo.X, rectangleInfo.Y + rectangleInfo.Height / 2);
            Platform agentPlatform = utils.onPlatform(circleInfo.X, circleInfo.Y + circleInfo.Radius);
            //check for possible control without planning
            //if a diamond is on the same platform has the agents, none of them can catch it alone but can catch it together
            foreach (DiamondInfo diamond in Diamonds)
            {
                Platform dPlatform = diamond.getPlatform();
                if (diamond.getY() + diamondRadius > dPlatform.getY() - dPlatform.getHeight() / 2 - rectangleMaxHeight - circleMaxJump &&
                    diamond.getY() < dPlatform.getY() - dPlatform.getHeight() / 2  - circleMaxJump)
                {
                    currentAction = Moves.NO_ACTION;
                        
                    //check if agents are on that platform
                    if ((agentPlatform != null && utils.samePlatform(dPlatform, agentPlatform) &&
                        partnerPlatform != null && utils.samePlatform(dPlatform, partnerPlatform)) ||
                        (agentPlatform == null && partnerPlatform != null && utils.samePlatform(dPlatform, partnerPlatform)))
                    {
                        currentAction = controllerMP.catchFromRectangle(circleInfo.X, circleInfo.Y, circleInfo.VelocityX, circleInfo.VelocityY, rectangleInfo.X, rectangleInfo.Y, rectangleInfo.VelocityX, rectangleInfo.Height, utils.getRectangleWidth(rectangleInfo.Height), diamond.getX(), diamond.getY(), getAcceleration());
                        return;
                    }
                }
            }

            //if the highest uncaught diamond is on a yellow platform and needs the rectangle to be used as a paltform to be caught 
            DiamondInfo highestDiamond = null;
            foreach(DiamondInfo diamond in Diamonds)
            {
                if (!diamond.wasCaught())
                {
                    highestDiamond = diamond;
                    break;
                }
            }
            
            //check if the diamond is on a yellow platform and rectangle is on that platform
            if(highestDiamond != null && highestDiamond.getPlatform().getType() == PlatformType.Yellow && partnerPlatform != null &&
               (agentPlatform != null || utils.onRectangle(circleInfo.X, circleInfo.Y + circleInfo.Radius, rectangleInfo.X, rectangleInfo.Y, rectangleInfo.Height, utils.getRectangleWidth(rectangleInfo.Height), 25)) && 
               utils.samePlatform(partnerPlatform, highestDiamond.getPlatform()))
            {
                currentAction = controllerMP.catchFromRectangleOnYellowPlatform(circleInfo.X, circleInfo.Y, circleInfo.VelocityX, circleInfo.VelocityY, rectangleInfo.X, rectangleInfo.Y, rectangleInfo.VelocityX, rectangleInfo.Height, utils.getRectangleWidth(rectangleInfo.Height), highestDiamond, agentPlatform , getAcceleration());
                return;
            }


            //Plan
            if (planRRT && predictor != null && predictor.CharactersReady())
            {
                controlling = false;
                planSolutionMultiplayer();
            }
            else if (getDebugInfo)
            {
                if (cutplan)
                {
                    if (goalMode == GoalType.Coop)
                    {
                        debugInfo = RRTMP.getDebugInfo(TMP, pathPlanMP.cleanPlan(obstaclesInfo, Diamonds, area, circleInfo.Radius, true, true)).ToArray();
                    }
                    else
                    {
                        debugInfo = RRT.getDebugInfo(T, pathPlan.cleanPlan(obstaclesInfo, Diamonds, area, circleInfo.Radius, true, true)).ToArray();
                    }
                }
                else
                {
                    if (goalMode == GoalType.Coop)
                    {
                        debugInfo = RRTMP.getDebugInfo(TMP, pathPlanMP.debugCleanPlan()).ToArray();
                    }
                    else
                    {
                        debugInfo = RRT.getDebugInfo(T, pathPlan.debugCleanPlan()).ToArray();
                    }
                }

                getDebugInfo = false;
            }
            else if (!getDebugInfo)
            {
                debugInfo = null;
            }
            if (goalMode == GoalType.Coop)
            {
                //Control - if there is a plan then execute it
                if (pathPlanMP.getPathPoints() != null && pathPlanMP.getPathPoints().Count != 0 && hasStarted)
                {
                    controlling = true;
                    planExecutionMP();
                }
                else if (pathPlanMP.getTotalCollectibles() != 0 && pathPlanMP.getTotalCollectibles() == uncaughtCollectibles.Count)
                {
                    //if currently the number of uncaught collectibles is the same as the one supposed to be at the end of the plan, stop the agent and replan
                    replanMP(false);
                }
                //make sure the agent replans when missing the last action
                else if (pathPlanMP.getPathPoints().Count == 0 && controlling)// && lastAction)
                {
                    lastActionReplanMP();
                }
                //if it none of the above work and the too much time has already passed, replan
                else if (controlling && (gameTime.ElapsedMilliseconds * 0.001f * gSpeed > pointTimeMargin * gSpeed))
                {
                    replanMP(true);
                }
                //if it does not have plan points, it is not in control mode or in plan mode, then the plan probably failed
                else if (pathPlanMP.getPathPoints().Count == 0 && !controlling && !planRRT)
                {
                    planRRT = true;
                    newPlan = true;
                }
            }
            else
            {
                //Control - if there is a plan then execute it
                if (pathPlan.getPathPoints() != null && pathPlan.getPathPoints().Count != 0 && hasStarted)
                {
                    controlling = true;
                    planExecution();
                }
                else if (controlling && pathPlan.getTotalCollectibles() != 0 && pathPlan.getTotalCollectibles() == uncaughtCollectibles.Count)
                {
                    //if currently the number of uncaught collectibles is the same as the one supposed to be at the end of the plan, stop the agent and replan
                    replan(false);
                }
                //make sure the agent replans when missing the last action
                else if (pathPlan.getPathPoints().Count == 0 && controlling)// && lastAction)
                {
                    lastActionReplan();
                }
                //if it none of the above work and the too much time has already passed, replan
                else if (controlling && (gameTime.ElapsedMilliseconds * 0.001f * gSpeed > pointTimeMargin * gSpeed))
                {
                    replan(true);
                }
                //if it does not have plan points, it is not in control mode or in plan mode, then the plan probably failed
                else if (pathPlan.getPathPoints().Count == 0 && !controlling && !planRRT)
                {
                    planRRT = true;
                    newPlan = true;
                }
            }
        }

        //implements abstract circle interface: gets the debug information that is to be visually represented by the agents manager
        public DebugInformation[] GetDebugInformation()
        {
            return debugInfo;
        }

        private List<Moves> getPossibleMoves()
        {
            List<Moves> moves = new List<Moves>();

            moves = new List<Moves>();
            moves.Add(Moves.ROLL_LEFT);
            moves.Add(Moves.ROLL_RIGHT);
            moves.Add(Moves.JUMP);

            return moves;
        }

        private List<Moves[]> getPossibleMovesMP()
        {
            List<Moves[]> moves = new List<Moves[]>();

            //CAUTION - agents moves first, partners second
            List<Moves> circleActions = new List<Moves>();
            circleActions.Add(Moves.ROLL_LEFT);
            circleActions.Add(Moves.ROLL_RIGHT);
            circleActions.Add(Moves.JUMP);
            circleActions.Add(Moves.NO_ACTION);

            List<Moves> rectangleActions = new List<Moves>();
            rectangleActions.Add(Moves.MOVE_LEFT);
            rectangleActions.Add(Moves.MOVE_RIGHT);
            rectangleActions.Add(Moves.MORPH_UP);
            rectangleActions.Add(Moves.MORPH_DOWN);
            rectangleActions.Add(Moves.NO_ACTION);

            int circleActionCount = 4; //counting with NO_ACTION
            int rectangleActionCount = 5; //counting with NO_ACTION

            for (int i = 0; i < circleActionCount; i++)
            {
                for (int j = 0; j < rectangleActionCount; j++)
                {
                    //do not count with the pair No_action, No_action
                    if (i == circleActionCount - 1 && j == rectangleActionCount - 1)
                    {
                        break;
                    }
                    Moves[] newMove = new Moves[2];
                    newMove[0] = circleActions[i];
                    newMove[1] = rectangleActions[j];
                    moves.Add(newMove);
                }
            }

            return moves;
        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                     PLANNING                                         ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        private void planSolutionMultiplayer()
        {
            //The agent must be still so it starts at the same position as the one in the first point of the plan
            //This is to guarantee that the agent stops before start planning, and keeps still
            if (circleInfo.VelocityY < correctVelYMargin && circleInfo.VelocityY > -correctVelYMargin &&
                circleInfo.VelocityX < correctVelXMargin && circleInfo.VelocityX > -correctVelXMargin)
            {
                //make sure there is nothing moving the agent when planning
                currentAction = Moves.NO_ACTION;

                //if the plan is new build a new tree
                if (newPlan)
                {
                    List<DiamondInfo> remainingDiamonds = new List<DiamondInfo>();
                    List<DiamondInfo> caughtDiamonds = new List<DiamondInfo>();
                    foreach (DiamondInfo diamond in Diamonds)
                    {
                        if (!diamond.wasCaught())
                        {
                            remainingDiamonds.Add(diamond);
                        }
                        else
                        {
                            caughtDiamonds.Add(diamond);
                        }
                    }

                    //FIRST STEP
                    //always try to catch a diamond by itself first - to be changed
                    goalMode = GoalType.FirstPossible;
                    //update the diamond list
                    RRT.setDiamonds(Diamonds);
                    //create initial state
                    State initialState = new State(circleInfo.X, circleInfo.Y, circleInfo.VelocityX, circleInfo.VelocityY, circleInfo.Radius / 2, 0, caughtDiamonds, remainingDiamonds);
                    //Creates simulator
                    Simulator sim = new CircleSimulator(Platforms);
                    sim.setSimulator(circleInfo.X, circleInfo.Y, circleInfo.VelocityX, circleInfo.VelocityY, Diamonds);

                    //run algorithm
                    T = RRT.buildNewMPRRT(initialState, sim, goalMode, iterationsFirst);
                }
                else //continue the previous tree
                {
                    if (goalMode == GoalType.Coop)
                    {
                        TMP = RRTMP.RRT(TMP);
                    }
                    else
                    {
                        T = RRT.RRT(T);
                    }
                }
                //if a diamond was found when searching for a single diamond
                if (T.getGoal() != null && goalMode == GoalType.FirstPossible)
                {
                    List<DiamondInfo> remainingDiamonds = new List<DiamondInfo>();
                    List<DiamondInfo> caughtDiamonds = new List<DiamondInfo>();
                    foreach (DiamondInfo diamond in Diamonds)
                    {
                        if (!diamond.wasCaught())
                        {
                            remainingDiamonds.Add(diamond);
                        }
                        else
                        {
                            caughtDiamonds.Add(diamond);
                        }
                    }

                    //check if it is possible to return
                    State goalState = T.getGoal().getState();
                    goalMode = GoalType.Return;
                    //create initial state
                    State finalState = new State(goalState.getPosX(), goalState.getPosY(), goalState.getVelX(), goalState.getVelY(), goalState.getHeight() / 2, 0, caughtDiamonds, remainingDiamonds);
                    //run algorithm
                    RRTUtils tempRRT = new RRTUtils(actionTime, simTime, simTimeFinish, getPossibleMoves(), type, area, collectiblesInfo.Length, RRTTypes.Bias, RRTTypes.STP, obstaclesInfo, gSpeed, Diamonds, Platforms, utils, true, true);
                    float[] returnPos = new float[2];
                    returnPos[0] = T.getRoot().getState().getPosX();
                    returnPos[1] = T.getRoot().getState().getPosY();
                    tempRRT.setReturnPos(returnPos);
                    NodeSimulator simul = (NodeSimulator) T.getGoal();
                    TReturn = tempRRT.buildNewMPRRT(finalState, simul.getSimulator(), goalMode, iterationsReturn);

                    //if a return path was found
                    if (TReturn.getGoal() != null)
                    {
                        pathPlan = RRT.getPlan(T);

                        firstAction = true;

                        //do not plan on the next iteration
                        planRRT = false;
                        getDebugInfo = true;

                        //save a copy of the original plan
                        originalPlan = pathPlan.clone();
                        pathPlan.saveOriginal();
                    }
                    else
                    {//get diamond caught and make the next search ignore it
                        Node auxNode = T.getGoal();
                        int prevDiamonds = auxNode.getState().getNumberUncaughtDiamonds();
                        while (auxNode.getParent() != null)
                        {
                            int currentDiamonds = auxNode.getParent().getState().getNumberUncaughtDiamonds();
                            if (currentDiamonds > prevDiamonds)
                            {
                                List<DiamondInfo> prevDiamondsList = auxNode.getState().getUncaughtDiamonds();
                                List<DiamondInfo> currentDiamondsList = auxNode.getParent().getState().getUncaughtDiamonds();

                                foreach (DiamondInfo d1 in prevDiamondsList)
                                {
                                    bool inCurrent = false;
                                    foreach (DiamondInfo d2 in currentDiamondsList)
                                    {
                                        if (d1.getX() == d2.getX() && d1.getY() == d2.getY())
                                        {
                                            inCurrent = true;
                                            break;
                                        }
                                    }
                                    if (!inCurrent)
                                    {
                                        foreach (DiamondInfo d3 in Diamonds)
                                        {
                                            if (Math.Round(d1.getX()) == Math.Round(d3.getX()) &&
                                                Math.Round(d1.getY()) == Math.Round(d3.getY()))
                                            {
                                                RRT.diamondToIgnore(d3);
                                            }
                                        }
                                        break;
                                    }
                                }
                                break;
                            }
                            auxNode = auxNode.getParent();
                        }

                        RRT.removeGoal(T);
                    }
                }
                //SECOND STEP
                if (goalMode == GoalType.FirstPossible || goalMode == GoalType.Return)
                {
                    goalMode = GoalType.HighestSingle;
                //    //try to get the highest diamond in the higest platform in single player mode
                //    goalMode = GoalType.HighestSingle;
                //    //reset ignored diamonds
                //    RRT.resetDiamondsToIgnore();
                //    //create initial state
                //    State initialState = new State(circleInfo.X, circleInfo.Y, circleInfo.VelocityX, circleInfo.VelocityY, circleInfo.Radius / 2, 0, caughtCollectibles, uncaughtCollectibles);
                //    //run algorithm
                //    RRT.removeGoal(T);
                //    T = RRT.buildNewMPRRT(initialState, predictor, goalMode, iterationsHighest);
                //}
                ////catch it if plan found
                //if (T.getGoal() != null && goalMode == GoalType.HighestSingle)
                //{
                //    pathPlan = RRT.getPlan(T);

                //    firstAction = true;

                //    //do not plan on the next iteration
                //    planRRT = false;
                //    getDebugInfo = true;

                //    //save a copy of the original plan
                //    originalPlan = pathPlan.clone();
                //    pathPlan.saveOriginal();
                //    goalMode = GoalType.FirstPossible;
                }
                /************************************************************************************************/
                //THIRD STEP
                if (T.getGoal() == null && goalMode == GoalType.HighestSingle)
                {
                    //try to get the highest diamond in the higest platform in single player mode
                    goalMode = GoalType.Coop;
                    //reset ignored diamonds
                    //RRTMP.resetDiamondsToIgnore();
                    //create initial state
                    if (newPlan)
                    {
                        StateMP initialState = new StateMP(circleInfo.X, circleInfo.Y, circleInfo.VelocityX, circleInfo.VelocityY, rectangleInfo.Height / 2, rectangleInfo.X, rectangleInfo.Y, rectangleInfo.VelocityX, rectangleInfo.VelocityY, 0, caughtCollectibles, uncaughtCollectibles);
                        //run algorithm
                        TMP = RRTMP.buildNewMPRRT(initialState, predictor, goalMode, iterationsMP);
                    }
                    else
                    {
                        TMP = RRTMP.RRT(TMP);
                    }
                    
                }
                if (TMP.getGoal() != null && goalMode == GoalType.Coop)
                {
                    //TODO
                    //this plan must be dealt differently
                    //the agent must know when to wait for the other agent
                    pathPlanMP = RRTMP.getPlan(TMP);
                    pathPlanMP.setTree(TMP);

                    firstAction = true;

                    //do not plan on the next iteration
                    planRRT = false;
                    getDebugInfo = true;

                    //save a copy of the original plan
                    originalPlanMP = pathPlanMP.clone();
                    pathPlanMP.saveOriginal();
                }
                else if(TMP.getGoal() == null && goalMode == GoalType.Coop)
                {
                    //try changing between searches
                   // goalMode = GoalType.FirstPossible;
                }

                //draw the nodes of the tree
                if (debugTree)
                {
                    if (goalMode == GoalType.Coop)
                    {
                        debugInfo = RRTMP.getDebugTreeInfo(TMP).ToArray();
                    }
                    else
                    {
                        debugInfo = RRT.getDebugTreeInfo(T).ToArray();
                    }
                }

                newPlan = false;
            }
            else
            {
                keepStill();
            }
        }

        /*******************************************************/
        /*                  Planning - aux                     */
        /*******************************************************/

        //used when trying to recover a plan where the agent is not on a platform and state of the plan
        //joinplans on utils

        //simple plan recovery - check if agent is on a platform that belongs to the plan and with the same state
        private void recoverPlan()
        {
            //TODO - update debug drawing
            currentAction = Moves.NO_ACTION;
            lastMove = Moves.NO_ACTION;
            controller.jumpReached = false;
            controller.rollReached = false;

            //see if there is a point in the original plan that is the same as the one the agent is on and that has the same number or less of diamonds caught
            bool pointFound = false;

            Platform currentPlatform = utils.onPlatform(circleInfo.X, circleInfo.Y + circleInfo.Radius, 25, 10);
            List<Point> pathPoints = pathPlan.getOriginalPoints();

            int i;
            //start from the end
            for (i = pathPoints.Count - 1; i >= 0; i--)
            {
                if (pathPoints[i].getUncaughtColl().Count >= uncaughtCollectibles.Count)
                {
                    Platform pointPlatform = utils.onPlatform(pathPoints[i].getPosX(), pathPoints[i].getPosY() + circleInfo.Radius, 25, 10);

                    if (utils.samePlatform(currentPlatform, pointPlatform) && !utils.obstacleBetween(circleInfo.X, pathPoints[i].getPosX(), currentPlatform))
                    {
                        pointFound = true;
                        break;
                    }
                }
            }

            if (pointFound)
            {
                //create a new plan from the point we got previously
                pathPlan = new PathPlan(cutplan, remaining.Count, utils);
                pathPlan.setTotalCollectibles(originalPlan.getTotalCollectibles());
                pathPlan.setCurrentPoint(i);

                for (int j = i; j < pathPoints.Count; j++)
                {
                    pathPlan.addPointEnd(pathPoints[j]);
                }
                pathPlan.setOriginalPoints(pathPoints);
                firstAction = true;
            }
            //if no platform in common was found, then replan
            else if (pathPlan.getPathPoints().Count != 0)
            {
                List<DiamondInfo> remainingDiamonds = new List<DiamondInfo>();
                List<DiamondInfo> caughtDiamonds = new List<DiamondInfo>();
                foreach (DiamondInfo diamond in Diamonds)
                {
                    if (!diamond.wasCaught())
                    {
                        remainingDiamonds.Add(diamond);
                    }
                    else
                    {
                        caughtDiamonds.Add(diamond);
                    }
                }

                //Simulator
                Simulator sim = new CircleSimulator(Platforms);
                sim.setSimulator(circleInfo.X, circleInfo.Y, circleInfo.VelocityX, circleInfo.VelocityY, Diamonds);

                State initialState = new State(circleInfo.X, circleInfo.Y, circleInfo.VelocityX, circleInfo.VelocityY, circleInfo.Radius, circleInfo.Radius, caughtDiamonds, remainingDiamonds);
                float[] returnPos = new float[2];
                returnPos[0] = pathPlan.getPathPoints()[0].getPosX();
                returnPos[1] = pathPlan.getPathPoints()[0].getPosY();
                RRT.setReturnPos(returnPos);
                Tree t = RRT.buildNewMPRRT(initialState, sim, GoalType.Return, iterationsS);

                if (t.getGoal() != null)
                {
                    PathPlan shortPlan = RRT.getPlan(t);
                    pathPlan = pathPlan.joinPlans(shortPlan, pathPlan);
                    pathPlan.cleanPlan(obstaclesInfo, Diamonds, area, circleInfo.Radius, true, true);
                    getDebugInfo = true;
                }
                else
                {
                    replan(false);
                }
            }
        }

        //simple plan recovery - check if agent is on a platform that belongs to the plan and with the same state
        private void recoverPlanMP()
        {
            //TODO - update debug drawing
            currentAction = Moves.NO_ACTION;
            lastMove = Moves.NO_ACTION;
            controller.jumpReached = false;
            controller.rollReached = false;

            //see if there is a point in the original plan that is the same as the one the agent is on and that has the same number or less of diamonds caught
            bool pointFound = false;

            Platform currentPlatform = utils.onPlatform(circleInfo.X, circleInfo.Y + circleInfo.Radius, 25, 10);
            List<PointMP> pathPoints = pathPlanMP.getOriginalPoints();

            int i;
            for (i = 0; i < pathPoints.Count; i++)
            {
                if (pathPoints[i].getUncaughtColl().Count >= uncaughtCollectibles.Count)
                {
                    Platform pointPlatform = utils.onPlatform(pathPoints[i].getPosX(), pathPoints[i].getPosY() + circleInfo.Radius, 25, 10);

                    if (utils.samePlatform(currentPlatform, pointPlatform) && !utils.obstacleBetween(circleInfo.X, pathPoints[i].getPosX(), currentPlatform))
                    {
                        pointFound = true;
                        break;
                    }
                }
            }

            if (pointFound)
            {
                //create a new plan from the point we got previously
                pathPlanMP = new PathPlanMP(cutplan, remaining.Count, pathPlanMP.getTree(), utils);
                pathPlanMP.setTotalCollectibles(originalPlan.getTotalCollectibles());
                pathPlanMP.setCurrentPoint(i);

                for (int j = i; j < pathPoints.Count; j++)
                {
                    pathPlanMP.addPointEnd(pathPoints[j]);
                }
                pathPlanMP.setOriginalPoints(pathPoints);
                firstAction = true;
            }
            //if no platform in common was found, then replan
            else
            {
                replanMP(false);
            }
        }

        private void replan(bool correct)
        {
            //if correct is true, it means the agent should try to first recover the previous plan
            //but it should also be careful not to repeat the same failures and should understand when it completed its plan
            if (correct && !pathPlan.checkIfConstantFail() && !checkPlanCompletion())
            {
                recoverPlan();
            }
            else
            {
                currentAction = Moves.NO_ACTION;
                planRRT = true;
                newPlan = true;
                controller.jumpReached = false;
                controller.rollReached = false;
                previousPlan = pathPlan;
                pathPlan = new PathPlan(cutplan, remaining.Count, utils);
            }
        }

        private void replanMP(bool correct)
        {
            //if correct is true, it means the agent should try to first recover the previous plan
            //but it should also be careful not to repeat the same failures and should understand when it completed its plan
            if (correct && !pathPlanMP.checkIfConstantFail() && !checkPlanCompletionMP())
            {
                recoverPlanMP();
            }
            else
            {
                currentAction = Moves.NO_ACTION;
                planRRT = true;
                newPlan = true;
                controllerMP.jumpReached = false;
                controllerMP.rollReached = false;
                previousPlanMP = pathPlanMP;
                pathPlanMP = new PathPlanMP(cutplan, remaining.Count, null, utils);
            }
        }

        private bool checkPlanCompletion()
        {
            if (uncaughtCollectibles.Count == pathPlan.getTotalCollectibles())
            {
                return true;
            }
            return false;
        }

        private bool checkPlanCompletionMP()
        {
            if (uncaughtCollectibles.Count == pathPlanMP.getTotalCollectibles())
            {
                return true;
            }
            return false;
        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                     CONTROL                                          ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        private void planExecution()
        {
            /***********************************************/
            /*                 first action                */
            /***********************************************/
            if (firstAction)
            {
                firstAction = false;
                firstAction2 = true;
                //start counting the time for the actions of the plan
                timestep = new Stopwatch();
                gameTime = new Stopwatch();
                timestep.Start();
                gameTime.Start();
                remainingActions = pathPlan.getPathPoints().Count;
            }

            /***********************************************/
            /*            check if control failed          */
            /***********************************************/
            //correct the plan or replan if it takes too long to get to the next point 
            if ((gameTime.ElapsedMilliseconds * 0.001f * gSpeed > pointTimeMargin * gSpeed))
            {
                //if the state isn't the same but it has already passed more time than it should to take the action, apply plan recovery
                replan(true);
            }
            //if the agent catches a diamond when it was not supposed to
            //might not be necessary when the checkactionfailure is implemented
            else if (uncaughtCollectibles.Count < pathPlan.getPathPoints()[0].getUncaughtColl().Count && pathPlan.getPathPoints().Count > 1 && pathPlan.getPathPoints()[1].getUncaughtColl().Count != uncaughtCollectibles.Count)
            {
                //replan for the previous plan is now invalid
                replan(false);
            }
            //if the agent fails an action
            else if (checkActionFailure())
            {
                replan(true);
            }
            /***********************************************/
            /*                 get action                  */
            /***********************************************/
            else
            {
                getAction();
            }

        }

        //get the next action according to the plan and the agent state
        private void getAction()
        {
            currentAction = controller.computeAction(pathPlan.getPathPoints()[0], circleInfo.VelocityX, circleInfo.X, circleInfo.Y, timestep.ElapsedMilliseconds * 0.001f, acceleration);
            timestep.Restart();
            if (currentAction == Moves.JUMP)
            {
                jumpPerformed = true;
            }

            //change to next point when a point is reached or when a jump was performed
            if (!correctRRT && controller.rollReached)
            {
                lastMove = pathPlan.getPathPoints()[0].getAction();

                if (firstAction3)
                {
                    firstAction3 = false;
                }
                else
                {
                    pathPlan.nextPoint();
                }

                if (pathPlan.getPathPoints().Count == 0)
                {
                    lastAction = true;
                }

                if (controller.jumpReached)
                {
                    justJumped = true;
                }
                else if (justJumped)
                {
                    justJumped = false;
                }
                controller.jumpReached = false;
                controller.rollReached = false;
                remainingActions--;
                gameTime.Restart();
                timestep.Restart();
            }
            else if (firstAction2)
            {
                firstAction2 = false;
                firstAction3 = true;
                pathPlan.getOriginalPoints()[pathPlan.getCurrentPoint()].passedThrough();
            }
        }

        private void planExecutionMP()
        {
            /***********************************************/
            /*                 first action                */
            /***********************************************/
            if (firstAction)
            {
                firstAction = false;
                firstAction2 = true;
                //start counting the time for the actions of the plan
                timestep = new Stopwatch();
                gameTime = new Stopwatch();
                timestep.Start();
                gameTime.Start();
                remainingActions = pathPlanMP.getPathPoints().Count;
            }

            /***********************************************/
            /*            check if control failed          */
            /***********************************************/

            //correct the plan or replan if it takes too long to get to the next point 
            if ((gameTime.ElapsedMilliseconds * 0.001f * gSpeed > pointTimeMargin * gSpeed))
            {
                //if the state isn't the same but it has already passed more time than it should to take the action, apply plan recovery
                replanMP(true);
            }
            //if the agent catches a diamond when it was not supposed to
            //might not be necessary when the checkactionfailure is implemented
            else if (uncaughtCollectibles.Count < pathPlanMP.getPathPoints()[0].getUncaughtColl().Count && pathPlanMP.getPathPoints().Count > 1 && pathPlanMP.getPathPoints()[1].getUncaughtColl().Count != uncaughtCollectibles.Count)
            {
                //replan for the previous plan is now invalid
                replanMP(false);
            }
            //if the agent fails an action
            else if (checkActionFailureMP())
            {
                replanMP(true);
            }
            /***********************************************/
            /*                 get action                  */
            /***********************************************/
            else
            {
                getActionMP();
            }
            //check if the goal changed or if the plan is now invalid
            //if (pathPlanMP.getPathPoints().Count != 0)
            //{
            //    checkMPReplanning();
            //}

        }

        //get the next action according to the plan and the agent state
        private void getActionMP()
        {
            currentAction = controllerMP.computeAction(pathPlanMP.getPathPoints()[0], circleInfo.VelocityX, circleInfo.X, circleInfo.Y, timestep.ElapsedMilliseconds * 0.001f, acceleration);
            timestep.Restart();
            if (currentAction == Moves.JUMP)
            {
                jumpPerformed = true;
            }

            //change to next point when a point is reached or when a jump was performed
            if (!correctRRT && controllerMP.rollReached)
            {
                lastMove = pathPlanMP.getPathPoints()[0].getAction();

                if (firstAction3)
                {
                    firstAction3 = false;
                }
                else
                {
                    pathPlanMP.nextPoint();
                }

                if (pathPlanMP.getPathPoints().Count == 0)
                {
                    lastAction = true;
                }

                if (controllerMP.jumpReached)
                {
                    justJumped = true;
                }
                else if (justJumped)
                {
                    justJumped = false;
                }
                controllerMP.jumpReached = false;
                controllerMP.rollReached = false;
                remainingActions--;
                gameTime.Restart();
                timestep.Restart();
            }
            else if (firstAction2)
            {
                firstAction2 = false;
                firstAction3 = true;
                pathPlanMP.getOriginalPoints()[pathPlanMP.getCurrentPoint()].passedThrough();
            }
        }

        private void keepStill()
        {
            //if it is moving right, move left
            if (circleInfo.VelocityX > correctVelXMargin)
            {
                currentAction = Moves.ROLL_LEFT;
            }
            //if it is moving left, move right
            else if (circleInfo.VelocityX < -correctVelXMargin)
            {
                currentAction = Moves.ROLL_RIGHT;
            }
        }

        /*******************************************************/
        /*                  control - aux                      */
        /*******************************************************/
        private float getAcceleration()
        {
            velVar = circleInfo.VelocityX - previousVelX;
            var timeStep = timestep.ElapsedMilliseconds * 0.001f;
            if (timeStep == 0)
            {
                timeStep = 0.1f;
            }
            var tempA = velVar / timeStep;

            return tempA;
        }

        private bool checkActionFailure()
        {
            Platform currentPlatform = utils.onPlatform(circleInfo.X, circleInfo.Y + circleInfo.Radius, 50, 10);
            Platform nextPlatform = utils.onPlatform(pathPlan.getPathPoints()[0].getPosX(), pathPlan.getPathPoints()[0].getPosY() + circleInfo.Radius, 50, 10);
            //check for jump failure
            if (jumpPerformed)
            {
                jumpPerformed = false;
            }
            else if (checkJumpFailure(currentPlatform, nextPlatform))
            {
                return true;
            }
            //check if agent is below the platform it should be
            if (!controller.jumping && currentPlatform != null && nextPlatform != null && currentPlatform.getY() > nextPlatform.getY())
            {
                return true;
            }

            Point currentPoint = pathPlan.getPathPoints()[0];
            //check if agent is too low to reach a point when the action is not jump
            if (!controller.jumping && currentPlatform != null && circleInfo.Y - currentPoint.getPosY() > 50)
            {
                return true;
            }

            if (pathPlan.getPathPoints().Count != 0)
            {
                //check if the platform of the agent is right above the next point in the plan
                float pointX = pathPlan.getPathPoints()[0].getPosX();

                if (currentPlatform != null && nextPlatform != null && currentPlatform.getY() < nextPlatform.getY() &&
                    pointX > currentPlatform.getX() - currentPlatform.getWidth() / 2 &&
                    pointX < currentPlatform.getX() + currentPlatform.getWidth() / 2)
                {
                    return true;
                }
            }

            return false;
        }
        private bool checkJumpFailure(Platform currentPlatform, Platform nextPlatform)
        {
            if (lastMove == Moves.JUMP && !controller.jumping)
            {
                while (nextPlatform == null && pathPlan.getPathPoints().Count > 1)
                {
                    pathPlan.nextPoint();
                    if (pathPlan.getPathPoints().Count == 0)
                    {
                        break;
                    }
                    nextPlatform = utils.onPlatform(pathPlan.getPathPoints()[0].getPosX(), pathPlan.getPathPoints()[0].getPosY() + circleInfo.Radius, 25, 10);
                }
                if (pathPlan.getPathPoints().Count == 0)
                {
                    lastAction = true;
                }
                //if the agents falls on a different platform or one same one but with an obstacle between then the jump failed
                if (currentPlatform != null && nextPlatform != null && !utils.samePlatform(currentPlatform, nextPlatform) ||//(currentPlatform.getX() != nextPlatform.getX() || currentPlatform.getY() != nextPlatform.getY()) ||
                    utils.samePlatform(currentPlatform, nextPlatform) && utils.obstacleBetween(circleInfo.X, pathPlan.getPathPoints()[0].getPosX(), currentPlatform))
                {
                    return true;
                }
            }
            return false;
        }

        private bool checkActionFailureMP()
        {
            Platform currentPlatform = utils.onPlatform(circleInfo.X, circleInfo.Y + circleInfo.Radius, 50, 10);
            Platform nextPlatform = utils.onPlatform(pathPlanMP.getPathPoints()[0].getPosX(), pathPlanMP.getPathPoints()[0].getPosY() + circleInfo.Radius, 50, 10);
            //check for jump failure
            if (jumpPerformed)
            {
                jumpPerformed = false;
            }
            else if (checkJumpFailureMP(currentPlatform, nextPlatform))
            {
                return true;
            }
            //check if agent is below the platform it should be
            if (!controller.jumping && currentPlatform != null && nextPlatform != null && currentPlatform.getY() > nextPlatform.getY())
            {
                return true;
            }

            if (pathPlanMP.getPathPoints().Count != 0)
            {
                //check if the platform of the agent is right above the next point in the plan
                float pointX = pathPlanMP.getPathPoints()[0].getPosX();

                if (currentPlatform != null && nextPlatform != null && currentPlatform.getY() < nextPlatform.getY() &&
                    pointX > currentPlatform.getX() - currentPlatform.getWidth() / 2 &&
                    pointX < currentPlatform.getX() + currentPlatform.getWidth() / 2)
                {
                    return true;
                }
            }

            return false;
        }
        private bool checkJumpFailureMP(Platform currentPlatform, Platform nextPlatform)
        {
            if (lastMove == Moves.JUMP && !controller.jumping)
            {
                while (nextPlatform == null && pathPlanMP.getPathPoints().Count > 1)
                {
                    pathPlanMP.nextPoint();
                    if (pathPlanMP.getPathPoints().Count == 0)
                    {
                        break;
                    }
                    nextPlatform = utils.onPlatform(pathPlanMP.getPathPoints()[0].getPosX(), pathPlanMP.getPathPoints()[0].getPosY() + circleInfo.Radius, 25, 10);
                }
                if (pathPlanMP.getPathPoints().Count == 0)
                {
                    lastAction = true;
                }
                //if the agents falls on a different platform or one same one but with an obstacle between then the jump failed
                if (currentPlatform != null && nextPlatform != null && !utils.samePlatform(currentPlatform, nextPlatform) ||//(currentPlatform.getX() != nextPlatform.getX() || currentPlatform.getY() != nextPlatform.getY()) ||
                    utils.samePlatform(currentPlatform, nextPlatform) && utils.obstacleBetween(circleInfo.X, pathPlanMP.getPathPoints()[0].getPosX(), currentPlatform))
                {
                    return true;
                }
            }
            return false;
        }

        private void lastActionReplan()
        {
            //TODO - verificar se completou ou não o plano que era suposto
            //if the state isn't the same but it has already passed more time than it should to take the action, apply plan recovery
            if ((currentAction == Moves.JUMP && gameTime.ElapsedMilliseconds * 0.001f * gSpeed > (simTime + jumpTimeMargin) * gSpeed) ||
                        (currentAction != Moves.JUMP && gameTime.ElapsedMilliseconds * 0.001f * gSpeed > (simTime + actionTimeMargin) * gSpeed) && !correctRRT)
            {
                lastAction = false;

                currentAction = Moves.NO_ACTION;
                replan(true);
            }
            else if (currentAction == Moves.JUMP)
            {
                currentAction = Moves.NO_ACTION;
            }
        }

        private void lastActionReplanMP()
        {
            //TODO - verificar se completou ou não o plano que era suposto
            //if the state isn't the same but it has already passed more time than it should to take the action, apply plan recovery
            if ((currentAction == Moves.JUMP && gameTime.ElapsedMilliseconds * 0.001f * gSpeed > (simTime + jumpTimeMargin) * gSpeed) ||
                        (currentAction != Moves.JUMP && gameTime.ElapsedMilliseconds * 0.001f * gSpeed > (simTime + actionTimeMargin) * gSpeed) && !correctRRT)
            {
                lastAction = false;

                currentAction = Moves.NO_ACTION;
                replanMP(true);
            }
            else if (currentAction == Moves.JUMP)
            {
                currentAction = Moves.NO_ACTION;
            }
        }

        private void checkIfGameStarted()
        {
            //checks if the game has started 
            //TODO - make it deal with pauses yet
            if (!hasStarted)
            {
                if (circleInfo.X <= previousCirclePosX + 1 && circleInfo.X >= previousCirclePosX - 1 && circleInfo.Y <= previousCirclePosY + 1 && circleInfo.Y >= previousCirclePosY - 1)
                {
                    hasStarted = false;
                }
                else
                {
                    hasStarted = true;
                    searchTime = new Stopwatch();
                    searchTime.Start();
                }
            }
        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                      DEBUG                                           ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

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

       
        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                     OTHER                                            ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        private void checkMPReplanning()
        {
            //check replanning - DRRT
            //MP-RRT - if the goal changed it needs to replan
            if (checkGoalChanges())
            {
                replan(false);
            }

            //get any invalid node of the path plan
            var invalidNodes = checkInvalidNodes();
            //if there is any
            if (invalidNodes.Count != 0)
            {
                //save nodes and positions
                RRTMP.saveOldNodes(TMP);
                //see if there is a state in the tree that is the same as the current one
                NodeMP root = RRTMP.findNode(circleInfo.X, circleInfo.Y, rectangleInfo.X, rectangleInfo.Y, Diamonds.Count - remaining.Count, false);
                RRTMP.setReplan(true);

                if (root == null)
                {
                    //new tree
                    //create initial state
                    StateMP initialState = new StateMP(circleInfo.X, circleInfo.Y, circleInfo.VelocityX, circleInfo.VelocityY, rectangleInfo.Height / 2, rectangleInfo.X, rectangleInfo.Y, rectangleInfo.VelocityX, rectangleInfo.VelocityY, 0, caughtCollectibles, uncaughtCollectibles);
                    TMP = RRTMP.buildNewMPRRT(initialState, predictor, goalMode, iterationsMP);
                }
                //update root - the previous node (the current has the final position)
                TMP = RRTMP.newRoot(root);
                
                //remove them and their children from the tree
                removeInvalidNodes(invalidNodes, TMP);
                

                currentAction = Moves.NO_ACTION;
                planRRT = true;
                //do not start a new plan and continue with the updated tree
                newPlan = false;
                controller.jumpReached = false;
                controller.rollReached = false;
                previousPlan = pathPlan;
                pathPlan = new PathPlan(cutplan, remaining.Count, utils);
            }
        }

        private bool checkGoalChanges()
        {
            HashSet<CollectibleRepresentation> diamondsCaught = new HashSet<CollectibleRepresentation>();
            List<PointMP> points = pathPlanMP.getPathPoints();
            //for each path point
            for (int i = 0; i < pathPlanMP.getPathPoints().Count; i++)
            {
                //check if the current remaining diamonds
                foreach (CollectibleRepresentation diamond in points[i].getUncaughtColl())
                {
                    if (!remaining.Exists(d => Math.Round(d.X) == Math.Round(diamond.X) && Math.Round(d.Y) == Math.Round(diamond.Y)))
                    {
                        diamondsCaught.Add(diamond);
                    }
                }
            }

            //if no diamond was caught by the other player that the agent should catch then the goal was not changed
            if (diamondsCaught.Count == 0)
            {
                return false;
            }

            //get the diamonds to be caught during the plan execution
            List<CollectibleRepresentation> diamondsToCatch = new List<CollectibleRepresentation>();
            if (points.Count != 0)
            {
                foreach (CollectibleRepresentation diamond in points[0].getUncaughtColl())
                {
                    if (!points[points.Count - 1].getUncaughtColl().Contains(diamond))
                    {
                        diamondsToCatch.Add(diamond);
                    }
                }
            }

            //check if there is a diamond to be caught in the plan that wasn't already caught
            if (diamondsToCatch.Count > diamondsCaught.Count)
            {
                //if the number of diamonds to catch is higher than the ones already caught, then there is at least one diamond the agent can catch
                //with this plan
                return false;
            }
            else
            {
                foreach (CollectibleRepresentation diamond in diamondsToCatch)
                {
                    //if one do the diamonds to catch was not already caught, then there is at least one diamond the agent can catch with this plan
                    if (!diamondsCaught.Contains(diamond))
                    {
                        return false;
                    }
                }
                //if not, then replan
                return true;
            }
        }

        ////check if there the current plan was invalidated and get the nodes that were affected
        private List<NodeMP> checkInvalidNodes()
        {
            //list of nodes to be returned
            List<NodeMP> invalidNodes = new List<NodeMP>();
            //info from the circle of the simulated state
            var circleX = 0.0f;
            var circleY = 0.0f;
            //check if the rectangle character is blocking the planned path only during the next few points
            List<PointMP> points = pathPlanMP.getPathPoints();
            for (int i = 0; i < Math.Min(VLODmaxPoints, points.Count); i++)
            {
                NodeMP node = points[i].getNode();
                //get the position of the circle of the simulated state
                circleX = node.getState().getPosX();
                circleY = node.getState().getPosY();
                //check if it is colliding the the current position of the rectangle
                if (checkCollision(rectangleInfo, circleX, circleY))
                {
                    //add node to the list of colliding nodes
                    invalidNodes.Add(node);
                }
                else
                {
                    float xLeft = rectangleInfo.X - utils.getRectangleWidth(rectangleInfo.Height) / 2;
                    float xRight = rectangleInfo.X + utils.getRectangleWidth(rectangleInfo.Height) / 2;
                    float yUp = rectangleInfo.Y + rectangleInfo.Height / 2;
                    float yDown = rectangleInfo.Y - rectangleInfo.Height / 2;

                    if (i == 0)
                    {
                        //up edge
                        if (utils.lineIntersection(circleInfo.X, circleInfo.Y, circleX, circleY, xLeft, yUp, xRight, yUp))
                        {
                            invalidNodes.Add(node);
                        }
                        //down edge
                        else if (utils.lineIntersection(circleInfo.X, circleInfo.Y, circleX, circleY, xLeft, yDown, xRight, yDown))
                        {
                            invalidNodes.Add(node);
                        }
                        //left edge
                        else if (utils.lineIntersection(circleInfo.X, circleInfo.Y, circleX, circleY, xLeft, yUp, xLeft, yDown))
                        {
                            invalidNodes.Add(node);
                        }
                        //right edge
                        else if (utils.lineIntersection(circleInfo.X, circleInfo.Y, circleX, circleY, xRight, yUp, xRight, yDown))
                        {
                            invalidNodes.Add(node);
                        }
                    }
                    else
                    {
                        NodeMP prevNode = points[i - 1].getNode();
                        float prevCircleX = prevNode.getState().getPosX();
                        float prevCircleY = prevNode.getState().getPosY();
                        if (points[i - 1].getAction() != Moves.JUMP)
                        {
                            //up edge
                            if (utils.lineIntersection(prevCircleX, prevCircleY, circleX, circleY, xLeft, yUp, xRight, yUp))
                            {
                                invalidNodes.Add(node);
                            }
                            //down edge
                            else if (utils.lineIntersection(prevCircleX, prevCircleY, circleX, circleY, xLeft, yDown, xRight, yDown))
                            {
                                invalidNodes.Add(node);
                            }
                            //left edge
                            else if (utils.lineIntersection(prevCircleX, prevCircleY, circleX, circleY, xLeft, yUp, xLeft, yDown))
                            {
                                invalidNodes.Add(node);
                            }
                            //right edge
                            else if (utils.lineIntersection(prevCircleX, prevCircleY, circleX, circleY, xRight, yUp, xRight, yDown))
                            {
                                invalidNodes.Add(node);
                            }
                        }


                    }

                }
            }
            //foreach(PointMP point in pathPlanMP.getPathPoints())
            //{
            //    NodeMP node = point.getNode();
            //    //get the position of the circle of the simulated state
            //    circleX = node.getState().getPosX();
            //    circleY = node.getState().getPosY();
            //    //check if it is colliding the the current position of the rectangle
            //    if (checkCollision(rectangleInfo, circleX, circleY))
            //    {
            //        //add node to the list of colliding nodes
            //        invalidNodes.Add(node);
            //    }

            //}
            return invalidNodes;

        }

        private void removeInvalidNodes(List<NodeMP> invalidNodes, TreeMP t)
        {
            List<NodeMP> nodesToRemove = new List<NodeMP>();
            List<NodeMP> childrenToRemove = new List<NodeMP>();
            NodeMP node;

            while (invalidNodes.Count != 0)
            {
                node = invalidNodes[0];
                invalidNodes.RemoveAt(0);
                NodeMP parent = node.getParent();

                //add to the list of nodes to remove from the list
                nodesToRemove.Add(node);

                //remove this node from its parent children
                parent.removeChild(node);

                //remove all children and children's children by adding them to a list and clearing the list of children
                childrenToRemove.AddRange(node.getChildren());
                node.removeChildren();
            }

            //remove all children
            while (childrenToRemove.Count != 0)
            {
                node = childrenToRemove[0];
                childrenToRemove.RemoveAt(0);
                //add children to remove
                childrenToRemove.AddRange(node.getChildren());
                //remove all chilren
                node.removeChildren();
                //add to remove from tree
                nodesToRemove.Add(node);
            }

            //remove all nodes to remove from tree
            t.removeNodes(nodesToRemove);
        }

        //check collision between the circle and the rectangle
        private bool checkCollision(RectangleRepresentation rectangle, float circleX, float circleY)
        {
            var circleDX = Math.Abs(circleX - rectangle.X);
            var circleDY = Math.Abs(circleY - rectangle.Y);
            var rectangleWidth = utils.getRectangleWidth(rectangle.Height);

            if ((circleDX > (rectangleWidth / 2 + circleInfo.Radius)) || (circleDY > (rectangle.Height / 2 + circleInfo.Radius)))
            {
                return false;
            }
            if ((circleDX <= (rectangleWidth / 2)) || (circleDY <= (rectangle.Height / 2)))
            {
                return true;
            }

            var cornerD = Math.Pow(circleDX - rectangleWidth / 2, 2) + Math.Pow(circleDY - rectangle.Height / 2, 2);

            if (cornerD <= Math.Pow(circleInfo.Radius, 2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

