using System;

namespace GeometryFriendsAgents.Utils {
    class Utils {
        public static double CalculateL2(double xPos, double yPos, double p1, double p2) {
            return Math.Abs(Math.Pow(p1 - xPos, 2) + Math.Pow(p2 - yPos, 2));
        }
    }
}
