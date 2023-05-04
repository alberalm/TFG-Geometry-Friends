using GeometryFriends;
using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Communication;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace GeometryFriendsAgents
{
    //TODO - corrigir a sincronização do plano
    public class RectangleAgent : AbstractRectangleAgent
    {
        //agent implementation specificiation
        private bool implementedAgent;
        private string agentName = "RRTRectangle2017";

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
        //plan
        private int remainingActions;
        //if a new plan is needed
        private bool planRRT = true;
        private bool newPlan = true;
        //simulator
        //time of simulation
        private float actionTime = 2.1f;
        private float actionTimeMargin = 0.5f;
        private float simTime = 2.0f;
        private float simTimeFinish = 0.1f;
        //control
        private RectangleController controller;
        private PathPlan pathPlan;
        private PathPlan originalPlan;
        private float pointTimeMargin = 10;
        private Moves lastMove = Moves.NO_ACTION;
        private bool controlling = false;
        private Stopwatch timestep;

        private float acceleration;
        private float velVar = 0;
        private float previousVelX = 0;
        private float previousAcceleration = 0;
        //rectangle info - area
        private float rectangleArea;
        //rectangle info
        private float previousRectanglePosX;
        private float previousRectanglePosY;
        private float maxHeight = 192.3077f;
        private float minHeight = 51.30769f;
        private int type = 1;  //0 -> circle  1 -> rectangle
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

        //correction
        private bool correctRRT = false;
        private float correctVelYMargin = 20.0f;
        private float correctVelXMargin = 1.0f;
        private PathPlan previousPlan;

        //Area of the game screen
        private Rectangle area;

        //game ended
        private bool gameOver = false;

        //debug
        private float rectangleMaxHeight;
        private float rectangleInitialY;
        private bool initialY = true;

        //for tests
        private bool cutplan;
        private bool written;
        private bool testing = true;
        private bool timer = true;
        private int i = 0;

        public RectangleAgent()
        {
            //Change flag if agent is not to be used
            implementedAgent = true;

            currentAction = Moves.NO_ACTION;
            rnd = new Random();

            //prepare the possible moves  
            possibleMoves = new List<Moves>();
            possibleMoves.Add(Moves.MOVE_LEFT);
            possibleMoves.Add(Moves.MOVE_RIGHT);
            possibleMoves.Add(Moves.MORPH_UP);
            possibleMoves.Add(Moves.MORPH_DOWN);
            // possibleMoves.Add(Moves.NO_ACTION);

            //history keeping
            uncaughtCollectibles = new List<CollectibleRepresentation>();
            caughtCollectibles = new List<CollectibleRepresentation>();
            remaining = new List<CollectibleRepresentation>();

        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                     MAIN SETUP                                       ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

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
            uncaughtCollectibles = new List<CollectibleRepresentation>(collectiblesInfo);
            this.area = area;
            gSpeed = 1.0f;

            //setup level layout
            levelLayout = new int[area.Right, area.Bottom];
            getLevelLayout();

            //calculates de area of the rectangle since only the info of the height is available
            setRectangleArea(rectangleInfo.Height);

            //gets the initial position of the Rectangle to test is the game has started or is still at the menu
            previousRectanglePosX = rectangleInfo.X;
            previousRectanglePosY = rectangleInfo.Y;

            ground = new Platform(0, area.Bottom, 0, 0);
            Platforms = new List<Platform>();
            setupPlatforms();

            utils = new Utils(Platforms, ground, rectangleInfo.Height / 2);

            setupDiamonds();

            /*************FINAL*************/
            RRT = new RRTUtils(actionTime, simTime, simTimeFinish, getPossibleMoves(), type, area, collectiblesInfo.Length, RRTTypes.Bias, RRTTypes.STP, obstaclesInfo, gSpeed, Diamonds, Platforms, utils, true, true);
            cutplan = false;

            RRT.setRadius(rectangleInfo.Height / 2);
            pathPlan = new PathPlan(cutplan);
            controller = new RectangleController(gSpeed);

            //tests
            written = false;
            //testActions = new List<Moves>();
            //for(int i = 0; i < 100; i++)
            //{
            //    testActions.Add(Moves.MORPH_UP);
            //}
            //for (int i = 0; i < 100; i++)
            //{
            //    testActions.Add(Moves.MORPH_DOWN);
            //}
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

        //implements abstract rectangle interface: GeometryFriends agents manager gets the current action intended to be actuated in the enviroment for this agent
        public override Moves GetAction()
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

        //implements abstract rectangle interface: updates the agent state logic and predictions
        public override void Update(TimeSpan elapsedGameTime)
        {
            //if(testActions.Count > 0)
            //{
            //    writeHeight(rectangleInfo.Height);
            //    currentAction = testActions[0];
            //    testActions.RemoveAt(0);
            //}

            //if(i < 100)
            //{
            //    currentAction = Moves.MORPH_UP;
            //    i++;
            //}
            //else
            //{
            //    currentAction = Moves.MOVE_RIGHT;
            //}

            //writeHeight(rectangleInfo.Height);

            checkIfGameStarted();

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
                    debugInfo = RRT.getDebugInfo(T, pathPlan.cleanPlan(obstaclesInfo, Diamonds, area, rectangleInfo.Height / 2, true)).ToArray();
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
            if (pathPlan.getPathPoints() != null && pathPlan.getPathPoints().Count != 0 && hasStarted && !gameOver)
            {
                controlling = true;
                planExecution();
            }
            else if (pathPlan.getTotalCollectibles() != 0 && /*pathPlan.getPathPoints().Count != 0 &&*/ pathPlan.getTotalCollectibles() == uncaughtCollectibles.Count)
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
            if (uncaughtCollectibles.Count == 0)
            {
                writeTimeToFile(2);
            }
        }

        public override DebugInformation[] GetDebugInformation()
        {
            return debugInfo;
        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                       SETUP                                          ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        //Organize the diamonds by height and platform
        private void setupDiamonds()
        {
            Diamonds = new List<DiamondInfo>();
            Platform platform;
            int i;

            foreach (CollectibleRepresentation diamond in collectiblesInfo)
            {
                //create diamond info
                platform = utils.getDiamondPlatform(diamond.X, diamond.Y);

                //the levelLayout is used to calculate the areas of bias of the diamond
                DiamondInfo diamondInfo = new DiamondInfo(diamond.X, diamond.Y, platform, levelLayout);

                //add diamond to the list according to its position and platform - the highest diamonds on the highest platforms come first
                i = 0;
                if (Diamonds.Count == 0)
                {
                    Diamonds.Add(diamondInfo);
                }
                else
                {
                    foreach (DiamondInfo dInfo in Diamonds)
                    {
                        //if the diamond belongs to a higher platform
                        if (diamondInfo.getPlatform().getY() < dInfo.getPlatform().getY())
                        {
                            Diamonds.Insert(i, diamondInfo);
                            break;
                        }
                        else if (diamondInfo.getPlatform().getY() == dInfo.getPlatform().getY())
                        {
                            //if the diamond is in a platform at the same height but is higher than the other
                            if (diamondInfo.getY() < dInfo.getY())
                            {
                                Diamonds.Insert(i, diamondInfo);
                                break;
                            }
                        }
                        //if it is at the end of the list, then it is the lowest 
                        if (i == Diamonds.Count - 1)
                        {
                            Diamonds.Add(diamondInfo);
                            break;
                        }
                        i++;
                    }
                }
            }
            //give the diamonds their respective id
            for (i = 0; i < Diamonds.Count; i++)
            {
                Diamonds[i].setId(i);
                Diamonds[i].getPlatform().addDiamondOn(i);
            }
        }

        private void setupPlatforms()
        {
            foreach (ObstacleRepresentation platform in obstaclesInfo)
            {
                Platforms.Add(new Platform(platform.X, platform.Y, platform.Width, platform.Height));
            }
            Platforms.Add(ground);
        }

        //create a layout where the platforms are 1 and the other areas are 0
        private void getLevelLayout()
        {
            //platform boundaries
            int lX, rX, tY, bY;

            foreach (ObstacleRepresentation platform in obstaclesInfo)
            {
                lX = (int)Math.Round(platform.X - platform.Width / 2);
                rX = (int)Math.Round(platform.X + platform.Width / 2);
                tY = (int)Math.Round(platform.Y - platform.Height / 2);
                bY = (int)Math.Round(platform.Y + platform.Height / 2);

                //assign the value 1 to each position that containts a platform
                for (int i = lX; i <= rX; i++)
                {
                    for (int j = tY; j <= bY; j++)
                    {
                        if (i < levelLayout.GetLength(0) && j < levelLayout.GetLength(1))
                            levelLayout[i, j] = 1;
                    }
                }
            }
        }

        private void setRectangleArea(float height)
        {
            rectangleArea = height * height;
        }

        private float getRectangleWidth(float height)
        {
            return rectangleArea / height;
        }

        private List<Moves> getPossibleMoves()
        {
            List<Moves> moves = new List<Moves>();

            moves = new List<Moves>();
            moves.Add(Moves.MOVE_LEFT);
            moves.Add(Moves.MOVE_RIGHT);
            moves.Add(Moves.MORPH_UP);
            moves.Add(Moves.MORPH_DOWN);
            // moves.Add(Moves.NO_ACTION);

            return moves;
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
            if (rectangleInfo.VelocityY < correctVelYMargin && rectangleInfo.VelocityY > -correctVelYMargin &&
                rectangleInfo.VelocityX < correctVelXMargin && rectangleInfo.VelocityX > -correctVelXMargin)
            {
                //make sure there is nothing moving the agent when planning
                currentAction = Moves.NO_ACTION;

                //if the plan is new build a new tree
                if (newPlan)
                {
                    //update the diamond list
                    RRT.setDiamonds(Diamonds);
                    //create initial state
                    State initialState = new State(rectangleInfo.X, rectangleInfo.Y, rectangleInfo.VelocityX, rectangleInfo.VelocityY, rectangleInfo.Height / 2, 0, caughtCollectibles, uncaughtCollectibles);
                    //run algorithm
                    T = RRT.buildNewRRT(initialState, predictor);
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
                        writeTimeToFile(1);
                    }

                    pathPlan = RRT.getPlan(T);

                    firstAction = true;

                    //do not plan on the next iteration
                    planRRT = false;
                    getDebugInfo = true;

                    //save a copy of the original plan
                    originalPlan = pathPlan.clone();
                    pathPlan.saveOriginal();
                }
            }
            else
            {
                keepStill();
            }
        }
        /***********************************/

        /*******************************************************/
        /*                  Planning - aux                     */
        /*******************************************************/

        //used when trying to recover a plan where the agent is not on a platform and state of the plan
        private PathPlan joinPlans(PathPlan correction, PathPlan original, Point connectionPoint)
        {
            List<Point> points = original.getPathPoints();
            int i;
            for (i = 0; i < points.Count; i++)
            {
                if (points[i].getPosX() == connectionPoint.getPosX() && points[i].getPosY() == connectionPoint.getPosY() &&
                    points[i].getUncaughtColl().Count == connectionPoint.getUncaughtColl().Count)
                {
                    break;
                }
            }
            for (int j = i; j < points.Count; j++)
            {
                correction.addPoint(points[j]);
            }

            return correction;
        }

        //simple plan recovery - check if agent is on a platform that belongs to the plan and with the same state
        private void recoverPlan()
        {
            //TODO - update debug drawing
            currentAction = Moves.NO_ACTION;
            controller.morphReached = false;
            controller.slideReached = false;
            controller.resetPID();

            //see if there is a point in the original plan that is the same as the one the agent is on and that has the same number or less of diamonds caught
            bool pointFound = false;

            Platform currentPlatform = pathPlan.onPlatform(rectangleInfo.X, rectangleInfo.Y + rectangleInfo.Height / 2, obstaclesInfo, area);
            List<Point> pathPoints = pathPlan.getOriginalPoints();

            int i;
            for (i = 0; i < pathPoints.Count; i++)
            {
                if (pathPoints[i].getUncaughtColl().Count >= uncaughtCollectibles.Count)
                {
                    Platform pointPlatform = pathPlan.onPlatform(pathPoints[i].getPosX(), pathPoints[i].getPosY() + rectangleInfo.Height / 2, obstaclesInfo, area);

                    if (utils.samePlatform(currentPlatform, pointPlatform) && !utils.obstacleBetween(rectangleInfo.X, pathPoints[i].getPosX(), currentPlatform))
                    {
                        pointFound = true;
                        break;
                    }
                }
            }

            if (pointFound)
            {
                //create a new plan from the point we got previously
                pathPlan = new PathPlan(cutplan);
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
            else
            {
                replan(false);
            }

            //TODO - make a more complex plan recover for when this one fails
            //TODO - be careful for some plans the agent might repeat the error over and over and should be able to replan if that is the case
        }

        private void replan(bool correct)
        {
            //TODO - make a new version for this
            correct = false;
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
                controller.slideReached = false;
                controller.morphReached = false;
                controller.resetPID();
                previousPlan = pathPlan;
                pathPlan = new PathPlan(cutplan);
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
            if (timer && (gameTime.ElapsedMilliseconds * 0.001f * gSpeed > pointTimeMargin * gSpeed))
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
            acceleration = getAcceleration();
            var timeStep = timestep.ElapsedMilliseconds * 0.001f;
            if(timeStep < 0.001f)
            {
                timeStep = 0.001f;
            }
            currentAction = controller.computeAction(pathPlan.getPathPoints()[0], rectangleInfo.VelocityX, rectangleInfo.VelocityY, rectangleInfo.X, rectangleInfo.Y, timeStep, acceleration);
            timestep.Restart();

            //change to next point when a point is reached or when a jump was performed
            if (!correctRRT && (controller.slideReached || controller.morphReached))
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

                controller.slideReached = false;
                controller.morphReached = false;
                controller.resetPID();
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
            currentAction = Moves.NO_ACTION;
            ////if it is moving right, move left
            //if (rectangleInfo.VelocityX > correctVelXMargin)
            //{
            //    currentAction = Moves.MOVE_LEFT;
            //}
            ////if it is moving left, move right
            //else if (rectangleInfo.VelocityX < -correctVelXMargin)
            //{
            //    currentAction = Moves.MOVE_RIGHT;
            //}
        }

        /*******************************************************/
        /*                  control - aux                      */
        /*******************************************************/
        private float getAcceleration()
        {
            velVar = rectangleInfo.VelocityX - previousVelX;
            var timeStep = timestep.ElapsedMilliseconds * 0.001f;
            if (timeStep < 0.1f)
            {
                timeStep = 0.001f;
            }
                var tempA = velVar / timeStep;

            return tempA;
        }

        private bool checkActionFailure()
        {
            Platform currentPlatform = pathPlan.onPlatform(rectangleInfo.X, rectangleInfo.Y + rectangleInfo.Height / 2, rectangleInfo.Y + getRectangleWidth(rectangleInfo.Height) /2, obstaclesInfo, area);
            Platform nextPlatform = pathPlan.onPlatform(pathPlan.getPathPoints()[0].getPosX(), pathPlan.getPathPoints()[0].getPosY() + rectangleInfo.Height / 2, pathPlan.getPathPoints()[0].getPosY() + getRectangleWidth(pathPlan.getPathPoints()[0].getHeight()) / 2, obstaclesInfo, area);

            //check if agent is below the platform it should be - won't work 100% of the times
            if (currentPlatform != null && nextPlatform != null && currentPlatform.getY() > nextPlatform.getY() &&
                Math.Abs(rectangleInfo.Y - (nextPlatform.getY() - nextPlatform.getHeight()/2)) > Math.Max(rectangleInfo.Height, getRectangleWidth(rectangleInfo.Height)))
            {
                return true;
            }

            //check if the platform of the agent is right above the next point in the plan
            //float pointX = pathPlan.getPathPoints()[0].getPosX();

            //if (currentPlatform != null && nextPlatform != null && currentPlatform.getY() < nextPlatform.getY() &&
            //    pointX > currentPlatform.getX() - currentPlatform.getWidth() / 2 &&
            //    pointX < currentPlatform.getX() + currentPlatform.getWidth() / 2)
            //{
            //    return true;
            //}

            return false;
        }


        private void lastActionReplan()
        {
            //TODO - verificar se completou ou não o plano que era suposto
            //if the state isn't the same but it has already passed more time than it should to take the action, apply plan recovery
            if ((gameTime.ElapsedMilliseconds * 0.001f * gSpeed > (simTime + actionTimeMargin) * gSpeed) && !correctRRT)
            {
                lastAction = false;

                currentAction = Moves.NO_ACTION;
                replan(true);
            }
        }

        private void checkIfGameStarted()
        {
            //checks if the game has started 
            //TODO - make it deal with pauses yet
            if (!hasStarted)
            {
                hasStarted = true;
                searchTime = new Stopwatch();
                searchTime.Start();
            }
        }

        /********************************************************************************************/
        /********************************************************************************************/
        /***                                                                                      ***/
        /***                                      DEBUG                                           ***/
        /***                                                                                      ***/
        /********************************************************************************************/
        /********************************************************************************************/

        //For tests:  1- search; 2- completion
        public void writeTimeToFile(int type)
        {
            if (testing)
            {
                float timeTaken = searchTime.ElapsedMilliseconds * 0.001f * gSpeed;
                string timeText = timeTaken.ToString();

                if (type == 1 && !written)
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@".\searchtime.txt", true))
                    {
                        file.WriteLine(timeText);
                    }
                    written = true;
                }
                else if (type == 2)
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@".\completiontime.txt", true))
                    {
                        file.WriteLine(timeText);
                    }
                }
            }

        }

        public void writeHeight(float height)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@".\rectangleHeight.txt", true))
            {
                file.WriteLine(height);
            }
        }

    }
}