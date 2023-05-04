using GeometryFriends.AI.Perceptions.Information;
using GeometryFriends.AI.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace GeometryFriendsAgents
{
    public class DiamondInfo
    {
        private int id;
        private float posX;
        private float posY;
        private Platform platform;
        private List<Node> closestStates;
        private List<Node> secondClosestStates;
        private List<NodeMP> closestStatesMP;
        private List<NodeMP> secondClosestStatesMP;
        private List<Area> smallArea;
        private List<Area> bigArea;
        private Random rnd;
        private int smallAreaRadius = 300;
        private int bigAreaRadius = 600;
        private bool caught;

        public DiamondInfo(float x, float y, Platform p, int[,] levelLayout)
        {
            posX = x;
            posY = y;
            platform = p;
            closestStates = new List<Node>();
            secondClosestStates = new List<Node>();
            closestStatesMP = new List<NodeMP>();
            secondClosestStatesMP = new List<NodeMP>();
            rnd = new Random();
            smallArea = new List<Area>();
            bigArea = new List<Area>();
            getAreas(levelLayout);
            caught = false;
        }

        private void getAreas(int[,] levelLayout)
        {
            //see at which points it is possible to go from the diamond to the sides 
            int topY = (int)Math.Round(this.posY);
            int bottomY = (int)Math.Round(this.posY);
            int leftX = (int)Math.Round(this.posX);
            int rightX = (int)Math.Round(this.posX);

            //0 - empty ; 1 - full
            while (topY - 1 >= 0 && levelLayout[(int)Math.Round(this.posX), topY - 1] == 0 && topY >= (int)Math.Round(posY - smallAreaRadius))
            {
                topY--;
            }
            while (leftX - 1 >= 0 && levelLayout[leftX - 1, (int)Math.Round(this.posY)] == 0 && leftX >= (int)Math.Round(posX - smallAreaRadius))
            {
                leftX--;
            }
            while (bottomY + 1 < levelLayout.GetLength(1) && levelLayout[(int)Math.Round(this.posX), bottomY + 1] == 0 && bottomY <= (int)Math.Round(posY + smallAreaRadius))
            {
                bottomY++;
            }
            while (rightX + 1 < levelLayout.GetLength(0) && levelLayout[rightX + 1, (int)Math.Round(this.posY)] == 0 && rightX <= (int)Math.Round(posX + smallAreaRadius))
            {
                rightX++;
            }

            smallArea.Add(new Area(leftX, rightX, topY, bottomY, true));

            //check if the area can be expanded
            Area area = checkMoreAreas(smallArea, smallAreaRadius, levelLayout);
            while (area != null)
            {
                expandArea(area, smallArea, smallAreaRadius, levelLayout);
                area = checkMoreAreas(smallArea, smallAreaRadius, levelLayout);
            }

            //calculate the big area around the diamond
            while (topY - 1 >= 0 && levelLayout[(int)Math.Round(this.posX), topY - 1] == 0 && topY >= (int)Math.Round(posY - bigAreaRadius))
            {
                topY--;
            }
            while (bottomY + 1 < levelLayout.GetLength(1) && levelLayout[(int)Math.Round(this.posX), bottomY + 1] == 0 && bottomY <= (int)Math.Round(posY + bigAreaRadius))
            {
                bottomY++;
            }
            while (leftX - 1 >= 0 && levelLayout[leftX - 1, (int)Math.Round(this.posY)] == 0 && leftX >= (int)Math.Round(posX - bigAreaRadius))
            {
                leftX--;
            }
            while (rightX + 1 < levelLayout.GetLength(0) && levelLayout[rightX + 1, (int)Math.Round(this.posY)] == 0 && rightX <= (int)Math.Round(posX + bigAreaRadius))
            {
                rightX++;
            }

            bigArea.Add(new Area(leftX, rightX, topY, bottomY, true));

            //check if the area can be expanded
            area = checkMoreAreas(bigArea, bigAreaRadius, levelLayout);
            while (area != null)
            {
                expandArea(area, bigArea, bigAreaRadius, levelLayout);
                area = checkMoreAreas(bigArea, bigAreaRadius, levelLayout);
            }
        }

        private void expandArea(Area area, List<Area> areas, int radius, int[,] levelLayout)
        {
            //TODO - avoid some unwanted areas
            int edge = area.possibleToExpand();
            int originEdge = -1;

            int newLX = 0;
            int newRX = 0;
            int newTY = 0;
            int newBY = 0;

            //if top
            if(edge == 0)
            {
                //the origin edge is bottom of the new area
                originEdge = 2;
                //the bottom Y of the new area corresponds to the top Y of the current area
                newBY = (int)Math.Round(area.tY() - 1);
                //get the new area left X
                for (int i = (int)Math.Round(area.lX()); i <= area.rX(); i++)
                {
                    //the left X is when the first empty pixel appears along the edge
                    //this has to happen always
                    if(levelLayout[i, newBY] == 0)
                    {
                        newLX = i;
                        break;
                    }
                }
                //get the new area right X
                for (int i = newLX; i <= area.rX(); i++)
                {
                    //the right X is when the first full pixel appears after obtaining the left X
                    if (levelLayout[i, newBY] == 1)
                    {
                        newRX = i - 1;
                        break;
                    }
                    //if none was found
                    newRX = (int)Math.Round(area.rX());
                }
                //get the new area top Y
                newTY = newBY;
                
                while (newTY - 1 >= 0 && levelLayout[newLX, newTY - 1] == 0 && newTY >= (int)Math.Round(posY - radius))
                {
                    newTY--;
                }
            }

            //if right
            if (edge == 1)
            {
                //the origin edge is left of the new area
                originEdge = 2;
                //the left X of the new area corresponds to the right X of the current area
                newLX = (int)Math.Round(area.rX() + 1);
                //get the new area top Y
                for (int i = (int)Math.Round(area.tY()); i <= area.bY(); i++)
                {
                    //the top Y is when the first empty pixel appears along the edge
                    if (levelLayout[newLX, i] == 0)
                    {
                        newTY = i;
                        break;
                    }
                }
                //get the new area bottom Y
                for (int i = newTY; i <= area.bY(); i++)
                {
                    //the bottom Y is when the first full pixel appears after obtaining the top Y
                    if (levelLayout[newLX, i] == 1)
                    {
                        newBY = i - 1;
                        break;
                    }
                    newBY = (int)Math.Round(area.bY());
                }
                //get the new area right X
                newRX = newLX;
                while (newRX + 1 < levelLayout.GetLength(0) && levelLayout[newRX + 1, newTY] == 0 && newRX <= (int)Math.Round(posX + radius))
                {
                    newRX++;
                }
            }

            //if bottom
            if (edge == 2)
            {
                //the origin edge is top of the new area
                originEdge = 0;
                //the top Y of the new area corresponds to the bottom Y of the current area
                newTY = (int)Math.Round(area.bY() + 1);
                //get the new area left X
                for (int i = (int)Math.Round(area.lX()); i <= area.rX(); i++)
                {
                    //the left X is when the first empty pixel appears along the edge
                    if (levelLayout[i, newTY] == 0)
                    {
                        newLX = i;
                        break;
                    }
                }
                //get the new area right X
                for (int i = newLX; i <= area.rX(); i++)
                {
                    //the right X is when the first full pixel appears after obtaining the left X
                    if (levelLayout[i, newTY] == 1)
                    {
                        newRX = i - 1;
                        break;
                    }

                    newRX = (int)Math.Round(area.rX());
                }
                //get the new area bottom Y
                newBY = newTY;
                
                while (newBY + 1 < levelLayout.GetLength(1) && levelLayout[newLX, newBY + 1] == 0 && newBY <= (int)Math.Round(posY + radius))
                {
                    newBY++;
                }
            }

            //if left
            if (edge == 3)
            {
                //the origin edge is right of the new area
                originEdge = 1;
                //the right X of the new area corresponds to the left X of the current area
                newRX = (int)Math.Round(area.lX() - 1);
                //get the new area top Y
                for (int i = (int)Math.Round(area.tY()); i <= area.bY(); i++)
                {
                    //the top Y is when the first empty pixel appears along the edge
                    if (levelLayout[newRX, i] == 0)
                    {
                        newTY = i;
                        break;
                    }
                }
                //get the new area bottom Y
                for (int i = newTY; i <= area.bY(); i++)
                {
                    //the bottom Y is when the first full pixel appears after obtaining the top Y
                    if (levelLayout[newRX, i] == 1)
                    {
                        newBY = i - 1;
                        break;
                    }

                    newBY = (int)Math.Round(area.bY());
                }
                //get the new area left X
                newLX = newRX;
                
                while (newLX - 1 >= 0 && levelLayout[newLX - 1, newTY] == 0 && newLX >= (int)Math.Round(posX - radius))
                {
                    newLX--;
                }
            }

            //create the new Area and inform which edges should not be considered
            Area newArea = new Area(newLX, newRX, newTY, newBY, false);
            foreach (Area otherArea in areas)
            {
                //if the top collides with the bottom of another area
                if(newTY >= otherArea.tY() && newTY <= otherArea.bY() && (newLX <= otherArea.rX() && newLX >= otherArea.lX()) ||
                                              (newRX <= otherArea.rX() && newRX >= otherArea.lX()))
                {
                    //do not check the top edge
                    newArea.visitedEdge(0);
                }
                //if the right collides with the left of another area
                if (newRX <= otherArea.rX() && newRX >= otherArea.lX() && (newTY <= otherArea.bY() && newTY >= otherArea.tY()) ||
                                                                          (newBY <= otherArea.bY() && newBY >= otherArea.tY()))
                {
                    //do not check the right edge
                    newArea.visitedEdge(1);
                }
                //if the bottom collides with the top of another area
                if (newBY <= otherArea.bY() && newBY >= otherArea.tY() && (newLX <= otherArea.rX() && newLX >= otherArea.lX()) ||
                                                                          (newRX <= otherArea.rX() && newRX >= otherArea.lX()))
                {
                    //do not check the bottom edge
                    newArea.visitedEdge(2);
                }
                //if the left collides with the right of another area
                if (newLX >= otherArea.lX() && newLX <= otherArea.rX() && (newTY <= otherArea.bY() && newTY >= otherArea.tY()) ||
                                                   (newBY <= otherArea.bY() && newBY >= otherArea.tY()))
                {
                    //do not check the bottom edge
                    newArea.visitedEdge(3);
                }
                //if this area invalidates all edges then break
                if (newArea.allEdgesVisited())
                {
                    break;
                }

            }
            newArea.visitedEdge(originEdge);
            areas.Add(newArea);
        }

        private Area checkMoreAreas(List<Area> areas, int radius, int[,] levelLayout)
        {
            foreach(Area area in areas)
            {
                //only expand initial area and its sides that were not expanded yet
                if (!area.isOriginal() || area.allEdgesVisited())
                {
                    continue;
                }
                //check top
                if (area.tY() > posY - radius && !area.edgeVisited(0))
                {
                    //inform this edge was checked
                    area.visitedEdge(0);
                    //see if there is a opening above
                    for (int i = (int)Math.Round(area.lX()); i <= area.rX(); i++)
                    {
                        //if so
                        if(area.tY() > 0 && levelLayout[i, (int)Math.Round(area.tY() - 1)] == 0)
                        {
                            area.expand(0);
                            return area;
                        }
                    }
                }
                //check right
                if (area.rX() < posX + radius && !area.edgeVisited(1))
                {
                    //inform this edge was checked
                    area.visitedEdge(1);
                    //see if there is a opening on the right
                    for (int i = (int)Math.Round(area.tY()); i <= area.bY(); i++)
                    {
                        //if so
                        if (area.rX() < levelLayout.GetLength(0) - 1 && levelLayout[(int)Math.Round(area.rX() + 1), i] == 0)
                        {
                            area.expand(1);
                            return area;
                        }
                    }
                }
                //check bottom
                if (area.bY() < posY + radius && !area.edgeVisited(2))
                {
                    //inform this edge was checked
                    area.visitedEdge(2);
                    //see if there is a opening on the bottom
                    for (int i = (int)Math.Round(area.lX()); i <= area.rX(); i++)
                    {
                        //if so
                        if (area.bY() < levelLayout.GetLength(1) - 1 && levelLayout[i, (int)Math.Round(area.bY() + 1)] == 0)
                        {
                            area.expand(2);
                            return area;
                        }
                    }
                }
                //check left
                if (area.lX() > posX - radius && !area.edgeVisited(3))
                {
                    //inform this edge was checked
                    area.visitedEdge(3);
                    //see if there is a opening on the right
                    for (int i = (int)Math.Round(area.tY()); i <= area.bY(); i++)
                    {
                        //if so
                        if (area.lX() > 0 && levelLayout[(int)Math.Round(area.lX() - 1), i] == 0)
                        {
                            area.expand(3);
                            return area;
                        }
                    }
                }

            }
            return null;
        }

        public List<Area> getSamllAreasList()
        {
            return smallArea;
        }

        public void resetStates()
        {
            closestStates.Clear();
            secondClosestStates.Clear();
        }

        public float getX()
        {
            return posX;
        }

        public float getY()
        {
            return posY;
        }

        public Platform getPlatform()
        {
            return platform;
        }

        public void setId(int i)
        {
            id = i;
        }

        public int getId()
        {
            return id;
        }

        public void removeClosestSate(Node node)
        {
            closestStates.Remove(node);
        }

        public void removeSecondClosestSate(Node node)
        {
            secondClosestStates.Remove(node);
        }

        public void removeClosestSateMP(NodeMP node)
        {
            closestStatesMP.Remove(node);
        }

        public void removeSecondClosestSateMP(NodeMP node)
        {
            secondClosestStatesMP.Remove(node);
        }

        public Node getRandomClosestState()
        {
            if (closestStates.Count != 0)
            {
                Node node = closestStates[rnd.Next(closestStates.Count)];

                //check if node is not a closed one
                while (closestStates.Count != 0 && node.getRemainingMoves().Count == 0)
                {
                    removeClosestSate(node);
                    if (closestStates.Count != 0)
                    {
                        node = closestStates[rnd.Next(closestStates.Count)];
                    }
                }
                if (closestStates.Count != 0)
                {
                    return node;
                }
            }
            if (secondClosestStates.Count != 0)
            {
                Node node = secondClosestStates[rnd.Next(secondClosestStates.Count)];

                //check if node is not a closed one
                while (secondClosestStates.Count != 0 && node.getRemainingMoves().Count == 0)
                {
                    removeSecondClosestSate(node);
                    if(secondClosestStates.Count != 0)
                    {
                        node = secondClosestStates[rnd.Next(secondClosestStates.Count)];
                    }
                }
                if (secondClosestStates.Count != 0)
                {
                    return node;
                }
            }
            return null;
        }

        public NodeMP getRandomClosestStateMP()
        {
            if (closestStatesMP.Count != 0)
            {
                NodeMP node = closestStatesMP[rnd.Next(closestStatesMP.Count)];

                //check if node is not a closed one
                while (closestStates.Count != 0 && node.getRemainingMoves().Count == 0)
                {
                    removeClosestSateMP(node);
                    if (closestStatesMP.Count != 0)
                    {
                        node = closestStatesMP[rnd.Next(closestStatesMP.Count)];
                    }
                }
                if (closestStatesMP.Count != 0)
                {
                    return node;
                }
            }
            if (secondClosestStatesMP.Count != 0)
            {
                NodeMP node = secondClosestStatesMP[rnd.Next(secondClosestStatesMP.Count)];

                //check if node is not a closed one
                while (secondClosestStatesMP.Count != 0 && node.getRemainingMoves().Count == 0)
                {
                    removeSecondClosestSateMP(node);
                    if (secondClosestStatesMP.Count != 0)
                    {
                        node = secondClosestStatesMP[rnd.Next(secondClosestStatesMP.Count)];
                    }
                }
                if (secondClosestStatesMP.Count != 0)
                {
                    return node;
                }
            }
            return null;
        }

        public void insertClosestNodesMP(NodeMP node)
        {
            if(node.getChildren().Count == 0)
            {
                insertClosestNodeMP(node);
            }
            foreach(NodeMP child in node.getChildren())
            {
                insertClosestNodesMP(child);
            }
        }

        public void insertClosestNode(Node node)
        {
            //check if diamond has already been caught
            bool uncaught = false;

            if(node.getState().getUncaughtDiamonds() != null)
            {
                foreach (DiamondInfo diamond in node.getState().getUncaughtDiamonds())
                {
                    if (Math.Round(diamond.getX()) == Math.Round(this.posX) && Math.Round(diamond.getY()) == Math.Round(this.posY))
                    {
                        uncaught = true;
                    }
                }
            }
            else
            {
                foreach (CollectibleRepresentation diamond in node.getState().getUncaughtCollectibles())
                {
                    if (Math.Round(diamond.X) == Math.Round(this.posX) && Math.Round(diamond.Y) == Math.Round(this.posY))
                    {
                        uncaught = true;
                    }
                }
            }

            //if the diamond has been caught in this state, then it is not a close state
            if (!uncaught)
            {
                return;
            }

            float pX = (float)Math.Round(node.getState().getPosX());
            float pY = (float)Math.Round(node.getState().getPosY());

            for (int i = 0; i < smallArea.Count(); i++)
            {
                //check if in small area
                if (pX >= smallArea[i].lX() &&
                    pX <= smallArea[i].rX() &&
                    pY >= smallArea[i].tY() &&
                    pY <= smallArea[i].bY())
                {
                    closestStates.Add(node);
                    return;
                }
            }
            for (int i = 0; i < bigArea.Count(); i++)
            {
                if (pX >= bigArea[i].lX() &&
                pX <= bigArea[i].rX() &&
                pY >= bigArea[i].tY() &&
                pY <= bigArea[i].bY())
                {
                    secondClosestStates.Add(node);
                    return;
                }
            }

        }
        
        public void insertClosestNodeMP(NodeMP node)
        {
            //check if diamond has already been caught
            bool uncaught = false;
            foreach (CollectibleRepresentation diamond in node.getState().getUncaughtCollectibles())
            {
                if (Math.Round(diamond.X) == Math.Round(this.posX) && Math.Round(diamond.Y) == Math.Round(this.posY))
                {
                    uncaught = true;
                }
            }

            //if the diamond has been caught in this state, then it is not a close state
            if (!uncaught)
            {
                return;
            }

            float pX = (float)Math.Round(node.getState().getPosX());
            float pY = (float)Math.Round(node.getState().getPosY());

            for (int i = 0; i < smallArea.Count(); i++)
            {
                //check if in small area
                if (pX >= smallArea[i].lX() &&
                    pX <= smallArea[i].rX() &&
                    pY >= smallArea[i].tY() &&
                    pY <= smallArea[i].bY())
                {
                    closestStatesMP.Add(node);
                    return;
                }
            }
            for (int i = 0; i < bigArea.Count(); i++)
            {
                if (pX >= bigArea[i].lX() &&
                pX <= bigArea[i].rX() &&
                pY >= bigArea[i].tY() &&
                pY <= bigArea[i].bY())
                {
                    secondClosestStatesMP.Add(node);
                    return;
                }
            }

        }

        public void setCaught()
        {
            caught = true;
        }

        public bool wasCaught()
        {
            return caught;
        }

        public List<DebugInformation> getAreaDebug()
        {
            List<DebugInformation> debugInfo = new List<DebugInformation>();


            for(int i = 0; i < smallArea.Count(); i++)
            {
                debugInfo.Add(GeometryFriends.AI.Debug.DebugInformationFactory.CreateLineDebugInfo(new PointF(smallArea[i].lX(), smallArea[i].tY()), new PointF(smallArea[i].lX(), smallArea[i].bY()), new GeometryFriends.XNAStub.Color(255, 0, 0)));
                debugInfo.Add(GeometryFriends.AI.Debug.DebugInformationFactory.CreateLineDebugInfo(new PointF(smallArea[i].lX(), smallArea[i].tY()), new PointF(smallArea[i].rX(), smallArea[i].tY()), new GeometryFriends.XNAStub.Color(255, 0, 0)));
                debugInfo.Add(GeometryFriends.AI.Debug.DebugInformationFactory.CreateLineDebugInfo(new PointF(smallArea[i].rX(), smallArea[i].tY()), new PointF(smallArea[i].rX(), smallArea[i].bY()), new GeometryFriends.XNAStub.Color(255, 0, 0)));
                debugInfo.Add(GeometryFriends.AI.Debug.DebugInformationFactory.CreateLineDebugInfo(new PointF(smallArea[i].lX(), smallArea[i].bY()), new PointF(smallArea[i].rX(), smallArea[i].bY()), new GeometryFriends.XNAStub.Color(255, 0, 0)));

            }

            for(int i = 0; i < bigArea.Count(); i++)
            {
                debugInfo.Add(GeometryFriends.AI.Debug.DebugInformationFactory.CreateLineDebugInfo(new PointF(bigArea[i].lX(), bigArea[i].tY()), new PointF(bigArea[i].lX(), bigArea[i].bY()), new GeometryFriends.XNAStub.Color(255, 0, 0)));
                debugInfo.Add(GeometryFriends.AI.Debug.DebugInformationFactory.CreateLineDebugInfo(new PointF(bigArea[i].lX(), bigArea[i].tY()), new PointF(bigArea[i].rX(), bigArea[i].tY()), new GeometryFriends.XNAStub.Color(255, 0, 0)));
                debugInfo.Add(GeometryFriends.AI.Debug.DebugInformationFactory.CreateLineDebugInfo(new PointF(bigArea[i].rX(), bigArea[i].tY()), new PointF(bigArea[i].rX(), bigArea[i].bY()), new GeometryFriends.XNAStub.Color(255, 0, 0)));
                debugInfo.Add(GeometryFriends.AI.Debug.DebugInformationFactory.CreateLineDebugInfo(new PointF(bigArea[i].lX(), bigArea[i].bY()), new PointF(bigArea[i].rX(), bigArea[i].bY()), new GeometryFriends.XNAStub.Color(255, 0, 0)));

            }

            return debugInfo;
        }

        public void clearAreaLists()
        {
            closestStates.Clear();
            closestStatesMP.Clear();
            secondClosestStates.Clear();
            secondClosestStatesMP.Clear();
        }
    }
}
