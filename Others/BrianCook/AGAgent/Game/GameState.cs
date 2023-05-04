using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    public class GameState
    {
        public GameArea GameArea;
        public ObstacleGrid ObstacleGrid;
        public CircleStateMapper StateMapper;
        public CircleRepresentation Circle;
        public int[] RemainingCollectibles;
        public double ElapsedGameTime;
        public double ElapsedClockTime;

        public GameState Copy()
        {
            return new GameState()
            {
                GameArea = GameArea,
                ObstacleGrid = ObstacleGrid,
                StateMapper = StateMapper,
                Circle = Circle,
                RemainingCollectibles = RemainingCollectibles,
                ElapsedGameTime = ElapsedGameTime,
                ElapsedClockTime = ElapsedClockTime
            };
        }

        public override string ToString()
        {
            return $"[Circle X={Circle.X} Y={Circle.Y} XVel={Circle.VelocityX} YVel={Circle.VelocityY}]";
        }
    }
}
