using GeometryFriends.AI;
using GeometryFriends.AI.ActionSimulation;
using GeometryFriends.AI.Debug;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GeometryFriendsAgents
{
    public class Simulation
    {
        public List<Point> Trajectory = new List<Point>();
        public List<Moves> Actions = null;
        public List<CollectibleRepresentation> CaughtCollectibles = new List<CollectibleRepresentation>();
        public float NearestCorner = float.MaxValue;
        public float ElapsedTime = 0;
        static public float TimeStep = 0.04f;
        public bool RecordTrajectory { get; set; } = true;
        public bool RecordActions { get; set; } = false;

        public bool Simulate(GameState gameState, ActionSimulator simulator, Policy<GameState> policy, Func<Simulation, bool> isFinished)
        {
            if (!simulator.CharactersReady())
                return false;
            simulator.DebugInfo = false;

            Trajectory.Clear();
            var lastPoint = new Point((int)simulator.CirclePositionX, (int)simulator.CirclePositionY);
            if (RecordTrajectory)
                Trajectory.Add(lastPoint);
            if (RecordActions)
                Actions = new List<Moves>();
            ActionSimulator.CollectibleCaughtSimulatorHandler collectedAction = delegate (Object o, CollectibleRepresentation col) { CaughtCollectibles.Add(col); };
            simulator.SimulatorCollectedEvent += collectedAction;
            gameState.Circle = new CircleRepresentation(simulator.CirclePositionX, simulator.CirclePositionY, simulator.CircleVelocityX, simulator.CircleVelocityY, simulator.CircleVelocityRadius);

            policy.Initialize(gameState, TimeStep);
            ElapsedTime = 0;
            while (!isFinished(this))
            {
                if (policy.IsFinished(gameState))
                    break;
                var x = simulator.CirclePositionX;
                var y = simulator.CirclePositionY;
                var xVelocity = simulator.CircleVelocityX;
                var yVelocity = simulator.CircleVelocityY;
                var circleRadius = simulator.CircleVelocityRadius;
                gameState.Circle = new CircleRepresentation(x, y, xVelocity, yVelocity, circleRadius);
                var action = policy.GetAction(gameState);
                simulator.RemoveInstructions();
                simulator.AddInstruction(action, TimeStep + 0.00001f);
                simulator.SimulatorStep = 0.01f;
                var groundDistance = gameState.ObstacleGrid.GetDistanceToGround(x, y, circleRadius);
                if (groundDistance < GamePhysics.CIRCLE_RADIUS / 2)
                {
                    if (simulator.CircleVelocityY > 300)
                        simulator.SimulatorStep = 0.0005f;
                    else if (simulator.CircleVelocityY > 200)
                        simulator.SimulatorStep = 0.002f;
                }
                simulator.Update(TimeStep);
                ElapsedTime += TimeStep;

                var point = new Point((int)simulator.CirclePositionX, (int)simulator.CirclePositionY);
                var manhattanDistance = Math.Abs(point.X - lastPoint.X) + Math.Abs(point.Y - lastPoint.Y);
                if (manhattanDistance > 1)
                {
                    lastPoint = point;
                    if (RecordTrajectory)
                        Trajectory.Add(point);

                    if (policy.ShouldAvoidCorners(gameState))
                    {
                        var distance = gameState.ObstacleGrid.GetNearestCornerDistanceSquared(point);
                        NearestCorner = Math.Min(distance, NearestCorner);
                    }
                }
                if (RecordActions)
                    Actions.Add(action);
            }

            simulator.SimulatorCollectedEvent -= collectedAction;
            NearestCorner = (float)Math.Sqrt(NearestCorner);

            policy.NearestCorner = NearestCorner;

            return true;
        }

        public DebugInformation[] GetTrajectoryDebug()
        {
            return GetTrajectoryDebug(Trajectory);
        }

        public DebugInformation[] GetTrajectoryDebug(List<Point> points)
        {
            var debugInfo = new List<DebugInformation>();
            debugInfo.Add(DebugInformationFactory.CreateClearDebugInfo());

            float radius = 1f;
            foreach (var point in points)
                debugInfo.Add(DebugInformationFactory.CreateCircleDebugInfo(point, radius, GeometryFriends.XNAStub.Color.Red));

            return debugInfo.ToArray();
        }
    }
}
