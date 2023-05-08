using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public class Area
    {
        private float leftX;
        private float rightX;
        private float topY;
        private float bottomY;
        private bool[] visited;
        private bool[] possibleExpansion;
        private int visitedEdges = 0;

        public Area(float lX, float rX, float tY, float bY)
        {
            if(lX < 0)
            {
                leftX = 0;
            } else
            {
                leftX = lX;
            }
            if(tY < 0)
            {
                topY = 0;
            } else
            {
                topY = tY;
            }
            rightX = rX;
            
            bottomY = bY;

            visited = new bool[4];
            possibleExpansion = new bool[4];
        }

        public float lX()
        {
            return leftX;
        }

        public float rX()
        {
            return rightX;
        }

        public float tY()
        {
            return topY;
        }

        public float bY()
        {
            return bottomY;
        }

        //visited edges when creating areas
        //0 - top; 1 - right; 2 - down; 3 - left
        public void visitedEdge(int edge)
        {
            visited[edge] = true;
            visitedEdges++;
        }

        public bool edgeVisited(int edge)
        {
            return visited[edge];
        }

        public bool allEdgesVisited()
        {
            if (visitedEdges == 4)
            {
                return true;
            }
            return false;
        }

        public void expand(int edge)
        {
            possibleExpansion[edge] = true;
        }

        //checks if there is an edge to expand, returns it while taking it of the list of possible expansions
        public int possibleToExpand()
        {
            for(int i = 0; i < 4; i++)
            {
                if(possibleExpansion[i])
                {
                    possibleExpansion[i] = false;
                    return i;
                }
            }
            return -1;
        }
    }
}
