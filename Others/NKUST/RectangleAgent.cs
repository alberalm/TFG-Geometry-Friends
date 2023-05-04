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

        //Sensors Information
        private int[] numbersInfo;
        private float[] rectangleInfo;
        private float[] circleInfo;
        private float[] obstaclesInfo;
        private float[] rectanglePlatformsInfo;
        private float[] circlePlatformsInfo;
        private bool circlePlatformsFlag=false;
        public static float[] collectiblesInfo;

        public static int nCollectiblesLeft;

        private string agentName = "rectangle";

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
        private int counter = 0;

        //RightDown Edge
        int xThreshold = 80;

        // driver
        int moveStep;
        Driver driver;

        bool firstUpdate;

        // output
        public static bool abstractionOutput = false;
        public static bool output = false;

        //runAlgorithm = 4 --> Permutation AStar
        int runAlgorithm = 4;

        public RectangleAgent() 
        {
            //Change flag if agent is not to be used
            SetImplementedAgent(true);

            lastMoveTime = DateTime.Now.Second;
            currentAction = Moves.NO_ACTION;

            possibleMoves = new List<Moves>();
            possibleMoves.Add(Moves.MOVE_LEFT);
            possibleMoves.Add(Moves.MOVE_RIGHT);
            possibleMoves.Add(Moves.MORPH_UP);
            possibleMoves.Add(Moves.MORPH_DOWN);
        }

        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
            ObstacleRepresentation[] a = oI.ToArray();
            firstUpdate = true;
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
            this.area = area;
            int temp;
            
            numbersInfo = new int[4];
            int i;
            for (i = 0; i < nI.Length; i++)
            {
                numbersInfo[i] = nI[i];
            }

            nCollectiblesLeft = nI[3];
            

            rectangleInfo = new float[5];
            rectangleInfo[0] = sI[0];
            rectangleInfo[1] = sI[1];
            rectangleInfo[2] = sI[2];
            rectangleInfo[3] = sI[3];
            rectangleInfo[4] = sI[4];
            
            circleInfo = new float[5];
            circleInfo[0] = cI[0];
            circleInfo[1] = cI[1];
            circleInfo[2] = cI[2];
            circleInfo[3] = cI[3];
            circleInfo[4] = cI[4];
            
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
            
            if (numbersInfo[2] > 0)
            {
                circlePlatformsInfo = new float[numbersInfo[2] * 4];
                circlePlatformsFlag = true;
            }
            else
            {
                circlePlatformsInfo = new float[4];
            }

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
            System.Diagnostics.Stopwatch sw2 = System.Diagnostics.Stopwatch.StartNew();
            CreateObstacleOpenSpaceArray();
            CreateNodes();
            CreateEdgesAndAdjacencyMatrix();

            Queue<Node> route = new Queue<Node>();

            if (runAlgorithm == 4) // Permutation AStar
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                route = calcShortestRouteAStarAllPermutations();
            }

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

            int maxSize = diamondNodes.Count;
            while (true)
            {
                if (maxSize == 0)
                {
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
                List<Node> shortestRouteMostDiamonds = allCompleteList[shortestIndex];
                Queue<Node> completeQueue = new Queue<Node>();
                for (int i = 0; i < shortestRouteMostDiamonds.Count; i++)
                {
                    completeQueue.Enqueue(shortestRouteMostDiamonds[i]);
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
            return shortRoute;
        }

        private void CreateObstacleOpenSpaceArray()
        {
            obstacleOpenSpace = new bool[fullHeight, fullWidth];
            int obstaclePixelCounter = 0;
            //創建遊戲區域的黑色邊框
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
            //Fill in obstacles
            for (int i = 0; i < obstaclesInfo.Length; i = i + 4)
            {
                int x = (int)obstaclesInfo[i];
                int y = (int)obstaclesInfo[i + 1];
                int h = (int)obstaclesInfo[i + 2];
                int w = (int)obstaclesInfo[i + 3];
                int upperLeftX = x - (w / 2);
                int upperLeftY = y - (h / 2);
                for (int j = upperLeftY; j < upperLeftY + h; j++)
                {
                    for (int k = upperLeftX; k < upperLeftX + w; k++)
                    {
                        obstacleOpenSpace[j, k] = true;
                        obstaclePixelCounter++;
                    }
                }
            }
            if (circlePlatformsFlag)
            {
                for (int i = 0; i < circlePlatformsInfo.Length; i = i + 4)
                {
                    int x = (int)circlePlatformsInfo[i];
                    int y = (int)circlePlatformsInfo[i + 1];
                    int h = (int)circlePlatformsInfo[i + 2];
                    int w = (int)circlePlatformsInfo[i + 3];
                    int upperLeftX = x - (w / 2);
                    int upperLeftY = y - (h / 2);
                    for (int j = upperLeftY; j < upperLeftY + h; j++)
                    {
                        for (int k = upperLeftX; k < upperLeftX + w; k++)
                        {
                            obstacleOpenSpace[j, k] = true;
                            obstaclePixelCounter++;
                        }
                    }
                }
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

                //如果upper是free，則創建左節點
                if (rawY!=0 &&!obstacleOpenSpace[rawY - 1, rawX])
                {
                    Node node1;
                    //如果左上角和左上角是自由的，則創建左上角節點
                    if (rawX != 0 &&!obstacleOpenSpace[rawY - 1, rawX - 1] && !obstacleOpenSpace[rawY, rawX - 1])
                    {
                        node1 = new Node(rawX - 1, rawY - 1, false);
                        nodes.Add(node1);
                        //由障礙物創造的節點倒塌
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
                    //如果左上角和左上角是障礙物，則創建上部節點
                    else if (rawX != 0 && obstacleOpenSpace[rawY - 1, rawX - 1] && obstacleOpenSpace[rawY, rawX - 1])
                    {
                        node1 = new Node(rawX, rawY - 1, false);
                        if (!nodes.Contains(node1))
                        {
                            nodes.Add(node1);
                        }
                    }

                }

                rawX = x + (w / 2) - 1;
                //如果upper是free，則創建右節點
                if (rawY != 0 && !obstacleOpenSpace[rawY - 1, rawX])
                {
                    Node node1;
                    //如果右上角和右上角是自由的，則創建右上角節點
                    if (rawX-1 < fullWidth && !obstacleOpenSpace[rawY - 1, rawX + 1] && !obstacleOpenSpace[rawY, rawX + 1])
                    {
                        node1 = new Node(rawX + 1, rawY - 1, false);
                        nodes.Add(node1);
                        //由障礙物創造的節點倒塌
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
                    //如果右上角和右上角是障礙物，則創建上部節點
                    else if (rawX - 1 < fullWidth && obstacleOpenSpace[rawY - 1, rawX + 1] && obstacleOpenSpace[rawY, rawX + 1])
                    {
                        node1 = new Node(rawX, rawY - 1, false);
                        if (!nodes.Contains(node1))
                        {
                            nodes.Add(node1);
                        }
                    }

                }

            }
            //Nodes created by circlePlatformsInfo
            if (circlePlatformsFlag)
            {
                for (int i = 0; i < circlePlatformsInfo.Length; i = i + 4)
                {
                    int x = (int)circlePlatformsInfo[i];
                    int y = (int)circlePlatformsInfo[i + 1];
                    int h = (int)circlePlatformsInfo[i + 2];
                    int w = (int)circlePlatformsInfo[i + 3];
                    int rawX = x - (w / 2);
                    int rawY = y - (h / 2);

                    //如果upper是free，則創建左節點
                    if (rawY != 0 && !obstacleOpenSpace[rawY - 1, rawX])
                    {
                        Node node1;
                        //如果左上角和左上角是自由的，則創建左上角節點
                        if (rawX != 0&&!obstacleOpenSpace[rawY - 1, rawX - 1] && !obstacleOpenSpace[rawY, rawX - 1])
                        {
                            node1 = new Node(rawX - 1, rawY - 1, false);
                            nodes.Add(node1);
                            //由障礙物創造的節點倒塌
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
                        //如果左上角和左上角是障礙物，則創建上部節點
                        else if (rawX != 0 && obstacleOpenSpace[rawY - 1, rawX - 1] && obstacleOpenSpace[rawY, rawX - 1])
                        {
                            node1 = new Node(rawX, rawY - 1, false);
                            if (!nodes.Contains(node1))
                            {
                                nodes.Add(node1);
                            }
                        }

                    }

                    rawX = x + (w / 2) - 1;
                    //如果upper是free，則創建右節點
                    if (rawY != 0 && !obstacleOpenSpace[rawY - 1, rawX])
                    {
                        Node node1;
                        //如果右上角和右上角是自由的，則創建右上角節點
                        if (rawX-1 < fullWidth && !obstacleOpenSpace[rawY - 1, rawX + 1] && !obstacleOpenSpace[rawY, rawX + 1])
                        {
                            node1 = new Node(rawX + 1, rawY - 1, false);
                            nodes.Add(node1);
                            //由障礙物創造的節點倒塌
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
                        //如果右上角和右上角是障礙物，則創建上部節點
                        else if (rawX - 1 < fullWidth && obstacleOpenSpace[rawY - 1, rawX + 1] && obstacleOpenSpace[rawY, rawX + 1])
                        {
                            node1 = new Node(rawX, rawY - 1, false);
                            if (!nodes.Contains(node1))
                            {
                                nodes.Add(node1);
                            }
                        }

                    }

                }
            }

            //由鑽石創建的節點
            for (int i = 0; i < collectiblesInfo.Length; i = i + 2)
            {
                int x = (int)collectiblesInfo[i];
                int y = (int)collectiblesInfo[i + 1];
                Node node1 = new Node(x, y, true);
                Node node2 = null;
                //落下鑽石節點
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
                else if (j > diamondFallDownThreshold  )
                {
                    nodes.Add(node1);
                    nodes.Add(node2);
                    
                }
            }
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
                    int[] actionDirectionDistance = CheckEdge(n1, n2);
                    adjacencyMatrix[i, j] = actionDirectionDistance[0];
                    directionMap[i, j] = actionDirectionDistance[1];
                    directDistanceMap[i, j] = actionDirectionDistance[2];
                    if (actionDirectionDistance[1] == 2 && actionDirectionDistance[0] != 0)
                    {
                        nodes[i].setLeadsToFallDown(true);
                    }
                    //adjacencyMatrix[i, j] = CheckEdge(n1, n2);
                }
            }

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
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].getDiamond())
                {
                    int j;
                    //From any node to i
                    bool unreachable = true;
                    bool doublecheck = true;
                    for (j = 0; j < nodes.Count; j++)
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
                    if (unreachable)
                    {
                        nodes[i].setDiamond(false);
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

                    unreachable = true;
                    for (j = 0; j < nodes.Count; j++)
                    {
                        if (j == i||nodes[j].getDiamond())
                        {
                            continue;
                        }
                        if (adjacencyMatrix[i, j] != 0)
                        {
                            unreachable = false;
                            break;
                        }
                    }
                    if (unreachable)
                    {
                        nodes[i].setDiamond(false);
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

                    if (doublecheck&&j<nodes.Count)
                    {
                        if (nodes[i].getX() == nodes[j].getX() && nodes[j].getY() > nodes[i].getY())
                        {
                            for (int k = 0; k < nodes.Count; k++)
                            {
                                if (k == j|| k == i||nodes[k].getDiamond())
                                {
                                    continue;
                                }
                                int[] actionDirectionDistance = CheckEdge(nodes[k], nodes[j]);
                                adjacencyMatrix[k, j] = actionDirectionDistance[0];
                                directionMap[k, j] = actionDirectionDistance[1];
                                directDistanceMap[k, j] = actionDirectionDistance[2];

                                if (actionDirectionDistance[0] != 0)
                                {
                                    int t;
                                    int l;
                                    int f;
                                    for (t = 0; t < nodes.Count; t++)
                                    {
                                        if (adjacencyMatrix[t, k] != 0 && !nodes[t].getDiamond() && nodes[t].getLeadsToFallDown())
                                        {
                                            for(l = 0; l < nodes.Count; l++)
                                            {
                                                if (l != t && l != k && l != j && adjacencyMatrix[l, j] != 0 && !nodes[l].getDiamond() && !nodes[l].getLeadsToFallDown() )
                                                {
                                                    for (f = 0; f < nodes.Count; f++) {
                                                        if (f != t && f != k && f != j && f != l && adjacencyMatrix[f, l] != 0 && !nodes[f].getDiamond() && nodes[f].getLeadsToFallDown())
                                                        {
                                                            if(Math.Abs(nodes[t].getY() - nodes[f].getY()) <10 && Math.Abs(nodes[l].getY() - nodes[k].getY()) < 10 && Math.Abs(nodes[f].getY() - nodes[l].getY()) >80  && Math.Abs(nodes[t].getX() - nodes[f].getX()) < 210 && Math.Abs(nodes[t].getX() - nodes[f].getX()) > 49)
                                                            {
                                                                nodes[j].setDiamond(true);
                                                            }
                                                        }

                                                    }

                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            
        }

        public static int[] CheckEdge(Node n1, Node n2)
        {
            int deltaX = n1.getX() - n2.getX();
            int deltaY = n1.getY() - n2.getY();
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


            if (!((deltaY >= 245 && deltaX == 0) || (deltaY >= 50 && deltaX < 0) || (deltaY >= 50 && deltaX > 0) || (direction == 6 && deltaY < 200 && deltaY > 75 && deltaX == 0 && !n2.getDiamond())))
            {

                //bool[,] obstacleOpenSpaceCopy = (bool[,])obstacleOpenSpace.Clone();

                //Square    
                bool obstacle;
                obstacle = checkSquareSize(hSquareSize,  direction, n1, n2);

                //if obstacle false ok else horizontal
                if (!obstacle)
                {
                    return new int[] { 2, direction, distance };
                }
                else
                {
                    obstacle = checkSquareSize(nSquareSize, direction, n1, n2);
                }

                //if obstacle false ok else vertical
                if (!obstacle)
                {
                    return new int[] { 1, direction, distance };
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
                            return true;
                        }
                    }
                    else
                    {
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
                            return true;
                        }
                    }
                    else
                    {
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
            else if (direction == (int)Direction.Up && nSquareSize[1] >= y-45)
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
                    int tmp = y;
                    if(y> n1.getY())
                    {
                        tmp = n1.getY();
                    }
                    for (int i = 0; i < tmp; i++)
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
                    int tmp = y;
                    if (y > n1.getY())
                    {
                        tmp = n1.getY();
                    }
                    for (int i = 0; i < tmp; i++)
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
            }
            else if (direction == (int)Direction.LeftDown)
            {

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

            if (Math.Abs(rectangleInfo[2])<3&&Math.Abs(rectangleInfo[3])<3)
            {
                counter++;
            }
            else
            {
                counter = 0;
            }
            if (counter > 30 && colI.Length/2==1)
            {
                for (int n =0;n<nodes.Count;n++)
                {
                    if (nodes[n].getX() == colI[0] && nodes[n].getY() == colI[1])
                    {
                        if (n + 1 < nodes.Count&&nodes[n+1].getX()==colI[0] && nodes[n + 1].getY() > colI[1])
                        {
                            nodes[n + 1].setDiamond(true);
                            break;
                        }
                    }
                }
            }
        }

        // this method is deprecated, please use SensorsUpdated instead
        public override void UpdateSensors(int nC, float[] sI, float[] cI, float[] colI)
        {
            
        }

        public override void Update(TimeSpan elapsedGameTime)
        {
                   
            if (firstUpdate)
            {
                //calc route
                Queue<Node> route = calculateRoute();
                //Create driver
                driver = new Driver(nodes, adjacencyMatrix, directionMap, route);
                firstUpdate = false;
            }

            moveStep = moveStep % 2;
            if (moveStep == 0)
            {
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