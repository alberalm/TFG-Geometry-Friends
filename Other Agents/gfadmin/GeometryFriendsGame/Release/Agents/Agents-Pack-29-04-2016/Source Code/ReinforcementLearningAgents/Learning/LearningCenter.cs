using GeometryFriends.AI;
using GeometryFriendsAgents.Model;
using GeometryFriendsAgents.WorldObjects;
using System;
using System.Collections.Generic;
using System.IO;

namespace GeometryFriendsAgents.Learning {
    abstract class LearningCenter {
        protected const string path = @"..\..\..\..\GeometryFriendsAgents";
        //protected const string path = @"..\..\..\..\Agents";

        protected string _learningFolder;
        protected Dictionary<string, Dictionary<Moves, double>> _intraplatformLessonsLearnt;
        protected Dictionary<string, Dictionary<Moves, double>> _interplatformLessonsLearnt;
        protected WorldModel _model;
        protected bool _toSave;

        protected bool interPlatform = false;

        private Dictionary<int, List<IntraPlatformPlayedGameStateInfo>> _intraplatformPlayedStates;
        private Dictionary<int, List<InterPlatformPlayedGameStateInfo>> _interplatformPlayedStates;

        protected LearningCenter(string filename, WorldModel model) {
            _learningFolder = filename;
            _model = model;
            _interplatformLessonsLearnt = new Dictionary<string, Dictionary<Moves, double>>();
            _intraplatformLessonsLearnt = new Dictionary<string, Dictionary<Moves, double>>();
            _intraplatformPlayedStates = new Dictionary<int, List<IntraPlatformPlayedGameStateInfo>>();
            _interplatformPlayedStates = new Dictionary<int, List<InterPlatformPlayedGameStateInfo>>();
        }

        public void setSave(bool option) {
            _toSave = option;
        }

        public static bool checkIfLearning() {
            return File.Exists(@"..\..\..\AgentsDLL\learning.file");
        }

        public void AddState(Moves move) {
            Platform agentPlatform = _model.AgentPlatform;
            if (agentPlatform != null) {
                if (agentPlatform.PercentageCollectiblesCaught < 100) {
                    if (!_intraplatformPlayedStates.ContainsKey(agentPlatform.ID)) {
                        _intraplatformPlayedStates.Add(agentPlatform.ID, new List<IntraPlatformPlayedGameStateInfo>());
                    }
                    _intraplatformPlayedStates[agentPlatform.ID].Add(new IntraPlatformPlayedGameStateInfo(_model.GetGameState(), move, _model.AgentPlatform.ID, DateTime.Now));
                } else {
                    GraphLink closestLink = GetLink();
                    if (closestLink != null) {
                        interPlatform = true;
                        if (!_interplatformPlayedStates.ContainsKey(closestLink.ID)) {
                            _interplatformPlayedStates.Add(closestLink.ID, new List<InterPlatformPlayedGameStateInfo>());
                        }
                        _interplatformPlayedStates[closestLink.ID].Add(new InterPlatformPlayedGameStateInfo(_model.GetGameState(), move, closestLink, DateTime.Now));
                    }
                }
            }
        }

        protected abstract GraphLink GetLink();



        public void InitializeLearning() {
            if (File.Exists(Path.Combine(Path.Combine(path, _learningFolder), "IntraPlatformLearning.csv"))) {
                FileStream fileStream = new FileStream(Path.Combine(Path.Combine(path, _learningFolder),"IntraPlatformLearning.csv"), FileMode.Open);
                createLearningFromFile(fileStream, ref _intraplatformLessonsLearnt);
                fileStream.Close();
            } else {
                createEmptyLearning(ref _intraplatformLessonsLearnt);
            }
            if (File.Exists(Path.Combine(Path.Combine(path, _learningFolder), "InterPlatformLearning.csv"))) {
                FileStream fileStream = new FileStream(Path.Combine(Path.Combine(path, _learningFolder), "InterPlatformLearning.csv"), FileMode.Open);
                createLearningFromFile(fileStream, ref _interplatformLessonsLearnt);
                fileStream.Close();
            } else {
                createEmptyLearning(ref _interplatformLessonsLearnt);
            }

        }

        private void createLearningFromFile(FileStream fileStream, ref Dictionary<string, Dictionary<Moves, double>> lessons) {
            lessons = new Dictionary<string, Dictionary<Moves, double>>();
            StreamReader sr = new StreamReader(fileStream);
            string line;
            while ((line = sr.ReadLine()) != null) {
                string[] lineSplit = line.Split(',');
                string stateId = lineSplit[0];
                lessons.Add(stateId, new Dictionary<Moves, double>());

                addStateMovementValue(lineSplit, stateId, ref lessons);
            }
        }

        private void createEmptyLearning(ref Dictionary<string, Dictionary<Moves, double>> lessons) {
            lessons = new Dictionary<string, Dictionary<Moves, double>>();
        }

        public void EndGame(float knownStatesRatio) {
            if (_toSave) {
                FileStream fileStream = new FileStream(Path.Combine(Path.Combine(path, _learningFolder), "Ratios.csv"), FileMode.Append);
                StreamWriter sw = new StreamWriter(fileStream);
                sw.WriteLine(knownStatesRatio);
                sw.Close();
                fileStream.Close();
                fileStream = new FileStream(Path.Combine(Path.Combine(path, _learningFolder), "IntraPlatformLearning.csv"), FileMode.Create);
                DumpLearning(fileStream, _intraplatformPlayedStates);
                fileStream.Close();
                if (interPlatform) {
                    fileStream = new FileStream(Path.Combine(Path.Combine(path, _learningFolder), "InterPlatformLearning.csv"), FileMode.Create);
                    DumpLearning(fileStream, _interplatformPlayedStates);
                    fileStream.Close();

                }
            }
        }

        private void DumpLearning(FileStream fileStream, Dictionary<int, List<InterPlatformPlayedGameStateInfo>> playedStates) {
            foreach (int i in _interplatformPlayedStates.Keys) {
                foreach (InterPlatformPlayedGameStateInfo state in playedStates[i]) {
                    UpdateLearning(state, _model.GetGraph().GetGraphLink(i).EndTime);
                }
            }

            StreamWriter sw = new StreamWriter(fileStream);
            foreach (string state in _interplatformLessonsLearnt.Keys) {
                string s = state.ToString();
                Dictionary<Moves, double> stateLesson = _interplatformLessonsLearnt[state];

                s = writeMovements(s, stateLesson);

                sw.WriteLine(s);
            }
            sw.Close();
        }

        private double CalculateCollectibleDistanceValue(InterPlatformPlayedGameStateInfo state) {
            double agentXPosition = state.State.xPos;
            double agentYPosition = state.State.yPos;

            switch (state.Move) {
                case Moves.ROLL_LEFT:
                    agentXPosition -= 2 * CircleWorldModel.DEFAULT_RADIUS;
                    break;
                case Moves.ROLL_RIGHT:
                    agentXPosition += 2 * CircleWorldModel.DEFAULT_RADIUS;
                    break;
                case Moves.JUMP:
                    agentYPosition -= 2 * CircleWorldModel.DEFAULT_RADIUS;
                    break;
                default:
                    break;
            }

            return 10000 * 1 / Utils.Utils.CalculateL2(agentXPosition, agentYPosition, state.Link.FromXPos, state.Link.FromNode.Platform.Top);
        }


        private void UpdateLearning(IntraPlatformPlayedGameStateInfo state) {


            Platform p = _model.GetPlatform(state.PlatformId);



            double stateValue = p.NumberCollectiblesCaught * 1000;

            stateValue += state.State.GetNumberCollectiblesCaught() * 1000;

            stateValue += CalculateCollectibleDistanceValue(state);

            stateValue += ((_model.TimeLimit / _model.TimeElapsed) - 1) * 1000;


            if (stateValue > 0) {
                if (_intraplatformLessonsLearnt.ContainsKey(state.State.GetStateId())) {
                    if (_intraplatformLessonsLearnt[state.State.GetStateId()].ContainsKey(state.Move)) {
                        _intraplatformLessonsLearnt[state.State.GetStateId()][state.Move] = 0.99 * _intraplatformLessonsLearnt[state.State.GetStateId()][state.Move] + 0.01 * stateValue;
                    } else {
                        _intraplatformLessonsLearnt[state.State.GetStateId()].Add(state.Move, stateValue);
                    }
                } else {
                    _intraplatformLessonsLearnt.Add(state.State.GetStateId(), new Dictionary<Moves, double>());
                    _intraplatformLessonsLearnt[state.State.GetStateId()].Add(state.Move, stateValue);
                }
            }
        }

        private double CalculateCollectibleDistanceValue(IntraPlatformPlayedGameStateInfo state) {
            double agentXPosition = state.State.xPos;
            double agentYPosition = state.State.yPos;

            Collectible c = state.State.GetClosestCollectible();

            switch (state.Move) {
                case Moves.ROLL_LEFT:
                    agentXPosition -= 2 * CircleWorldModel.DEFAULT_RADIUS;
                    break;
                case Moves.ROLL_RIGHT:
                    agentXPosition += 2 * CircleWorldModel.DEFAULT_RADIUS;
                    break;
                case Moves.JUMP:
                    agentYPosition -= 2 * CircleWorldModel.DEFAULT_RADIUS;
                    break;
                default:
                    break;
            }

            return 10000 * 1 / Utils.Utils.CalculateL2(agentXPosition, agentYPosition, c.xPos, c.yPos);

        }

        private void DumpLearning(FileStream fileStream, Dictionary<int, List<IntraPlatformPlayedGameStateInfo>> playedStates) {

            foreach (int i in playedStates.Keys) {
                foreach (IntraPlatformPlayedGameStateInfo state in playedStates[i]) {
                    UpdateLearning(state);
                }
            }

            StreamWriter sw = new StreamWriter(fileStream);
            foreach (string state in _intraplatformLessonsLearnt.Keys) {
                string s = state.ToString();
                Dictionary<Moves, double> stateLesson = _intraplatformLessonsLearnt[state];

                s = writeMovements(s, stateLesson);

                sw.WriteLine(s);
            }
            sw.Close();
        }

        protected abstract string writeMovements(string s, Dictionary<Moves, double> stateLesson);

        protected string addMovementInformation(string s, Dictionary<Moves, double> stateLesson, Moves move) {
            if (stateLesson.ContainsKey(move)) {
                s += "," + stateLesson[move];
            } else {
                s += "," + 0;
            }

            return s;
        }




        public abstract void addStateMovementValue(string[] lineSplit, string stateId, ref Dictionary<string, Dictionary<Moves, double>> lessons);

        internal bool ContainsState(string stateId) {
            if (_model.AgentPlatform != null && _model.AgentPlatform.PercentageCollectiblesCaught < 100) {
                return _intraplatformLessonsLearnt.ContainsKey(stateId);
            } else {
                return _interplatformLessonsLearnt.ContainsKey(stateId);
            }

        }

        internal IEnumerable<Moves> GetMovesForState(string stateId) {
            if (_model.AgentPlatform.PercentageCollectiblesCaught < 100) {
                return new List<Moves>(_intraplatformLessonsLearnt[stateId].Keys);
            } else if (_model.ClosestLink != null) {
                return new List<Moves>(_interplatformLessonsLearnt[stateId].Keys);
            } else {
                return new List<Moves>();
            }
        }

        internal double GetMoveValueInState(Moves move, string stateId) {
            if (_model.AgentPlatform.PercentageCollectiblesCaught < 100) {
                return _intraplatformLessonsLearnt[stateId][move];
            } else {
                return _interplatformLessonsLearnt[stateId][move];
            }

        }

        internal Dictionary<int, List<IntraPlatformPlayedGameStateInfo>> GetIntraPlatformsPlayedStates() {
            return _intraplatformPlayedStates;
        }

        internal Dictionary<int, List<InterPlatformPlayedGameStateInfo>> GetInterPlatformsPlayedStates() {
            return _interplatformPlayedStates;
        }

        internal void CloseLink(GraphLink link, DateTime endTime) {
            try {
                List<InterPlatformPlayedGameStateInfo> states = _interplatformPlayedStates[link.ID];
                _interplatformPlayedStates.Remove(link.ID);
                link.StartTime = states[0].PlayedTime;

                foreach (InterPlatformPlayedGameStateInfo state in states) {
                    UpdateLearning(state, endTime);
                }
            } catch (KeyNotFoundException ex) {
                //nothing this falls here when is jumping to other agent
            }
        }

        private void UpdateLearning(InterPlatformPlayedGameStateInfo state, DateTime endTime) {
            double stateValue = 1 + 100 * (1 - (endTime - state.Link.StartTime).TotalSeconds / _model.TimeLimit);

            if (stateValue > 1) {
                stateValue *= (state.PlayedTime - state.Link.StartTime).TotalMilliseconds;
            }

            if (stateValue > 0) {
                if (_interplatformLessonsLearnt.ContainsKey(state.State.GetStateId())) {
                    if (_interplatformLessonsLearnt[state.State.GetStateId()].ContainsKey(state.Move)) {
                        _interplatformLessonsLearnt[state.State.GetStateId()][state.Move] = 0.99 * _interplatformLessonsLearnt[state.State.GetStateId()][state.Move] + 0.01 * stateValue;
                    } else {
                        _interplatformLessonsLearnt[state.State.GetStateId()].Add(state.Move, stateValue);
                    }
                } else {
                    _interplatformLessonsLearnt.Add(state.State.GetStateId(), new Dictionary<Moves, double>());
                    _interplatformLessonsLearnt[state.State.GetStateId()].Add(state.Move, stateValue);
                }
            }
        }
    }
}
