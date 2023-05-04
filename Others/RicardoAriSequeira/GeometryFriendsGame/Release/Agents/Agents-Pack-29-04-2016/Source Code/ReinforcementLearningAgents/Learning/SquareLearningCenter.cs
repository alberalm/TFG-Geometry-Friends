using GeometryFriends.AI;
using System.Collections.Generic;
using System.Globalization;

namespace GeometryFriendsAgents.Learning {
    class SquareLearningCenter : LearningCenter {

        public SquareLearningCenter(SquareWorldModel model) : base("SquareLearning", model) { }

        public override void addStateMovementValue(string[] lineSplit, string stateId, ref Dictionary<string, Dictionary<Moves, double>> lessons) {
            if (!lineSplit[1].Equals("0")) {
                lessons[stateId].Add(Moves.MOVE_LEFT, double.Parse(lineSplit[1], CultureInfo.InvariantCulture));
            }

            if (!lineSplit[2].Equals("0")) {
                lessons[stateId].Add(Moves.MOVE_RIGHT, double.Parse(lineSplit[2], CultureInfo.InvariantCulture));
            }

            if (!lineSplit[3].Equals("0")) {
                lessons[stateId].Add(Moves.MORPH_UP, double.Parse(lineSplit[3], CultureInfo.InvariantCulture));
            }

            if (!lineSplit[4].Equals("0")) {
                lessons[stateId].Add(Moves.MORPH_DOWN, double.Parse(lineSplit[4], CultureInfo.InvariantCulture));
            }
            
        }

        protected override string writeMovements(string s, Dictionary<Moves, double> stateLesson) {           
            s = addMovementInformation(s, stateLesson, Moves.MOVE_LEFT);
            s = addMovementInformation(s, stateLesson, Moves.MOVE_RIGHT);
            s = addMovementInformation(s, stateLesson, Moves.MORPH_UP);
            s = addMovementInformation(s, stateLesson, Moves.MORPH_DOWN);
            return s;
        
        }

        protected override Model.GraphLink GetLink() {
            if (_model.ClosestLink != null) {
                return _model.ClosestLink;
            } else {
                return null;
            }
        }
    }
}
