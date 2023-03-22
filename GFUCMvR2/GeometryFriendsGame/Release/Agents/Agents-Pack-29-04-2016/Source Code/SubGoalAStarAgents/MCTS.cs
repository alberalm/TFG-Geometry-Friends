using GeometryFriends;
using System.Collections.Generic;

namespace GeometryFriendsAgents
{
    class MCTS
    {
        public static List<Node> nodes;
        public static int[,] adjacencyMatrix;
        public static int diamonds;
        public static List<int> visitedIndex;
        public List<int> outputNodeIndex;
        public static List<Node> bestNodeList;
        public static List<int> outputBestNodeIndex;
        public static bool endStateFound;
        public static int bestCollected;
        public static double bestValue;

        int timeLimit;

        bool output = false;

        public MCTS(List<Node> nodes, int[,] adjacencyMatrix, int diamonds, int time)
        {
            MCTS.nodes = nodes;
            MCTS.adjacencyMatrix = adjacencyMatrix;
            MCTS.diamonds = diamonds;
            this.timeLimit = time;
        }

        public Queue<Node> Run()
        {
            endStateFound = false;
            bestCollected = 0;
            bestValue = 0;
            MCTSNode mn = new MCTSNode();
            //int n = 1000;
            int iterations = 0;
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            while(sw.ElapsedMilliseconds <= timeLimit) //for (int i = 0; i < n; i++)
            {
                mn.selectAction();
                iterations++;
                if(endStateFound)
                {
                    if (output)
                    {
                        Log.LogInformation("MCTS iterations: " + iterations);
                    }
                    break;
                }       
            }
            Log.LogInformation("MCTS iterations:" + iterations);
            sw.Stop();

            Queue<Node> queue = new Queue<Node>();
            List<int> outputNodeIndex = new List<int>();

            for (int i = bestNodeList.Count - 1; i >= 0; i--)
            {
                queue.Enqueue(bestNodeList[i]);
                outputNodeIndex.Add(outputBestNodeIndex[i]);
            }

            if (output)
            {
                Log.LogInformation("Route: " + outputNodeIndex.Count + " nodes");
                foreach (int k in outputNodeIndex)
                {
                    Log.LogInformation("Route NodeIndex: " + k);
                }
            }

            this.outputNodeIndex = outputNodeIndex;

            return queue;
        }

        public static void SetBestRoute(MCTSNode mn)
        {
            List<Node> list = new List<Node>();
            List<int> outputNodeIndex = new List<int>();

            while(mn != null)
            {
                list.Add(MCTS.nodes[mn.nodeIndex]);
                outputNodeIndex.Add(mn.nodeIndex);
                mn = mn.parent;
            }
            bestNodeList = list;
            outputBestNodeIndex = outputNodeIndex;
        }

    }
}
