using GeometryFriends;
using GeometryFriends.AI;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GeometryFriendsAgents
{
    class RectangleAgent : AbstractRectangleAgent
    {
        private bool implementedAgent;
        private Moves currentAction;
        private List<Moves> possibleMoves;
        private long lastMoveTime;
        private Random rnd;

        //Sensors Information
        private int[] numbersInfo;
        private float[] rectangleInfo;
        private float[] circleInfo;
        private float[] obstaclesInfo;
        private float[] rectanglePlatformsInfo;
        private float[] circlePlatformsInfo;
        public static float[] collectiblesInfo;

        public static int nCollectiblesLeft;

        private string agentName = "RandRect";

        //Area of the game screen
        protected Rectangle area;

        //Obstacle and open space
        public static int fullHeight = 800;
        public static int fullWidth = 1280;
        public static bool[,] obstacleOpenSpace;

        //Distance map
        private float[,] distanceMap;
        public static int[,] directDistanceMap;

        //Node list
        public static List<Node> nodes;

        //Adjacency matrix
        public static int[,] adjacencyMatrix;
        public static int[,] directionMap;
        enum Direction { Right, RightDown, Down, LeftDown, Left, LeftUp, Up, RightUp };

        //Rectangle size
        public static int[] nSquareSize = { 100, 100 };
        public static int[] hSquareSize = { 200, 50 };
        public static int[] vSquareSize = { 50, 200 };

        //Extra diamond fall down node
        public static int diamondFallDownThreshold = 80;

        //RightDown Edge
        int xThreshold = 80;

        // driver
        int moveStep;
        Driver2 driver;

        //variable to recognize if it is the first update to calc route and create driver
        bool firstUpdate;

        // mcts
        MCTS mcts;

        // output
        public static bool abstractionOutput = false;
        public static bool output = false;

        //runAlgorithm = 0 --> MCTS and AStar
        //runAlgorithm = 1 --> MCTS
        //runAlgorithm = 2 --> Y-Heuristic AStar
        //runAlgorithm = 3 --> Greedy Goal AStar
        //runAlgorithm = 4 --> Permutation AStar
        //runAlgorithm = 5 --> Subgoal AStar
        int runAlgorithm = 5;

        public RectangleAgent() 
        {
            //Change flag if agent is not to be used
            SetImplementedAgent(true);

            lastMoveTime = DateTime.Now.Second;
            currentAction = Moves.NO_ACTION;
            rnd = new Random();

            //prepare the possible moves  
            possibleMoves = new List<Moves>();
            possibleMoves.Add(Moves.MOVE_LEFT);
            possibleMoves.Add(Moves.MOVE_RIGHT);
            possibleMoves.Add(Moves.MORPH_UP);
            possibleMoves.Add(Moves.MORPH_DOWN);
        }

        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
            //convert to old way in order to maintain compatibility
            DeprecatedSetup(
                nI.ToArray(),
                rI.ToArray(),
                cI.ToArray(),
                ObstacleRepresentation.RepresentationArrayToFloatArray(oI),
                ObstacleRepresentation.RepresentationArrayToFloatArray(rPI),
                ObstacleRepresentation.RepresentationArrayToFloatArray(cPI),
                CollectibleRepresentation.RepresentationArrayToFloatArray(colI),
                area,
                timeLimit);
        }

        public void DeprecatedSetup(int[] nI, float[] sI, float[] cI, float[] oI, float[] sPI, float[] cPI, float[] colI, Rectangle area, double timeLimit)
        {
            // Time limit is the time limit of the level - if negative then there is no time constrains
            this.area = area;
            int temp;

            // numbersInfo[] Description
            //
            // Index - Information
            //
            //   0   - Number of Obstacles
            //   1   - Number of Rectangle Platforms
            //   2   - Number of Circle Platforms
            //   3   - Number of Collectibles

            numbersInfo = new int[4];
            int i;
            for (i = 0; i < nI.Length; i++)
            {
                numbersInfo[i] = nI[i];
            }

            nCollectiblesLeft = nI[3];

            // rectangleInfo[] Description
            //
            // Index - Information
            //
            //   0   - Rectangle X Position
            //   1   - Rectangle Y Position
            //   2   - Rectangle X Velocity
            //   3   - Rectangle Y Velocity
            //   4   - Rectangle Height

            rectangleInfo = new float[5];

            rectangleInfo[0] = sI[0];
            rectangleInfo[1] = sI[1];
            rectangleInfo[2] = sI[2];
            rectangleInfo[3] = sI[3];
            rectangleInfo[4] = sI[4];

            // circleInfo[] Description
            //
            // Index - Information
            //
            //   0  - Circle X Position
            //   1  - Circle Y Position
            //   2  - Circle X Velocity
            //   3  - Circle Y Velocity
            //   4  - Circle Radius

            circleInfo = new float[5];

            circleInfo[0] = cI[0];
            circleInfo[1] = cI[1];
            circleInfo[2] = cI[2];
            circleInfo[3] = cI[3];
            circleInfo[4] = cI[4];


            // Obstacles and Platforms Info Description
            //
            //  X = Center X Coordinate
            //  Y = Center Y Coordinate
            //
            //  H = Platform Height
            //  W = Platform Width
            //
            //  Position (X=0,Y=0) = Left Superior Corner

            // obstaclesInfo[] Description
            //
            // Index - Information
            //
            // If (Number of Obstacles > 0)
            //  [0 ; (NumObstacles * 4) - 1]      - Obstacles' info [X,Y,H,W]
            // Else
            //   0                                - 0
            //   1                                - 0
            //   2                                - 0
            //   3                                - 0

            if (numbersInfo[0] > 0)
                obstaclesInfo = new float[numbersInfo[0] * 4];
            else obstaclesInfo = new float[4];

            temp = 1;
            if (nI[0] > 0)
            {
                while (temp <= nI[0])
                {
                    obstaclesInfo[(temp * 4) - 4] = oI[(temp * 4) - 4];
                    obstaclesInfo[(temp * 4) - 3] = oI[(temp * 4) - 3];
                    obstaclesInfo[(temp * 4) - 2] = oI[(temp * 4) - 2];
                    obstaclesInfo[(temp * 4) - 1] = oI[(temp * 4) - 1];
                    temp++;
                }
            }
            else
            {
                obstaclesInfo[0] = oI[0];
                obstaclesInfo[1] = oI[1];
                obstaclesInfo[2] = oI[2];
                obstaclesInfo[3] = oI[3];
            }

            // rectanglePlatformsInfo[] Description
            //
            // Index - Information
            //
            // If (Number of Rectangle Platforms > 0)
            //  [0; (numRectanglePlatforms * 4) - 1]   - Rectangle Platforms' info [X,Y,H,W]
            // Else
            //   0                                  - 0
            //   1                                  - 0
            //   2                                  - 0
            //   3                                  - 0

            if (numbersInfo[1] > 0)
                rectanglePlatformsInfo = new float[numbersInfo[1] * 4];
            else
                rectanglePlatformsInfo = new float[4];

            temp = 1;
            if (nI[1] > 0)
            {
                while (temp <= nI[1])
                {
                    rectanglePlatformsInfo[(temp * 4) - 4] = sPI[(temp * 4) - 4];
                    rectanglePlatformsInfo[(temp * 4) - 3] = sPI[(temp * 4) - 3];
                    rectanglePlatformsInfo[(temp * 4) - 2] = sPI[(temp * 4) - 2];
                    rectanglePlatformsInfo[(temp * 4) - 1] = sPI[(temp * 4) - 1];
                    temp++;
                }
            }
            else
            {
                rectanglePlatformsInfo[0] = sPI[0];
                rectanglePlatformsInfo[1] = sPI[1];
                rectanglePlatformsInfo[2] = sPI[2];
                rectanglePlatformsInfo[3] = sPI[3];
            }

            // circlePlatformsInfo[] Description
            //
            // Index - Information
            //
            // If (Number of Circle Platforms > 0)
            //  [0; (numCirclePlatforms * 4) - 1]   - Circle Platforms' info [X,Y,H,W]
            // Else
            //   0                                  - 0
            //   1                                  - 0
            //   2                                  - 0
            //   3                                  - 0

            if (numbersInfo[2] > 0)
                circlePlatformsInfo = new float[numbersInfo[2] * 4];
            else
                circlePlatformsInfo = new float[4];

            temp = 1;
            if (nI[2] > 0)
            {
                while (temp <= nI[2])
                {
                    circlePlatformsInfo[(temp * 4) - 4] = cPI[(temp * 4) - 4];
                    circlePlatformsInfo[(temp * 4) - 3] = cPI[(temp * 4) - 3];
                    circlePlatformsInfo[(temp * 4) - 2] = cPI[(temp * 4) - 2];
                    circlePlatformsInfo[(temp * 4) - 1] = cPI[(temp * 4) - 1];
                    temp++;
                }
            }
            else
            {
                circlePlatformsInfo[0] = cPI[0];
                circlePlatformsInfo[1] = cPI[1];
                circlePlatformsInfo[2] = cPI[2];
                circlePlatformsInfo[3] = cPI[3];
            }

            //Collectibles' To Catch Coordinates (X,Y)
            //
            //  [0; (numCollectibles * 2) - 1]   - Collectibles' Coordinates (X,Y)

            collectiblesInfo = new float[numbersInfo[3] * 2];

            temp = 1;
            while (temp <= nI[3])
            {

                collectiblesInfo[(temp * 2) - 2] = colI[(temp * 2) - 2];
                collectiblesInfo[(temp * 2) - 1] = colI[(temp * 2) - 1];

                temp++;
            }

            //DebugSensorsInfo();

            firstUpdate = true;
        }

        public Queue<Node> calculateRoute()
        {
            Log.LogInformation("Abstraction calc start");
            System.Diagnostics.Stopwatch sw2 = System.Diagnostics.Stopwatch.StartNew();
            CreateObstacleOpenSpaceArray();
            CreateNodes();
            CreateEdgesAndAdjacencyMatrix();
            Log.LogInformation("Elapsed abstraction calculation time in ms: " + sw2.ElapsedMilliseconds);
            Log.LogInformation("Abstraction calc end");

            Log.LogInformation("Route calc start");

            Queue<Node> route = new Queue<Node>();
            if (runAlgorithm == 0) // MCTS and AStar
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                mcts = new MCTS(nodes, adjacencyMatrix, nCollectiblesLeft, 2000);
                route = mcts.Run();
                route = ClearRoute(route.ToArray(), mcts.outputNodeIndex);

                Node[] routeAsArray = route.ToArray();
                List<Node> diamondNodes = new List<Node>();
                for (int n = 0; n < routeAsArray.Length; n++)
                {
                    if (routeAsArray[n].getDiamond() && !diamondNodes.Contains(routeAsArray[n]))
                    {
                        diamondNodes.Add(routeAsArray[n]);
                    }
                }

                route = calcShortestRouteWithDiamondOrderAStar(diamondNodes);
                Log.LogInformation("Elapsed MCTS and AStar time in ms: " + sw.ElapsedMilliseconds);
            }
            else if (runAlgorithm == 1) // MCTS
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                mcts = new MCTS(nodes, adjacencyMatrix, nCollectiblesLeft, 2000);
                route = mcts.Run();
                route = ClearRoute(route.ToArray(), mcts.outputNodeIndex);
                Log.LogInformation("Elapsed MCTS time in ms: " + sw.ElapsedMilliseconds);
            }
            else if (runAlgorithm == 2) // Y-Heuristic AStar
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                List<Node> diamondNodes = new List<Node>();
                for (int n = 0; n < nodes.Count; n++)
                {
                    if (nodes[n].getDiamond())
                    {
                        diamondNodes.Add(nodes[n]);
                    }
                }
                diamondNodes = calcDiamondOrder(diamondNodes);
                route = calcShortestRouteWithDiamondOrderAStar(diamondNodes);
                Log.LogInformation("Elapsed Y-Heuristic AStar time in ms: " + sw.ElapsedMilliseconds);
            }
            else if (runAlgorithm == 3) // Greedy Goal AStar
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                route = calcShortestRouteAStar();
                Log.LogInformation("Elapsed Greedy Goal AStar time in ms: " + sw.ElapsedMilliseconds);
            }
            else if (runAlgorithm == 4) // Permutation AStar
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                route = calcShortestRouteAStarAllPermutations();
                Log.LogInformation("Elapsed Permutation AStar time in ms: " + sw.ElapsedMilliseconds);
            }
            else if (runAlgorithm == 5) // Subgoal AStar
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                List<int> diamondNodes = new List<int>();
                for (int n = 0; n < nodes.Count; n++)
                {
                    if (nodes[n].getDiamond())
                    {
                        diamondNodes.Add(n);
                    }
                }
                SubgoalAStar sgAstar = new SubgoalAStar(0, diamondNodes, 2000, 0);
                route = sgAstar.Run();
                int diamondsToCollect = nCollectiblesLeft - 1;
                while (route == null)
                {
                    if (diamondsToCollect == 0)
                    {
                        route = new Queue<Node>();
                        break;
                    }
                    sgAstar = new SubgoalAStar(0, diamondNodes, 2000, diamondsToCollect);
                    route = sgAstar.Run();
                    diamondsToCollect--;
                }
                Log.LogInformation("Elapsed Subgoal AStar time in ms: " + sw.ElapsedMilliseconds);
            }
            Log.LogInformation("Route calc end");

            return route;
        }

        public static Queue<Node> calcShortestRouteAStarAllPermutations()
        {
            List<int> diamondNodes = new List<int>();
            for (int n = 0; n < nodes.Count; n++)
            {
                if (nodes[n].getDiamond())
                {
                    diamondNodes.Add(n);
                }
            }
            List<int> diamondRouteList = new List<int>();
            List<List<int>> listOfAllPermuations = new List<List<int>>();

            AddPermutationsToList(diamondRouteList, diamondNodes, listOfAllPermuations);
            if (output)
            {
                Log.LogInformation("Permutation AStar - size of all permutations: " + listOfAllPermuations.Count);
            }

            int maxSize = diamondNodes.Count;
            while (true)
            {
                if (maxSize == 0)
                {
                    if (output)
                    {
                        Log.LogInformation("Permutation AStar - no possible route found!");
                    }
                    return new Queue<Node>();
                }
                List<List<Node>> allCompleteList = new List<List<Node>>();
                List<int> allDistanceCompleteList = new List<int>();
                foreach (List<int> list in listOfAllPermuations)
                {
                    if (list.Count == maxSize)
                    {
                        bool routeFailed = false;
                        List<Queue<Node>> queueList = new List<Queue<Node>>();
                        List<int> distancList = new List<int>();
                        AStar astar = new AStar(0, list[0]);
                        Queue<Node> partRoute = astar.Run();
                        if (partRoute == null)
                        {
                            continue;
                        }
                        queueList.Add(partRoute);
                        distancList.Add(astar.GetCompleteDistance());
                        for (int i = 0; i < list.Count - 1; i++)
                        {
                            astar = new AStar(list[i], list[i + 1]);
                            Queue<Node> partRoute2 = astar.Run();
                            if (partRoute2 == null)
                            {
                                routeFailed = true;
                                break;
                            }
                            queueList.Add(partRoute2);
                            distancList.Add(astar.GetCompleteDistance());
                        }
                        if (routeFailed)
                        {
                            continue;
                        }
                        List<Node> completeList = new List<Node>();
                        completeList.AddRange(queueList[0].ToList());
                        int completeDistance = distancList[0];
                        for (int i = 1; i < queueList.Count; i++)
                        {
                            List<Node> temp = queueList[i].ToList();
                            temp.RemoveAt(0);
                            completeList.AddRange(temp);
                            completeDistance += distancList[i];
                        }


                        allCompleteList.Add(completeList);
                        allDistanceCompleteList.Add(completeDistance);
                    }
                }
                if (allCompleteList.Count == 0)
                {
                    maxSize--;
                    continue;
                }
                int shortest = allDistanceCompleteList[0];
                int shortestIndex = 0;
                for (int i = 1; i < allDistanceCompleteList.Count; i++)
                {
                    if (allDistanceCompleteList[i] < shortest)
                    {
                        shortest = allDistanceCompleteList[i];
                        shortestIndex = i;
                    }
                }
                if (output)
                {
                    Log.LogInformation("Permutation AStar - route found start");
                }
                List<Node> shortestRouteMostDiamonds = allCompleteList[shortestIndex];
                Queue<Node> completeQueue = new Queue<Node>();
                for (int i = 0; i < shortestRouteMostDiamonds.Count; i++)
                {
                    completeQueue.Enqueue(shortestRouteMostDiamonds[i]);
                    if (output)
                    {
                        Log.LogInformation("Permutation AStar - route found:" + nodes.IndexOf(shortestRouteMostDiamonds[i]));
                    }
                }
                if (output)
                {
                    Log.LogInformation("Permutation AStar - route found end");
                }
                return completeQueue;
            }
        }

        private static void AddPermutationsToList(List<int> diamondRouteList, List<int> diamondNodes, List<List<int>> listOfAllPermuations)
        {
            if (diamondRouteList.Count > 0)
            {
                diamondNodes.Remove(diamondRouteList[diamondRouteList.Count - 1]);
                if (diamondNodes.Count == 0)
                {
                    return;
                }
            }
            for (int i = 0; i < diamondNodes.Count; i++)
            {
                List<int> diamondRouteListCopy = new List<int>(diamondRouteList);
                diamondRouteListCopy.Add(diamondNodes[i]);
                listOfAllPermuations.Add(diamondRouteListCopy);
                List<int> diamondNodesCopy = new List<int>(diamondNodes);
                AddPermutationsToList(diamondRouteListCopy, diamondNodesCopy, listOfAllPermuations);
            }
        }

        public static Queue<Node> calcShortestRouteAStar()
        {
            //All diamond nodes
            List<int> diamondIndex = new List<int>();
            for (int n = 0; n < nodes.Count; n++)
            {
                if (nodes[n].getDiamond())
                {
                    diamondIndex.Add(n);
                }
            }

            int startIndex = 0;
            List<Queue<Node>> queueList = new List<Queue<Node>>();
            while (diamondIndex.Count > 0)
            {
                //calc AStar to each diamond
                List<Queue<Node>> queueListTemp = new List<Queue<Node>>();
                List<int> queueDistanceListTemp = new List<int>();
                AStar astar;
                for (int i = 0; i < diamondIndex.Count; i++)
                {
                    astar = new AStar(startIndex, diamondIndex[i]);
                    queueListTemp.Add(astar.Run());
                    queueDistanceListTemp.Add(astar.GetCompleteDistance());
                    if (output)
                    {
                        Log.LogInformation("Greedy Goal AStar - Next Route: from " + startIndex + " to " + diamondIndex[i] + " distance " + astar.GetCompleteDistance());
                    }
                }
                //search shortest
                int shortest = queueDistanceListTemp[0];
                for (int i = 1; i < queueDistanceListTemp.Count; i++)
                {
                    if (queueDistanceListTemp[i] < shortest)
                    {
                        shortest = queueDistanceListTemp[i];
                    }
                }
                //if shortest is a route add to route list, set new start, remove diamond

                if (shortest != int.MaxValue)
                {
                    int shortestIndex = queueDistanceListTemp.IndexOf(shortest);
                    queueList.Add(queueListTemp[shortestIndex]);
                    if (output)
                    {
                        Log.LogInformation("Greedy Goal AStar - Shortest route: from " + startIndex + " to " + diamondIndex[shortestIndex] + " distance " + shortest);
                    }
                    startIndex = diamondIndex[shortestIndex];
                    diamondIndex.Remove(startIndex);
                }
                else
                {
                    if (output)
                    {
                        Log.LogInformation("Greedy Goal AStar - No more possible routes");
                    }
                    break;
                }
            }

            //merge all single routes to one
            if (queueList.Count == 0 || queueList[0] == null)
            {
                return new Queue<Node>();
            }
            List<Node> completeList = new List<Node>();
            completeList.AddRange(queueList[0].ToList());

            for (int i = 1; i < queueList.Count; i++)
            {
                List<Node> temp = queueList[i].ToList();
                temp.RemoveAt(0);
                completeList.AddRange(temp);
            }

            Queue<Node> completeQueue = new Queue<Node>();
            for (int i = 0; i < completeList.Count; i++)
            {
                completeQueue.Enqueue(completeList[i]);
            }
            return completeQueue;
        }

        public static Queue<Node> calcShortestRouteWithDiamondOrderAStar(List<Node> diamondNodes)
        {
            if (diamondNodes.Count == 0)
            {
                if (output)
                {
                    Log.LogInformation("AStar not possible, no diamond nodes");
                }
                return new Queue<Node>();
            }
            //get indices of nodes
            int[] indices = new int[diamondNodes.Count + 1];
            for (int i = 0; i < diamondNodes.Count; i++)
            {
                indices[i + 1] = nodes.IndexOf(diamondNodes[i]);
            }

            //calc AStar routes of each node pair
            List<Queue<Node>> queueList = new List<Queue<Node>>();
            AStar astar;
            for (int i = 0; i < indices.Length - 1; i++)
            {
                astar = new AStar(indices[i], indices[i + 1]);
                queueList.Add(astar.Run());
                if (output)
                {
                    Log.LogInformation("Next AStar Route");
                }
            }

            //merge all single routes to one
            if (queueList.Count == 0 || queueList[0] == null)
            {
                return new Queue<Node>();
            }
            List<Node> completeList = new List<Node>();
            completeList.AddRange(queueList[0].ToList());

            for (int i = 1; i < queueList.Count; i++)
            {
                if (queueList[i] == null)
                {
                    continue;
                }
                List<Node> temp = queueList[i].ToList();
                temp.RemoveAt(0);
                completeList.AddRange(temp);
            }

            Queue<Node> completeQueue = new Queue<Node>();
            for (int i = 0; i < completeList.Count; i++)
            {
                completeQueue.Enqueue(completeList[i]);
            }
            return completeQueue;
        }

        public static List<Node> calcDiamondOrder(List<Node> diamondNodes)
        {
            List<Node> diamondNodesNewOrder = new List<Node>();

            //Order y, highest first means lowest y first           
            while (diamondNodes.Count > 0)
            {
                int selectedIndex = 0;
                Node selected = diamondNodes[0];
                for (int i = 1; i < diamondNodes.Count; i++)
                {
                    if (selected.getY() > diamondNodes[i].getY())
                    {
                        selected = diamondNodes[i];
                        selectedIndex = i;
                    }
                }
                diamondNodesNewOrder.Add(selected);
                diamondNodes.RemoveAt(selectedIndex);
            }
            return diamondNodesNewOrder;
        }

        public static Queue<Node> ClearRoute(Node[] routeNodes, List<int> routeIndex)
        {
            Queue<Node> shortRoute = new Queue<Node>();
            List<int> shortIndex = new List<int>();
            List<Node> visitedDiamond = new List<Node>();
            for (int i = 0; i < routeNodes.Length; i++)
            {
                shortRoute.Enqueue(routeNodes[i]);
                shortIndex.Add(routeIndex[i]);
                int index = 0;
                if (routeNodes[i].getDiamond() && !visitedDiamond.Contains(routeNodes[i]))
                {
                    visitedDiamond.Add(routeNodes[i]);
                }
                for (int j = i + 1; j < routeNodes.Length; j++)
                {
                    if (routeNodes[j].getDiamond() && !visitedDiamond.Contains(routeNodes[j]))
                    {
                        break;
                    }
                    if (routeNodes[i].Equals(routeNodes[j]))
                    {
                        index = j;
                    }
                }
                if (index != 0)
                {
                    i = index;
                }
            }
            if (output)
            {
                Log.LogInformation("Short route: " + shortIndex.Count + " nodes");
                foreach (int k in shortIndex)
                {
                    Log.LogInformation("Short route NodeIndex: " + k);
                }
            }
            return shortRoute;
        }

        private void CreateObstacleOpenSpaceArray()
        {
            obstacleOpenSpace = new bool[fullHeight, fullWidth];
            int obstaclePixelCounter = 0;
            //create black borders of the game area
            for (int i = 0; i < 40; i++)
            {
                for (int j = 0; j < fullWidth; j++)
                {
                    obstacleOpenSpace[i, j] = true;
                    obstaclePixelCounter++;
                    obstacleOpenSpace[fullHeight - 1 - i, j] = true;
                    obstaclePixelCounter++;
                }
                for (int k = 40; k < fullHeight - 40; k++)
                {
                    obstacleOpenSpace[k, i] = true;
                    obstaclePixelCounter++;
                    obstacleOpenSpace[k, fullWidth - 1 - i] = true;
                    obstaclePixelCounter++;
                }
            }
            if (abstractionOutput)
            {
                Log.LogInformation("SQUARE - obstaclePixelCounter - borders - " + obstaclePixelCounter);
            }
            //Fill in obstacles
            for (int i = 0; i < obstaclesInfo.Length; i = i + 4)
            {
                int x = (int)obstaclesInfo[i];
                int y = (int)obstaclesInfo[i + 1];
                int h = (int)obstaclesInfo[i + 2];
                int w = (int)obstaclesInfo[i + 3];
                int upperLeftX = x - (w / 2);
                int upperLeftY = y - (h / 2);
                if (abstractionOutput)
                {
                    Log.LogInformation("SQUARE - obstaclePixelCounter - ULX " + upperLeftX + " - ULY " + upperLeftY + " - h " + h + " - w " + w);
                }
                for (int j = upperLeftY; j < upperLeftY + h; j++)
                {
                    for (int k = upperLeftX; k < upperLeftX + w; k++)
                    {
                        obstacleOpenSpace[j, k] = true;
                        obstaclePixelCounter++;
                    }
                }
            }
            if (abstractionOutput)
            {
                Log.LogInformation("SQUARE - obstaclePixelCounter - borders and obstacles - " + obstaclePixelCounter);
            }

        }

        private void CreateNodes()
        {

            nodes = new List<Node>();

            //Square node
            int squareX = (int)rectangleInfo[0];
            int squareY = (int)rectangleInfo[1];

            int s = 1;
            while (!obstacleOpenSpace[squareY + s, squareX])
            {
                s++;
            }
            Node square = new Node(squareX, squareY + s - 1, false);
            nodes.Add(square);

            //Nodes created by obstacles
            for (int i = 0; i < obstaclesInfo.Length; i = i + 4)
            {
                int x = (int)obstaclesInfo[i];
                int y = (int)obstaclesInfo[i + 1];
                int h = (int)obstaclesInfo[i + 2];
                int w = (int)obstaclesInfo[i + 3];
                int rawX = x - (w / 2);
                int rawY = y - (h / 2);

                //if upper is free create left node
                if (!obstacleOpenSpace[rawY - 1, rawX])
                {
                    Node node1;
                    //if upper left and left is free create upper left node
                    if (!obstacleOpenSpace[rawY - 1, rawX - 1] && !obstacleOpenSpace[rawY, rawX - 1])
                    {
                        node1 = new Node(rawX - 1, rawY - 1, false);
                        nodes.Add(node1);
                        //Node created by obstacles fall down
                        for (int j = rawY; j < fullHeight; j++)
                        {
                            if (obstacleOpenSpace[j, rawX - 1])
                            {
                                Node node2 = new Node(rawX - 1, j - 1, false);
                                if (!nodes.Contains(node2))
                                {
                                    nodes.Add(node2);
                                }
                                break;
                            }
                        }
                    }
                    //if upper left and left is obstacle create upper node
                    else if (obstacleOpenSpace[rawY - 1, rawX - 1] && obstacleOpenSpace[rawY, rawX - 1])
                    {
                        node1 = new Node(rawX, rawY - 1, false);
                        if (!nodes.Contains(node1))
                        {
                            nodes.Add(node1);
                        }
                    }

                }

                rawX = x + (w / 2) - 1;
                //If upper is free create right node
                if (!obstacleOpenSpace[rawY - 1, rawX])
                {
                    Node node1;
                    //if upper right and right is free create upper right node
                    if (!obstacleOpenSpace[rawY - 1, rawX + 1] && !obstacleOpenSpace[rawY, rawX + 1])
                    {
                        node1 = new Node(rawX + 1, rawY - 1, false);
                        nodes.Add(node1);
                        //Node created by obstacles fall down
                        for (int j = rawY; j < fullHeight; j++)
                        {
                            if (obstacleOpenSpace[j, rawX + 1])
                            {
                                Node node2 = new Node(rawX + 1, j - 1, false);
                                if (!nodes.Contains(node2))
                                {
                                    nodes.Add(node2);
                                }
                                break;
                            }
                        }
                    }
                    //if upper right and right is obstacle create upper node
                    else if (obstacleOpenSpace[rawY - 1, rawX + 1] && obstacleOpenSpace[rawY, rawX + 1])
                    {
                        node1 = new Node(rawX, rawY - 1, false);
                        if (!nodes.Contains(node1))
                        {
                            nodes.Add(node1);
                        }
                    }

                }

            }

            //Nodes created by diamonds
            for (int i = 0; i < collectiblesInfo.Length; i = i + 2)
            {
                int x = (int)collectiblesInfo[i];
                int y = (int)collectiblesInfo[i + 1];
                Node node1 = new Node(x, y, true);
                Node node2 = null;
                //Fall down nodes of diamonds
                int j = 1;
                while (!obstacleOpenSpace[y + j, x])
                {
                    j++;
                }
                if (j > 1)
                {
                    node2 = new Node(x, y + j - 1, false);

                }

                if (j == 1)
                {
                    nodes.Add(node1);
                }
                else if (j <= diamondFallDownThreshold)
                {
                    node2.setDiamond(true);
                    nodes.Add(node2);
                }
                else if (j > diamondFallDownThreshold)
                {
                    nodes.Add(node1);
                    nodes.Add(node2);
                }
            }

            if (abstractionOutput)
            {
                //output
                int index = 0;
                foreach (Node n in nodes)
                {
                    Log.LogInformation("SQUARE - Nodes - " + index + " - " + n);
                    index++;
                }
            }
            Log.LogInformation("SQUARE - Number of nodes: " + nodes.Count);
            Log.LogInformation("SQUARE - Number of diamonds: " + collectiblesInfo.Length / 2);
        }

        public static void CreateEdgesAndAdjacencyMatrix()
        {
            adjacencyMatrix = new int[nodes.Count, nodes.Count];
            directionMap = new int[nodes.Count, nodes.Count];
            directDistanceMap = new int[nodes.Count, nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    Node n1 = nodes[i];
                    Node n2 = nodes[j];
                    if (abstractionOutput)
                    {
                        Log.LogInformation("N1: " + n1 + " N2: " + n2);
                    }
                    int[] actionDirectionDistance = CheckEdge(n1, n2);
                    adjacencyMatrix[i, j] = actionDirectionDistance[0];
                    directionMap[i, j] = actionDirectionDistance[1];
                    directDistanceMap[i, j] = actionDirectionDistance[2];
                    if (actionDirectionDistance[1] == 2 && actionDirectionDistance[0] != 0)
                    {
                        nodes[i].setLeadsToFallDown(true);
                    }
                    //adjacencyMatrix[i, j] = CheckEdge(n1, n2);
                    if (abstractionOutput)
                    {
                        Log.LogInformation("Edge: " + adjacencyMatrix[i, j]);
                    }
                }
            }

            //delete diagonal lines between two fall down nodes to prevent to get stuck in a gap
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (adjacencyMatrix[i, j] != 0)
                    {
                        if (nodes[i].getLeadsToFallDown() && nodes[j].getLeadsToFallDown() && (directionMap[i, j] == 1 || directionMap[i, j] == 3) && directDistanceMap[i, j] < 150)
                        {
                            adjacencyMatrix[i, j] = 0;
                        }
                    }
                }
            }

            //create edge for diamonds, which only can be reached by falling down
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].getDiamond())
                {
                    //From any node to i
                    bool unreachable = true;
                    for (int j = 0; j < nodes.Count; j++)
                    {
                        if (j == i)
                        {
                            continue;
                        }
                        if (adjacencyMatrix[j, i] != 0)
                        {
                            unreachable = false;
                            break;
                        }
                    }
                    //unreachable diamond found
                    if (unreachable)
                    {
                        nodes[i].setDiamond(false);
                        //check edges as normal node
                        for (int k = 0; k < nodes.Count; k++)
                        {
                            if (k == i)
                            {
                                continue;
                            }
                            int[] actionDirectionDistance = CheckEdge(nodes[k], nodes[i]);
                            adjacencyMatrix[k, i] = actionDirectionDistance[0];
                            directionMap[k, i] = actionDirectionDistance[1];
                            directDistanceMap[k, i] = actionDirectionDistance[2];
                        }
                        nodes[i].setDiamond(true);
                    }

                    //From i to other nodes
                    unreachable = true;
                    for (int j = 0; j < nodes.Count; j++)
                    {
                        if (j == i)
                        {
                            continue;
                        }
                        if (adjacencyMatrix[i, j] != 0)
                        {
                            unreachable = false;
                            break;
                        }
                    }
                    //unreachable diamond found
                    if (unreachable)
                    {
                        nodes[i].setDiamond(false);
                        //check edges as normal node
                        for (int k = 0; k < nodes.Count; k++)
                        {
                            if (k == i)
                            {
                                continue;
                            }
                            int[] actionDirectionDistance = CheckEdge(nodes[i], nodes[k]);
                            adjacencyMatrix[i, k] = actionDirectionDistance[0];
                            directionMap[i, k] = actionDirectionDistance[1];
                            directDistanceMap[i, k] = actionDirectionDistance[2];
                        }
                        nodes[i].setDiamond(true);
                    }
                }
            }

            if (abstractionOutput)
            {
                //Debug output
                Log.LogInformation("Edges: ");
                for (int i = 0; i < adjacencyMatrix.GetLength(0); i++)
                {
                    for (int k = 0; k < adjacencyMatrix.GetLength(1); k++)
                    {
                        System.Diagnostics.Debug.Write(adjacencyMatrix[i, k] + " ");
                    }

                    Log.LogInformation("");
                }
                //Debug output
                Log.LogInformation("Directions: ");
                for (int i = 0; i < directionMap.GetLength(0); i++)
                {
                    for (int k = 0; k < directionMap.GetLength(1); k++)
                    {
                        System.Diagnostics.Debug.Write(directionMap[i, k] + " ");
                    }

                    Log.LogInformation("");
                }
                //Debug output
                Log.LogInformation("Distances: ");
                for (int i = 0; i < directDistanceMap.GetLength(0); i++)
                {
                    for (int k = 0; k < directDistanceMap.GetLength(1); k++)
                    {
                        System.Diagnostics.Debug.Write(directDistanceMap[i, k] + " ");
                    }

                    Log.LogInformation("");
                }
            }
        }

        public static int[] CheckEdge(Node n1, Node n2)
        {
            int deltaX = n1.getX() - n2.getX();
            int deltaY = n1.getY() - n2.getY();
            if (abstractionOutput)
            {
                Log.LogInformation("DX: " + deltaX + " - DY: " + deltaY);
            }
            int edge = 0;
            int direction = -1;
            int distance = -1;
            if (deltaX < 0 && deltaY == 0)
            {
                direction = (int)Direction.Right;
                distance = deltaX * -1;
            }
            if (deltaX < 0 && deltaY < 0 && !(n1.getDiamond() || n2.getDiamond()))
            {
                direction = (int)Direction.RightDown;
                distance = (int)Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
            }
            if (deltaX == 0 && deltaY < 0)
            {
                direction = (int)Direction.Down;
                distance = deltaY * -1;
            }
            if (deltaX > 0 && deltaY < 0 && !(n1.getDiamond() || n2.getDiamond()))
            {
                direction = (int)Direction.LeftDown;
                distance = (int)Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
            }
            if (deltaX > 0 && deltaY == 0)
            {
                direction = (int)Direction.Left;
                distance = deltaX;
            }
            if (deltaX > 0 && deltaY > 0 && !(n1.getDiamond() || n2.getDiamond()))
            {
                direction = (int)Direction.LeftUp;
                distance = (int)Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
            }
            if (deltaX == 0 && deltaY > 0)
            {
                direction = (int)Direction.Up;
                distance = deltaY;
            }
            if (deltaX < 0 && deltaY > 0 && !(n1.getDiamond() || n2.getDiamond()))
            {
                direction = (int)Direction.RightUp;
                distance = (int)Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
            }
            if (abstractionOutput)
            {
                Log.LogInformation("Direction: " + direction);
            }

            if (!((deltaY >= 200 && deltaX == 0) || (deltaY >= 50 && deltaX < 0) || (deltaY >= 50 && deltaX > 0) || (direction == 6 && deltaY < 200 && deltaY > 75 && deltaX == 0 && !n2.getDiamond())))
            {

                //bool[,] obstacleOpenSpaceCopy = (bool[,])obstacleOpenSpace.Clone();

                //Square    
                bool obstacle = checkSquareSize(nSquareSize, direction, n1, n2);

                //if obstacle false ok else horizontal
                if (!obstacle)
                {
                    return new int[] { 1, direction, distance };
                }
                else
                {
                    obstacle = checkSquareSize(hSquareSize, direction, n1, n2);
                }

                //if obstacle false ok else vertical
                if (!obstacle)
                {
                    return new int[] { 2, direction, distance };
                }
                else
                {
                    obstacle = checkSquareSize(vSquareSize, direction, n1, n2);
                }

                //if obstacle false ok else nothing
                if (!obstacle)
                {
                    return new int[] { 3, direction, distance };
                }
            }

            return new int[] { edge, direction, distance };
        }

        public static bool checkSquareSize(int[] nSquareSize, int direction, Node n1, Node n2)
        {
            int x0 = n1.getX();
            int y0 = n1.getY();
            int x1 = n2.getX();
            int y1 = n2.getY();

            int x = Math.Abs(x1 - x0);
            int y = Math.Abs(y1 - y0);
            if (abstractionOutput)
            {
                Log.LogInformation("X: " + x + "Y: " + y);
            }

            if (direction == (int)Direction.Right)
            {
                //Shift edge to right
                int distanceWithoutSurface = 0;
                for (int i = 0; i < x; i++)
                {
                    for (int j = 0; j < nSquareSize[1]; j++)
                    {
                        if (obstacleOpenSpace[n1.getY() - j, n1.getX() + i])
                        {
                            return true;
                        }
                    }
                    //distance where is no surface
                    if (!obstacleOpenSpace[n1.getY() + 1, n1.getX() + i])
                    {
                        distanceWithoutSurface++;
                        if (distanceWithoutSurface > 200 || (distanceWithoutSurface + 5) >= Math.Abs(n1.getX() - n2.getX()))
                        {
                            //Log.LogInformation("No edge because of no surface true - " + distanceWithoutSurface + " - " + n1 + " - " + n2);
                            return true;
                        }
                    }
                    else
                    {
                        //Log.LogInformation("No surface false - " + distanceWithoutSurface + " - " + n1 + " - " + n2);
                        distanceWithoutSurface = 0;
                    }
                }
            }
            else if (direction == (int)Direction.Left)
            {
                //Shift edge to left
                int distanceWithoutSurface = 0;
                for (int i = 0; i < x; i++)
                {
                    for (int j = 0; j < nSquareSize[1]; j++)
                    {
                        if (obstacleOpenSpace[n1.getY() - j, n1.getX() - i])
                        {
                            return true;
                        }
                    }
                    //distance where is no surface
                    if (!obstacleOpenSpace[n1.getY() + 1, n1.getX() - i])
                    {
                        distanceWithoutSurface++;
                        if (distanceWithoutSurface > 200 || (distanceWithoutSurface + 5) >= Math.Abs(n1.getX() - n2.getX()))
                        {
                            //Log.LogInformation("No edge because of no surface true - " + distanceWithoutSurface + " - " + n1 + " - " + n2);
                            return true;
                        }
                    }
                    else
                    {
                        //Log.LogInformation("No surface false - " + distanceWithoutSurface + " - " + n1 + " - " + n2);
                        distanceWithoutSurface = 0;
                    }
                }
            }
            else if (direction == (int)Direction.Down)
            {
                //Shift edge to down
                bool toRight = false;
                bool toLeft = false;
                if (n1.getX() + nSquareSize[0] - 1 > fullWidth)
                {
                    toRight = true;
                }
                else
                {
                    for (int i = 0; i < y; i++)
                    {
                        for (int j = 0; j < nSquareSize[0]; j++)
                        {
                            //Log.LogInformation("Output: " + (n1.getY() - i) + "output2: " + (n1.getX() + j));
                            if (obstacleOpenSpace[n1.getY() + i, n1.getX() + j])
                            {
                                toRight = true;
                            }
                        }
                    }
                }
                if (n1.getX() - nSquareSize[0] + 1 < 0)
                {
                    toLeft = true;
                }
                else
                {
                    for (int i = 0; i < y; i++)
                    {
                        for (int j = 0; j < nSquareSize[0]; j++)
                        {
                            if (obstacleOpenSpace[n1.getY() + i, n1.getX() - j])
                            {
                                toLeft = true;
                            }
                        }
                    }
                }
                if (toRight && toLeft)
                {
                    return true;
                }
            }
            else if (direction == (int)Direction.Up && nSquareSize[1] >= y)
            {
                //Shift edge to up
                bool toRight = false;
                bool toLeft = false;
                if (n1.getX() + nSquareSize[0] - 1 > fullWidth)
                {
                    toRight = true;
                }
                else
                {
                    for (int i = 0; i < y; i++)
                    {
                        for (int j = 0; j < nSquareSize[0]; j++)
                        {
                            if (obstacleOpenSpace[n1.getY() - i, n1.getX() + j])
                            {
                                toRight = true;
                            }
                        }
                    }
                }
                if (n1.getX() - nSquareSize[0] + 1 < 0)
                {
                    toLeft = true;
                }
                else
                {
                    for (int i = 0; i < y; i++)
                    {
                        for (int j = 0; j < nSquareSize[0]; j++)
                        {
                            if (obstacleOpenSpace[n1.getY() - i, n1.getX() - j])
                            {
                                toLeft = true;
                            }
                        }
                    }
                }
                if (toRight && toLeft)
                {
                    return true;
                }
            }
            else if (direction == (int)Direction.RightDown) //  || direction == (int)Direction.LeftDown || direction == (int)Direction.LeftUp || direction == (int)Direction.RightUp)
            {
                if (abstractionOutput)
                {
                    Log.LogInformation(nSquareSize + "RightDown: N1: " + n1 + " X: " + x + " Y: " + y);
                    Log.LogInformation(nSquareSize + "RightDown: N2: " + n2);
                }

                List<int[]> pixels = PixelLine(x0, y0, x1, y1);
                if (pixels == null)
                {
                    return true;
                }
                //squaresize control
                bool squareSizeControl = CheckSquareSizeDiagonalLine(pixels);
                if (squareSizeControl)
                {
                    return true;
                }
                if (abstractionOutput)
                {
                    Log.LogInformation(nSquareSize + "RightDown: N1: " + n1 + " X: " + x + " Y: " + y);
                }
            }
            else if (direction == (int)Direction.LeftDown)
            {
                if (abstractionOutput)
                {
                    Log.LogInformation(nSquareSize + "LeftDown: N1: " + n1 + " X: " + x + " Y: " + y);
                    Log.LogInformation(nSquareSize + "LeftDown: N2: " + n2);
                }

                List<int[]> pixels = PixelLine(x0, y0, x1, y1);
                if (pixels == null)
                {
                    return true;
                }
                //squaresize control
                bool squareSizeControl = CheckSquareSizeDiagonalLine(pixels);
                if (squareSizeControl)
                {
                    return true;
                }
                if (abstractionOutput)
                {
                    Log.LogInformation(nSquareSize + "LeftDown: N1: " + n1 + " X: " + x + " Y: " + y);
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        public static bool CheckSquareSizeDiagonalLine(List<int[]> pixels)
        {
            //squaresize control
            foreach (int[] p in pixels)
            {
                bool rightObstacle = false;
                int i;
                for (i = 0; i < nSquareSize[0]; i++)
                {
                    if (obstacleOpenSpace[p[1], p[0] + i])
                    {
                        rightObstacle = true;
                        break;
                    }
                }
                if (rightObstacle)
                {
                    for (int j = 0; j < nSquareSize[0] - i; j++)
                    {
                        if (obstacleOpenSpace[p[1], p[0] - j])
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Bresenham's line algorithm
        public static List<int[]> PixelLine(int x0, int y0, int x1, int y1)
        {
            List<int[]> pixels = new List<int[]>();

            int dx = Math.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            int e2;

            while (true)
            {
                //if obstacle return empty list
                if (obstacleOpenSpace[y0, x0])
                {
                    return null;
                }
                pixels.Add(new int[] { x0, y0 });
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }
                e2 = 2 * err;
                if (e2 > dy)
                {
                    err += dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }

            return pixels;
        }

        private void SetImplementedAgent(bool b)
        {
            implementedAgent = b;
        }

        public override bool ImplementedAgent()
        {
            return implementedAgent;
        }

        private void RandomAction()
        {
            /*
             Rectangle Actions
             MOVE_LEFT = 5
             MOVE_RIGHT = 6
             MORPH_UP = 7
             MORPH_DOWN = 8
            */

            SetAction(possibleMoves[rnd.Next(possibleMoves.Count)]);
        }

        private void SetAction(Moves a)
        {
            currentAction = a;
        }

        //Manager gets this action from agent
        public override Moves GetAction()
        {
            return currentAction;
        }

        public override void SensorsUpdated(int nC, RectangleRepresentation rI, CircleRepresentation cI, CollectibleRepresentation[] colI)
        {
            DeprecatedSensorsUpdated(nC,
                rI.ToArray(),
                cI.ToArray(),
                CollectibleRepresentation.RepresentationArrayToFloatArray(colI));
        }

        public void DeprecatedSensorsUpdated(int nC, float[] sI, float[] cI, float[] colI)
        {
            int temp;

            nCollectiblesLeft = nC;

            rectangleInfo[0] = sI[0];
            rectangleInfo[1] = sI[1];
            rectangleInfo[2] = sI[2];
            rectangleInfo[3] = sI[3];
            rectangleInfo[4] = sI[4];

            circleInfo[0] = cI[0];
            circleInfo[1] = cI[1];
            circleInfo[2] = cI[2];
            circleInfo[3] = cI[3];
            circleInfo[4] = cI[4];

            Array.Resize(ref collectiblesInfo, (nCollectiblesLeft * 2));

            temp = 1;
            while (temp <= nCollectiblesLeft)
            {
                collectiblesInfo[(temp * 2) - 2] = colI[(temp * 2) - 2];
                collectiblesInfo[(temp * 2) - 1] = colI[(temp * 2) - 1];

                temp++;
            }
        }


        public override void Update(TimeSpan elapsedGameTime)
        {

            //Console.WriteLine("    now = {0}   --  square last {1} ", DateTime.Now.Second, lastMoveTime);

            //if (lastMoveTime == 60)
            //    lastMoveTime = 0;

            //if ((lastMoveTime) <= (DateTime.Now.Second) && (lastMoveTime < 60))
            //{
            //    if (!(DateTime.Now.Second == 59))
            //    {
            //        RandomAction();
            //        lastMoveTime = lastMoveTime + 1;
            //        //DebugSensorsInfo();
            //    }
            //    else
            //        lastMoveTime = 60;
            //}

            if (firstUpdate)
            {
                Log.LogInformation("First update start");
                //calc route
                Queue<Node> route = calculateRoute();
                //Create driver
                Log.LogInformation("Create driver start");
                driver = new Driver2(nodes, adjacencyMatrix, directionMap, route);
                Log.LogInformation("Create driver end");
                firstUpdate = false;
                Log.LogInformation("First update end");
            }

            moveStep = moveStep % 4;
            if (moveStep == 0)
            {
                if (output)
                {
                    int t = 0;
                    foreach (long i in rectangleInfo)
                    {
                        Log.LogInformation("SQUARE - Square info - " + t + " - " + i);
                        t++;
                    }
                }
                SetAction(driver.GetAction(rectangleInfo));
            }
            moveStep++;
        }

        public void toggleDebug()
        {
            //this.agentPane.AgentVisible = !this.agentPane.AgentVisible;
        }

        protected void DebugSensorsInfo()
        {
            int t = 0;
            /*
            foreach (int i in numbersInfo)
            {
                Log.LogInformation("RECTANGLE - Numbers info - " + t + " - " + i);
                t++;
            }
            */

            Log.LogInformation("RECTANGLE - collectibles left - " + nCollectiblesLeft);
            Log.LogInformation("RECTANGLE - collectibles info size - " + collectiblesInfo.Count());

            /*
            t = 0;
            foreach (long i in rectangleInfo)
            {
                Log.LogInformation("RECTANGLE - Rectangle info - " + t + " - " + i);
                t++;
            }

            t = 0;
            foreach (long i in circleInfo)
            {
                Log.LogInformation("RECTANGLE - Circle info - " + t + " - " + i);
                t++;
            }
            
            t = 0;
            foreach (long i in obstaclesInfo)
            {
                Log.LogInformation("RECTANGLE - Obstacles info - " + t + " - " + i);
                t++;
            }

            t = 0;
            foreach (long i in rectanglePlatformsInfo)
            {
                Log.LogInformation("RECTANGLE - Rectangle Platforms info - " + t + " - " + i);
                t++;
            }

            t = 0;
            foreach (long i in circlePlatformsInfo)
            {
                Log.LogInformation("RECTANGLE - Circle Platforms info - " + t + " - " + i);
                t++;
            }
            */
            t = 0;
            foreach (float i in collectiblesInfo)
            {
                Log.LogInformation("RECTANGLE - Collectibles info - " + t + " - " + i);
                t++;
            }
        }

        public override string AgentName()
        {
            return agentName;
        }

        public override void EndGame(int collectiblesCaught, int timeElapsed)
        {
            Log.LogInformation("RECTANGLE - Collectibles caught = " + collectiblesCaught + ", Time elapsed - " + timeElapsed);
        }
    }
}