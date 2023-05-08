using System;

namespace GeometryFriendsAgents.Model {
    class GraphLink : IComparable {

        private int id;

        public int ID {
            get { return id; }
        }
        
        
        private GraphNode toNode;

        public GraphNode ToNode {
            get { return toNode; }
        }

        private GraphNode fromNode;

        public GraphNode FromNode {
            get { return fromNode; }
        }

        private float fromXPos;

        public float FromXPos {
            get { return fromXPos; }
        }

        private float deltaX;

        public float DeltaX {
            get { return deltaX; }
        }

        private float deltaY;

        public float DeltaY {
            get { return deltaY; }
        }

        public GraphLink(int id, GraphNode fromNode, float xPos, GraphNode toNode, float deltaX, float deltaY) {
            this.toNode = toNode;
            this.fromXPos = xPos;
            this.fromNode = fromNode;
            this.deltaY = deltaY;
            this.deltaX = deltaX;
            this.id = id;
        }

        internal GraphLink Clone() {
            return new GraphLink(this.id, new GraphNode(fromNode.Platform.Clone()), fromXPos, new GraphNode(toNode.Platform.Clone()), deltaX, deltaY);
        }

        public override string ToString() {
            return "[ToNode: " + ToNode.Platform.ID + " JumpPoint: " + fromXPos + "]" ;
        }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int CompareTo(object obj) {
            GraphLink other = ((GraphLink)obj);
            return this.id.CompareTo(other.id);
        }

        public override bool Equals(object obj) {
            GraphLink other = ((GraphLink)obj);
            return this.id == other.id;
        }
    }
}
