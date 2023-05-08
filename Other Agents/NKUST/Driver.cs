using GeometryFriends;
using GeometryFriends.AI;
using System;
using System.Collections.Generic;

namespace GeometryFriendsAgents
{
    class Driver

    {
        private List<Node> nodes;
        private int[,] adjacencyMatrix;
        private int[,] directionMap;
        private Queue<Node> route;

        private Node previousNode;
        private Node nextNode;
        private Node nextNode2;

        private int previousDirection;
        private int direction;
        private int direction2;
        private int previousAction;
        private int action;
        private int action2;


        private float distance;
        private List<float> distanceList;

        bool output = false;

        enum Direction { Right, RightDown, Down, LeftDown, Left, LeftUp, Up, RightUp };
        
        public Driver(List<Node> nodes, int[,] adjacencyMatrix, int[,] directionMap, Queue<Node> route)
        {
            this.nodes = nodes;
            this.adjacencyMatrix = adjacencyMatrix;
            this.directionMap = directionMap;
            this.route = route;
            distanceList = new List<float>();
            if (route.Count > 0)
            {
                this.previousNode = route.Dequeue();
            }
            if (route.Count > 0)
            {
                this.nextNode = route.Dequeue();
                this.action = adjacencyMatrix[nodes.IndexOf(previousNode), nodes.IndexOf(nextNode)];
                this.direction = directionMap[nodes.IndexOf(previousNode), nodes.IndexOf(nextNode)];
            }
            if (route.Count > 0)
            {
                this.nextNode2 = route.Dequeue();
                this.direction2 = directionMap[nodes.IndexOf(nextNode), nodes.IndexOf(nextNode2)];
                this.action2 = adjacencyMatrix[nodes.IndexOf(nextNode), nodes.IndexOf(nextNode2)];
            }
            if (direction2 == 2)
            {
                Node newNext = GetOppositeFallDownNode(nextNode);
                if (newNext != null)
                {
                    nextNode = newNext;
                    this.action = 2;

                    distance = (float)Math.Sqrt(Math.Pow(nodes[0].getX() - nextNode.getX(), 2) + Math.Pow(nodes[0].getY() - nextNode.getY(), 2));
                    distanceList.Add(distance);
                }
            }

        }

        public Moves GetAction(float[] squareInfo)
        {
            int x = (int)squareInfo[0];
            int y = (int)squareInfo[1];
            int vX = (int)squareInfo[2];
            int vY = (int)squareInfo[3];
            int h = (int)squareInfo[4];

            int hHalf = h/2;
            int w = 10000 / h;
            
            int centerY = y;

            int alwaysCorrectH = h;
            int alwaysCorrectHHalf = hHalf;
            int alwaysCorrectW = w;
            if (IsTwisted(hHalf, h, x, centerY, w))
            {
                alwaysCorrectH = w;
                alwaysCorrectW = 10000 / alwaysCorrectH;
                alwaysCorrectHHalf = alwaysCorrectH / 2;
            }
            y = y + alwaysCorrectHHalf;

            if (nextNode == null)
            {
                return Moves.NO_ACTION;
            }

            distance = (float)Math.Sqrt(Math.Pow(x - nextNode.getX(), 2) + Math.Pow(y - nextNode.getY(), 2));
            distanceList.Add(distance);

            //Algorithms
            if (distanceList.Count == 40 && distanceList[0] == distanceList[39] && RectangleAgent.nCollectiblesLeft > 0)
            {
                distanceList = new List<float>();
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                int astarAction = UseAStar(x, centerY);
                if (astarAction >= 0)
                {
                    return (Moves)astarAction;
                }


            }
            //Algorithms end

            if (distanceList.Count >= 40)
            {
                distanceList = new List<float>();
            }

            if (((distance - 3) <= (alwaysCorrectW / 2) && !nextNode.getPseudo()) || (direction == 6 && distance <= alwaysCorrectH) || ((direction == 2 || direction == 1 || direction == 3) && (nextNode.getY() - y) < 4 && distance < 3 * alwaysCorrectW) || (nextNode.getPseudo() && (distance - 3 < 3)))// (direction == 2)
            {
                distanceList = new List<float>();
                this.previousAction = action;
                this.previousDirection = direction;
                previousNode = nextNode;
                if(nextNode2 != null)
                {
                    nextNode = nextNode2;
                    this.action = action2; 
                    this.direction = direction2;

                    distance = (float)Math.Sqrt(Math.Pow(x - nextNode.getX(), 2) + Math.Pow(y - nextNode.getY(), 2));
                    distanceList.Add(distance);
                }
                else
                {
                    System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                    UseAStar(x, centerY);

                    return Moves.NO_ACTION;
                }
                if (route.Count > 0)
                { 
                    nextNode2 = route.Dequeue();
                    this.action2 = adjacencyMatrix[nodes.IndexOf(nextNode), nodes.IndexOf(nextNode2)];
                    this.direction2 = directionMap[nodes.IndexOf(nextNode), nodes.IndexOf(nextNode2)];
                    
                    //fall down case, create pseudo node
                    if (direction2 == 2)
                    {
                        Node newNext = GetOppositeFallDownNode(nextNode);
                        if(newNext != null)
                        {
                            nextNode = newNext;
                            this.action = 2;
                         
                            distance = (float)Math.Sqrt(Math.Pow(x - nextNode.getX(), 2) + Math.Pow(y - nextNode.getY(), 2));
                            distanceList.Add(distance);
                        }
                    }
                }
                else
                {
                    nextNode2 = null;
                    this.action2 = -1;
                    this.direction2 = -1;
                }
                
            }

            if(nextNode.getPseudo() && distanceList.Count >= 10 && distanceList[0] == distanceList[9])
            {
                if(direction == 0 && distance < 200)
                {
                    return Moves.MOVE_LEFT;
                }
                if (direction == 4 && distance < 200)
                {
                    return Moves.MOVE_RIGHT;
                }
            }

            if (IsDiagonalOrientation(hHalf, h, x, centerY, w) && distanceList.Count >= 15 && distanceList[0] == distanceList[14])
            {     
               Random random = new Random();
               int ran = random.Next(2);
               if(ran == 0)
               {
                   return Moves.MOVE_RIGHT;
               }
               else if( ran == 1)
               {
                   return Moves.MOVE_LEFT;
               }
            }

            if(previousDirection == 6 && (direction == 0 || direction == 4) && (Math.Abs(y - previousNode.getY())  > 4 ) )
            {
                if (alwaysCorrectH < 160 && CanMorphUp(y-alwaysCorrectH-35,x,alwaysCorrectW))
                {
                    return Moves.MORPH_UP;
                }
                else if(direction == 0)
                {
                    return Moves.MOVE_RIGHT;
                }
                else if(direction == 4)
                {
                    return Moves.MOVE_LEFT;
                }

            }
            
            if (action == 1 && alwaysCorrectH > 102 && direction != 6)
            {
                return Moves.MORPH_DOWN;
            }
            if (action == 1 && alwaysCorrectH < 98 && CanMorphUp(y-alwaysCorrectH,x,alwaysCorrectW))
            {
                return Moves.MORPH_UP;
            }
            if (action == 2 && alwaysCorrectH > 55 && direction!=6 )//&& ((nextNode2!=null&& (nextNode.getY() - nextNode2.getY()<=0))||((y - nextNode.getY()<=0) &&nextNode2==null)))
            {
                return Moves.MORPH_DOWN;
            }
            if (action == 3 && direction != 6 && alwaysCorrectH < 194 && CanMorphUp(y - alwaysCorrectH, x, alwaysCorrectW) && !IsDiagonalOrientation(hHalf, h, x, centerY, w) && vY < 5)
            {
                return Moves.MORPH_UP;
            }
            if(direction == 0 || direction == 7 || direction == 1 )
            {         
                if((distance < 100 && vX > 50 ) || (vX > 380 && distance > 200 ) || (vX > 150 && distance < 200))
                {
                    return Moves.MOVE_LEFT;
                }
                else
                {
                    if(nextNode.getPseudo() && distance < 12 && vX > 2 )
                    {
                        return Moves.MOVE_LEFT;
                    }
                    if (nextNode.getPseudo() && distance < 6 && vX < 2 && vX > -2)
                    {
                        return Moves.NO_ACTION;
                    }
                    return Moves.MOVE_RIGHT;
                }
                
            }
            if (direction == 4 || direction == 5 || direction == 3 )
            {
                if ((distance < 100 && vX < -50 ) || (vX < -380 && distance > 200)||(vX < -150 && distance < 200))
                {
                    return Moves.MOVE_RIGHT;
                }
                else
                {
                    if (nextNode.getPseudo() && distance < 12 && vX < -2)
                    {
                        return Moves.MOVE_RIGHT;
                    }
                    if (nextNode.getPseudo() && distance < 6 && vX < 2 && vX > -2)
                    {
                        return Moves.NO_ACTION;
                    }
                    return Moves.MOVE_LEFT;
                }
            }
            if(direction == 6)
            {
                if (alwaysCorrectH < 194 && CanMorphUp(y-alwaysCorrectH,x,alwaysCorrectW))
                {              
                    return Moves.MORPH_UP;
                }
                else if (Math.Abs(nextNode.getX() - x) > (alwaysCorrectW / 2))
                {
                    if ((x - nextNode.getX()) < 0)
                    {
                        return Moves.MOVE_RIGHT;
                    }
                    else
                    {
                        return Moves.MOVE_LEFT;
                    }
                }

            }
            if ((direction == 2) && nextNode.getLeadsToFallDown() && ((Math.Abs(y - previousNode.getY()) > alwaysCorrectH - 15 && Math.Abs(previousNode.getY() - nextNode.getY()) > 200) || (Math.Abs(y - previousNode.getY()) > alwaysCorrectH - 45 && Math.Abs(previousNode.getY() - nextNode.getY()) <= 200)))
            {
                return Moves.MORPH_DOWN;
            }
            if ((!previousNode.getPseudo() && direction == 2 && (previousDirection == 0 || previousDirection == 4) && (Math.Abs(y - previousNode.getY()) < 5)) || ((previousDirection == 0 || previousDirection == 4) && !CanMorphUp(y - alwaysCorrectH, x, alwaysCorrectW)))
            {
                if (previousDirection == 0 && vX < 50)
                {
                    return Moves.MOVE_RIGHT;
                }
                else if(previousDirection == 4 && vX < -50)
                {
                    return Moves.MOVE_LEFT;
                }
            }
            if (previousNode.getLeadsToFallDown() && !previousNode.getPseudo() && direction == 2 || direction == 3 || direction == 1 && CanMorphUp(y - alwaysCorrectH, x, alwaysCorrectW) && (vX > 50 || vX < -50))
            {
                if(previousNodeToObstacleDistance() <= 125)
                {
                    return Moves.MORPH_UP;
                }
            }
            return Moves.NO_ACTION;
        }

        private int previousNodeToObstacleDistance()
        {
            int iter = 7;
            int i = 1;
            int x = previousNode.getX();
            int y = previousNode.getY();
            if(previousDirection == 3 || previousDirection == 4)
            {
                iter = iter * -1;
            }
            while(!RectangleAgent.obstacleOpenSpace[y,x+(i*iter)])
            {
                i++;
            }
            return Math.Abs(i * iter);
        }

        public bool CanMorphUp(int upperY, int xCenter, int w)
        {
            int yUpperThreshold = 10;
            int step = 10;
            int xStart = xCenter - (w / 2);    
                    
            for (int i = 0; i < w; i=i+step)
            {
                if (xStart>=0&&RectangleAgent.obstacleOpenSpace[upperY - yUpperThreshold, xStart+i])
                {
                    return false;
                }
            }
            if (RectangleAgent.obstacleOpenSpace[upperY - yUpperThreshold, xStart + w-1])
            {
                return false;
            }
            return true;
        }

        public bool IsTwisted(int hHalf, int h, int x, int y, int w)
        {
            int index = 1;
            while(!RectangleAgent.obstacleOpenSpace[y+index,x])
            {
                index++;
            }
            if ( !(w >= 98 && w <= 102) && Math.Abs((index*2) - w) <=5)
            {
                if (index > 101)
                {
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsDiagonalOrientation(int hHalf, int h, int x, int y, int w)
        {
            int index = 1;
            while (!RectangleAgent.obstacleOpenSpace[y + index, x])
            {
                index++;
            }
            if (Math.Abs(hHalf - index) >= 2.5 && Math.Abs((index * 2) - w) > 5)
            {
                if(index > 101)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public Node GetOppositeFallDownNode(Node nextNode)
        {
            List<Node> possibleNodes = new List<Node>();
            foreach (Node node in nodes)
            {
                if (node.getLeadsToFallDown() && node.getY() == nextNode.getY() && Math.Abs(node.getX() - nextNode.getX()) <= 200 && (node.getX() - nextNode.getX()) != 0)
                {
                    Node newNode = new Node(((node.getX() + nextNode.getX()) / 2), nextNode.getY(), false);
                    newNode.setPseudo(true);
                    possibleNodes.Add(newNode);
                }
            }
            Node selectedNode = null;
            
            for (int i = 0; i < possibleNodes.Count; i++)
            {
                if (direction == 0 && possibleNodes[i].getX() > previousNode.getX())
                {
                    selectedNode = possibleNodes[i];
                }
                if (direction == 4 && possibleNodes[i].getX() < previousNode.getX())
                {
                    selectedNode = possibleNodes[i];
                }
            }
            return selectedNode;
        }

        private int UseAStar(int x, int centerY)
        {
            
            int s = 1;
            while (!RectangleAgent.obstacleOpenSpace[centerY + s, x])
            {
                s++;
            }
            Node square = new Node(x, centerY + s - 1, false);
            this.nodes[0] = square;
            int y = square.getY();

            deleteCollectedDiamonds();

            RectangleAgent.nodes = this.nodes;
            RectangleAgent.CreateEdgesAndAdjacencyMatrix();
            this.adjacencyMatrix = RectangleAgent.adjacencyMatrix;
            this.directionMap = RectangleAgent.directionMap;

            this.route = RectangleAgent.calcShortestRouteAStarAllPermutations();

            return recalcNextNodes("Permutation AStar", x, y);
        }
        
        private void deleteCollectedDiamonds()
        {
            float[] colInfo = RectangleAgent.collectiblesInfo;
            List<Node> colLeftList = new List<Node>();
            for (int i = 0; i < colInfo.Length; i = i + 2)
            {
                Node node = new Node((int)colInfo[i], (int)colInfo[i + 1], true);
                colLeftList.Add(node);
            }

            for (int index = 0; index < nodes.Count; index++)
            {
                Node nodeOfFullList = nodes[index];
                if (nodeOfFullList.getDiamond())
                {
                    bool isDiamond = false;
                    foreach (Node leftDiamond in colLeftList)
                    {
                        if ((nodeOfFullList.getX() == leftDiamond.getX() && nodeOfFullList.getY() == leftDiamond.getY()) || ((nodeOfFullList.getY() - leftDiamond.getY()) <= 80 && (nodeOfFullList.getY() - leftDiamond.getY()) > 0 && nodeOfFullList.getX() == leftDiamond.getX()))
                        {
                            isDiamond = true;
                        }
                    }
                    if (!isDiamond)
                    {
                        nodeOfFullList.setDiamond(false);
                        nodes[index] = nodeOfFullList;
                    }
                    if (!nodeOfFullList.getDiamond() && index + 1 < nodes.Count && !nodes[index + 1].getDiamond())
                    {
                        if (nodes[index + 1].getX() == nodes[index].getX() && nodes[index + 1].getY() > nodes[index].getY())
                        {
                            nodes[index + 1].setDiamond(false);
                        }
                    }
                }


            }
        }

        public int recalcNextNodes(String output, int x, int y)
        {
            if(route == null)
            {
                return -1;
            }
            if (route.Count > 0)
            {
                this.previousNode = route.Dequeue();
            }
            else
            {
                return (int)Moves.NO_ACTION;
            }
            if (route.Count > 0)
            {
                this.nextNode = route.Dequeue();
                this.action = adjacencyMatrix[nodes.IndexOf(previousNode), nodes.IndexOf(nextNode)];
                this.direction = directionMap[nodes.IndexOf(previousNode), nodes.IndexOf(nextNode)];
                distance = (float)Math.Sqrt(Math.Pow(x - nextNode.getX(), 2) + Math.Pow(y - nextNode.getY(), 2));
            }
            else
            {
                return (int)Moves.NO_ACTION;
            }
            if (route.Count > 0)
            {
                this.nextNode2 = route.Dequeue();
                this.action2 = adjacencyMatrix[nodes.IndexOf(nextNode), nodes.IndexOf(nextNode2)];
                this.direction2 = directionMap[nodes.IndexOf(nextNode), nodes.IndexOf(nextNode2)];
            }
            else
            {
                nextNode2 = null;
                this.action2 = -1;
                this.direction2 = -1;
            }
            //fall down case, create pseudo node
            if (direction2 == 2)
            {
                Node newNext = GetOppositeFallDownNode(nextNode);
                if (newNext != null)
                {
                    nextNode = newNext;
                    this.action = 2;

                    distance = (float)Math.Sqrt(Math.Pow(nodes[0].getX() - nextNode.getX(), 2) + Math.Pow(nodes[0].getY() - nextNode.getY(), 2));
                    distanceList.Add(distance);
                }
            }
            return -1;
        }
    }
}
