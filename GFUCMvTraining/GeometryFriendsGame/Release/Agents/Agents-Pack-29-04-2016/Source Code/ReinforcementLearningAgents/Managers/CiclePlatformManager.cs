using GeometryFriendsAgents.Model;
using GeometryFriendsAgents.WorldObjects;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GeometryFriendsAgents.Managers {
    class CirclePlatformManager : PlatformManager {

        public CirclePlatformManager(CircleWorldModel circleWorldModel) : base(circleWorldModel) { }



        internal override void CreatePlatforms(float[] oI, float[] aI, int numberGeneric, int numberAgent, float[] agentsInfo) {
            List<Platform> platforms = new List<Platform>();
            int temp = 1;
            if (numberGeneric > 0) {
                while (temp <= numberGeneric) {
                    Platform platform = new CirclePlatform((int)oI[(temp * 4) - 4], (int)oI[(temp * 4) - 3], (int)oI[(temp * 4) - 2], (int)oI[(temp * 4) - 1], _model);
                    if (!(platform.Bottom <= _model.Area.Top || platform.Top >= _model.Area.Bottom || platform.Left >= _model.Area.Right || platform.Right <= _model.Area.Left)) {
                        platforms.Add(platform);
                    }
                    temp++;
                }
            }

            temp = 1;
            if (numberAgent > 0) {
                while (temp <= numberAgent) {
                    Platform platform = new CirclePlatform((int)oI[(temp * 4) - 4], (int)oI[(temp * 4) - 3], (int)oI[(temp * 4) - 2], (int)oI[(temp * 4) - 1], _model);
                    if (!(platform.Bottom <= _model.Area.Top || platform.Top >= _model.Area.Bottom || platform.Left >= _model.Area.Right || platform.Right <= _model.Area.Left)) {
                        platforms.Add(platform);
                    }
                    temp++;
                }
            }
            Platform groundPlatform = new CirclePlatform(_model.Area.Left + _model.Area.Width / 2, _model.Area.Bottom, 0, _model.Area.Width, _model);
            groundPlatform.LeftWall = true;
            groundPlatform.RightWall = true;
            platforms.Add(groundPlatform);

            int squareArea = SquareWorldModel.DEFAULT_HEIGHT * SquareWorldModel.DEFAULT_HEIGHT;

            int width = squareArea / (int)agentsInfo[4];

            if (OtherAgentIsInPlay(agentsInfo)) {
                Platform squareAgent = new CirclePlatform((int)agentsInfo[0], (int)agentsInfo[1], (int)agentsInfo[4], width, _model);
                platforms.Add(squareAgent);
            }

            platforms.Sort();



            List<Platform> finalPlatforms = SplitPlatforms(platforms);

            _platforms = new ConcurrentDictionary<int, Platform>();
            for (temp = 0; temp < finalPlatforms.Count; temp++) {
                finalPlatforms[temp].ID = temp;
                _platforms.GetOrAdd(temp, finalPlatforms[temp]);
            }

            graph = new CircleGraph(new List<Platform>(_platforms.Values));

         //   ConsolePrinter.PrintLine(graph.ToString());
        }

        internal override Platform FindPlatform(float xPos, float yPos) {
            foreach (Platform p in _platforms.Values) {
                if (xPos >= p.Left && xPos <= p.Right) {
                    if (yPos <= p.Top && yPos >= p.Top - 1.1 * CircleWorldModel.DEFAULT_RADIUS) {
                        return p;
                    }
                }
            }
            return null;
        }

        internal override Platform FindAgentPlatform(float xPos, float yPos) {
            foreach (Platform p in _platforms.Values) {
                if (xPos >= p.Left && xPos <= p.Right) {
                    if (yPos <= p.Top && yPos >= p.Top - 1.01 * CircleWorldModel.DEFAULT_RADIUS) {
                        return p;
                    }
                }
            }
            return null;
        }
    }
}
