using GeometryFriendsAgents.WorldObjects;
using System;
using System.Collections.Generic;

namespace GeometryFriendsAgents.Model {
    class SquareGraph : Graph {

        public SquareGraph(List<Platform> platforms) : base(platforms) { }

        protected override void CreateConnection(GraphNode analysingNode, GraphNode test, List<Platform> dummyPlatforms) {

            foreach (Platform p in dummyPlatforms) {
                if (p.Left > analysingNode.Platform.Model.Area.Left + 1.1f * SquareWorldModel.MIN_HEIGHT || p.Right < analysingNode.Platform.Model.Area.Right - 1.1f + SquareWorldModel.MIN_HEIGHT) {
                    if (p.Height == 1.1f * SquareWorldModel.MIN_HEIGHT) {
                        CreateHorizontalConnection(analysingNode, test, p);
                    } else if (p.Width <= 1.1f * SquareWorldModel.MIN_HEIGHT) {
                        CreateVerticalConnection(analysingNode, test, p);
                    } else {
                        CreateDiagonalConnection(analysingNode, test, p);
                    }
                }
            }
        }

        private void CreateDiagonalConnection(GraphNode analysingNode, GraphNode test, Platform p) {
            GraphNode topNode, bottomNode;
            if (analysingNode.Platform.Top < test.Platform.Top) {
                topNode = analysingNode;
                bottomNode = test;
            } else {
                topNode = test;
                bottomNode = analysingNode;
            }


            GraphLink topToBottom;
            float deltaY = bottomNode.Platform.Top - topNode.Platform.Top;
            float deltaX = bottomNode.Platform.Left - topNode.Platform.Right;
            if (p.Left == topNode.Platform.Right) {
                topToBottom = new GraphLink(GetLinkID(), topNode, topNode.Platform.Right, bottomNode, deltaX, deltaY);
            } else {
                topToBottom = new GraphLink(GetLinkID(), topNode, topNode.Platform.Left, bottomNode, deltaX, deltaY);
            }

            links.Add(topToBottom);
            topNode.addAjacentNode(topToBottom);
        }

        private void CreateHorizontalConnection(GraphNode analysingNode, GraphNode test, Platform p) {
            GraphNode leftNode, rightNode;

            if (analysingNode.Platform.Right < test.Platform.Left) {
                leftNode = analysingNode;
                rightNode = test;
            } else {
                leftNode = test;
                rightNode = analysingNode;
            }

            float deltaX = rightNode.Platform.Left - leftNode.Platform.Right;

            if (deltaX <= 1.2f * SquareWorldModel.DEFAULT_HEIGHT) {
                GraphLink leftToRight = new GraphLink(GetLinkID(), leftNode, leftNode.Platform.Right, rightNode, deltaX, 0);
                leftNode.addAjacentNode(leftToRight);
                links.Add(leftToRight);

                deltaX = -deltaX;
                GraphLink rightToLeft = new GraphLink(GetLinkID(), rightNode, rightNode.Platform.Left, leftNode, deltaX, 0);
                rightNode.addAjacentNode(rightToLeft);
                links.Add(rightToLeft);
            }

        }

        private void CreateVerticalConnection(GraphNode analysingNode, GraphNode test, Platform p) {
            GraphNode topNode, bottomNode;
            if (analysingNode.Platform.Top < test.Platform.Top) {
                topNode = analysingNode;
                bottomNode = test;
            } else {
                topNode = test;
                bottomNode = analysingNode;
            }

            float xPos;
            float f = p.xPos - p.Width / 2;
            float deltaX = SquareWorldModel.MIN_HEIGHT;

            if (f == topNode.Platform.Right) {
                xPos = topNode.Platform.Right + SquareWorldModel.MIN_HEIGHT/2f;
            } else {
                xPos = topNode.Platform.Left - SquareWorldModel.MIN_HEIGHT / 2f;
            }

            

            float deltaY = bottomNode.Platform.Top - topNode.Platform.Top;
            GraphLink topToBottom = new GraphLink(GetLinkID(), topNode, xPos, bottomNode, deltaX, deltaY);

            links.Add(topToBottom);

            topNode.addAjacentNode(topToBottom);

            if (p.Height <= 1.1f*SquareWorldModel.MIN_HEIGHT) {
                deltaY = -deltaY;
                GraphLink bottomToTop = new GraphLink(GetLinkID(), bottomNode, xPos, topNode, deltaX, deltaY);
                bottomNode.addAjacentNode(bottomToTop);
                links.Add(bottomToTop);
            }
        }


        protected override List<Platform> CreatePlatforms(GraphNode analysingNode, GraphNode test) {
            List<Platform> toReturn = new List<Platform>();

            if (test.Platform.Top == analysingNode.Platform.Top) {
                if (test.Platform.Right == analysingNode.Platform.Left) {
                    toReturn.Add(new SquarePlatform(test.Platform.Right, test.Platform.Top, 0, 0, analysingNode.Platform.Model));
                    return toReturn;
                }
                if (test.Platform.Left == analysingNode.Platform.Right) {
                    toReturn.Add(new SquarePlatform(test.Platform.Left, test.Platform.Top, 0, 0, analysingNode.Platform.Model));
                    return toReturn;
                }
            }

            if (test.Platform.Top != analysingNode.Platform.Top) {
                if (test.Platform.Right < analysingNode.Platform.Left) {
                    toReturn.Add(new SquarePlatform((test.Platform.Right + analysingNode.Platform.Left) / 2, ((test.Platform.Top + analysingNode.Platform.Top) / 2) - ((int)(1.1f * SquareWorldModel.MIN_HEIGHT)), Math.Abs(test.Platform.Top - analysingNode.Platform.Top) + SquareWorldModel.MIN_HEIGHT, Math.Abs(analysingNode.Platform.Left - test.Platform.Right), analysingNode.Platform.Model));
                    return toReturn;
                }

                if (test.Platform.Left > analysingNode.Platform.Right) {
                    toReturn.Add(new SquarePlatform((test.Platform.Left + analysingNode.Platform.Right) / 2, ((test.Platform.Top + analysingNode.Platform.Top) / 2) - ((int)(1.1 * SquareWorldModel.MIN_HEIGHT)), Math.Abs(test.Platform.Top - analysingNode.Platform.Top) +SquareWorldModel.MIN_HEIGHT, Math.Abs(analysingNode.Platform.Right - test.Platform.Left), analysingNode.Platform.Model));
                    return toReturn;
                }
            }

            Platform topPlatform, bottomPlatform;

            if (analysingNode.Platform.Top < test.Platform.Top) {
                topPlatform = analysingNode.Platform;
                bottomPlatform = test.Platform;
            } else {
                topPlatform = test.Platform;
                bottomPlatform = analysingNode.Platform;
            }

            int yPos = (topPlatform.Top + bottomPlatform.Top) / 2;
            int height = bottomPlatform.Top - topPlatform.Top;
            int width = SquareWorldModel.MIN_HEIGHT;

            if (topPlatform.Left >= bottomPlatform.Left + SquareWorldModel.DEFAULT_HEIGHT) {
                toReturn.Add(new SquarePlatform(topPlatform.Left - ((int)((1f / 2) * SquareWorldModel.MIN_HEIGHT)), yPos, height, width, analysingNode.Platform.Model));
            }

            if (topPlatform.Right <= bottomPlatform.Right - SquareWorldModel.DEFAULT_HEIGHT) {
                toReturn.Add(new SquarePlatform(topPlatform.Right + ((int)((1f / 2) * SquareWorldModel.MIN_HEIGHT)), yPos, height, width, analysingNode.Platform.Model));
            }
            return toReturn;
        }


        //protected override List<Platform> CreatePlatforms(GraphNode analysingNode, GraphNode test) {
        //    List<Platform> toReturn = new List<Platform>();

        //    if (test.Platform.Right <= analysingNode.Platform.Left || test.Platform.Left >= analysingNode.Platform.Right) {
        //        toReturn.Add(new SquarePlatform((test.Platform.Right + analysingNode.Platform.Left) / 2, ((test.Platform.Top + analysingNode.Platform.Top) / 2), SquareWorldModel.MIN_HEIGHT, Math.Abs(analysingNode.Platform.Left - test.Platform.Right), analysingNode.Platform.Model));
        //        return toReturn;
        //    }

        //    Platform topPlatform, bottomPlatform;

        //    if (analysingNode.Platform.Top < test.Platform.Top) {
        //        topPlatform = analysingNode.Platform;
        //        bottomPlatform = test.Platform;
        //    } else {
        //        topPlatform = test.Platform;
        //        bottomPlatform = analysingNode.Platform;
        //    }

        //    int yPos = (topPlatform.Top + bottomPlatform.Top) / 2;
        //    int height = bottomPlatform.Top - topPlatform.Top;
        //    int width =  SquareWorldModel.MIN_HEIGHT;

        //    if (topPlatform.Left > bottomPlatform.Left + 1.1f * SquareWorldModel.MIN_HEIGHT) {
        //        toReturn.Add(new SquarePlatform(topPlatform.Left - ((int)(1.1 / 2) * SquareWorldModel.MIN_HEIGHT), yPos, height, width, analysingNode.Platform.Model));
        //    }

        //    if (topPlatform.Right < bottomPlatform.Right - 1.1f * SquareWorldModel.MIN_HEIGHT) {
        //        toReturn.Add(new SquarePlatform(topPlatform.Right + ((int)(1.1 / 2) * SquareWorldModel.MIN_HEIGHT), yPos, height, width, analysingNode.Platform.Model));
        //    }
        //    return toReturn;
        //}
    }
}
