using GeometryFriends.AI;
using System.Collections.Generic;
using System.Globalization;

namespace GeometryFriendsAgents.Learning {
    class CircleLearningCenter : LearningCenter {

        public CircleLearningCenter(CircleWorldModel model) : base("CircleLearning", model) { }

        public override void addStateMovementValue(string[] lineSplit, string stateId, ref Dictionary<string, Dictionary<Moves, double>> lessons) {
            if (!lineSplit[1].Equals("0")) {
                lessons[stateId].Add(Moves.ROLL_LEFT, double.Parse(lineSplit[1], CultureInfo.InvariantCulture));
            }

            if (!lineSplit[2].Equals("0")) {
                lessons[stateId].Add(Moves.ROLL_RIGHT, double.Parse(lineSplit[2], CultureInfo.InvariantCulture));
            }

            if (!lineSplit[3].Equals("0")) {
                lessons[stateId].Add(Moves.JUMP, double.Parse(lineSplit[3], CultureInfo.InvariantCulture));
            }
        }

        protected override string writeMovements(string s, Dictionary<Moves, double> stateLesson) {
            s = addMovementInformation(s, stateLesson, Moves.ROLL_LEFT);
            s = addMovementInformation(s, stateLesson, Moves.ROLL_RIGHT);
            s = addMovementInformation(s, stateLesson, Moves.JUMP);
            return s;
        }

        protected override Model.GraphLink GetLink() {
            return _model.ClosestLink;
        }
    }
}
