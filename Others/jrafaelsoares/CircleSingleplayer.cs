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
    public class CircleSingleplayer
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
        private Tree T;
        private int iterationsS = 150;
        private int iterationsSControl = 1;

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
        private PathPlan pathPlan;
        private PathPlan originalPlan;
        private float pointTimeMargin = 10;
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
        private GoalType goalMode;

        //correction
        private bool correctRRT = false;
        private float correctVelYMargin = 20.0f;
        private float correctVelXMargin = 5.0f;
        private int VLODmaxPoints = 1;
        private PathPlan previousPlan;

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

        public CircleSingleplayer(bool cp, bool test, bool timr)
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
            obstaclesInfo = utils.joinObstacles(oI, cPI);
            rectanglePlatformsInfo = rPI;
            circlePlatformsInfo = cPI;
            collectiblesInfo = colI;
            uncaughtCollectibles = new List<CollectibleRepresentation>(collectiblesInfo);
            this.area = area;
            gSpeed = 1.0f;
            goalMode = GoalType.All;
            
            //setup level layout
            levelLayout = utils.getLevelLayout(obstaclesInfo, area);

            //calculates de area of the rectangle since only the info of the height is available
            rectangleArea = utils.setRectangleArea(rectangleInfo.Height);

            //gets the initial position of the circle to test is the game has started or is still at the menu
            previousCirclePosX = circleInfo.X;
            previousCirclePosY = circleInfo.Y;

            Platforms = utils.setupPlatforms(obstaclesInfo, rPI, cPI);
            Diamonds = utils.setupDiamonds(collectiblesInfo, levelLayout);

            /*************TESTS*************/
            //inform a level has started
            utils.writeStart(0);

            //search - state - original; action - original; no partial plans
            //RRT = new RRTUtils(actionTime, simTime, simTimeFinish, getPossibleMoves(), type, area, collectiblesInfo.Length, RRTTypes.Original, RRTTypes.Original, obstaclesInfo, gSpeed, Diamonds, Platforms, utils, false, false);
            //search - state - original; action - STP; biasSTP - 0.25/0.5/0.75; no partial plans
            //RRT = new RRTUtils(actionTime, simTime, simTimeFinish, getPossibleMoves(), type, area, collectiblesInfo.Length, RRTTypes.Original, RRTTypes.STP, obstaclesInfo, gSpeed, Diamonds, Platforms, utils, false, false);
            //search - state - bias - 0.25/0.50/0.75; action - STP; biasSTP - ??; no partial plans
            //RRT = new RRTUtils(actionTime, simTime, simTimeFinish, getPossibleMoves(), type, area, collectiblesInfo.Length, RRTTypes.Bias, RRTTypes.STP, obstaclesInfo, gSpeed, Diamonds, Platforms, utils, false, false);
            //search - state - bgt - 10/50/10; action - STP; biasSTP; no partial plans
            //RRT = new RRTUtils(actionTime, simTime, simTimeFinish, getPossibleMoves(), type, area, collectiblesInfo.Length, RRTTypes.BGT, RRTTypes.STP, obstaclesInfo, gSpeed, Diamonds, Platforms, utils, false, false);
            //search - state - bgt zoom; action - STP; biasSTP; no partial plans
            //RRT = new RRTUtils(actionTime, simTime, simTimeFinish, getPossibleMoves(), type, area, collectiblesInfo.Length, RRTTypes.BGTAreaBias, RRTTypes.STP, obstaclesInfo, gSpeed, Diamonds, Platforms, utils, false, false);
            //search - state - bgt bias; action - STP; biasSTP; no partial plans
            //RRT = new RRTUtils(actionTime, simTime, simTimeFinish, getPossibleMoves(), type, area, collectiblesInfo.Length, RRTTypes.BGTBias, RRTTypes.STP, obstaclesInfo, gSpeed, Diamonds, Platforms, utils, false, false);
            //search - state - zoom; action - STP; biasSTP; no partial plans
            //RRT = new RRTUtils(actionTime, simTime, simTimeFinish, getPossibleMoves(), type, area, collectiblesInfo.Length, RRTTypes.AreaBias, RRTTypes.STP, obstaclesInfo, gSpeed, Diamonds, Platforms, utils, false, false);

            /*************FINAL*************/
            RRT = new RRTUtils(actionTime, simTime, simTimeFinish, getPossibleMoves(), type, area, collectiblesInfo.Length, RRTTypes.BGT, RRTTypes.STP, obstaclesInfo, gSpeed, Diamonds, Platforms, utils, true, true);

            //RRT = new RRTUtils(actionTime, simTime, simTimeFinish, getPossibleMoves(), type, area, collectiblesInfo.Length, RRTTypes.BGT, RRTTypes.Original, obstaclesInfo, gSpeed, Diamonds, Platforms, utils, false, false);

            RRT.setRadius(circleInfo.Radius);
            pathPlan = new PathPlan(cutplan, colI.GetLength(0), utils);
            controller = new CircleController(gSpeed);

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
                    SinglePUpdate(elapsedGameTime);
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
        }

        public void SinglePUpdate(TimeSpan elapsedGameTime)
        {
            //Plan
            if (planRRT && predictor != null && predictor.CharactersReady())
            {
                controlling = false;
                planSolution();
            }
            else if (getDebugInfo)
            {
                if (cutplan)
                {
                    debugInfo = RRT.getDebugInfo(T, pathPlan.cleanPlan(obstaclesInfo, Diamonds, area, circleInfo.Radius, true, true)).ToArray();
                }
                else
                {
                    debugInfo = RRT.getDebugInfo(T, pathPlan.debugCleanPlan()).ToArray();
                }
                //debugInfo = RRT.getDebugInfo(T, null).ToArray();

                getDebugInfo = false;
            }
            else if (!getDebugInfo)
            {
                debugInfo = null;
            }

            //Control - if there is a plan then execute it
            if (pathPlan.getPathPoints() != null && pathPlan.getPathPoints().Count != 0 && hasStarted)
            {
                controlling = true;
                planExecution();
            }
            else if (pathPlan.getTotalCollectibles() != 0 && controlling && pathPlan.getTotalCollectibles() == uncaughtCollectibles.Count)
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
            else if (pathPlan.getPathPoints().Count == 0 && !controlling && !planRRT)
            {
                planRRT = true;
                newPlan = true;
            }
        }

        //implements abstract circle interface: gets the debug information that is to be visually represented by the agents manager
        public DebugInformation[] GetDebugInformation()
        {
            return debugInfo;
        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                     PLANNING                                         ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        private void planSolution()
        {
            //The agent must be still so it starts at the same position as the one in the first point of the plan
            //This is to guarantee that the agent stops before start planning, and keeps still
            if (circleInfo.VelocityY < correctVelYMargin && circleInfo.VelocityY > -correctVelYMargin &&
                circleInfo.VelocityX < correctVelXMargin && circleInfo.VelocityX > -correctVelXMargin &&
                utils.onPlatform(circleInfo.X, circleInfo.Y + circleInfo.Radius, 25, 10) != null)
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

                    //Simulator
                    Simulator sim = new CircleSimulator(Platforms);
                    sim.setSimulator(circleInfo.X, circleInfo.Y, circleInfo.VelocityX, circleInfo.VelocityY, remainingDiamonds);

                    //update the diamond list
                    RRT.setDiamonds(Diamonds);
                    //create initial state
                    State initialState = new State(circleInfo.X, circleInfo.Y, circleInfo.VelocityX, circleInfo.VelocityY, circleInfo.Radius, circleInfo.Radius, caughtDiamonds, remainingDiamonds);
                    //run algorithm
                    T = RRT.buildNewRRT(initialState, sim, iterationsS);
                }
                else //continue the previous tree
                {
                    T = RRT.RRT(T);
                }
                //draw the nodes of the tree
                if (debugTree)
                {
                    debugInfo = RRT.getDebugTreeInfo(T).ToArray();
                }

                newPlan = false;

                //if the argorithm reached a goal state or a semi plan then get the plan
                if (T.getGoal() != null)
                {
                    if (!written)
                    {
                        int exploredNodesOnce = RRT.getExploredNodesOnce();
                        int exploredNodesTotal = RRT.getExploredNodesTotal();
                        int totalNodes = T.getNodes().Count;
                        utils.writeTimeToFile(1, 0, searchTime, exploredNodesOnce, exploredNodesTotal, totalNodes, gSpeed);
                        written = true;
                    }

                    pathPlan = RRT.getPlan(T);

                    firstAction = true;
                    lastMove = Moves.NO_ACTION;


                    //do not plan on the next iteration
                    planRRT = false;
                    getDebugInfo = true;

                    //save a copy of the original plan
                    if (cutplan)
                    {
                        pathPlan.cleanPlan(obstaclesInfo, Diamonds, area, circleInfo.Radius, true, true);
                    }
                    originalPlan = pathPlan.clone();
                    pathPlan.saveOriginal();
                }
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
        //join plans on utils

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
                if (pathPoints[i].getUncaughtDiamonds().Count >= uncaughtCollectibles.Count)
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
                return;
            }
            //if no platform in common was found, then try to find a way to a platform in the plan and replan if this fails
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
                sim.setSimulator(circleInfo.X, circleInfo.Y, circleInfo.VelocityX, circleInfo.VelocityY, remainingDiamonds);

                //TEST
                RRT.setDiamonds(Diamonds);

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
                    pathPlan.cleanPlan(obstaclesInfo, remainingDiamonds, area, circleInfo.Radius, true, true);
                    getDebugInfo = true;
                    return;
                }
                else
                {
                    replan(false);
                }
            }
            replan(false);
        }

        private void replan(bool correct)
        {
            //if correct is true, it means the agent should try to first recover the previous plan
            //but it should also be careful not to repeat the same failures and should understand when it completed its plan
            
            //Since search is so fast, removed recovering due to faulty plans (e.g bad physics simulation on a platform
            /*
            if (correct && !pathPlan.checkIfConstantFail() && !checkPlanCompletion())
            {
                recoverPlan();
            }
            else
            {
            */
                currentAction = Moves.NO_ACTION;
                planRRT = true;
                newPlan = true;
                controller.jumpReached = false;
                controller.rollReached = false;
                previousPlan = pathPlan;
                pathPlan = new PathPlan(cutplan, remaining.Count, utils);
            //}
        }

        private bool checkPlanCompletion()
        {
            if (uncaughtCollectibles.Count == pathPlan.getTotalCollectibles())
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
            else if (uncaughtCollectibles.Count < pathPlan.getPathPoints()[0].getUncaughtDiamonds().Count && pathPlan.getPathPoints().Count > 1 && pathPlan.getPathPoints()[1].getUncaughtDiamonds().Count != uncaughtCollectibles.Count)
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

        private void checkIfGameStarted()
        {
            //checks if the game has started 
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
        /***                                       MOVES                                          ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/
        private List<Moves> getPossibleMoves()
        {
            List<Moves> moves = new List<Moves>();

            moves = new List<Moves>();
            moves.Add(Moves.ROLL_LEFT);
            moves.Add(Moves.ROLL_RIGHT);
            moves.Add(Moves.JUMP);

            return moves;
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

       
    }
}

