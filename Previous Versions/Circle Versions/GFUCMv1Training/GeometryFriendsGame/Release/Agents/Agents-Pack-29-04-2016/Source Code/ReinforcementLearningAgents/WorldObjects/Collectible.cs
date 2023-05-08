using System;

namespace GeometryFriendsAgents.WorldObjects{
    class Collectible : WorldObject, IComparable {

        public Collectible(float xPos, float yPos) {
            _xPos = (int)xPos;
            _yPos = (int)yPos;
        }

        public double Distance { get; set; }

        public int CompareTo(object obj) {
            Collectible col = (Collectible)obj;
            if (Distance < col.Distance) {
                return -1;
            } else if (Distance > col.Distance) {
                return 1;
            } else {
                return 0;
            }
        }
    }
}
