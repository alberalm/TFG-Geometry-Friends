using System;
using System.Collections.Generic;
using System.Linq;
using GeometryFriends.AI.ActionSimulation;

namespace GeometryFriendsAgents
{
    [Serializable]
    public class SegmentInfo
    {
        public int ExplorationAttempts { get; set; }
        public List<int> collectibles = new List<int>();
        public List<int> segmentCollectibles = new List<int>();

        public bool SearchedSegmentCollectibles { get; set; }
        public List<int> failedCollectibles = new List<int>();

        public Dictionary<Segment, List<SegmentTransition>> connections = new Dictionary<Segment, List<SegmentTransition>>();
        public Dictionary<Segment, int> explored = new Dictionary<Segment, int>();
        public List<SegmentFailure> failures = new List<SegmentFailure>();

        public Dictionary<Segment, List<SegmentPotentialJumpTransition>> potentialJumpTransitions = new Dictionary<Segment, List<SegmentPotentialJumpTransition>>();
        public Dictionary<Segment, List<SegmentPotentialRollTransition>> potentialRollTransitions = new Dictionary<Segment, List<SegmentPotentialRollTransition>>();
        public Dictionary<int, List<SegmentPotentialJumpCollect>> potentialJumpCollects = new Dictionary<int, List<SegmentPotentialJumpCollect>>();
        public Dictionary<int, List<SegmentPotentialRollCollect>> potentialRollCollects = new Dictionary<int, List<SegmentPotentialRollCollect>>();
        public List<SegmentExploringJump> exploringJumps = new List<SegmentExploringJump>();
        public ActionSimulator StoppedState { get; set; }

        public GameState StoppedGameState { get; set; }
        public int TotalOutgoingTransitions
        {
            get
            {
                int count = 0;
                foreach (var entry in connections)
                    count += entry.Value.Count;
                return count;
            } 
        }
        public int TotalAccessibleCollectibles
        {
            get
            {
                return collectibles.Count + segmentCollectibles.Count + failedCollectibles.Count;
            }
        }
        public void Clear()
        {
            collectibles.Clear();
            foreach (var segment in connections.Keys.ToArray())
                connections[segment].Clear();
            foreach (var segment in explored.Keys.ToArray())
                explored[segment] = 0;
            failures.Clear();
        }
        public void AddCollectibles(int[] collectibles)
        {
            if (collectibles != null)
                foreach (var collectible in collectibles)
                    if (!this.collectibles.Contains(collectible))
                        this.collectibles.Add(collectible);
        }
    }
}
