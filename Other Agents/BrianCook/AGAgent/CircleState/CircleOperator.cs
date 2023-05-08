using GeometryFriends.AI;
using System;

namespace GeometryFriendsAgents
{
    [Serializable]
    public abstract class CircleOperator : Operator, IEquatable<CircleOperator>
    {
        public Moves Move { get; protected set; }

        static public CircleOperator None = new CircleOperatorNone();
        static public CircleOperator Left = new CircleOperatorLeft();
        static public CircleOperator Right = new CircleOperatorRight();
        static public CircleOperator Jump = new CircleOperatorJump();

        public override string ToString()
        {
            return Move.ToString();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CircleOperator);
        }

        public bool Equals(CircleOperator other)
        {
            if (ReferenceEquals(other, null))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return GetType() == other.GetType();
        }

        public override int GetHashCode()
        {
            return (int)Move;
        }

        public static bool operator ==(CircleOperator lhs, CircleOperator rhs)
        {
            if (ReferenceEquals(lhs, null))
            {
                if (ReferenceEquals(rhs, null))
                    return true;
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator !=(CircleOperator lhs, CircleOperator rhs)
        {
            return !(lhs == rhs);
        }
    }

    [Serializable]
    public class CircleOperatorNone : CircleOperator
    {
        public CircleOperatorNone()
        {
            Move = Moves.NO_ACTION;
        }
    }

    [Serializable]
    public class CircleOperatorLeft : CircleOperator
    {
        public CircleOperatorLeft()
        {
            Move = Moves.ROLL_LEFT;
        }
    }

    [Serializable]
    public class CircleOperatorRight : CircleOperator
    {
        public CircleOperatorRight()
        {
            Move = Moves.ROLL_RIGHT;
        }
    }

    [Serializable]
    public class CircleOperatorJump : CircleOperator
    {
        public CircleOperatorJump()
        {
            Move = Moves.JUMP;
        }
    }
}
