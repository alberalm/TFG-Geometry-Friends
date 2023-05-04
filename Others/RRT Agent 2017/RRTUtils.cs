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
    public class RRTUtils
    {
        /******test variables******/
        private bool semiplanTest;
        private bool cutplan;
        private bool bgt;
        private int mu = 10;
        private float bias = 0.75f; //TODO test
        /******Algorithm variables******/
        //public Tree T;
        private float actionTime;
        private float simTime;
        private float simTimeFinish;
        private float radius;
        private float jumpBias = 0.3f; //TODO test
        private float samePlatformTime = 1.0f;
        private float samePlatformTimeInc = 1.0f;
        private float maxTime = 20.0f;
        private float smallPlatformTime = 0.1f;
        private float mediumPlatformTime = 1.0f;
        private float largePlatformTime = 2.0f;
        private float defaultActionTime;
        private List<Moves> possibleMoves;
        private int smallPlatformWidth = 100;
        private int largePlatformWidth = 400;
        private int iterations = 500;
        private int rrtIterations = 1;
        private int correctionIterations = 1000;
        private int matrixSize = 20;
        private int matrixDec = 5;
        private bool[,,] positions;
        private bool goal = false;
        private bool semiPlan = false;
        private bool samePlatformPlan = false;
        private RRTTypes stateSelection;
        private RRTTypes actionSelection;
        private ObstacleRepresentation[] platforms;
        private Stopwatch time;
        private Random rnd;

        /******STP variables ******/
        private float skillYmargin = 70;
        private float skillXmargin = 70;
        private float skillVelMargin = 10;
        private float skillJumpBias = 0.5f;
        private float stpBias = 0.3f;

        /******Plan variables******/
        private List<Moves> plan = new List<Moves>();
        private List<Node> planNodes = new List<Node>();
        private float marginMinX = 1.0f;
        private float marginMinY = 1.0f;
        private float marginMinVX = 20.0f;
        private float marginMinVY = 20.0f;
        private float marginDenominator = 15.0f;

        /******correct plan variables******/
        private bool correction = false;
        private bool newPlan = false;
        private float correctionPosMargin = 5.0f;
        private float correctionVelMargin = 20.0f;
        private Node connectNode;
        private Node bestSemiPlanNode;
        private bool samePlatform;
        private PathPlan previousPoints;
        private Point connectionPoint;
        private List<Platform> previousPlatforms;

        /******debug variables******/
        private List<DebugInformation> debugInfo;
        private bool debugTree = true;

        /******level variables ******/
        private int charType; //0 -> circle; 1 -> rectangle
        private int totalCollectibles;
        private float circleMaxJump = 400;
        private float gSpeed = 1;
        private float initialPosX;
        private float initialPosY;
        private Platform initialPlatform;
        private List<DiamondInfo> diamondsInfo;
        private List<Platform> platformsInfo;
        private ActionSimulator simulator;
        private Rectangle area;
        private Utils utils;

        /******circle and rectangle STP******/
        private CircleSTP circleSTP;
        private RectangleSTP rectangleSTP;        

        public RRTUtils(float aTime, float sTime, float sTimeFinish, List<Moves> pMoves, int cType, Rectangle levelArea, int totalColl, RRTTypes sSelect, RRTTypes aSelect, ObstacleRepresentation[] plt, float gameSpeed, List<DiamondInfo> dmds, List<Platform> plfms, Utils utls, bool sp, bool c)
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
            circleSTP = new CircleSTP(utils, rnd, platforms, area);
            rectangleSTP = new RectangleSTP(utils, rnd, platforms, area);

            //for tests
            semiplanTest = sp;
            cutplan = c;
            if (sSelect == RRTTypes.BGT || sSelect == RRTTypes.BGTBias || sSelect == RRTTypes.BGTAreaBias)
            {
                bgt = true;
            } else
            {
                bgt = false;
            }
        }

        /*******************************************************/
        /*                  RRT funtions                       */
        /*******************************************************/

        //Builds a new tree from scratch
        public Tree buildNewRRT(State initialState, ActionSimulator predictor)
        {
            goal = false;
            simulator = predictor;
            time.Restart();

            initialPosX = initialState.getPosX();
            initialPosY = initialState.getPosY();
            initialPlatform = platformBelow(initialPosX, initialPosY);

            foreach (DiamondInfo diamond in diamondsInfo)
            {
                diamond.resetStates();
            }

            return buildRRT(initialState, simulator);
        }

        //RRT algorithm
        public Tree buildRRT(State initialState, ActionSimulator predictor)
        {
            //initialize tree
            goal = false;
            positions = new bool[area.Right / matrixSize, area.Bottom / matrixSize, totalCollectibles + 1];
            Tree currentTree = new Tree(initialState, predictor, copyMoves(), bgt);
            simulator = predictor;
            return RRT(currentTree);
        }

        public Tree RRT(Tree currentTree)
        {
            //if it has already passed too much time and there is a state with caught diamonds, then start the plan from there
            if(semiplanTest && samePlatformTime < maxTime)
            {
                if((time.ElapsedMilliseconds * 0.001f * gSpeed > samePlatformTime && bestSemiPlanNode != null && samePlatformPlan && !correction))
                {
                    currentTree.setGoal(bestSemiPlanNode);
                    bestSemiPlanNode = null;
                    samePlatformPlan = false;
                    return currentTree;
                }
            }
            if (semiplanTest && time.ElapsedMilliseconds * 0.001f * gSpeed > maxTime && bestSemiPlanNode != null && !correction )
            {
                currentTree.setGoal(bestSemiPlanNode);
                bestSemiPlanNode = null;
                return currentTree;
            }

            //if there are no more open nodes, start new tree with new position matrix
            while(currentTree.getOpenNodes().Count == 0)
            {

                if(matrixSize > 15)
                {
                    matrixSize -= 3;
                } else if(matrixSize > 10)
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
                
                //positions = new bool[area.Right / matrixSize, area.Bottom / matrixSize, totalCollectibles + 1];
                //currentTree = new Tree(currentTree.getRoot().getState(), currentTree.getRoot().getPredictor(), copyMoves(), bgt);
                goal = false;
            }
            Node randNode;
            //tree with a maximum number (iterations) of branches 
            for (int i = 1; i <= iterations; i++)
            {
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

        private void extend(Tree tree, Node node)
        {
            Moves newAction;
            //selecting a random action
            switch (actionSelection)
            {
                case RRTTypes.Original:
                    newAction = randomAction(node);
                    break;
                case RRTTypes.STP:
                    newAction = STPAction(node);
                    break;
                default:
                    newAction = randomAction(node);
                    break;
            }

            //If the STP returned no action then all the actions of the skill where exhausted and the bias indicated to ignore this state
            if(actionSelection == RRTTypes.STP && newAction == Moves.NO_ACTION)
            {
                return;
            }

            //if this is the last action to test on this node then close it
            if (node.possibleMovesCount() == 0)
            {
                tree.closeNode(node);
            }

            State selectedState = node.getState();
            ActionSimulator selectedSim = node.getPredictor().CreateUpdatedSimulator();

            //simulate action applied to the selected state
            State newState = applyPrediction(selectedState, node, newAction, selectedSim);

            //check if the state already exists - if it does, do not put it in the tree
            if (repeatedState(newState))
            {
                return;
            }

            List<Moves> newMoves;
            //check if circle agent is on a platform or middle jump/fall
            if(charType == 0)
            {
                if (onPlatform(newState.getPosX(), newState.getPosY()+radius, 0) != null)
                {
                    newMoves = copyMoves();
                } else
                {
                    newMoves = copyMovesNoJump();
                }
            } else
            {
                newMoves = copyMoves();
            }

            //add node to the tree
            Node newNode = new Node(node, newState, newAction, selectedSim, newMoves);
            node.addChild(newNode);
            tree.addNode(newNode);

            //see if the node is close to any diamond
            foreach(DiamondInfo diamond in diamondsInfo)
            {
                diamond.insertClosestNode(newNode);
            }

            //check if the new state is a goal
            //TODO - make the goal verification in a different method to be easier to indicate what is the goal to be considered
            //if it is a plan correction, check if the agent is in a platform of the previous plan with the same caught/uncaught collectibles
            if (correction)
            {
                Platform currentPlatform = onPlatform(newState.getPosX(), newState.getPosY(), 50);
                if(currentPlatform != null)
                {
                    List<Point> points = previousPoints.getPathPoints();
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (newState.getUncaughtCollectibles().Count == points[i].getUncaughtColl().Count)
                        {
                            bool samePlatformState = true;
                            //check is the platform is the same
                            if(currentPlatform.getX() != previousPlatforms[i].getX() || currentPlatform.getY() != previousPlatforms[i].getY())
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

        private State applyPrediction(State selectedState, Node node, Moves newAction, ActionSimulator stateSim)
        {
            //get the size of the platform the agent is on to calculate the duration of the simulation
            Platform plat = onPlatform(selectedState.getPosX(), selectedState.getPosY(), 50);
            if(plat != null)
            {
                //TODO check obstacles
                if(plat.getWidth() == 0 || plat.getWidth() > largePlatformWidth)
                {
                    actionTime = largePlatformTime;
                }
                else if(plat.getWidth() < smallPlatformWidth)
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
            stateSim.AddInstruction(newAction, actionTime);
            stateSim.SimulatorCollectedEvent += delegate (Object o, CollectibleRepresentation col) { simCaughtCollectibles.Add(col); };

            stateSim.Update(.05f);

            for (float i = 0; i < actionTime; i += .05f)
            {
                stateSim.Update(.05f);
            }

            //prepare all the debug information to be passed to the agents manager
            debugInfo = new List<DebugInformation>();

            //add the simulator debug information
            debugInfo.AddRange(stateSim.SimulationHistoryDebugInformation);

            //create the resulting state
            State newState;
            if (charType == 0)
            {
                newState = new State(stateSim.CirclePositionX, stateSim.CirclePositionY, stateSim.CircleVelocityX, stateSim.CircleVelocityY, radius, stateSim.CircleVelocityRadius, selectedState.getCaughtCollectibles(), stateSim.CollectiblesUncaught);
            }
            else
            {
                newState = new State(stateSim.RectanglePositionX, stateSim.RectanglePositionY, stateSim.RectangleVelocityX, stateSim.RectangleVelocityY, stateSim.RectangleHeight/2, 0, selectedState.getCaughtCollectibles(), stateSim.CollectiblesUncaught);
            }

            return newState;
        }

        //get a node according to the selection type
        private Node getNextNode(Tree tree)
        {
            switch (stateSelection)
            {
                case RRTTypes.Original:
                    return getNearestNode(tree, false);
                case RRTTypes.Bias:
                    return getNodeBiasedState(tree);
                case RRTTypes.BGT:
                    return getBGTNode(tree);
                case RRTTypes.BGTBias:
                    return getRandomNode(tree, true);
                case RRTTypes.BGTAreaBias:
                    return getBGTAreaBiasNode(tree);
                case RRTTypes.AreaBias:
                    return getAreaBiasNode(tree);
                default:
                    return getNearestNode(tree, false);
            }
        }

        //get a random node from tree biased to a diamond area
        private Node getAreaBiasNode(Tree tree)
        {
            Node randomNode;

            if (rnd.NextDouble() <= bias)
            {
                //get a random diamond to bias to
                DiamondInfo randomDiamond = diamondsInfo[rnd.Next(diamondsInfo.Count)];

                randomNode = randomDiamond.getRandomClosestState();

                if (stateSelection == RRTTypes.STP)
                {
                    while (randomNode != null && !randomNode.anyRemainingSTPActions() && rnd.NextDouble() >= stpBias)
                    {
                        randomNode = randomDiamond.getRandomClosestState();
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
        private Node getBGTAreaBiasNode(Tree tree)
        {
            Node randomNode;
            
            if(rnd.NextDouble() <= bias)
            {
                //get a random diamond to bias to
                DiamondInfo randomDiamond = diamondsInfo[rnd.Next(diamondsInfo.Count)];

                randomNode = randomDiamond.getRandomClosestState();

                if(stateSelection == RRTTypes.STP)
                {
                    while (randomNode != null && !randomNode.anyRemainingSTPActions() && rnd.NextDouble() >= stpBias)
                    {
                        randomNode = randomDiamond.getRandomClosestState();
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
        private Node getNearestNode(Tree tree, bool biased)
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
            } else
            {
                stateCol = rnd.Next(totalCollectibles);
                var random = rnd.NextDouble();
                stateX = (float)random * (area.Right - area.Left) + area.Left;
                random = rnd.NextDouble();
                stateY = (float)random * (area.Bottom - area.Top) + area.Top;
            }
            
            //by default the nearest node is the first of the open nodes
            Node nearestNode = tree.getOpenNodes()[0];
            int nearestColl = totalCollectibles + 1;
            float nearestPos = (float)Math.Pow(area.Right * area.Bottom, 2);
            State currentState;
            //search for the nearest node in the open nodes of the tree
            foreach (Node node in tree.getOpenNodes())
            {
                currentState = node.getState();
                //when it is biased to a goal state but the agent already caught the selected diamond then it is not a close state
                if (biased && checkDiamond(tree.getRoot().getState().getUncaughtCollectibles()[randomDiamond], currentState.getUncaughtCollectibles()))
                {
                    continue;
                }
                else if (Math.Abs(stateCol - currentState.getUncaughtCollectibles().Count) < nearestColl)
                {
                    nearestNode = node;
                    nearestColl = stateCol - currentState.getUncaughtCollectibles().Count;
                    nearestPos = getDistance(stateX, stateY, currentState.getPosX(), currentState.getPosY());
                    if (nearestPos == 0)
                    {
                        nearestPos = (float)distance(stateX, currentState.getPosX(), stateY, currentState.getPosY());
                    }
                }
                else {
                    double dist = 0;
                    //check if there is a platform between the agent and the selected position
                    dist = getDistance(stateX, stateY, currentState.getPosX(), currentState.getPosY());
                    //if not, get the euclidean distance
                    if(dist == 0)
                    {
                        dist = distance(stateX, currentState.getPosX(), stateY, currentState.getPosY());
                    }
                    
                    if (dist < nearestPos)
                    {
                        nearestNode = node;
                        nearestColl = currentState.getUncaughtCollectibles().Count;
                        nearestPos = (float)dist;
                    }
                }
            }
            tree.addVisitedNode();
            return nearestNode;
        }

        private Node getNodeBiasedState(Tree tree)
        {
            float random = (float)rnd.NextDouble();

            //from 0% to bias% choose a goal state
            if(random <= bias)
            {
                return getNearestNode(tree, true);
            }
            else //else choose a random state
            {
                return getNearestNode(tree, false);
            }
        }

        private Node getBGTNode(Tree tree)
        {
            float ratio = tree.ratio();

            if(ratio > mu)
            {
                return tree.getRandomNonLeafNode();
            }
            else
            {
                return tree.getRandomLeafNode();
            }
        }
        
        private Node getRandomNode(Tree tree, bool biased)
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

        private bool repeatedState(State state)
        {
            int posX = (int)Math.Round(state.getPosX()/matrixSize);
            int posY = (int)Math.Round(state.getPosY()/matrixSize);

            //make sure it is within bounds
            if (posX >= positions.GetLength(0))
            {
                posX = positions.GetLength(0) - 1;
            }
            if (posY >= positions.GetLength(1))
            {
                posY = positions.GetLength(1) - 1;
            }

            //check if there is already a state at that position
            if (positions[posX, posY, state.getNumberUncaughtCollectibles()])
            {
                return true;
            }
            //if it is not a repeated state, then update the matrix
            positions[posX, posY, state.getNumberUncaughtCollectibles()] = true;

            return false;
        }

        /*******************************************************/
        /*                          STP                        */
        /*******************************************************/
        private Moves STPAction(Node node)
        {
            //Tactic/skills - select diamond, go to diamond, catch diamond
            Moves action = Moves.NO_ACTION;
     
            //select the next diamond to catch - One on the platform if any 
            DiamondInfo highestDiamond = null;
            foreach(DiamondInfo dInfo in diamondsInfo)
            {
                foreach(CollectibleRepresentation diamond in node.getState().getUncaughtCollectibles())
                {
                    if(Math.Round(dInfo.getX()) == Math.Round(diamond.X) && 
                        Math.Round(dInfo.getY()) == Math.Round(diamond.Y))
                    {
                        //get the current platform the agent is on
                        Platform platform = onPlatform(node.getState().getPosX(), node.getState().getPosY(), 50);

                        //if the agent is on the same platform as the diamond
                        //make sure there isn't a platform blocking the way
                        if(platform != null && dInfo.getPlatform().getX() == platform.getX() && 
                            dInfo.getPlatform().getY() == platform.getY() &&
                            Math.Abs(dInfo.getY() - node.getState().getPosY()) <= circleMaxJump &&
                            !utils.obstacleBetween(node.getState().getPosX(), dInfo.getX(), platform))
                        {
                            //if circle
                            if(charType == 0)
                            {
                                action = circleSTP.skillCatchDiamond(node, dInfo);
                            } 
                            //if rectangle
                            else if(charType == 1)
                            {
                                action = rectangleSTP.skillCatchDiamond(node, dInfo);
                            }
                            else
                            {
                                throw new Exception("the character id must be either 0 (circle) or 1 (rectangle)");
                            }
                            
                            return getSTPAction(node, action);
                        }
                        
                    }
                }
            }

            //get the list of caught and uncaught collectibles
            int[] cCol = getCaughtDiamondsIndexes(node.getState().getUncaughtCollectibles());
            foreach (DiamondInfo diamond in diamondsInfo)
            {
                //the first uncaught collectible is the highest
                if(cCol[diamond.getId()] == 0)
                {
                    highestDiamond = diamond;
                    break;
                }
            }
            //if there isn't any diamond on the same platform as the agent, select the highest diamond in the highest platform
            if(highestDiamond != null)
            {
                action = skillCatchHighestDiamond(node, highestDiamond);
            }
            
            return getSTPAction(node, action);
        }

        private Moves getSTPAction(Node node, Moves action)
        {
            if (action != Moves.NO_ACTION)
            {
                node.removeMove(action);
                return action;
            }
            else
            {
                return randomAction(node);
            }
        }

        private Moves skillCatchHighestDiamond(Node node, DiamondInfo dInfo)
        {
            Moves action = Moves.NO_ACTION;

            //if the platform of the diamond is higher than the one the agent is on
            if(node.getState().getPosY() > dInfo.getPlatform().getY())
            {
                //if circle
                if (charType == 0)
                {
                    action = circleSTP.skillHigherPlatform(node, dInfo);
                }
                //if rectangle
                else if (charType == 1)
                {
                    action = rectangleSTP.skillHigherPlatform(node, dInfo);
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
                    action = circleSTP.skillLowerPlatform(node, dInfo);
                }
                //if rectangle
                else if (charType == 1)
                {
                    action = rectangleSTP.skillLowerPlatform(node, dInfo);
                }
                else
                {
                    throw new Exception("the character id must be either 0 (circle) or 1 (rectangle)");
                }
                
            }
            return action;
        }
        
        /*******************************************************/
        /*                Correction funtions                  */
        /*******************************************************/

        private Tree repopulateMatrix(Tree t)
        {
            //new matrix
            positions = new bool[area.Right / matrixSize, area.Bottom / matrixSize, totalCollectibles + 1];

            List<Moves> newMoves;

            //reset tree info
            t.resetTree();

            foreach (Node node in t.getNodes())
            {
                //check if circle agent is on a platform or middle jump/fall
                if (charType == 0)
                {
                    if (onPlatform(node.getState().getPosX(), node.getState().getPosY() + radius, 0) != null)
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
                repeatedState(node.getState());
            }
            
            goal = false;

            return t;
        }

        public void correctPlan(PathPlan plan)
        {
            correction = true;
            //get only the points where the agent is on a platform so it can be tested if the platforms have been reached
            previousPoints = new PathPlan(cutplan);
            previousPlatforms = new List<Platform>();
            List<Point> points = plan.getPathPoints();
            for(int i = 0; i <  points.Count; i++)
            {
                Platform pointPl = onPlatform(points[i].getPosX(), points[i].getPosY() + radius, 25);
                if (pointPl != null)
                {
                    previousPoints.addPoint(points[i]);
                    previousPlatforms.Add(pointPl);
                }
            }
        }

        //Builds a tree that will connect with the previous version
        public Tree correctRRT(State initialState, ActionSimulator predictor, Tree t)
        {
            correction = true;
            newPlan = false;
            goal = false;
            iterations = correctionIterations;
            Tree newTree = buildRRT(initialState, predictor);
            //if we got a new plan for the original goal replace the tree completely
            if (goal)
            {
                if (newPlan)
                {
                    t = newTree;
                }
                else {
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

        private Tree connectTrees(Tree newTree, Tree previousTree)
        {
            //connect the new tree node to the next node in the plan
            connectNode.connectNodes(planNodes[0]);
            return null;
        }

        //see if the current calculated state is the same as the next one in the plan
        private bool stateReached(State state)
        {
            //TODO - check any state in the plan
            return checkState(state, planNodes[0].getState());
        }

        //temporary - plan from file
        private bool stateReachedTemp(State state)
        {
            //TODO - check any state in the plan
            return checkStateTemp(state, planNodes[0].getState());
        }

        private bool checkState(State state1, State state2)
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
        private bool checkStateTemp(State state1, State state2)
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

        public void getPathPlan(Tree t)
        {
            //create a list with the ordered actions to aplly from the tree
            Node currentNode = t.getGoal();
            while (currentNode.getParent() != null)
            {
                //insert action to the plan
                plan.Insert(0, currentNode.getAction());
                //insert the state it is before applying the action
                planNodes.Insert(0, currentNode.getParent());

                currentNode = currentNode.getParent();
            }
        }

        public List<Node> getPathPlan(Node node)
        {
            //create a list with the ordered actions to aplly from the tree
            Node currentNode = node;
            List<Node> currentPlanNodes = new List<Node>();
            while (currentNode.getParent() != null)
            {
                currentPlanNodes.Insert(0, currentNode.getParent());
                currentNode = currentNode.getParent();
            }
            return currentPlanNodes;
        }
        public PathPlan getPlan(Tree t)
        {
            PathPlan p = new PathPlan(cutplan);

            Node currentNode = t.getGoal();

            p.setTotalCollectibles(currentNode.getState().getNumberUncaughtCollectibles());

            State currentState;
            Point point;

            while (currentNode.getParent() != null)
            {

                currentState = currentNode.getState();
                if(charType == 0)
                {
                    //the jump action needs the point it has to make the jump and not where to reach
                    if(currentNode.getAction()== Moves.JUMP)
                    {
                        point = new Point(currentNode.getParent().getState().getPosX(), currentNode.getParent().getState().getPosY(), currentNode.getParent().getState().getVelX(), currentState.getHeight(), currentNode.getAction(), currentState.getUncaughtCollectibles());
                    }
                    else
                    {
                        point = new Point(currentState.getPosX(), currentState.getPosY(), currentState.getVelX(), currentState.getHeight(), currentNode.getAction(), currentState.getUncaughtCollectibles());
                    }
                }
                else
                {
                    point = new Point(currentState.getPosX(), currentState.getPosY(), currentState.getVelX(), currentState.getHeight(), currentNode.getAction(), currentState.getUncaughtCollectibles());
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

        public bool checkState(Point point, float X, float Y, float velX, float velY, List<CollectibleRepresentation> uncaughtColl)
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

        public bool checkState(State planState, float X, float Y, float velX, float velY, List<CollectibleRepresentation> caughtColl)
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
        public bool checkStateTemp(State planState, float X, float Y, float velX, float velY, List<CollectibleRepresentation> caughtColl)
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

        public List<Moves> getPlanMoves()
        {
            return plan;
        }

        public List<Node> getPlanNodes()
        {
            return planNodes;
        }

        private bool checkPlan(Tree tree)
        {
            //get plan
            getPathPlan(tree);
            List<Moves> actionList = plan;

            //prepare simulator
            List<CollectibleRepresentation> simCaughtCollectibles = new List<CollectibleRepresentation>();
            ActionSimulator toSim = simulator;

            //add all actions of the plan to simulate
            foreach(Moves action in actionList)
            {
                toSim.AddInstruction(action, actionTime);
            }

            toSim.SimulatorCollectedEvent += delegate (Object o, CollectibleRepresentation col) { simCaughtCollectibles.Add(col); };

            //simulate
            for (float i = 0; i < actionTime * actionList.Count; i += .05f)
            {
                toSim.Update(.05f);
            }
            toSim.Update(.05f);

            //if the final state of the simulation is a goal state then return true
            if(toSim.CollectiblesUncaughtCount == 0)
            {
                return true;
            } else
            {
                return false;
            }
                      
        }

        private void checkSemiPlan(Tree tree, Node newNode)
        {
            //if the agent is in the same platform as a diamond, this might be the safest semi-plan so far
            if(onSamePlatform(0, initialPlatform, newNode.getState().getUncaughtCollectibles(), true))
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
        private Node getOnlyHighest(Node node)
        {
            Node currentNode = node;
            int[] caughtDiamonds;
            int lowerDiamonds = 0;
            int newIndex;

            //get the order of the caught diamonds
            //get the path
            List<Node> currentPlan = getPathPlan(currentNode);
            int lastDiamondCount = currentPlan[0].getState().getNumberUncaughtCollectibles();
            int totalDiamonds = lastDiamondCount;
            newIndex = currentPlan.Count;

            for (int i = 0; i < currentPlan.Count; i++)
            {
                caughtDiamonds = getCaughtDiamondsIndexes(currentPlan[i].getState().getUncaughtCollectibles());
                if(caughtDiamonds[0] == 0 && currentPlan[i].getState().getNumberUncaughtCollectibles() < lastDiamondCount)
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
                    } else
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

            for(int i = 0; i < indexes.Length; i++)
            {
                indexes[i] = 1;
            }

            foreach(CollectibleRepresentation diamond in diamonds)
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

            if(currentHighest > bestHighest)
            {
                return true;
            }

            return false;
        }

        private int getHighestNumber(int[] diamonds)
        {
            //if the highest diamond in the highest platform wasn't caught, return 0
            if(diamonds[0] == 0)
            {
                return 0;
            }
            //else, count how many of the highest were caught
            else
            {
                int i = 1;
                while(diamonds[i] == 1)
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

            for(int i = 0; i < diamondsInfo.Count; i++)
            {
                //if the highest caught diamond belongs to the current plan then return true
                if(diamonds[i] == 1 && bestCaughtDiamonds[i] == 0)
                {
                    return true;
                }
                if(bestCaughtDiamonds[i] == 1)
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

        private List<Moves> copyMoves()
        {
            List<Moves> newMovesList = new List<Moves>();

            foreach (Moves move in possibleMoves)
            {
                newMovesList.Add(move);
            }

            return newMovesList;
        }

        private List<Moves> copyMovesNoJump()
        {
            List<Moves> newMovesList = new List<Moves>();

            foreach (Moves move in possibleMoves)
            {
                if(move != Moves.JUMP)
                {
                    newMovesList.Add(move);
                }
            }

            return newMovesList;
        }

        private double distance(float x1, float x2, float y1, float y2)
        {
            return (Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

        private Moves randomAction(Node node)
        {
            //Random rnd = new Random();
            Moves action;

            if (charType == 0) //circle
            {
                if(rnd.NextDouble() > jumpBias || node.possibleMovesCount() <= 1)
                {
                    //choose from all moves
                    action = node.getMoveAndRemove(rnd.Next(node.possibleMovesCount()));
                } else
                {
                    //chose a move that is not jump unless it is the only move left
                    int random = rnd.Next(node.possibleMovesCount());
                    if(node.getRemainingMoves()[random] == Moves.JUMP)
                    {
                        random = (random + 1) % node.possibleMovesCount();
                    }
                    action = node.getMoveAndRemove(random);
                }
            } else //rectangle
            {
                if (rnd.NextDouble() > jumpBias || node.possibleMovesCount() <= 2)
                {
                    //choose from all moves
                    action = node.getMoveAndRemove(rnd.Next(node.possibleMovesCount()));
                }
                else
                {
                    //exclude the morph moves
                    action = node.getMoveAndRemove(rnd.Next(node.possibleMovesCount() - 2));
                }
            }

            return action;
        }

        private double minDistance(float x1, float y1, float x2, float y2, ObstacleRepresentation platform)
        {
            double distance = 0;
            //if a point is at the left of the platform but not below
            if(x1 - (platform.X - platform.Width/2) < 0)
            {
                distance = Math.Abs(y2 - y1) + x2 - (platform.X - platform.Width / 2);
            }
            //if a point is at the right of the platform but not below 
            else if (x1 - (platform.X + platform.Width / 2) > 0){
                distance = Math.Abs(y2 - y1) + (platform.X + platform.Width / 2) - x2;
            }
            else
            {
                distance = Math.Min(Math.Abs(y2 - y1) + (x1 - (platform.X - platform.Width / 2)) + (x2 - (platform.X - platform.Width / 2)), Math.Abs(y2 - y1) + x1 - (platform.X + platform.Width / 2) + (platform.X + platform.Width / 2) - x2);
            }
            return distance;
        }

        private bool checkObstacle(float dPosX, float dPosY, float cPosX, float cPosY, ObstacleRepresentation obstacle)
        {
            //TODO - count with the side edges
            //platform top and bottom edges
            float xLeft = obstacle.X - obstacle.Width / 2;
            float xRight = obstacle.X + obstacle.Width / 2;
            float yUp = obstacle.Y + obstacle.Height / 2;
            float yDown = obstacle.Y - obstacle.Height / 2;

            bool intersection = lineIntersection(dPosX, dPosY, cPosX, cPosY, xLeft, yUp, xRight, yUp);

            if (intersection)
            {
                return true;
            }

            intersection = lineIntersection(dPosX, dPosY, cPosX, cPosY, xLeft, yDown, xRight, yDown);

            if (intersection)
            {
                return true;
            }

            return false;
        }

        private bool lineIntersection(float x1,float y1,float x2, float y2, float x3, float y3, float x4, float y4)
        {

            if ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4) == 0)
            {
                return false;
            }

            float pX = ((x1 * y2 - y1 * x2) * (x3 - x4) - (x1 - x2) * (x3 * y4 - y3 * x4)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));

            float pY = ((x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));

            if (x1 >= x2)
            {
                if (!(x2 <= pX && pX <= x1))
                {
                    return false;
                }
            }
            else {
                if (!(x1 <= pX && pX <= x2)) { return false; }
            }
            if (x3 >= x4)
            {
                if (!(x4 <= pX && pX <= x3)) { return false; }
            }
            else {
                if (!(x3 <= pX && pX <= x4)) { return false; }
            }
            if (y1 >= y2)
            {
                if (!(y2 <= pY && pY <= y1)) { return false; }
            }
            else {
                if (!(y1 <= pY && pY <= y2)) { return false; }
            }
            if (y3 >= y4)
            {
                if (!(y4 <= pY && pY <= y3)) { return false; }
            }
            else {
                if (!(y3 <= pY && pY <= y4)) { return false; }
            }
            return true;
        }

        private float getDistance(float x1, float y1, float x2, float y2)
        {
            double dist = 0;
            foreach (ObstacleRepresentation platform in platforms)
            {
                if (checkObstacle(x1, y1, x2, y2, platform))
                {
                    dist = minDistance(x1, y1, x2, y2, platform);
                }
            }


            return (float)Math.Pow(dist, 2);
        }

        private Platform platformBelow(float x, float y)
        {
            List<ObstacleRepresentation> platformsBelow = new List<ObstacleRepresentation>();

            foreach (ObstacleRepresentation platform in platforms)
            {
                if (x >= (platform.X - platform.Width / 2) &&
                   x <= (platform.X + platform.Width / 2) &&
                   y <= platform.Y)
                {
                    //make sure the highest platform is chosen if there are more than 1 platform below the agent
                    if(platformsBelow.Count == 1 && platformsBelow[0].Y > platform.Y)
                    {
                        platformsBelow.RemoveAt(0);
                    }
                    if (platformsBelow.Count == 0)
                    {
                        platformsBelow.Add(platform);
                    }
                    
                }
            }
            if(platformsBelow.Count == 1)
            {
                return new Platform(platformsBelow[0].X, platformsBelow[0].Y, platformsBelow[0].Width, platformsBelow[0].Height);
            }
            if(platformsBelow.Count == 0)
            {
                return new Platform(0, area.Bottom, 0, 0);
            }
            return null;
        }

        public Platform onPlatform(float x, float y, float margin)
        {
            foreach(ObstacleRepresentation platform in platforms)
            {
                if(x >= (platform.X - platform.Width/2) &&
                   x <= (platform.X + platform.Width/2) &&
                   y + 10 >= (platform.Y - platform.Height/2 - margin) &&
                   y + 10 <= (platform.Y + platform.Height/2 + margin))
                {
                    return new Platform(platform.X, platform.Y, platform.Width, platform.Height);
                }
            }
            if (y + 10 >= 720)
            {
                return new Platform(0, area.Bottom, 0, 0);
            }
            return null;
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


        /*******************************************************/
        /*                   Debug funtions                    */
        /*******************************************************/

        //gets the information of the plan path and of the visited and total nodes
        public List<DebugInformation> getDebugInfo(Tree t, List<DebugInformation> otherInfo)
        {

            List<DebugInformation> debugInfo = new List<DebugInformation>();

            debugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());
            debugInfo.AddRange(t.getGoal().getDebugInfo());

            if(otherInfo != null)
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

        public void writePlanToFile()
        {
            string[] planText = new String[plan.Count * 6]; //6 indica o nr de info por casa passo do plano

            for (int i = 0; i < plan.Count*6; i += 6)
            {
                planText[i] = planNodes[i/6].getAction().ToString();
                planText[i + 1] = planNodes[i/6].getState().getPosX().ToString();
                planText[i + 2] = planNodes[i/6].getState().getPosY().ToString();
                planText[i + 3] = planNodes[i/6].getState().getVelX().ToString();
                planText[i + 4] = planNodes[i/6].getState().getVelY().ToString();
                planText[i + 5] = planNodes[i/6].getState().getNumberUncaughtCollectibles().ToString();
            }
            System.IO.File.WriteAllLines(@".\SimulatedPlan.txt", planText);
        }

        public void readPlanFromFile()
        {
            String line;
            plan = new List<Moves>();
            planNodes = new List<Node>();
            Moves action;
            Node node;
            State state;
            float posX;
            float posY;
            float velX;
            float velY;
            int collectibles;

            System.IO.StreamReader file = new System.IO.StreamReader(@".\SimulatedPlan.txt");

            while ((line = file.ReadLine()) != null)
            {
                //get action
                Enum.TryParse(line, out action);

                //get position and velocity
                line = file.ReadLine();
                posX = Convert.ToSingle(line);
                line = file.ReadLine();
                posY = Convert.ToSingle(line);
                line = file.ReadLine();
                velX = Convert.ToSingle(line);
                line = file.ReadLine();
                velY = Convert.ToSingle(line);

                //get number of uncaught collectibles 
                line = file.ReadLine();
                collectibles = Convert.ToInt32(line);

                //add to plan
                plan.Add(action);

                state = new State(posX, posY, velX, velY, 0, 0, null, null);
                node = new Node(null, state, action, null, null);
            }
            file.Close();
        }

        public List<DebugInformation> getDebugTreeInfo(Tree tree)
        {
            List<DebugInformation> debugInfo = new List<DebugInformation>();

            debugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());

            foreach(DiamondInfo diamond in diamondsInfo)
            {
                debugInfo.AddRange(diamond.getAreaDebug());
            }

            foreach(Node node in tree.getNodes())
            {
                debugInfo.Add(GeometryFriends.AI.Debug.DebugInformationFactory.CreateCircleDebugInfo(new PointF(node.getState().getPosX(), node.getState().getPosY()), 2.0f, new GeometryFriends.XNAStub.Color(255, 255, 255)));
            }
            
            return debugInfo;
        }

        public bool getCorrection()
        {
            return correction;
        }

        public Point getConnectionPoint()
        {
            return connectionPoint;
        }

    }
}