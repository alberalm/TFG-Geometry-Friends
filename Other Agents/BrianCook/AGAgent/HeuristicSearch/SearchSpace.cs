using System.Collections.Generic;

namespace GeometryFriendsAgents
{
    public class SearchSpace<TState, TAction>
    {
        Dictionary<TState, SearchNode<TState, TAction>> nodes = new Dictionary<TState, SearchNode<TState, TAction>>();

        public SearchNode<TState, TAction> GetNode(TState state)
        {
            SearchNode<TState, TAction> node = null;
            nodes.TryGetValue(state, out node);
            return node;
        }
    }
}
