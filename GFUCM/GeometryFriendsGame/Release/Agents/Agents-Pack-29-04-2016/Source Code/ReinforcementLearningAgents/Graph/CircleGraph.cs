using GeometryFriendsAgents.WorldObjects;
using System;
using System.Collections.Generic;

namespace GeometryFriendsAgents.Model {
    class CircleGraph : Graph {

        public CircleGraph(List<Platform> platforms) : base(platforms) { }

        protected override void CreateConnection(GraphNode analysingNode, GraphNode test, List<Platform> dummyPlatforms) {

            foreach (Platform p in dummyPlatforms) {
                if (p.Left > analysingNode.Platform.Model.Area.Left && p.Right < analysingNode.Platform.Model.Area.Right) {
                    if (p.Height == 2.1f * CircleWorldModel.DEFAULT_RADIUS) {
                        CreateHorizontalConnection(analysingNode, test, p);
                    } else if (p.Width <= 2.1f * CircleWorldModel.DEFAULT_RADIUS) {
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

            topNode.addAjacentNode(topToBottom);
            links.Add(topToBottom);

            if (p.Height <= 8 * CircleWorldModel.DEFAULT_RADIUS) {
                GraphLink bottomToTop;
                deltaY = topNode.Platform.Top - bottomNode.Platform.Top;
                deltaX = topNode.Platform.Left - bottomNode.Platform.Right;
                if (p.Left == bottomNode.Platform.Right) {
                    bottomToTop = new GraphLink(GetLinkID(), bottomNode, bottomNode.Platform.Right, topNode, deltaX, deltaY);
                } else {
                    bottomToTop = new GraphLink(GetLinkID(), bottomNode, bottomNode.Platform.Left, topNode, deltaX, deltaY);
                }
                bottomNode.addAjacentNode(bottomToTop);
                links.Add(bottomToTop);
            }

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

            GraphLink leftToRight = new GraphLink(GetLinkID(), leftNode, leftNode.Platform.Right, rightNode, deltaX, 0);
            leftNode.addAjacentNode(leftToRight);
            links.Add(leftToRight);

            deltaX = -deltaX;
            GraphLink rightToLeft = new GraphLink(GetLinkID(), rightNode, rightNode.Platform.Left, leftNode, deltaX, 0);
            rightNode.addAjacentNode(rightToLeft);
            links.Add(rightToLeft);

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

            if (p.xPos - p.Width / 2 == topNode.Platform.Right) {
                xPos = topNode.Platform.Right;
            } else {
                xPos = topNode.Platform.Left;
            }

            float deltaY = bottomNode.Platform.Top - topNode.Platform.Top;
            GraphLink topToBottom = new GraphLink(GetLinkID(), topNode, xPos, bottomNode, 0, deltaY);

            topNode.addAjacentNode(topToBottom);
            links.Add(topToBottom);

            if (deltaY < 8 * CircleWorldModel.DEFAULT_RADIUS) {
                deltaY = -deltaY;
                GraphLink bottomToTop = new GraphLink(GetLinkID(), bottomNode, xPos, topNode, 0, deltaY);
                bottomNode.addAjacentNode(bottomToTop);
                links.Add(bottomToTop);
            }
        }

        protected override List<Platform> CreatePlatforms(GraphNode analysingNode, GraphNode test) {
            List<Platform> toReturn = new List<Platform>();

            if (test.Platform.Top == analysingNode.Platform.Top) {
                if (test.Platform.Right == analysingNode.Platform.Left) {
                    toReturn.Add(new CirclePlatform(test.Platform.Right, test.Platform.Top, 0, 0, analysingNode.Platform.Model));
                    return toReturn;                        
                }
                if (test.Platform.Left == analysingNode.Platform.Right) {
                    toReturn.Add(new CirclePlatform(test.Platform.Left, test.Platform.Top, 0, 0, analysingNode.Platform.Model));
                    return toReturn;
                }
            }

            if (test.Platform.Top != analysingNode.Platform.Top) {
                if (test.Platform.Right < analysingNode.Platform.Left) {
                    toReturn.Add(new CirclePlatform((test.Platform.Right + analysingNode.Platform.Left) / 2, ((test.Platform.Top + analysingNode.Platform.Top) / 2) - ((int)(2.1f * CircleWorldModel.DEFAULT_RADIUS)), Math.Abs(test.Platform.Top - analysingNode.Platform.Top) + 2 * CircleWorldModel.DEFAULT_RADIUS, Math.Abs(analysingNode.Platform.Left - test.Platform.Right), analysingNode.Platform.Model));
                    return toReturn;
                }

                if (test.Platform.Left > analysingNode.Platform.Right) {
                    toReturn.Add(new CirclePlatform((test.Platform.Left + analysingNode.Platform.Right) / 2, ((test.Platform.Top + analysingNode.Platform.Top) / 2) - ((int)(2.1 * CircleWorldModel.DEFAULT_RADIUS)), Math.Abs(test.Platform.Top - analysingNode.Platform.Top) + 2 * CircleWorldModel.DEFAULT_RADIUS, Math.Abs(analysingNode.Platform.Right - test.Platform.Left), analysingNode.Platform.Model));
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
            int width = 2 * CircleWorldModel.DEFAULT_RADIUS;

            if (topPlatform.Left >= bottomPlatform.Left + 2f * CircleWorldModel.DEFAULT_RADIUS) {
                toReturn.Add(new CirclePlatform(topPlatform.Left - ((int)((2f / 2) * CircleWorldModel.DEFAULT_RADIUS)), yPos, height, width, analysingNode.Platform.Model));
            }

            if (topPlatform.Right <= bottomPlatform.Right - 2f * CircleWorldModel.DEFAULT_RADIUS) {
                toReturn.Add(new CirclePlatform(topPlatform.Right + ((int)((2f / 2) * CircleWorldModel.DEFAULT_RADIUS)), yPos, height, width, analysingNode.Platform.Model));
            }
            return toReturn;
        }


    }
}
