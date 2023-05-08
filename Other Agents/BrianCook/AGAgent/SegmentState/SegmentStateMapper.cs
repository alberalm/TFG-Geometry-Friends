using System.Drawing;

namespace GeometryFriendsAgents
{
    public class SegmentStateMapper
    {
        public int PositionResolution; 
        public RectangleF PositionRange;
        public int VelocityResolution; 
        public RectangleF VelocityRange;
        public ObstacleGrid obstacleGrid;
        public float radius;
        float xPositionRangeSize;
        float xVelocityRangeSize, yVelocityRangeSize;
        public bool EnableDebugging = false;

        public SegmentStateMapper(int positionResolution, RectangleF positionRange, int velocityResolution, RectangleF velocityRange, 
            ObstacleGrid grid, float radius)
        {
            PositionResolution = positionResolution;
            PositionRange = positionRange;
            VelocityResolution = velocityResolution;
            VelocityRange = velocityRange;
            obstacleGrid = grid;
            this.radius = radius;

            xPositionRangeSize = PositionRange.Width / PositionResolution;
            xVelocityRangeSize = VelocityRange.Width / VelocityResolution;
            yVelocityRangeSize = VelocityRange.Height / VelocityResolution;
        }

        public PointF GetLocation(ref SegmentState state)
        {
            float x = PositionRange.Left + (state.x + 0.5f) * xPositionRangeSize;
            float y = state.y;

            return new PointF(x, y);
        }

        public void GetLocationRange(ref SegmentState state, out PointF from, out PointF to)
        {
            float xFrom = PositionRange.Left + (state.x) * xPositionRangeSize;
            float xTo = PositionRange.Left + (state.x + 1f) * xPositionRangeSize;
            float y = state.y;

            from = new PointF(xFrom, y);
            to = new PointF(xTo, y);
        }

        public PointF GetVelocity(ref SegmentState state)
        {
            float xVelocity = VelocityRange.Left + (state.xVelocity + 0.5f) * xVelocityRangeSize;
            float yVelocity = 0;

            return new PointF(xVelocity, yVelocity);
        }

        public string GetString(ref SegmentState state)
        {
            var location = GetLocation(ref state);
            var velocity = GetVelocity(ref state);
            return string.Format("(pos:{0} vel:{1})", location, velocity);
        }

        public static SegmentStateMapper CreateWithDefaultResolution(Rectangle area, ObstacleGrid obstacleGrid, float circleRadius)
        {
            int positionResolution = 100;
            var positionRange = new RectangleF(area.X, area.Y, area.Width, area.Height);
            int velocityResolution = 50;
            var velocityRange = new RectangleF(-200, -450, 500, 1050);

            return new SegmentStateMapper(positionResolution, positionRange, velocityResolution, velocityRange,
                obstacleGrid, circleRadius);
        }
    }
}
