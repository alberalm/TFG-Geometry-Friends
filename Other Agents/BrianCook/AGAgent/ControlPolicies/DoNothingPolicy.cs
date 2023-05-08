using GeometryFriends.AI;

namespace GeometryFriendsAgents
{
    public class DoNothingPolicy : Policy<GameState>
    {
        public override void Initialize(GameState state, float timeStep)
        {
            base.Initialize(state,  timeStep);
        }

        public override Moves GetAction(GameState state)
        {
            Steps++;

            return Moves.NO_ACTION;
        }

        public override string ToString()
        {
            return $"DoNothing";
        }
    }
}
