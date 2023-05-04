using GeometryFriends;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Perceptions.Information;

namespace GeometryFriendsAgents
{
    [Serializable]
    public class SegmentModel 
    {
        public Dictionary<Segment, SegmentInfo> modelInfo = new Dictionary<Segment, SegmentInfo>();
        public List<CollectibleRepresentation> Collectibles = new List<CollectibleRepresentation>();
        public int NumCollectibles { get { return Collectibles.Count; } }
        static public int[] NoCollectibles = new int[0];
        Random random = new Random();
        ObstacleGrid obstacleGrid;
        static public bool IsDebug = false;

        public SegmentModel()
        {
        }

        public void AddTransition(Segment fromSegment, SegmentTransition transition, Segment toSegment)
        {
            try
            {
                var info = modelInfo[fromSegment];
                info.connections[toSegment].Add(transition);
                info.AddCollectibles(transition.collectibles);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void AddFailure(Segment fromSegment, SegmentFailure failure)
        {
            try
            {
                if (failure.collectibles != null && failure.collectibles.Length > 0)
                {
                    var info = modelInfo[fromSegment];
                    info.failures.Add(failure);
                    info.AddCollectibles(failure.collectibles);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public bool IsExplored(Segment fromSegment, Segment toSegment)
        {
            return modelInfo[fromSegment].explored[toSegment] != 0;
        }

        public int GetExplored(Segment fromSegment, Segment toSegment)
        {
            return modelInfo[fromSegment].explored[toSegment];
        }

        public void SetExplored(Segment fromSegment, Segment toSegment, int value)
        {
            modelInfo[fromSegment].explored[toSegment] = value;
        }

        public void PredictAll(SegmentState state, int action, List<TransitionResult<SegmentState>> successors)
        {
            successors.Clear();
        }

        public List<Segment> GetAllSegments()
        {
            return modelInfo.Keys.OrderBy(s => -s.from.Y * 1500 + s.from.X).ToList();
        }

        public Segment GetSegment(float x, float y)
        {
            float yDelta = GamePhysics.CIRCLE_RADIUS * 2;
            float xEpsilon = 5f; 
            foreach (var segment in modelInfo.Keys)
                if (segment.to.X >= x - xEpsilon && segment.from.X <= x + xEpsilon && Math.Abs(segment.from.Y - y) <= yDelta)
                    return segment;

            return null;
        }

        public SegmentState GetSegmentState(CircleState state, int[] remainingCollectibles)
        {
            return GetSegmentState(state.x, state.y, state.xVelocity, remainingCollectibles);
        }

        public SegmentState GetSegmentState(float x, float y, float xVelocity, int[] remainingCollectibles)
        {
            return new SegmentState(GetSegment(x, y), x, xVelocity, remainingCollectibles);
        }

        public Segment GetSegmentBeneath(float x, float y)
        {
            y -= GamePhysics.MAX_COLLISION_DEPTH;

            float minDistance = float.MaxValue;
            Segment nearestSegment = null;
            foreach (var segment in modelInfo.Keys)
            {
                if (segment.from.X <= x + GamePhysics.MAX_COLLISION_DEPTH && segment.to.X >= x - GamePhysics.MAX_COLLISION_DEPTH && segment.from.Y >= y)
                {
                    var distance = segment.from.Y - y;
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestSegment = segment;
                    }
                }
            }

            if (nearestSegment == null)
            {
                DebugOutput($"WARNING: No segment beneath {x},{y}. Checked {modelInfo.Keys.Count} segments");
                foreach (var segment in modelInfo.Keys)
                    DebugOutput($" Searched segment {segment}");
            }

            return nearestSegment;
        }

        public SegmentInfo GetInfo(Segment segment)
        {
            return modelInfo[segment];
        }

        void DebugOutput(string message)
        {
            if (IsDebug)
            {
                Debug.WriteLine(message);
                Log.LogInformation(message);
            }
        }

        public DebugInformation[] GetDebugInfo()
        {
            var debugInfo = new List<DebugInformation>();
            debugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());

            foreach (var infoEntry in modelInfo)
            {
                var fromSegment = infoEntry.Key;
                var segmentInfo = infoEntry.Value;
                foreach (var connEntry in segmentInfo.connections)
                {
                    var toSegment = connEntry.Key;
                    var transitions = connEntry.Value;
                    int count = transitions.Count();
                    if (count > 0)
                        GetDebugInfo(transitions.ElementAt(random.Next(count)), debugInfo);
                }
                if (segmentInfo.failures.Count > 0)
                {
                    for (int collectible = 0; collectible < Collectibles.Count; collectible++)
                    {
                        var failures = segmentInfo.failures.Where(t => t.collectibles.Contains(collectible));
                        int count = failures.Count();
                        if (count > 0)
                            GetDebugInfo(failures.ElementAt(random.Next(count)), debugInfo);
                    }
                }
            }

            return debugInfo.ToArray();
        }

        void GetDebugInfo(SegmentTransitionAttempt transition, List<DebugInformation> debugInfo)
        {
            var color = transition.collectibles.Length > 0 ? GeometryFriends.XNAStub.Color.Green : GeometryFriends.XNAStub.Color.Red;
            var lastPoint = new Point();
            for (int i = 0; i < transition.trajectory.Length; i++)
                if (i == 0)
                    lastPoint = transition.trajectory[i];
                else
                {
                    var point = transition.trajectory[i];
                    if (Math.Abs(point.X - lastPoint.X) + Math.Abs(point.Y - lastPoint.Y) > 5)
                    {
                        debugInfo.Add(DebugInformationFactory.CreateLineDebugInfo(lastPoint, point, color));
                        lastPoint = point;
                    }
                }
        }
        public static SegmentModel Build(GameArea gameArea, ObstacleGrid obstacleGrid)
        {
            var circleRadius = gameArea.CircleRadius;
            var segments = new List<Segment>();
            var deltaX = 1f;
            foreach (var obstacle in obstacleGrid.obstacles)
            {
                float y = obstacle.Top - circleRadius;
                if (y < circleRadius)
                    continue;

                float startX = -1, endX = -1;
                bool isSegmentValid = false;
                for (float x = obstacle.Left; x <= obstacle.Right; x += deltaX)
                {
                    float minDistance;
                    obstacleGrid.GetDistanceToNearestObstacle(new PointF(x, y), circleRadius, out minDistance);
                    var hitObstacle = (minDistance < 0);
                    if (hitObstacle)
                    {
                        if (isSegmentValid && endX - startX > 0)
                            segments.Add(new Segment(new PointF(startX, y), new PointF(endX, y)));
                        isSegmentValid = false;
                    }
                    else
                    {
                        if (!isSegmentValid)
                        {
                            startX = x;
                            isSegmentValid = true;
                        }
                        endX = x;
                    }
                }
                if (isSegmentValid && endX - startX > 0)
                    segments.Add(new Segment(new PointF(startX, y), new PointF(endX, y)));
            }
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                var a = segments[i];
                var b = segments.FirstOrDefault(s => a.y == s.y && ((s.from.X >= a.from.X && s.from.X <= a.to.X + 1) || (s.to.X >= a.from.X - 1 && s.to.X <= a.to.X)));
                if (b != null && b != a)
                {
                    b.from.X = Math.Min(a.from.X, b.from.X);
                    b.to.X = Math.Max(a.to.X, b.to.X);
                    segments.RemoveAt(i);
                }
            }

            if (IsDebug)
            {
                Logger.Write($"IDENTIFIED SEGMENTS:");
                foreach (var segment in segments)
                    Logger.Write($"  {segment}");
            }
            var model = new SegmentModel() { obstacleGrid = obstacleGrid };
            foreach (var segment in segments)
            {
                var info = new SegmentInfo();
                model.modelInfo[segment] = info;
                foreach (var toSegment in segments)
                {
                    info.explored[toSegment] = 0;
                    info.connections[toSegment] = new List<SegmentTransition>();
                }
            }
            model.Collectibles.Clear();
            model.Collectibles.AddRange(gameArea.Collectibles);

            return model;
        }

        public void InitializePotentialTransitions(int xResolution, int xVelocityResolution)
        {
            var allSegments = GetAllSegments();
            foreach (var fromSegment in allSegments)
            {
                var info = modelInfo[fromSegment];
                foreach (var toSegment in allSegments)
                {
                    if (toSegment == fromSegment)
                        continue;

                    if (IsDebug) Logger.Write($"POTENTIAL JUMP TRANSITIONS FROM {fromSegment} TO {toSegment}:");

                    if (!info.potentialJumpTransitions.ContainsKey(toSegment))
                        info.potentialJumpTransitions[toSegment] = new List<SegmentPotentialJumpTransition>();
                    var potentialJumpTransitions = info.potentialJumpTransitions[toSegment];
                    potentialJumpTransitions.Clear();

                    var maxVelocity = (int)GamePhysics.GetAchievableXVelocity(fromSegment.width);
                    for (int xVelocity = -maxVelocity; xVelocity <= maxVelocity; xVelocity += xVelocityResolution)
                    {
                        float xMin, xMax;
                        if (GamePhysics.GetPlatformJumpLocations(xVelocity, fromSegment.from.X, fromSegment.to.X, fromSegment.from.Y, toSegment.from.X, toSegment.to.X, toSegment.from.Y, out xMin, out xMax))
                        {
                            var xValues = GetRangeValues(xMin, xMax, xResolution);
                            foreach (var x in xValues)
                            {
                                if (!IsAchievableVelocity(fromSegment, x, xVelocity))
                                    continue;
                                var potentialTransition = new SegmentPotentialJumpTransition(toSegment, x, xVelocity);
                                potentialTransition.SetCollisionPoint(GamePhysics.GetFirstCollisionPoint(x, fromSegment.from.Y, xVelocity, obstacleGrid));
                                potentialJumpTransitions.Add(potentialTransition);
                            }
                        }
                    }

                    if (IsDebug)
                        foreach (var transition in potentialJumpTransitions)
                            Logger.Write($"  {transition}");

                    if (IsDebug) Logger.Write($"POTENTIAL ROLL TRANSITIONS FROM {fromSegment} TO {toSegment}:");

                    if (!info.potentialRollTransitions.ContainsKey(toSegment))
                        info.potentialRollTransitions[toSegment] = new List<SegmentPotentialRollTransition>();
                    var potentialRollTransitions = info.potentialRollTransitions[toSegment];
                    potentialRollTransitions.Clear();

                    {
                        float fromXvel, toXvel;
                        if (GamePhysics.GetPlatformRollVelocities(true, fromSegment.from.X, fromSegment.to.X, fromSegment.from.Y, toSegment.from.X, toSegment.to.X, toSegment.from.Y, out fromXvel, out toXvel))
                        {
                            fromXvel = Math.Min(maxVelocity, fromXvel);
                            toXvel = Math.Min(maxVelocity, toXvel);
                            if (fromXvel < toXvel)
                            {
                                var velocities = GetRangeValues(fromXvel, toXvel, xVelocityResolution);
                                foreach (var xVel in velocities)
                                {
                                    if (!IsAchievableVelocity(fromSegment, fromSegment.to.X, xVel))
                                        continue;
                                    potentialRollTransitions.Add(new SegmentPotentialRollTransition(toSegment, xVel) { IsUpward = toSegment.y < fromSegment.y });
                                }
                            }
                        }
                        if (GamePhysics.GetPlatformRollVelocities(false, fromSegment.from.X, fromSegment.to.X, fromSegment.from.Y, toSegment.from.X, toSegment.to.X, toSegment.from.Y, out fromXvel, out toXvel))
                        {
                            fromXvel = Math.Max(-maxVelocity, fromXvel);
                            toXvel = Math.Max(-maxVelocity, toXvel);
                            if (fromXvel > toXvel)
                            {
                                var velocities = GetRangeValues(fromXvel, toXvel, xVelocityResolution);
                                foreach (var xVel in velocities)
                                {
                                    if (!IsAchievableVelocity(fromSegment, fromSegment.from.X, xVel))
                                        continue;
                                    potentialRollTransitions.Add(new SegmentPotentialRollTransition(toSegment, xVel) { IsUpward = toSegment.y < fromSegment.y });
                                }
                            }
                        }
                    }

                    foreach (var transition in potentialRollTransitions)
                        if (IsDebug) Logger.Write($"  {transition}");
                }

                for (int collectible = 0; collectible < NumCollectibles; collectible++)
                {
                    var goal = Collectibles[collectible];

                    if (!info.potentialJumpCollects.ContainsKey(collectible))
                        info.potentialJumpCollects[collectible] = new List<SegmentPotentialJumpCollect>();
                    var potentialJumpCollects = info.potentialJumpCollects[collectible];
                    potentialJumpCollects.Clear();

                    if (IsDebug) Logger.Write($"POTENTIAL JUMP COLLECTS FROM {fromSegment} TO collectible {collectible} at {goal}:");

                    var maxVelocity = (int)GamePhysics.GetAchievableXVelocity(fromSegment.width);
                    for (int xVelocity = 0; xVelocity <= maxVelocity; xVelocity += xVelocityResolution)
                    {
                        float xMin1, xMax1;
                        float xMin2, xMax2;
                        if (GamePhysics.GetGoalJumpLocations(xVelocity, fromSegment.from.X, fromSegment.to.X, fromSegment.from.Y, goal.X, goal.Y, out xMin1, out xMax1, out xMin2, out xMax2))
                        {
                            if (xMax1 >= xMin1 && xMin1 > 0 && xMax1 > 0)
                            {
                                var xValues = GetRangeValues(xMin1, xMax1, xResolution);
                                foreach (var x in xValues)
                                {
                                    if (!IsAchievableVelocity(fromSegment, x, xVelocity))
                                        continue;
                                    potentialJumpCollects.Add(new SegmentPotentialJumpCollect(collectible, x, xVelocity));
                                }
                            }
                            if (xMax2 >= xMin2 && xMin2 > 0 && xMax2 > 0)
                            {
                                var xValues = GetRangeValues(xMin2, xMax2, xResolution);
                                foreach (var x in xValues)
                                {
                                    if (!IsAchievableVelocity(fromSegment, x, xVelocity))
                                        continue;
                                    potentialJumpCollects.Add(new SegmentPotentialJumpCollect(collectible, x, xVelocity));
                                }
                            }
                        }
                        if (xVelocity > 0 && GamePhysics.GetGoalJumpLocations(-xVelocity, fromSegment.from.X, fromSegment.to.X, fromSegment.from.Y, goal.X, goal.Y, out xMin1, out xMax1, out xMin2, out xMax2))
                        {
                            if (xMax1 >= xMin1 && xMin1 > 0 && xMax1 > 0)
                            {
                                var xValues = GetRangeValues(xMin1, xMax1, xResolution);
                                foreach (var x in xValues)
                                {
                                    if (!IsAchievableVelocity(fromSegment, x, xVelocity))
                                        continue;
                                    potentialJumpCollects.Add(new SegmentPotentialJumpCollect(collectible, x, -xVelocity));
                                }
                            }
                            if (xMax2 >= xMin2 && xMin2 > 0 && xMax2 > 0)
                            {
                                var xValues = GetRangeValues(xMin2, xMax2, xResolution);
                                foreach (var x in xValues)
                                {
                                    if (!IsAchievableVelocity(fromSegment, x, xVelocity))
                                        continue;
                                    potentialJumpCollects.Add(new SegmentPotentialJumpCollect(collectible, x, -xVelocity));
                                }
                            }
                        }
                    }

                    if (potentialJumpCollects.Count > 0)
                    {
                        foreach (var pot in potentialJumpCollects)
                            if (IsDebug) Logger.Write($"   X: {pot.x} XVEL: {pot.xVelocity}");
                    }

                    if (!info.potentialRollCollects.ContainsKey(collectible))
                        info.potentialRollCollects[collectible] = new List<SegmentPotentialRollCollect>();
                    var potentialRollCollects = info.potentialRollCollects[collectible];
                    potentialRollCollects.Clear();

                    {
                        float fromXvel, toXvel;
                        if (GamePhysics.GetGoalRollVelocities(fromSegment.from.X, fromSegment.to.X, fromSegment.from.Y, goal.X, goal.Y, out fromXvel, out toXvel))
                        {
                            var velocities = GetRangeValues(fromXvel, toXvel, xVelocityResolution);
                            foreach (var xVel in velocities)
                                potentialRollCollects.Add(new SegmentPotentialRollCollect(collectible, xVel));
                        }
                    }

                    if (potentialRollCollects.Count > 0)
                    {
                        if (IsDebug) Logger.Write($"POTENTIAL ROLL COLLECTS FROM {fromSegment} TO collectible {goal}:");
                        foreach (var pot in potentialRollCollects)
                            if (IsDebug) Logger.Write($"   XVEL: {pot.xVel}");
                    }
                }
                {
                    var maxVelocity = (int)GamePhysics.GetAchievableXVelocity(fromSegment.width);
                    for (int xVelocity = -maxVelocity; xVelocity <= maxVelocity; xVelocity += xVelocityResolution / 2)
                    {
                        var xValues = GetRangeValues(fromSegment.from.X, fromSegment.to.X, xResolution / 2);
                        foreach (var x in xValues)
                        {
                            if (!IsAchievableVelocity(fromSegment, x, xVelocity))
                                continue;
                            {
                                var exploringJump = new SegmentExploringJump(x, xVelocity);
                                info.exploringJumps.Add(exploringJump);
                            }
                        }
                    }
                }
            }
        }

        List<float> GetRangeValues(float from, float to, float resolution)
        {
            float midpoint = (from + to) / 2;
            int increments = (int)(Math.Abs(from - midpoint) / resolution);
            var values = new List<float>(1 + 2 * increments);
            values.Add(midpoint);
            for (int i = 0; i < increments; i++)
            {
                values.Add(midpoint + (i + 1) * resolution);
                values.Add(midpoint - (i + 1) * resolution);
            }
            return values;
        }

        bool IsAchievableVelocity(Segment segment, float x, float xVelocity)
        {
            if (xVelocity > 0)
            {
                var maxVelocity = GamePhysics.GetAchievableXVelocity(x - segment.from.X);
                return xVelocity <= maxVelocity;
            }
            if (xVelocity < 0)
            {
                var maxVelocity = GamePhysics.GetAchievableXVelocity(x - segment.to.X);
                return xVelocity >= maxVelocity;
            }
            return true;
        }
    }
}
