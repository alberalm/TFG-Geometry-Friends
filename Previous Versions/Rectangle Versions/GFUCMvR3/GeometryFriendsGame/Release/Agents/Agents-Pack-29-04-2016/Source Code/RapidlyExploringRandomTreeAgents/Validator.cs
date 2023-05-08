using System;

namespace GeometryFriendsAgents
{
    public abstract class Validator
    {

        abstract public Boolean validate(CircleNode newnode, CircleNode oldnode, InfoDomain domain, Graph graph);
        abstract public Boolean validate(RectangleNode newnode, RectangleNode oldnode, InfoDomain domain, Graph graph);

        abstract protected Boolean outsideOfMap(State newstate);

        abstract protected Boolean isVerticalMovement(State oldstate, State newstate);

        abstract protected Boolean thereIsWallBetweenH(State newstate, State oldstate, InfoDomain domain);

        abstract protected Boolean thereIsWallBetweenV(State newstate, State oldstate, InfoDomain domain);
    }
}
