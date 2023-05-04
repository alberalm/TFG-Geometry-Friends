using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace GeometryFriendsAgents
{
    public class RRTUtilsMP
    {
        /******test variables******/
        private bool semiplanTest;
        private bool cutplan;
        private bool bgt;
        private int mu = 300;
        private float bias = 0.25f; //TODO test
        /******Algorithm variables******/
        private float actionTime = 1.0f;
        private float simTime;
        private float simTimeFinish;
        private float radius = 40;
        private float jumpBias = 0.3f; //TODO test
        private float samePlatformTime = 1.0f;
        private float samePlatformTimeInc = 1.0f;
        private float maxTime = 20.0f;
        private float smallPlatformTime = 0.1f;
        private float mediumPlatformTime = 1.0f;
        private float largePlatformTime = 2.0f;
        private float defaultActionTime;
        private List<Moves[]> possibleMoves;
        private int smallPlatformWidth = 100;
        private int largePlatformWidth = 400;
        private int iterations;
        private int rrtIterations = 1;
        private int correctionIterations = 1000;
        private int matrixSize;
        private int matrixDec;
        private int initialMatrixSize = 20;
        private int initialMatrixDec = 5;
        private NodeMP[,,,,] positions;
        private bool goal = false;
        private bool semiPlan = false;
        private bool samePlatformPlan = false;
        private RRTTypes stateSelection;
        private RRTTypes actionSelection;
        private ObstacleRepresentation[] platforms;
        private Stopwatch time;
        private Random rnd;
        private GoalType goalType = GoalType.All;
        private float[] returnPosition;
        private List<DiamondInfo> ignoreDiamonds;

        /******STP variables ******/
        private float skillYmargin = 70;
        private float skillXmargin = 70;
        private float skillVelMargin = 10;
        private float skillJumpBias = 0.5f;
        private float stpBias = 0.3f;

        /******Plan variables******/
        private List<Moves[]> plan = new List<Moves[]>();
        private List<NodeMP> planNodes = new List<NodeMP>();
        private float marginMinX = 1.0f;
        private float marginMinY = 1.0f;
        private float marginMinVX = 20.0f;
        private float marginMinVY = 20.0f;
        private float marginDenominator = 15.0f;

        /******correct plan variables******/
        private bool correction = false;
        private bool newPlan = false;
        private bool samePlatform;
        private bool replan = false;
        private float correctionPosMargin = 5.0f;
        private float correctionVelMargin = 20.0f;
        private NodeMP connectNode;
        private NodeMP bestSemiPlanNode;
        private PathPlanMP previousPoints;
        private PointMP connectionPoint;
        private List<Platform> previousPlatforms;
        private List<NodeMP> previousTreeNodes;
        private NodeMP[,,,,] previousPositions;

        /******debug variables******/
        private List<DebugInformation> debugInfo;
        private bool debugTree = true;

        /******level variables ******/
        private int charType; //0 -> circle; 1 -> rectangle
        private int totalCollectibles;
        private float circleMaxJump = 400;
        private float rectangleMaxHeight = 193;
        private float gSpeed = 1;
        private float initialPosX;
        private float initialPosY;
        private float diamondRadius = 30;
        private bool multiplayer = false;
        private Platform initialPlatform;
        private List<DiamondInfo> diamondsInfo;
        private List<Platform> platformsInfo;
        private ActionSimulator simulator;
        private Rectangle area;
        private Utils utils;

        /******circle and rectangle STP******/
        private CircleSTPMP circleSTP;
        private RectangleSTPMP rectangleSTP;

        public RRTUtilsMP(float aTime, float sTime, float sTimeFinish, List<Moves[]> pMoves, int cType, Rectangle levelArea, int totalColl, RRTTypes sSelect, RRTTypes aSelect, ObstacleRepresentation[] plt, float gameSpeed, List<DiamondInfo> dmds, List<Platform> plfms, Utils utls, bool sp, bool c)
        {
            actionTime = aTime;
            defaultActionTime = aTime;
            simTime = sTime;
            simTimeFinish = sTimeFinish;
            possibleMoves = pMoves;
            charType = cType;
            area = levelArea;
            totalCollectibles = totalColl;
            stateSelection = sSelect;
            actionSelection = aSelect;
            platforms = plt;
            time = new Stopwatch();
            time.Start();
            gSpeed = gameSpeed;
            diamondsInfo = dmds;
            platformsInfo = plfms;
            rnd = new Random();
            debugInfo = new List<DebugInformation>();
            samePlatform = false;
            utils = utls;
            circleSTP = new CircleSTPMP(utils, rnd, platforms, area, radius);
            rectangleSTP = new RectangleSTPMP(utils, rnd, platforms, area, radius);
            ignoreDiamonds = new List<DiamondInfo>();

            //for tests
            semiplanTest = sp;
            cutplan = c;
            if (sSelect == RRTTypes.BGT || sSelect == RRTTypes.BGTBias || sSelect == RRTTypes.BGTAreaBias)
            {
                bgt = true;
            }
            else
            {
                bgt = false;
            }
        }

        /*******************************************************/
        /*                  RRT funtions                       */
        /*******************************************************/

        //Builds a new tree from scratch
        public TreeMP buildNewRRT(StateMP initialState, ActionSimulator predictor, int it)
        {
            goal = false;
            simulator = predictor;
            time.Restart();

            initialPosX = initialState.getPosX();
            initialPosY = initialState.getPosY();
            initialPlatform = utils.platformBelow(initialPosX, initialPosY);
            iterations = it;
            matrixSize = initialMatrixSize;
            matrixDec = initialMatrixDec;

            foreach (DiamondInfo diamond in diamondsInfo)
            {
                diamond.resetStates();
            }

            return buildRRT(initialState, simulator);
        }

        public TreeMP buildNewMPRRT(StateMP initialState, ActionSimulator predictor, GoalType gType, int it)
        {
            //TODO / CAUTION - when using coop tree, do not call buildnewrrt
            goalType = gType;
            multiplayer = true;
            iterations = it;
            return buildNewRRT(initialState, predictor, it);
        }

        //RRT algorithm
        public TreeMP buildRRT(StateMP initialState, ActionSimulator predictor)
        {
            //initialize tree
            goal = false;
            positions = new NodeMP[area.Right / matrixSize, area.Bottom / matrixSize, area.Right / matrixSize, area.Bottom / matrixSize, totalCollectibles + 1];
            TreeMP currentTree = new TreeMP(initialState, predictor, copyMoves(), bgt);
            simulator = predictor;
            return RRT(currentTree);
        }

        public TreeMP RRT(TreeMP currentTree)
        {
            NodeMP randNode;
            //tree with a maximum number (iterations) of branches 
            for (int i = 1; i <= iterations; i++)
            {
                //if it has already passed too much time and there is a state with caught diamonds, then start the plan from there
                if (semiplanTest && samePlatformTime < maxTime)
                {
                    if ((time.ElapsedMilliseconds * 0.001f * gSpeed > samePlatformTime && bestSemiPlanNode != null && samePlatformPlan && !correction))
                    {
                        currentTree.setGoal(bestSemiPlanNode);
                        bestSemiPlanNode = null;
                        samePlatformPlan = false;
                        return currentTree;
                    }
                }
                if (semiplanTest && time.ElapsedMilliseconds * 0.001f * gSpeed > maxTime && bestSemiPlanNode != null && !correction)
                {
                    currentTree.setGoal(bestSemiPlanNode);
                    bestSemiPlanNode = null;
                    return currentTree;
                }

                //if there are no more open nodes, start new tree with new position matrix
                while (currentTree.getOpenNodes().Count == 0)
                {

                    if (matrixSize > 15)
                    {
                        matrixSize -= 3;
                    }
                    else if (matrixSize > 10)
                    {
                        matrixSize -= 2;
                    }
                    else if (matrixSize <= 10)
                    {
                        matrixSize--;
                    }
                    if (matrixSize < 1)
                    {
                        matrixSize = 1;
                    }
                    currentTree = repopulateMatrix(currentTree);

                    goal = false;
                }

                //select a random node according to selection type
                randNode = getNextNode(currentTree);
                extend(currentTree, randNode);
                if (goal)
                {
                    correction = false;
                    break;
                }
            }

            return currentTree;
        }

        private void extend(TreeMP tree, NodeMP node)
        {
            Moves[] newAction;
            //selecting a random action
            switch (actionSelection)
            {
                case RRTTypes.Original:
                    newAction = randomAction(node);
                    break;
                case RRTTypes.STP:
                    if (goalType == GoalType.Return)
                    {
                        newAction = randomAction(node);
                    }
                    else
                    {
                        newAction = STPAction(node);
                    }
                    break;
                default:
                    newAction = randomAction(node);
                    break;
            }

            node.removeMove(newAction);

            //If the STP returned no action then all the actions of the skill where exhausted and the bias indicated to ignore this state
            if (actionSelection == RRTTypes.STP && newAction[0] == Moves.NO_ACTION && newAction[1] == Moves.NO_ACTION)
            {
                return;
            }

            //if this is the last action to test on this node then close it
            if (node.possibleMovesCount() == 0)
            {
                tree.closeNode(node);
            }

            StateMP selectedState = node.getState();
            ActionSimulator selectedSim = node.getPredictor().CreateUpdatedSimulator();

            //simulate action applied to the selected state
            StateMP newState = applyPrediction(selectedState, node, newAction, selectedSim);

            //check if the state already exists - if it does, do not put it in the tree
            if (repeatedState(newState))
            {
                return;
            }

            //if replanning, check if there is a node in the bin of old nodes
            if(replan) 
            {
                NodeMP oldNode;
                NodeMP auxNode = findNode(newState.getPosX(), newState.getPosY(), newState.getPartnerX(), newState.getPartnerY(), newState.getUncaughtCollectibles().Count, true);
                if(auxNode != null)
                {
                    oldNode = auxNode.clone();
                    if (bgt)
                    {
                        //TODO - CAREFUL WITH THE NODES IN THE AREA LISTS
                    }
                    //make sure the old node has the current pair of actions instead of the old
                    oldNode.setAction(newAction);
                    node.addChild(oldNode);
                    tree.addNodesIterative(oldNode, true);
                    addNodesPositions(oldNode);
                    repopulateMatrix(tree);

                    //see if the node is close to any diamond
                    foreach (DiamondInfo diamond in diamondsInfo)
                    {
                        diamond.insertClosestNodesMP(oldNode);
                    }

                    return;
                }
            }

            List<Moves[]> newMoves;
            //check if circle agent is on a platform or middle jump/fall
            if (charType == 0)
            {
                if (utils.onPlatform(newState.getPosX(), newState.getPosY() + radius, 0, 10) != null)
                {
                    newMoves = copyMoves();
                }
                else
                {
                    newMoves = copyMovesNoJump();
                }
            }
            else
            {
                newMoves = copyMoves();
            }

            //add node to the tree
            NodeMP newNode = new NodeMP(node, newState, newAction, selectedSim, newMoves);
            node.addChild(newNode);
            tree.addNode(newNode);
            addNodePositions(newNode);

            //see if the node is close to any diamond
            foreach (DiamondInfo diamond in diamondsInfo)
            {
                diamond.insertClosestNodeMP(newNode);
            }

            //check if the new state is a goal
            //TODO - make the goal verification in a different method to be easier to indicate what is the goal to be considered
            //if it is a plan correction, check if the agent is in a platform of the previous plan with the same caught/uncaught collectibles
            if (correction)
            {
                Platform currentPlatform = utils.onPlatform(newState.getPosX(), newState.getPosY(), 50, 10);
                if (currentPlatform != null)
                {
                    List<PointMP> points = previousPoints.getPathPoints();
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (newState.getUncaughtCollectibles().Count == points[i].getUncaughtColl().Count)
                        {
                            bool samePlatformState = true;
                            //check is the platform is the same
                            if (currentPlatform.getX() != previousPlatforms[i].getX() || currentPlatform.getY() != previousPlatforms[i].getY())
                            {
                                samePlatformState = false;
                            }
                            //check if the diamonds match
                            if (samePlatformState)
                            {
                                foreach (CollectibleRepresentation diamond in newState.getUncaughtCollectibles())
                                {
                                    if (!points[i].getUncaughtColl().Contains(diamond))
                                    {
                                        samePlatformState = false;
                                        break;
                                    }
                                }
                            }
                            //if the platform is the same as well as the collectibles then a goal has been reached
                            if (samePlatformState)
                            {
                                tree.setGoal(newNode);
                                goal = true;
                                newPlan = true;
                                connectionPoint = points[i];
                                break;
                            }
                        }
                    }

                }

            }
            //if all collectibles where caught
            if (newState.getNumberUncaughtCollectibles() == 0)
            {
                //inform the Tree it has reached a goal state
                tree.setGoal(newNode);
                goal = true;
                newPlan = true;
                //make sure that if a new plan was found when searching for a recovery one, that the new plan is followed
                correction = false;
            }
            else if (semiplanTest && newState.getNumberUncaughtCollectibles() < node.getState().getNumberUncaughtCollectibles())
            {
                checkSemiPlan(tree, newNode);
            }

            //Update the node's debug information
            newNode.addDebugInfo(debugInfo);
        }

        private bool onSamePlatform(float x, Platform platform, List<CollectibleRepresentation> uCol, bool reverse)
        {
            if (platform == null)
            {
                return false;
            }
            //1 - caught; 0 - uncaught; 2 - previously caught -> do not count
            int[] cCol = getCaughtDiamondsIndexes(uCol);

            for (int i = 0; i < cCol.Length; i++)
            {
                if (reverse && cCol[i] == 1 && utils.samePlatform(platform, diamondsInfo[i].getPlatform()) &&
                   !utils.obstacleBetween(diamondsInfo[i].getX(), diamondsInfo[i].getX(), platform))
                {
                    return true;
                }
                else if (cCol[i] == 0 && utils.samePlatform(platform, diamondsInfo[i].getPlatform()) &&
                         !utils.obstacleBetween(x, diamondsInfo[i].getX(), platform))
                {
                    return true;
                }
            }
            return false;
        }

        private StateMP applyPrediction(StateMP selectedState, NodeMP node, Moves[] newAction, ActionSimulator stateSim)
        {
            //get the size of the platform the agent is on to calculate the duration of the simulation
            Platform plat = utils.onPlatform(selectedState.getPosX(), selectedState.getPosY(), 50, 10);
            if (plat != null)
            {
                //TODO check obstacles
                if (plat.getWidth() == 0 || plat.getWidth() > largePlatformWidth)
                {
                    actionTime = largePlatformTime;
                }
                else if (plat.getWidth() < smallPlatformWidth)
                {
                    actionTime = smallPlatformWidth;
                }
                else
                {
                    actionTime = mediumPlatformTime;
                }
            }
            List<CollectibleRepresentation> simCaughtCollectibles = new List<CollectibleRepresentation>();
            //add action to simulator with the associated time to be simulated
            stateSim.DebugInfo = true;
            //stateSim.AddInstruction(newAction[0], actionTime, 0);
            //stateSim.AddInstruction(newAction[1], actionTime, 0);
            stateSim.SimulatorCollectedEvent += delegate (Object o, CollectibleRepresentation col) { simCaughtCollectibles.Add(col); };


            for (float i = 0; i <= actionTime; i += .05f)
            {
                stateSim.AddInstruction(newAction[1], 0.05f);
                stateSim.AddInstruction(newAction[0], 0.05f);


                stateSim.Update(.05f);
            }



            //stateSim.Update(.05f);

            //for (float i = 0; i < actionTime; i += .05f)
            //{
            //    stateSim.Update(.05f);
            //}

            //prepare all the debug information to be passed to the agents manager
            debugInfo = new List<DebugInformation>();

            //add the simulator debug information
            debugInfo.AddRange(stateSim.SimulationHistoryDebugInformation);

            //create the resulting state
            StateMP newState;
            if (charType == 0)
            {
                newState = new StateMP(stateSim.CirclePositionX, stateSim.CirclePositionY, stateSim.CircleVelocityX, stateSim.CircleVelocityY, stateSim.RectangleHeight / 2, stateSim.CircleVelocityRadius, stateSim.RectanglePositionX, stateSim.RectanglePositionY, stateSim.RectangleVelocityX, stateSim.RectangleVelocityY, selectedState.getCaughtCollectibles(), stateSim.CollectiblesUncaught);
            }
            else
            {
                newState = new StateMP(stateSim.RectanglePositionX, stateSim.RectanglePositionY, stateSim.RectangleVelocityX, stateSim.RectangleVelocityY, stateSim.RectangleHeight / 2, stateSim.CircleVelocityRadius, stateSim.CirclePositionX, stateSim.CirclePositionY, stateSim.CircleVelocityX, stateSim.CircleVelocityY, selectedState.getCaughtCollectibles(), stateSim.CollectiblesUncaught);
            }

            return newState;
        }

        //get a node according to the selection type
        private NodeMP getNextNode(TreeMP tree)
        {
            switch (stateSelection)
            {
                case RRTTypes.Original:
                    return getNearestNode(tree, false);
                case RRTTypes.Bias:
                    return getNodeBiasedState(tree);
                case RRTTypes.BGT:
                    return getBGTNode(tree, false);
                case RRTTypes.BGTBias:
                    return getBGTNode(tree, true);
                case RRTTypes.BGTAreaBias:
                    return getBGTAreaBiasNode(tree);
                case RRTTypes.AreaBias:
                    return getAreaBiasNode(tree);
                default:
                    return getNearestNode(tree, false);
            }
        }

        //get a random node from tree biased to a diamond area
        private NodeMP getAreaBiasNode(TreeMP tree)
        {
            NodeMP randomNode;

            if (rnd.NextDouble() <= bias)
            {
                //get a random diamond to bias to
                DiamondInfo randomDiamond = diamondsInfo[rnd.Next(diamondsInfo.Count)];

                randomNode = randomDiamond.getRandomClosestStateMP();

                if (stateSelection == RRTTypes.STP)
                {
                    while (randomNode != null && !randomNode.anyRemainingSTPActions() && rnd.NextDouble() >= stpBias)
                    {
                        randomNode = randomDiamond.getRandomClosestStateMP();
                    }
                }

                if (randomNode != null)
                {
                    return randomNode;
                }
            }
            return getNearestNode(tree, false);
        }

        //get a random node from tree biased to a diamond
        private NodeMP getBGTAreaBiasNode(TreeMP tree)
        {
            NodeMP randomNode;

            if (rnd.NextDouble() <= bias)
            {
                //get a random diamond to bias to
                DiamondInfo randomDiamond = diamondsInfo[rnd.Next(diamondsInfo.Count)];

                randomNode = randomDiamond.getRandomClosestStateMP();

                if (stateSelection == RRTTypes.STP)
                {
                    while (randomNode != null && !randomNode.anyRemainingSTPActions() && rnd.NextDouble() >= stpBias)
                    {
                        randomNode = randomDiamond.getRandomClosestStateMP();
                    }
                }

                if (randomNode != null)
                {
                    return randomNode;
                }
            }

            randomNode = getRandomNode(tree, false);
            if (stateSelection == RRTTypes.STP)
            {
                while (!randomNode.anyRemainingSTPActions() && rnd.NextDouble() >= stpBias)
                {
                    randomNode = getRandomNode(tree, false);
                }
            }
            return randomNode;
        }

        //get nearest node to a random state 
        private NodeMP getNearestNode(TreeMP tree, bool biased)
        {
            //get a random "state"
            int stateCol;
            int randomDiamond = -1;
            float stateX;
            float stateY;
            if (biased)
            {
                //a goal state needs to have 0 uncaught collectibles
                stateCol = 0;
                //get a position of a random diamond 
                int random = (int)Math.Floor((rnd.NextDouble() * 10) % tree.getRoot().getState().getUncaughtCollectibles().Count);
                stateX = tree.getRoot().getState().getUncaughtCollectibles()[random].X;
                stateY = tree.getRoot().getState().getUncaughtCollectibles()[random].Y;

                randomDiamond = random;
            }
            else
            {
                stateCol = rnd.Next(totalCollectibles);
                var random = rnd.NextDouble();
                stateX = (float)random * (area.Right - area.Left) + area.Left;
                random = rnd.NextDouble();
                stateY = (float)random * (area.Bottom - area.Top) + area.Top;
            }

            //by default the nearest node is the first of the open nodes
            NodeMP nearestNode = tree.getOpenNodes()[0];
            int nearestColl = totalCollectibles + 1;
            float nearestPos = (float)Math.Pow(area.Right * area.Bottom, 2);
            bool possibleHeight = false;
            StateMP currentState;
            //search for the nearest node in the open nodes of the tree
            foreach (NodeMP node in tree.getOpenNodes())
            {
                currentState = node.getState();
                //when it is biased to a goal state but the agent already caught the selected diamond then it is not a close state
                if (biased && checkDiamond(tree.getRoot().getState().getUncaughtCollectibles()[randomDiamond], currentState.getUncaughtCollectibles()))
                {
                    continue;
                }
                //difference in diamonds is less than the previous selected state
                else if (Math.Abs(stateCol - currentState.getUncaughtCollectibles().Count) < nearestColl)
                {
                    nearestNode = node;
                    nearestColl = Math.Abs(stateCol - currentState.getUncaughtCollectibles().Count);
                    bool circleDistance = atHeight(stateX, stateY, currentState.getPosX(), currentState.getPosY(), 0);
                    bool rectangleDistance = atHeight(stateX, stateY, currentState.getPartnerX(), currentState.getPartnerY(), 1);

                    if (circleDistance && !rectangleDistance)
                    {
                        nearestPos = getDistance(stateX, stateY, currentState.getPosX(), currentState.getPosY());
                        if (nearestPos == 0)
                        {
                            nearestPos = (float)utils.eucDist(stateX, currentState.getPosX(), stateY, currentState.getPosY());
                        }
                    }
                    else if (!circleDistance && rectangleDistance)
                    {
                        nearestPos = getDistance(stateX, stateY, currentState.getPartnerX(), currentState.getPartnerY());
                        if (nearestPos == 0)
                        {
                            nearestPos = (float)utils.eucDist(stateX, currentState.getPartnerX(), stateY, currentState.getPartnerY());
                        }
                    }
                    else
                    {
                        nearestPos = Math.Min(getDistance(stateX, stateY, currentState.getPosX(), currentState.getPosY()),
                                              getDistance(stateX, stateY, currentState.getPartnerX(), currentState.getPartnerY()));
                        if (nearestPos == 0)
                        {
                            nearestPos = (float)Math.Min(utils.eucDist(stateX, currentState.getPosX(), stateY, currentState.getPosY()),
                                                         utils.eucDist(stateX, currentState.getPartnerX(), stateY, currentState.getPartnerY()));
                        }
                    }

                }
                //if the number of diamonds is the same - get the closest one in position
                else if (Math.Abs(stateCol - currentState.getUncaughtCollectibles().Count) == nearestColl)
                {
                    bool circleDistance = atHeight(stateX, stateY, currentState.getPosX(), currentState.getPosY(), 0);
                    bool rectangleDistance = atHeight(stateX, stateY, currentState.getPartnerX(), currentState.getPartnerY(), 1);

                    //if the other state has an agent at a possible height to reach the position directly but this one does not
                    if (possibleHeight && !circleDistance && !rectangleDistance)
                    {
                        continue;
                    }
                    //if the other state does not have an agent at a possible height and this one does
                    else if (!possibleHeight && (circleDistance || rectangleDistance))
                    {
                        nearestNode = node;
                        nearestColl = Math.Abs(stateCol - currentState.getUncaughtCollectibles().Count);
                        if (circleDistance && !rectangleDistance)
                        {
                            nearestPos = getDistance(stateX, stateY, currentState.getPosX(), currentState.getPosY());
                            if (nearestPos == 0)
                            {
                                nearestPos = (float)utils.eucDist(stateX, currentState.getPosX(), stateY, currentState.getPosY());
                            }
                        }
                        else if (!circleDistance && rectangleDistance)
                        {
                            nearestPos = getDistance(stateX, stateY, currentState.getPartnerX(), currentState.getPartnerY());
                            if (nearestPos == 0)
                            {
                                nearestPos = (float)utils.eucDist(stateX, currentState.getPartnerX(), stateY, currentState.getPartnerY());
                            }
                        }
                        else
                        {
                            nearestPos = Math.Min(getDistance(stateX, stateY, currentState.getPosX(), currentState.getPosY()),
                                                  getDistance(stateX, stateY, currentState.getPartnerX(), currentState.getPartnerY()));
                            if (nearestPos == 0)
                            {
                                nearestPos = (float)Math.Min(utils.eucDist(stateX, currentState.getPosX(), stateY, currentState.getPosY()),
                                                             utils.eucDist(stateX, currentState.getPartnerX(), stateY, currentState.getPartnerY()));
                            }
                        }
                    }
                    else
                    {
                        double dist = 0;

                        if (circleDistance && !rectangleDistance)
                        {
                            dist = getDistance(stateX, stateY, currentState.getPosX(), currentState.getPosY());
                            if (dist == 0)
                            {
                                dist = utils.eucDist(stateX, currentState.getPosX(), stateY, currentState.getPosY());
                            }
                        }
                        else if (!circleDistance && rectangleDistance)
                        {
                            dist = getDistance(stateX, stateY, currentState.getPartnerX(), currentState.getPartnerY());
                            if (dist == 0)
                            {
                                dist = utils.eucDist(stateX, currentState.getPartnerX(), stateY, currentState.getPartnerY());
                            }
                        }
                        else
                        {
                            dist = Math.Min(getDistance(stateX, stateY, currentState.getPosX(), currentState.getPosY()),
                                                  getDistance(stateX, stateY, currentState.getPartnerX(), currentState.getPartnerY()));
                            if (dist == 0)
                            {
                                dist = (float)Math.Min(utils.eucDist(stateX, currentState.getPosX(), stateY, currentState.getPosY()),
                                                             utils.eucDist(stateX, currentState.getPartnerX(), stateY, currentState.getPartnerY()));
                            }
                        }

                        if (dist < nearestPos)
                        {
                            nearestNode = node;
                            nearestColl = Math.Abs(stateCol - currentState.getUncaughtCollectibles().Count);
                            nearestPos = (float)dist;
                        }
                    }
                }
            }
            tree.addVisitedNode();
            return nearestNode;
        }

        //type - 0 circle; 1 rectangle
        private bool atHeight(float stateX, float stateY, float currentX, float currentY, int type)
        {
            //if circle
            if (type == 0)
            {
                //if the state is above the current Y with a difference greater than the circle can jump
                if (stateY < currentY && currentY - stateY > circleMaxJump)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            //rectangle
            else
            {
                //if the state is above the current Y with a difference greater than the circle can jump
                if (stateY < currentY && currentY - stateY > rectangleMaxHeight / 2)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private NodeMP getNodeBiasedState(TreeMP tree)
        {
            float random = (float)rnd.NextDouble();

            //from 0% to bias% choose a goal state
            if (random <= bias)
            {
                return getNearestNode(tree, true);
            }
            else //else choose a random state
            {
                return getNearestNode(tree, false);
            }
        }

        private NodeMP getBGTNode(TreeMP tree, bool biased)
        {
            if (biased)
            {
                float random = (float)rnd.NextDouble();

                //from 0% to bias% choose a goal state
                if (random <= bias)
                {
                    return getNearestNode(tree, true);
                }
            }

            float ratio = tree.ratio();

            if (ratio > mu)
            {
                return tree.getRandomNonLeafNode();
            }
            else
            {
                return tree.getRandomLeafNode();
            }
        }

        private NodeMP getRandomNode(TreeMP tree, bool biased)
        {
            if (biased)
            {
                float random = (float)rnd.NextDouble();

                //from 0% to bias% choose a goal state
                if (random <= bias)
                {
                    return getNearestNode(tree, true);
                }
            }
            return tree.getRandomNode(); ;
        }

        private void addNodesPositions(NodeMP node)
        {
            if(node.getChildren().Count == 0)
            {
                addNodePositions(node);
            }
            foreach(NodeMP child in node.getChildren())
            {
                addNodePositions(child);
            }
        }

        private void addNodePositions(NodeMP node)
        {
            StateMP state = node.getState();
            int posX = (int)Math.Round(state.getPosX() / matrixSize);
            int posY = (int)Math.Round(state.getPosY() / matrixSize);
            int partX = (int)Math.Round(state.getPartnerX() / matrixSize);
            int partY = (int)Math.Round(state.getPartnerY() / matrixSize);

            //make sure it is within bounds
            if (posX >= positions.GetLength(0))
            {
                posX = positions.GetLength(0) - 1;
            }
            if (posY >= positions.GetLength(1))
            {
                posY = positions.GetLength(1) - 1;
            }
            if (partX >= positions.GetLength(0))
            {
                partX = positions.GetLength(0) - 1;
            }
            if (partY >= positions.GetLength(1))
            {
                partY = positions.GetLength(1) - 1;
            }

            positions[posX, posY, partX, partY, state.getNumberUncaughtCollectibles()] = node;

        }

        private bool repeatedState(StateMP state)
        {
            int posX = (int)Math.Round(state.getPosX() / matrixSize);
            int posY = (int)Math.Round(state.getPosY() / matrixSize);
            int partX = (int)Math.Round(state.getPartnerX() / matrixSize);
            int partY = (int)Math.Round(state.getPartnerY() / matrixSize);

            //make sure it is within bounds
            if (posX >= positions.GetLength(0))
            {
                posX = positions.GetLength(0) - 1;
            }
            if (posY >= positions.GetLength(1))
            {
                posY = positions.GetLength(1) - 1;
            }
            if (partX >= positions.GetLength(0))
            {
                partX = positions.GetLength(0) - 1;
            }
            if (partY >= positions.GetLength(1))
            {
                partY = positions.GetLength(1) - 1;
            }

            //check if there is already a state at that position
            if (positions[posX, posY, partX, partY, state.getNumberUncaughtCollectibles()] != null)
            {
                return true;
            }

            return false;
        }

        private bool repeatedNode(NodeMP node, StateMP state)
        {
            int posX = (int)Math.Round(state.getPosX() / matrixSize);
            int posY = (int)Math.Round(state.getPosY() / matrixSize);
            int partX = (int)Math.Round(state.getPartnerX() / matrixSize);
            int partY = (int)Math.Round(state.getPartnerY() / matrixSize);

            //make sure it is within bounds
            if (posX >= positions.GetLength(0))
            {
                posX = positions.GetLength(0) - 1;
            }
            if (posY >= positions.GetLength(1))
            {
                posY = positions.GetLength(1) - 1;
            }
            if (partX >= positions.GetLength(0))
            {
                partX = positions.GetLength(0) - 1;
            }
            if (partY >= positions.GetLength(1))
            {
                partY = positions.GetLength(1) - 1;
            }

            //check if there is already a state at that position
            if (positions[posX, posY, partX, partY, state.getNumberUncaughtCollectibles()] != null)
            {
                return true;
            }
            //if it is not a repeated state, then update the matrix
            positions[posX, posY, partX, partY, state.getNumberUncaughtCollectibles()] = node;

            return false;
        }

        /*******************************************************/
        /*                          STP                        */
        /*******************************************************/
        private Moves[] STPAction(NodeMP node)
        {
            //Tactic/skills - select diamond, go to diamond, catch diamond
            Moves[] action = new Moves[2];
            action[0] = Moves.NO_ACTION;
            action[1] = Moves.NO_ACTION;

            //if there are no more actions to be performed with stp return a random one
            if (!node.anyRemainingSTPActions())
            {
                return getSTPAction(node, action);
            }

            //select the next diamond to catch - One on the platform if any 
            DiamondInfo highestDiamond = null;
            foreach (DiamondInfo dInfo in diamondsInfo)
            {
                foreach (CollectibleRepresentation diamond in node.getState().getUncaughtCollectibles())
                {
                    if (Math.Round(dInfo.getX()) == Math.Round(diamond.X) &&
                        Math.Round(dInfo.getY()) == Math.Round(diamond.Y))
                    {
                        //get the current platform the agent is on
                        Platform platform = utils.onPlatform(node.getState().getPosX(), node.getState().getPosY(), 50, 10);

                        //if the agent is on the same platform as the diamond
                        //make sure there isn't a platform blocking the way
                        if (platform != null && dInfo.getPlatform().getX() == platform.getX() &&
                            dInfo.getPlatform().getY() == platform.getY() &&
                            Math.Abs(dInfo.getY() - node.getState().getPosY()) <= circleMaxJump &&
                            !utils.obstacleBetween(node.getState().getPosX(), dInfo.getX(), platform))
                        {
                            //if circle
                            if (charType == 0)
                            {
                                action[0] = circleSTP.skillCatchDiamond(node, dInfo, node.getRemainingMoves());
                                List<Moves[]> rectangleMoves = new List<Moves[]>();
                                foreach (Moves[] moves in node.getRemainingMoves())
                                {
                                    if (moves[0] == action[0])
                                    {
                                        rectangleMoves.Add(moves);
                                    }
                                }
                                //if there are no more moves possible for the rectangle then these moves should be removed
                                if (rectangleMoves.Count == 0)
                                {
                                    //what the hell?
                                    action[1] = Moves.NO_ACTION;
                                }
                                else
                                {
                                    action[1] = rectangleSTP.skillCatchDiamond(node, dInfo, rectangleMoves);
                                }
                            }
                            //if rectangle
                            else if (charType == 1)
                            {
                                action[0] = rectangleSTP.skillCatchDiamond(node, dInfo, node.getRemainingMoves());
                                List<Moves[]> circleMoves = new List<Moves[]>();
                                foreach (Moves[] moves in node.getRemainingMoves())
                                {
                                    if (moves[0] == action[0])
                                    {
                                        circleMoves.Add(moves);
                                    }
                                }
                                //if there are no more moves possible for the rectangle then these moves should be removed
                                if (circleMoves.Count == 0)
                                {
                                    //what the hell?
                                    action[1] = Moves.NO_ACTION;
                                }
                                else
                                {
                                    action[1] = circleSTP.skillCatchDiamond(node, dInfo, circleMoves);
                                }
                            }
                            else
                            {
                                throw new Exception("the character id must be either 0 (circle) or 1 (rectangle)");
                            }

                            return getSTPAction(node, action);
                        }
                        //if the diamond is too high for the circle to catch by itself but low enought to be possible with the rectangle's help
                        else if (platform != null && dInfo.getPlatform().getX() == platform.getX() &&
                            dInfo.getPlatform().getY() == platform.getY() &&
                            Math.Abs(dInfo.getY() - (node.getState().getPartnerY() - node.getState().getHeight() / 2 - radius)) <= circleMaxJump &&
                            !utils.obstacleBetween(node.getState().getPosX(), dInfo.getX(), platform))
                        {
                            //TODO skill that tries to use the rectangle as a platform
                        }
                    }
                }
            }

            //get the list of caught and uncaught collectibles
            int[] cCol = getCaughtDiamondsIndexes(node.getState().getUncaughtCollectibles());
            foreach (DiamondInfo diamond in diamondsInfo)
            {
                //the first uncaught collectible is the highest
                if (cCol[diamond.getId()] == 0)
                {
                    highestDiamond = diamond;
                    break;
                }
            }
            //if there isn't any diamond on the same platform as the agent, select the highest diamond in the highest platform
            if (highestDiamond != null)
            {
                action = skillCatchHighestDiamond(node, highestDiamond);
            }

            return getSTPAction(node, action);
        }

        private Moves[] getSTPAction(NodeMP node, Moves[] action)
        {
            if (action[0] != Moves.NO_ACTION || action[1] != Moves.NO_ACTION)
            {
                node.removeMove(action);
                return action;
            }
            else
            {
                if (node.possibleMovesCount() == 0)
                {
                    return action;
                }
                return randomAction(node);
            }
        }

        private Moves[] skillCatchHighestDiamond(NodeMP node, DiamondInfo dInfo)
        {
            Moves[] action = new Moves[2];
            action[0] = Moves.NO_ACTION;
            action[1] = Moves.NO_ACTION;

            Platform dPlatform = dInfo.getPlatform();
            
            if (dPlatform != null)
            {
                //check if the diamond is between two platforms with a narrow space between them
                foreach (Platform platform in platformsInfo)
                {
                    if (platform.getY() + platform.getHeight() / 2 < dPlatform.getY() - dPlatform.getHeight() / 2 &&
                        dPlatform.getY() - dPlatform.getHeight() / 2 - (platform.getY() + platform.getHeight() / 2) < radius * 2 &&
                        dInfo.getX() < platform.getX() + platform.getWidth() / 2 && dInfo.getX() > platform.getX() - platform.getWidth() / 2)
                    {
                        if (charType == 0)
                        {
                            action[0] = circleSTP.skillClosePlatforms(node, dInfo, node.getRemainingMoves(), charType);
                            List<Moves[]> rectangleMoves = new List<Moves[]>();
                            foreach (Moves[] moves in node.getRemainingMoves())
                            {
                                if (moves[0] == action[0])
                                {
                                    rectangleMoves.Add(moves);
                                }
                            }
                            if (rectangleMoves.Count == 0)
                            {
                                action[0] = Moves.NO_ACTION;
                                action[1] = Moves.NO_ACTION;
                            }
                            else
                            {
                                action[1] = rectangleSTP.skillClosePlatforms(node, dInfo, rectangleMoves, charType);
                            }
                        }
                        //if rectangle
                        else if (charType == 1)
                        {
                            action[0] = rectangleSTP.skillClosePlatforms(node, dInfo, node.getRemainingMoves(), charType);
                            List<Moves[]> circleMoves = new List<Moves[]>();
                            foreach (Moves[] moves in node.getRemainingMoves())
                            {
                                if (moves[0] == action[0])
                                {
                                    circleMoves.Add(moves);
                                }
                            }
                            if (circleMoves.Count == 0)
                            {
                                action[0] = Moves.NO_ACTION;
                                action[1] = Moves.NO_ACTION;
                            }
                            else
                            {
                                action[1] = circleSTP.skillClosePlatforms(node, dInfo, circleMoves, charType);
                            }
                        }
                        else
                        {
                            throw new Exception("the character id must be either 0 (circle) or 1 (rectangle)");
                        }
                    }
                    //check if the diamond is too high to be caught alone but low enough to be caught in coop
                    else if (dInfo.getY() + diamondRadius < dPlatform.getY() - dPlatform.getHeight() / 2 - rectangleMaxHeight - circleMaxJump)
                    {
                        Platform agentPlatform = utils.onPlatform(node.getState().getPosX(), node.getState().getPosY());
                        //check if agent is on that platform
                        if(agentPlatform != null && utils.samePlatform(dPlatform, agentPlatform))
                        {
                            //if this is the circle
                            if(charType == 0)
                            {
                                action[0] = circleSTP.skillCoopHighDiamond(node, dInfo, node.getRemainingMoves(), charType);
                                List<Moves[]> rectangleMoves = new List<Moves[]>();
                                foreach (Moves[] moves in node.getRemainingMoves())
                                {
                                    if (moves[0] == action[0])
                                    {
                                        rectangleMoves.Add(moves);
                                    }
                                }
                                if (rectangleMoves.Count == 0)
                                {
                                    action[0] = Moves.NO_ACTION;
                                    action[1] = Moves.NO_ACTION;
                                }
                                else
                                {
                                    action[1] = rectangleSTP.skillCoopHighDiamond(node, dInfo, rectangleMoves, charType);
                                }
                            }
                            //if this is the rectangle
                            else 
                            {
                                action[0] = rectangleSTP.skillCoopHighDiamond(node, dInfo, node.getRemainingMoves(), charType);
                                List<Moves[]> circleMoves = new List<Moves[]>();
                                foreach (Moves[] moves in node.getRemainingMoves())
                                {
                                    if (moves[0] == action[0])
                                    {
                                        circleMoves.Add(moves);
                                    }
                                }
                                if (circleMoves.Count == 0)
                                {
                                    action[0] = Moves.NO_ACTION;
                                    action[1] = Moves.NO_ACTION;
                                }
                                else
                                {
                                    action[1] = circleSTP.skillCoopHighDiamond(node, dInfo, circleMoves, charType);
                                }
                            }
                        }
                    }
                }
                if (action[0] != Moves.NO_ACTION || action[1] != Moves.NO_ACTION)
                {
                    return action;
                }
            }

            //if the platform of the diamond is higher than the one the agent is on
            if (node.getState().getPosY() > dInfo.getPlatform().getY())
            {
                //if circle
                if (charType == 0)
                {
                    action[0] = circleSTP.skillHigherPlatform(node, dInfo, node.getRemainingMoves());
                    List<Moves[]> rectangleMoves = new List<Moves[]>();
                    foreach (Moves[] moves in node.getRemainingMoves())
                    {
                        if (moves[0] == action[0])
                        {
                            rectangleMoves.Add(moves);
                        }
                    }
                    if (rectangleMoves.Count == 0)
                    {
                        action[0] = Moves.NO_ACTION;
                        action[1] = Moves.NO_ACTION;
                    }
                    else
                    {
                        action[1] = rectangleSTP.skillHigherPlatform(node, dInfo, rectangleMoves);
                    }
                }
                //if rectangle
                else if (charType == 1)
                {
                    action[0] = rectangleSTP.skillHigherPlatform(node, dInfo, node.getRemainingMoves());
                    List<Moves[]> circleMoves = new List<Moves[]>();
                    foreach (Moves[] moves in node.getRemainingMoves())
                    {
                        if (moves[0] == action[0])
                        {
                            circleMoves.Add(moves);
                        }
                    }
                    if (circleMoves.Count == 0)
                    {
                        action[0] = Moves.NO_ACTION;
                        action[1] = Moves.NO_ACTION;
                    }
                    else
                    {
                        action[1] = circleSTP.skillHigherPlatform(node, dInfo, circleMoves);
                    }
                }
                else
                {
                    throw new Exception("the character id must be either 0 (circle) or 1 (rectangle)");
                }

            }
            //when the agent is on a higher platform than the diamond's
            else
            {
                //if circle
                if (charType == 0)
                {
                    action[0] = circleSTP.skillLowerPlatform(node, dInfo, node.getRemainingMoves());
                    List<Moves[]> rectangleMoves = new List<Moves[]>();
                    foreach (Moves[] moves in node.getRemainingMoves())
                    {
                        if (moves[0] == action[0])
                        {
                            rectangleMoves.Add(moves);
                        }
                    }
                    if (rectangleMoves.Count == 0)
                    {
                        action[0] = Moves.NO_ACTION;
                        action[1] = Moves.NO_ACTION;
                    }
                    else
                    {
                        action[1] = rectangleSTP.skillLowerPlatform(node, dInfo, rectangleMoves);
                    }
                }
                //if rectangle
                else if (charType == 1)
                {
                    action[0] = rectangleSTP.skillLowerPlatform(node, dInfo, node.getRemainingMoves());
                    List<Moves[]> circleMoves = new List<Moves[]>();
                    foreach (Moves[] moves in node.getRemainingMoves())
                    {
                        if (moves[0] == action[0])
                        {
                            circleMoves.Add(moves);
                        }
                    }
                    if (circleMoves.Count == 0)
                    {
                        action[0] = Moves.NO_ACTION;
                        action[1] = Moves.NO_ACTION;
                    }
                    else
                    {
                        action[1] = circleSTP.skillLowerPlatform(node, dInfo, circleMoves);
                    }
                }
                else
                {
                    throw new Exception("the character id must be either 0 (circle) or 1 (rectangle)");
                }

            }

            return action;
            //SHOULD NOT REACH HERE
            throw new Exception("It must return a possible action");
        }

        /*******************************************************/
        /*                Correction funtions                  */
        /*******************************************************/

        private TreeMP repopulateMatrix(TreeMP t)
        {
            //new matrix
            positions = new NodeMP[area.Right / matrixSize, area.Bottom / matrixSize, area.Right / matrixSize, area.Bottom / matrixSize, totalCollectibles + 1];

            List<Moves[]> newMoves;

            //reset tree info
            t.resetTree();

            if (replan)
            {
                foreach (DiamondInfo diamond in diamondsInfo)
                {
                    diamond.clearAreaLists();
                }
            }

            foreach (NodeMP node in t.getNodes())
            {
                //check if circle agent is on a platform or middle jump/fall
                if (charType == 0)
                {
                    if (utils.onPlatform(node.getState().getPosX(), node.getState().getPosY() + radius, 0, 10) != null)
                    {
                        newMoves = copyMoves();
                    }
                    else
                    {
                        newMoves = copyMovesNoJump();
                    }
                }
                else
                {
                    newMoves = copyMoves();
                }
                //reset node info
                node.resetNode(newMoves, t);
                //put the state in the new matrix
                if(!repeatedNode(node, node.getState()))
                {
                    //see if the node is close to any diamond
                    foreach (DiamondInfo diamond in diamondsInfo)
                    {
                        diamond.insertClosestNodeMP(node);
                    }
                }
            }

            goal = false;

            return t;
        }

        public void correctPlan(PathPlanMP plan)
        {
            correction = true;
            //get only the points where the agent is on a platform so it can be tested if the platforms have been reached
            previousPoints = new PathPlanMP(cutplan, plan.getTotalCollectibles(), plan.getTree(), utils);
            previousPlatforms = new List<Platform>();
            List<PointMP> points = plan.getPathPoints();
            for (int i = 0; i < points.Count; i++)
            {
                Platform pointPl = utils.onPlatform(points[i].getPosX(), points[i].getPosY() + radius, 25, 10);
                if (pointPl != null)
                {
                    previousPoints.addPoint(points[i]);
                    previousPlatforms.Add(pointPl);
                }
            }
        }

        //Builds a tree that will connect with the previous version
        public TreeMP correctRRT(StateMP initialState, ActionSimulator predictor, TreeMP t)
        {
            correction = true;
            newPlan = false;
            goal = false;
            iterations = correctionIterations;
            TreeMP newTree = buildRRT(initialState, predictor);
            //if we got a new plan for the original goal replace the tree completely
            if (goal)
            {
                if (newPlan)
                {
                    t = newTree;
                }
                else
                {
                    //else, connect the trees
                    t = connectTrees(newTree, t);
                }
            }
            else
            {
                //continue search towards a full plan
                iterations = rrtIterations;
                RRT(t);
            }

            return t;
        }

        //for MP replanning
        public TreeMP newRoot(NodeMP node)
        {
            //get all nodes from the node children and children children to put as open nodes
            List<NodeMP> children = getChildren(node);
            TreeMP t = new TreeMP(node.getState(), node.getPredictor(), copyMoves(), bgt);

            //add the children to the nodes and open lists
            foreach (NodeMP child in children)
            {
                t.addNode(child);
            }

            //repopulate matrix - it takes care of closing the supposed nodes
            repopulateMatrix(t);

            return t;
        }

        private List<NodeMP> getChildren(NodeMP node)
        {
            List<NodeMP> children = new List<NodeMP>();
            if (node.getChildren().Count != 0)
            {
                foreach (NodeMP child in node.getChildren())
                {
                    children.AddRange(getChildren(child));
                }
            }
            children.Add(node);
            return children;
        }

        private TreeMP connectTrees(TreeMP newTree, TreeMP previousTree)
        {
            //connect the new tree node to the next node in the plan
            connectNode.connectNodes(planNodes[0]);
            return null;
        }

        //see if the current calculated state is the same as the next one in the plan
        private bool stateReached(StateMP state)
        {
            //TODO - check any state in the plan
            return checkState(state, planNodes[0].getState());
        }

        //temporary - plan from file
        private bool stateReachedTemp(StateMP state)
        {
            //TODO - check any state in the plan
            return checkStateTemp(state, planNodes[0].getState());
        }

        private bool checkState(StateMP state1, StateMP state2)
        {
            float state1X = state1.getPosX();
            float state1Y = state1.getPosY();
            float stateVel1X = state1.getVelX();
            float stateVel1Y = state1.getVelY();
            List<CollectibleRepresentation> state1Coll = state1.getCaughtCollectibles();

            float state2X = state2.getPosX();
            float state2Y = state2.getPosY();
            float stateVel2X = state2.getVelX();
            float stateVel2Y = state2.getVelY();
            List<CollectibleRepresentation> state2Coll = state2.getCaughtCollectibles();

            if (state1X >= state2X - correctionPosMargin && state1X <= state2X + correctionPosMargin &&
                state1Y >= state2Y - correctionPosMargin && state1Y <= state2Y + correctionPosMargin &&
                stateVel1X >= stateVel2X - correctionVelMargin && stateVel1X <= stateVel2X + correctionVelMargin &&
                stateVel1Y >= stateVel2Y - correctionVelMargin && stateVel1Y <= stateVel2Y + correctionVelMargin &&
                state1Coll.Count == state2Coll.Count)
            {
                //Case they match, check if the states have the same caught collectibles
                foreach (CollectibleRepresentation coll in state1Coll)
                {
                    //Case there is on missing, then it is not the same state
                    if (!state2Coll.Exists(sc => sc.Equals(coll)))
                    {
                        return false;
                    }
                }
                //Case every collectible is the same, the state is the same
                return true;
            }
            return false;
        }

        //temporary
        private bool checkStateTemp(StateMP state1, StateMP state2)
        {
            float state1X = state1.getPosX();
            float state1Y = state1.getPosY();
            float stateVel1X = state1.getVelX();
            float stateVel1Y = state1.getVelY();

            float state2X = state2.getPosX();
            float state2Y = state2.getPosY();
            float stateVel2X = state2.getVelX();
            float stateVel2Y = state2.getVelY();

            if (state1X >= state2X - correctionPosMargin && state1X <= state2X + correctionPosMargin &&
                state1Y >= state2Y - correctionPosMargin && state1Y <= state2Y + correctionPosMargin &&
                stateVel1X >= stateVel2X - correctionVelMargin && stateVel1X <= stateVel2X + correctionVelMargin &&
                stateVel1Y >= stateVel2Y - correctionVelMargin && stateVel1Y <= stateVel2Y + correctionVelMargin)
            {
                //Case every collectible is the same, the state is the same
                return true;
            }
            return false;
        }
        /*******************************************************/
        /*                  Plan funtions                      */
        /*******************************************************/

        public void getPathPlan(TreeMP t)
        {
            //create a list with the ordered actions to aplly from the tree
            NodeMP currentNode = t.getGoal();
            while (currentNode.getParent() != null)
            {
                //insert action to the plan
                plan.Insert(0, currentNode.getActions());
                //insert the state it is before applying the action
                planNodes.Insert(0, currentNode.getParent());

                currentNode = currentNode.getParent();
            }
        }

        public List<NodeMP> getPathPlan(NodeMP node)
        {
            //create a list with the ordered actions to aplly from the tree
            NodeMP currentNode = node;
            List<NodeMP> currentPlanNodes = new List<NodeMP>();
            while (currentNode.getParent() != null)
            {
                currentPlanNodes.Insert(0, currentNode.getParent());
                currentNode = currentNode.getParent();
            }
            return currentPlanNodes;
        }
        public PathPlanMP getPlan(TreeMP t)
        {
            PathPlanMP p = new PathPlanMP(cutplan, t.getRoot().getState().getNumberUncaughtCollectibles(), t, utils);

            NodeMP currentNode = t.getGoal();

            p.setTotalCollectibles(currentNode.getState().getNumberUncaughtCollectibles());

            StateMP currentState;
            PointMP point;

            while (currentNode.getParent() != null)
            {

                currentState = currentNode.getState();
                if (charType == 0)
                {
                    //the jump action needs the point it has to make the jump and not where to reach
                    if (currentNode.getActions()[0] == Moves.JUMP)
                    {
                        point = new PointMP(currentNode.getParent().getState().getPosX(), currentNode.getParent().getState().getPosY(), currentNode.getParent().getState().getVelX(), currentState.getHeight(), currentNode.getParent().getState().getPartnerX(), currentNode.getParent().getState().getPartnerY(), currentNode.getActions()[0], currentState.getUncaughtCollectibles(), currentNode.getParent());
                    }
                    else
                    {
                        point = new PointMP(currentState.getPosX(), currentState.getPosY(), currentState.getVelX(), currentState.getHeight(), currentState.getPartnerX(), currentState.getPartnerY(), currentNode.getActions()[0], currentState.getUncaughtCollectibles(), currentNode);
                    }
                }
                else
                {
                    point = new PointMP(currentState.getPosX(), currentState.getPosY(), currentState.getVelX(), currentState.getHeight(), currentState.getPartnerX(), currentState.getPartnerY(), currentNode.getActions()[0], currentState.getUncaughtCollectibles(), currentNode);
                }

                //it adds the points in the reverse order
                p.addPoint(point);

                currentNode = currentNode.getParent();
            }

            ////add first point 
            //currentState = currentNode.getParent().getState();
            //point = new Point(currentState.getPosX(), currentState.getPosY(), currentState.getVelX(), currentNode.getAction(), currentState.getUncaughtCollectibles());
            //p.addPoint(point);


            return p;
        }

        public bool checkState(PointMP point, float X, float Y, float velX, float velY, List<CollectibleRepresentation> uncaughtColl)
        {
            //calculate the margins of error
            float marginPosX = Math.Abs(velX) / marginDenominator + marginMinX;
            float marginPosY = Math.Abs(velY) / marginDenominator + marginMinY;
            float marginVelX = Math.Abs(velX) + marginMinVX;

            if (Math.Abs(X - point.getPosX()) < 25 && Math.Abs(Y - point.getPosY()) < 25 &&
                //velX >= point.getVelX() - marginVelX && velX <= point.getVelX() + marginVelX &&
                uncaughtColl.Count == point.getUncaughtColl().Count)
            {
                //Case they match, check if the states have the same caught collectibles
                /* foreach (CollectibleRepresentation coll in uncaughtColl)
                 {
                     //Case there is on missing, then it is not the same state
                     if (!point.getUncaughtColl().Exists(sc => sc.Equals(coll)))
                     {
                         return false;
                     }
                 }*/
                //Case every collectible is the same, the state is the same
                return true;
            }


            return false;
        }

        public bool checkState(StateMP planState, float X, float Y, float velX, float velY, List<CollectibleRepresentation> caughtColl)
        {
            float stateX = planState.getPosX();
            float stateY = planState.getPosY();
            float stateVelX = planState.getVelX();
            float stateVelY = planState.getVelY();
            List<CollectibleRepresentation> stateColl = planState.getCaughtCollectibles();

            //TODO
            //calculate the margins of error
            float marginPosX = Math.Abs(velX) / marginDenominator + marginMinX;
            float marginPosY = Math.Abs(velY) / marginDenominator + marginMinY;
            float marginVelX = Math.Abs(velX) + marginMinVX;
            float marginVelY = Math.Abs(velY) + marginMinVY;

            //Compare states with a calculated margin of error for positions, velocity and number of caught collectibles
            if (X >= stateX - marginPosX && X <= stateX + marginPosX &&
                Y >= stateY - marginPosY && Y <= stateY + marginPosY &&
                velX >= stateVelX - marginVelX && velX <= stateVelX + marginVelX &&
                velY >= stateVelY - marginVelY && velY <= stateVelY + marginVelY &&
                caughtColl.Count == stateColl.Count)
            {
                //Case they match, check if the states have the same caught collectibles
                foreach (CollectibleRepresentation coll in caughtColl)
                {
                    //Case there is on missing, then it is not the same state
                    if (!stateColl.Exists(sc => sc.Equals(coll)))
                    {
                        return false;
                    }
                }
                //Case every collectible is the same, the state is the same
                return true;
            }
            return false;
        }

        //temporary - to use with a plan taken from a file - ignores collectibles
        public bool checkStateTemp(StateMP planState, float X, float Y, float velX, float velY, List<CollectibleRepresentation> caughtColl)
        {
            float stateX = planState.getPosX();
            float stateY = planState.getPosY();
            float stateVelX = planState.getVelX();
            float stateVelY = planState.getVelY();
            List<CollectibleRepresentation> stateColl = planState.getCaughtCollectibles();

            //TODO
            //calculate the margins of error
            float marginPosX = Math.Abs(velX) / marginDenominator + marginMinX;
            float marginPosY = Math.Abs(velY) / marginDenominator + marginMinY;
            float marginVelX = Math.Abs(velX) + marginMinVX;
            float marginVelY = Math.Abs(velY) + marginMinVY;

            //Compare states with a calculated margin of error for positions, velocity and number of caught collectibles
            if (X >= stateX - marginPosX && X <= stateX + marginPosX &&
                Y >= stateY - marginPosY && Y <= stateY + marginPosY &&
                velX >= stateVelX - marginVelX && velX <= stateVelX + marginVelX &&
                velY >= stateVelY - marginVelY && velY <= stateVelY + marginVelY)
            {

                return true;
            }
            return false;
        }

        public List<Moves[]> getPlanMoves()
        {
            return plan;
        }

        public List<NodeMP> getPlanNodes()
        {
            return planNodes;
        }

        private void checkSemiPlan(TreeMP tree, NodeMP newNode)
        {
            //if the agent is in the same platform as a diamond, this might be the safest semi-plan so far
            if (onSamePlatform(0, initialPlatform, newNode.getState().getUncaughtCollectibles(), true))
            {
                //if this semiplan is shorter than the previous one
                if (samePlatformPlan && newNode.getTreeDepth() < bestSemiPlanNode.getTreeDepth())
                {
                    bestSemiPlanNode = newNode;
                    return;
                }
                else if (!samePlatformPlan)
                {
                    bestSemiPlanNode = newNode;
                    samePlatformPlan = true;
                    return;
                }
            }

            //if there is no best semi-plan and the current state has less uncaught collectibles than the initial state, then this is the best semi-plan so far
            if (bestSemiPlanNode == null && newNode.getState().getNumberUncaughtCollectibles() < tree.getRoot().getState().getNumberUncaughtCollectibles())
            {
                bestSemiPlanNode = newNode;
                return;
            }
            // if this plans catches more of the highest diamonds
            else if (bestSemiPlanNode != null && higherPlatforms(getCaughtDiamondsIndexes(newNode.getState().getUncaughtCollectibles())))
            {
                //get only the highest

                bestSemiPlanNode = getOnlyHighest(newNode);
                return;
            }

        }
        /*******************************************************/
        /*                Auxiliar funtions                    */
        /*******************************************************/
        private NodeMP getOnlyHighest(NodeMP node)
        {
            NodeMP currentNode = node;
            int[] caughtDiamonds;
            int lowerDiamonds = 0;
            int newIndex;

            //get the order of the caught diamonds
            //get the path
            List<NodeMP> currentPlan = getPathPlan(currentNode);
            int lastDiamondCount = currentPlan[0].getState().getNumberUncaughtCollectibles();
            int totalDiamonds = lastDiamondCount;
            newIndex = currentPlan.Count;

            for (int i = 0; i < currentPlan.Count; i++)
            {
                caughtDiamonds = getCaughtDiamondsIndexes(currentPlan[i].getState().getUncaughtCollectibles());
                if (caughtDiamonds[0] == 0 && currentPlan[i].getState().getNumberUncaughtCollectibles() < lastDiamondCount)
                {
                    lastDiamondCount = currentPlan[i].getState().getNumberUncaughtCollectibles();
                    lowerDiamonds++;
                    continue;
                }
                else if (currentPlan[i].getState().getNumberUncaughtCollectibles() < lastDiamondCount)
                {
                    int highest = getHighestNumber(getCaughtDiamondsIndexes(currentPlan[i].getState().getUncaughtCollectibles()));
                    //check if the number os highest caught collectible is right
                    if (totalDiamonds - (highest + lowerDiamonds) != lastDiamondCount - 1)
                    {
                        return currentPlan[newIndex];
                    }
                    else
                    {
                        lastDiamondCount--;
                        newIndex = i;
                    }
                }
            }
            return node;
        }

        //get caught diamonds from the uncaught diamond list
        public int[] getCaughtDiamondsIndexes(List<CollectibleRepresentation> diamonds)
        {
            int[] indexes = new int[diamondsInfo.Count];

            for (int i = 0; i < indexes.Length; i++)
            {
                indexes[i] = 1;
            }

            foreach (CollectibleRepresentation diamond in diamonds)
            {
                for (int i = 0; i < diamondsInfo.Count; i++)
                {
                    //put 0 the uncaught diamonds and 1 the caught ones
                    if (Math.Round(diamond.X) == diamondsInfo[i].getX() && Math.Round(diamond.Y) == diamondsInfo[i].getY())
                    {
                        indexes[i] = 0;
                        break;
                    }
                    //} else
                    //{
                    //    indexes[i] = 1;
                    //}
                }
            }

            //put 2 to those previously caught before this plan
            for (int i = 0; i < diamondsInfo.Count; i++)
            {
                if (diamondsInfo[i].wasCaught())
                {
                    indexes[i] = 2;
                }
            }

            return indexes;
        }

        private bool higherPlatforms(int[] diamonds)
        {
            if (diamonds.Length == 0)
            {
                return true;
            }

            int[] bestCaughtDiamonds = getCaughtDiamondsIndexes(bestSemiPlanNode.getState().getUncaughtCollectibles());

            int currentHighest = getHighestNumber(diamonds);
            int bestHighest = getHighestNumber(bestCaughtDiamonds);

            if (currentHighest > bestHighest)
            {
                return true;
            }

            return false;
        }

        private int getHighestNumber(int[] diamonds)
        {
            //if the highest diamond in the highest platform wasn't caught, return 0
            if (diamonds[0] == 0)
            {
                return 0;
            }
            //else, count how many of the highest were caught
            else
            {
                int i = 1;
                while (diamonds[i] == 1)
                {
                    i++;
                }
                return i;
            }
        }

        private bool higherPlatform(int[] diamonds)
        {
            if (diamonds.Length == 0)
            {
                return true;
            }

            int[] bestCaughtDiamonds = getCaughtDiamondsIndexes(bestSemiPlanNode.getState().getUncaughtCollectibles());

            for (int i = 0; i < diamondsInfo.Count; i++)
            {
                //if the highest caught diamond belongs to the current plan then return true
                if (diamonds[i] == 1 && bestCaughtDiamonds[i] == 0)
                {
                    return true;
                }
                if (bestCaughtDiamonds[i] == 1)
                {
                    return false;
                }
            }

            return true;
        }

        public void setIterations(int iterationNumber)
        {
            iterations = iterationNumber;
        }

        private List<Moves[]> copyMoves()
        {
            List<Moves[]> newMovesList = new List<Moves[]>();

            foreach (Moves[] move in possibleMoves)
            {
                newMovesList.Add(move);
            }

            return newMovesList;
        }

        private List<Moves[]> copyMovesNoJump()
        {
            List<Moves[]> newMovesList = new List<Moves[]>();

            foreach (Moves[] move in possibleMoves)
            {
                if (move[0] != Moves.JUMP && move[1] != Moves.JUMP)
                {
                    newMovesList.Add(move);
                }
            }

            return newMovesList;
        }


        private Moves[] randomAction(NodeMP node)
        {
            //Random rnd = new Random();
            Moves[] action;

            if (charType == 0) //circle
            {
                if (rnd.NextDouble() > jumpBias || node.possibleMovesCount() <= 1)
                {
                    //choose from all moves
                    action = node.getMove(rnd.Next(node.possibleMovesCount()));
                }
                else
                {
                    //chose a move that is not jump unless it is the only move left
                    int random = rnd.Next(node.possibleMovesCount());
                    if (node.getRemainingMoves()[random][0] == Moves.JUMP || node.getRemainingMoves()[random][1] == Moves.JUMP)
                    {
                        random = (random + 1) % node.possibleMovesCount();
                    }
                    action = node.getMove(random);
                }
            }
            else //rectangle
            {
                if (rnd.NextDouble() > jumpBias || node.possibleMovesCount() <= 2)
                {
                    //choose from all moves
                    action = node.getMove(rnd.Next(node.possibleMovesCount()));
                }
                else
                {
                    //exclude the morph moves
                    action = node.getMove(rnd.Next(node.possibleMovesCount() - 2));
                }
            }

            return action;
        }

        private bool checkObstacle(float dPosX, float dPosY, float cPosX, float cPosY, Platform obstacle)
        {
            //TODO - count with the side edges
            //platform top and bottom edges
            float xLeft = obstacle.getX() - obstacle.getWidth() / 2;
            float xRight = obstacle.getX() + obstacle.getWidth() / 2;
            float yUp = obstacle.getY() + obstacle.getHeight() / 2;
            float yDown = obstacle.getY() - obstacle.getHeight() / 2;

            bool intersection = utils.lineIntersection(dPosX, dPosY, cPosX, cPosY, xLeft, yUp, xRight, yUp);

            if (intersection)
            {
                return true;
            }

            intersection = utils.lineIntersection(dPosX, dPosY, cPosX, cPosY, xLeft, yDown, xRight, yDown);

            if (intersection)
            {
                return true;
            }

            return false;
        }

        public float getDistance(float x1, float y1, float x2, float y2)
        {
            double dist = 0;
            foreach (Platform platform in platformsInfo)
            {
                if (checkObstacle(x1, y1, x2, y2, platform))
                {
                    dist = utils.minDistance(x1, y1, x2, y2, platform);
                }
            }


            return (float)Math.Pow(dist, 2);
        }


        public void setRadius(float rad)
        {
            radius = rad;
        }

        private bool checkDiamond(CollectibleRepresentation d, List<CollectibleRepresentation> diamonds)
        {
            foreach (CollectibleRepresentation diamond in diamonds)
            {
                if (d.Equals(diamond))
                {
                    return true;
                }
            }
            return false;
        }

        public bool isSemiPlan()
        {
            return semiPlan;
        }

        public void setDiamonds(List<DiamondInfo> newDiamonds)
        {
            diamondsInfo = newDiamonds;
        }

        public void setReturnPos(float[] position)
        {
            returnPosition = position;
        }

        public void diamondToIgnore(DiamondInfo diamond)
        {
            ignoreDiamonds.Add(diamond);
        }

        public void resetDiamondsToIgnore()
        {
            ignoreDiamonds = new List<DiamondInfo>();
        }

        public void removeGoal(TreeMP T)
        {
            T.setGoal(null);
            goal = false;
        }

        /********************************************************************************************/
        /***                                   Goal checking                                      ***/
        /********************************************************************************************/

        private bool checkGoal(TreeMP tree, NodeMP node)
        {
            StateMP newState = node.getState();

            if (goalType == GoalType.All)
            {
                //if all collectibles where caught
                if (newState.getNumberUncaughtCollectibles() == 0)
                {
                    goal = true;
                    newPlan = true;
                    return true;
                }
                return false;
            }

            else if (goalType == GoalType.FirstPossible)
            {
                //check if a diamond was caught
                if (newState.getUncaughtCollectibles().Count < totalCollectibles && notIgnored(node))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (goalType == GoalType.Return)
            {
                //what matters is that the agent can get to the same platform (side)
                Platform currentPlatform = utils.onPlatform(newState.getPosX(), newState.getPosY(), 50);

                if (currentPlatform == null)
                {
                    return false;
                }

                Platform previousPlatform = utils.onPlatform(returnPosition[0], returnPosition[1], 50);
                //if the agent is in the same platform as the initial position of the previous search with no obstacles in between
                if (utils.samePlatform(currentPlatform, previousPlatform) && !utils.obstacleBetween(returnPosition[0], newState.getPosX(), currentPlatform))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (goalType == GoalType.HighestSingle)
            {
                //check if diamond is the highest
                if (caughtHighest(tree, node))
                {
                    return true;
                }
                return false;
            }
            else if (goalType == GoalType.Coop)
            {
                if (caughtHighest(tree, node))
                {
                    return true;
                }
                return false;
                //TODO
                //this search must be done with a cooperative mode
            }
            return false;
        }

        private bool caughtHighest(TreeMP tree, NodeMP node)
        {
            //check which is the highest diamond that is currently uncaught (root)
            DiamondInfo highest = null;
            foreach (DiamondInfo diamond in diamondsInfo)
            {
                if (!diamond.wasCaught())
                {
                    highest = diamond;
                    break;
                }
            }
            if (highest == null)
            {
                throw new Exception("there must always be a diamond");
            }
            //check if the caught diamond in the node is that one
            List<CollectibleRepresentation> uCol = node.getState().getUncaughtCollectibles();
            foreach (CollectibleRepresentation diamond in uCol)
            {
                //if the highest is on the list of uncaught
                if (Math.Round(diamond.X) == Math.Round(highest.getX())
                    && Math.Round(diamond.Y) == Math.Round(highest.getY()))
                {
                    return false;
                }
            }
            return true;
        }

        //check if the caught diamond is to be ignored
        public bool notIgnored(NodeMP node)
        {
            //should only get one index
            int[] caught = getCaughtDiamondsIndexes(node.getState().getUncaughtCollectibles());

            for (int i = 0; i < caught.Length; i++)
            {
                //if this diamond belongs to the list of the ignored, then should not be considered
                foreach (DiamondInfo diamond in ignoreDiamonds)
                {
                    if (Math.Round(diamondsInfo[caught[i]].getX()) == Math.Round(diamond.getX()) ||
                       Math.Round(diamondsInfo[caught[i]].getY()) == Math.Round(diamond.getY()))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /*******************************************************/
        /*                   Debug funtions                    */
        /*******************************************************/

        //gets the information of the plan path and of the visited and total nodes
        public List<DebugInformation> getDebugInfo(TreeMP t, List<DebugInformation> otherInfo)
        {

            List<DebugInformation> debugInfo = new List<DebugInformation>();

            debugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());
            debugInfo.AddRange(t.getGoal().getDebugInfo());

            if (otherInfo != null)
            {
                debugInfo.AddRange(otherInfo);
            }

            String visitedNodesText = String.Format("Visited nodes: {0}", t.getVisitedNodes());
            String totalNodesText = String.Format("Total nodes: {0}", t.getTotalNodes());

            DebugInformation visitedNodes = GeometryFriends.AI.Debug.DebugInformationFactory.CreateTextDebugInfo(new PointF(5, 5), visitedNodesText, new GeometryFriends.XNAStub.Color(255, 255, 255));
            DebugInformation totalNodes = GeometryFriends.AI.Debug.DebugInformationFactory.CreateTextDebugInfo(new PointF(5, 50), totalNodesText, new GeometryFriends.XNAStub.Color(255, 255, 255));

            debugInfo.Add(visitedNodes);
            debugInfo.Add(totalNodes);

            return debugInfo;
        }

        public List<DebugInformation> getDebugTreeInfo(TreeMP tree)
        {
            List<DebugInformation> debugInfo = new List<DebugInformation>();

            debugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());

            foreach (DiamondInfo diamond in diamondsInfo)
            {
                debugInfo.AddRange(diamond.getAreaDebug());
            }

            foreach (NodeMP node in tree.getNodes())
            {
                //agent
                debugInfo.Add(GeometryFriends.AI.Debug.DebugInformationFactory.CreateCircleDebugInfo(new PointF(node.getState().getPosX(), node.getState().getPosY()), 2.0f, new GeometryFriends.XNAStub.Color(255, 255, 255)));
                //partner
                debugInfo.Add(GeometryFriends.AI.Debug.DebugInformationFactory.CreateCircleDebugInfo(new PointF(node.getState().getPartnerX(), node.getState().getPartnerY()), 2.0f, new GeometryFriends.XNAStub.Color(255, 0, 0)));
            }

            return debugInfo;
        }

        public bool getCorrection()
        {
            return correction;
        }

        public PointMP getConnectionPoint()
        {
            return connectionPoint;
        }

        public NodeMP findNode(float agentX, float agentY, float partnerX, float partnerY, int diamondNumber, bool bin)
        {
            int posX = (int)Math.Round(agentX / matrixSize);
            int posY = (int)Math.Round(agentY / matrixSize);
            int partX = (int)Math.Round(partnerX / matrixSize);
            int partY = (int)Math.Round(partnerY / matrixSize);

            //make sure it is within bounds
            if (posX >= positions.GetLength(0))
            {
                posX = positions.GetLength(0) - 1;
            }
            if (posY >= positions.GetLength(1))
            {
                posY = positions.GetLength(1) - 1;
            }
            if (partX >= positions.GetLength(0))
            {
                partX = positions.GetLength(0) - 1;
            }
            if (partY >= positions.GetLength(1))
            {
                partY = positions.GetLength(1) - 1;
            }

            if (bin)
            {
                if (previousPositions[posX, posY, partX, partY, diamondNumber] != null)
                {
                    return previousPositions[posX, posY, partX, partY, diamondNumber];
                }
                return null;
            }
            if (positions[posX, posY, partX, partY, diamondNumber] != null )
            {
                return positions[posX, posY, partX, partY, diamondNumber];
            }
            return null;
        }

        public void saveOldNodes(TreeMP t)
        {
            previousTreeNodes = new List<NodeMP>();
            foreach(NodeMP node in t.getNodes())
            {
                previousTreeNodes.Add(node);
            }

            previousPositions = new NodeMP[area.Right / matrixSize, area.Bottom / matrixSize, area.Right / matrixSize, area.Bottom / matrixSize, totalCollectibles + 1];

            for(int i = 0 ; i < area.Right / matrixSize; i++)
            {
                for(int j = 0; j < area.Bottom / matrixSize; j++)
                {
                    for(int k = 0; k < area.Right / matrixSize; k++)
                    {
                        for(int l = 0; l < area.Bottom / matrixSize; l++)
                        {
                            for (int m = 0; m < totalCollectibles + 1; m++)
                            {
                                previousPositions[i, j, k, l, m] = positions[i, j, k, l, m];
                            }
                        }
                    }
                }
            }
        }

        public void setReplan(bool r)
        {
            replan = r;
        }

    }
}