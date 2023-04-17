using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace GeometryFriendsAgents
{
    public class Graph
    {
        public class Diamond
        {
            public int id;
            public List<Tuple<int, string>> isAbovePlatform;

            public Diamond(int id)
            {
                this.id = id;
                this.isAbovePlatform = new List<Tuple<int, string>>();
            }
        }

        public class Node
        {
            public List<MoveInformation> plan_circle;
            public List<MoveInformation> plan_rectangle;
            public List<bool> caught;
            public int numCaught;
            public bool is_risky;

            public Node(List<MoveInformation> plan_circle, List<MoveInformation> plan_rectangle, List<bool> caught, int numCaught, bool is_risky)
            {
                this.plan_circle = plan_circle;
                this.plan_rectangle = plan_rectangle;
                this.caught = caught;
                this.numCaught = numCaught;
                this.is_risky = is_risky;
            }

            public int Value()
            {
                int v = 0;
                int pot = 1;
                foreach (bool b in caught)
                {
                    if (b)
                    {
                        v += pot;
                    }
                    pot *= 2;
                }
                if (is_risky)
                {
                    return -v;
                }
                else
                {
                    return v;
                }
            }
        }

        public List<Diamond> collectibles;
        public CollectibleRepresentation[] initial_collectibles;

        public List<Platform> circle_platforms;
        public List<Platform> rectangle_platforms;
        public Dictionary<int, int> circle_to_rectangle;

        public Stopwatch sw;
        public bool planIsComplete;

        public Node best_sol;

        public Graph(List<Platform> circle_platforms, List<Platform> rectangle_platforms,
            Dictionary<int, int> circle_to_rectangle, CollectibleRepresentation[] collectibles)
        {
            sw = new Stopwatch();
            this.circle_platforms = circle_platforms;
            this.rectangle_platforms = rectangle_platforms;
            this.initial_collectibles = collectibles;
            this.circle_to_rectangle = circle_to_rectangle;
            this.collectibles = new List<Diamond>();
            for(int i = 0; i < collectibles.Length; i++)
            {
                this.collectibles.Add(new Diamond(i));
            }
            // Add every possible move to the directed graph
            for (int i = 0; i < this.circle_platforms.Count(); i++)
            {
                foreach(int d in this.circle_platforms[i].ReachableCollectiblesLandingInThisPlatformWithoutCooperation())
                {
                    this.collectibles[d].isAbovePlatform.Add(new Tuple<int, string>(i, "c"));
                }
                foreach (int d in this.circle_platforms[i].ReachableCollectiblesLandingInThisPlatformWithCooperation())
                {
                    this.collectibles[d].isAbovePlatform.Add(new Tuple<int, string>(i, "cr"));
                }
            }
            for (int i = 0; i < this.rectangle_platforms.Count(); i++)
            {
                foreach (int d in this.rectangle_platforms[i].ReachableCollectiblesLandingInThisPlatformWithoutCooperation())
                {
                    this.collectibles[d].isAbovePlatform.Add(new Tuple<int, string>(i, "r"));
                }
                // TODO: with cooperation
            }
        }

        public int GetDiamondID(CollectibleRepresentation c)
        {
            for(int i = 0; i < initial_collectibles.Length; i++)
            {
                CollectibleRepresentation other = initial_collectibles[i];
                if(c.X == other.X && c.Y == other.Y)
                {
                    return i;
                }
            }
            return -1;
        }

        public List<MoveInformation> GetCirclePlan()
        {
            return best_sol.plan_circle;
        }

        public List<MoveInformation> GetRectanglePlan()
        {
            return best_sol.plan_rectangle;
        }

        public void SearchAlgorithm(int src_circle, int src_rectangle, CollectibleRepresentation[] uncaught/*, MoveInformation previous_move*/)
        {
            planIsComplete = false;
            Dictionary<int, int> diamonds = new Dictionary<int, int>();
            List<Diamond> newList = new List<Diamond>();
            foreach (CollectibleRepresentation c in uncaught)
            {
                int index = GetDiamondID(c);
                if(index != -1)
                {
                    foreach(Diamond d in collectibles) {
                        if (d.id == index) {
                            newList.Add(d);
                            diamonds[index] = newList.Count - 1;
                        }
                    }
                }
            }
            collectibles = newList;
            int not_risky_nodes = 1;
            List<Node> queue = new List<Node>();
            List<bool> auxlist = Enumerable.Repeat(false, collectibles.Count).ToList();
            HashSet<Tuple<Tuple<int, int>, int>> seen = new HashSet<Tuple<Tuple<int, int>, int>>();
            best_sol = new Node(new List<MoveInformation>(), new List<MoveInformation>(), auxlist, 0, false);
            queue.Add(new Node(new List<MoveInformation> { new MoveInformation(circle_platforms[src_circle]) },
                new List<MoveInformation> { new MoveInformation(rectangle_platforms[src_rectangle]) }, auxlist, 0, false));
            sw.Restart();

            while (queue.Count > 0)
            {
                Node n = queue[0];
                queue.RemoveAt(0);
                if (!n.is_risky)
                {
                    not_risky_nodes--;
                }

                // Process moves
                MoveInformation move_circle = n.plan_circle[n.plan_circle.Count - 1];
                foreach (int d in move_circle.diamondsCollected)
                {
                    if (diamonds.ContainsKey(d) && !n.caught[diamonds[d]])
                    {
                        n.caught[diamonds[d]] = true;
                        n.numCaught++;
                    }
                }
                MoveInformation move_rectangle = n.plan_rectangle[n.plan_rectangle.Count - 1];
                foreach (int d in move_rectangle.diamondsCollected)
                {
                    if (diamonds.ContainsKey(d) && !n.caught[diamonds[d]])
                    {
                        n.caught[diamonds[d]] = true;
                        n.numCaught++;
                    }
                }

                //Process platforms
                for (int i = 0; i < collectibles.Count; i++)
                {
                    if (!n.caught[i])
                    {
                        foreach(Tuple<int, string> tuple in collectibles[i].isAbovePlatform)
                        {
                            if (tuple.Item1 == move_circle.landingPlatform.id && tuple.Item2.Equals("c"))
                            {
                                n.caught[i] = true;
                                n.numCaught++;
                            }
                            if (tuple.Item1 == move_circle.landingPlatform.id && !move_circle.landingPlatform.real
                                && move_rectangle.landingPlatform.id == circle_to_rectangle[move_circle.landingPlatform.id]
                                && tuple.Item2.Equals("cr"))
                            {
                                n.caught[i] = true;
                                n.numCaught++;
                            }
                            if (tuple.Item1 == move_rectangle.landingPlatform.id && tuple.Item2.Equals("r"))
                            {
                                n.caught[i] = true;
                                n.numCaught++;
                            }
                        }
                    }
                }
                
                Tuple<Tuple<int, int>, int> node_tuple = new Tuple<Tuple<int, int>, int>
                    (
                    new Tuple<int,int>(n.plan_circle[n.plan_circle.Count - 1].landingPlatform.id, n.plan_rectangle[n.plan_rectangle.Count - 1].landingPlatform.id),
                    n.Value()
                    );

                // If we already have visited a similar node or enough time has passed, continue
                if (seen.Contains(node_tuple) || sw.ElapsedMilliseconds >= 500)
                {
                    continue;
                }
                seen.Add(node_tuple);
                // To eliminate false move
                if(n.plan_circle[0].moveType == MoveType.NOMOVE)
                {
                    n.plan_circle.RemoveAt(0);
                }
                if (n.plan_rectangle[0].moveType == MoveType.NOMOVE)
                {
                    n.plan_rectangle.RemoveAt(0);
                }
                // This is for incomplete solutions
                if (n.numCaught > best_sol.numCaught || (n.numCaught == best_sol.numCaught && best_sol.is_risky && !n.is_risky))
                {
                    best_sol = n;
                }
                if (n.numCaught == collectibles.Count)
                {
                    planIsComplete = true;
                    if (!n.is_risky || not_risky_nodes == 0)
                    {
                        return;
                    }
                }
                else
                {
                    List<MoveInformation> circle_moves = new List<MoveInformation>(move_circle.landingPlatform.moveInfoList);
                    circle_moves.Insert(0, new MoveInformation(move_circle.landingPlatform) { moveType = MoveType.COOPMOVE });

                    List<MoveInformation> rectangle_moves = new List<MoveInformation>(move_rectangle.landingPlatform.moveInfoList);
                    rectangle_moves.Insert(0, new MoveInformation(move_rectangle.landingPlatform) { moveType = MoveType.COOPMOVE });

                    foreach (MoveInformation mc in circle_moves)
                    {
                        foreach (MoveInformation mr in rectangle_moves)
                        {
                            if (AreCompatible(mc, mr))
                            {
                                List<MoveInformation> newCirclePlan = new List<MoveInformation>(n.plan_circle);
                                List<MoveInformation> newRectanglePlan = new List<MoveInformation>(n.plan_rectangle);
                                List<bool> newcaught = new List<bool>(n.caught);
                                newCirclePlan.Add(mc);
                                newRectanglePlan.Add(mr);
                                bool new_node_is_risky = n.is_risky || mc.risky || mr.risky /*|| (previous_move != null && m.IsEqual(previous_move))*/;
                                if (!new_node_is_risky)
                                {
                                    not_risky_nodes++;
                                }
                                queue.Add(new Node(newCirclePlan, newRectanglePlan, newcaught, n.numCaught, new_node_is_risky));
                            }
                        }
                        if (not_risky_nodes == 0 && planIsComplete)
                        {
                            return;
                        }
                    }
                }
            }
            // If we only find risky solutions or incomplete ones, we return the one that catches the most diamonds possible
            return;
        }

        private bool AreCompatible(MoveInformation mc, MoveInformation mr)
        {
            if(mc.moveType == MoveType.ADJACENT && mr.moveType == MoveType.ADJACENT)
            {
                int a =  0;
            }
            if (mc.departurePlatform.id == mc.landingPlatform.id && mr.departurePlatform.id == mr.landingPlatform.id)
            {
                return false;
            }
            if (mc.departurePlatform.id == mc.landingPlatform.id && mc.moveType != MoveType.COOPMOVE)
            {
                return false;
            }
            if (mr.departurePlatform.id == mr.landingPlatform.id && mr.moveType != MoveType.COOPMOVE)
            {
                return false;
            }
            if(mc.departurePlatform.real && mc.landingPlatform.real)
            {
                return true;
            }
            if (!mc.departurePlatform.real && mr.moveType == MoveType.COOPMOVE && circle_to_rectangle[mc.departurePlatform.id] == mr.departurePlatform.id)
            {
                return true;
            }
            if (!mc.landingPlatform.real && mr.moveType == MoveType.COOPMOVE && circle_to_rectangle[mc.landingPlatform.id] == mr.landingPlatform.id)
            {
                return true;
            }
            if (mc.moveType == MoveType.ADJACENT && mr.moveType == MoveType.ADJACENT && circle_to_rectangle[mc.departurePlatform.id] == mr.departurePlatform.id
                && mc.velocityX * mr.velocityX > 0)
            {
                return true;
            }
            return false;
        }
    }
}
