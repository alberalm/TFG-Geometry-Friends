using GeometryFriends.AI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;

namespace GeometryFriendsAgents
{
    class Learning
    {
        // 3 moves: roll right, roll left and do nothing
        Dictionary <string, Dictionary<Moves, double>> Q_table;
        List<Moves> moves;
        List<State> states;
        List<Moves> possibleMoves = new List<Moves>();
        Random random;
        public Learning()
        {
            Q_table = new Dictionary<string, Dictionary<Moves, double>>();
            possibleMoves.Add(Moves.ROLL_LEFT);
            possibleMoves.Add(Moves.ROLL_RIGHT);
            possibleMoves.Add(Moves.NO_ACTION);
            moves = new List<Moves>();
            states = new List<State>();
            random = new Random();
            LoadFile();
        }
        
        public void LoadFile()
        {
            StreamReader archivo = new StreamReader(GameInfo.Q_PATH);
            string line = archivo.ReadLine();
            while ((line = archivo.ReadLine()) != null && line != "")
            {
                string[] split = line.Split(';');
                State s = new State(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]));
                Moves m;
                if(split[3] == "ROLL_RIGHT")
                {
                    m = Moves.ROLL_RIGHT;
                }
                else if(split[3] == "ROLL_LEFT")
                {
                    m = Moves.ROLL_LEFT;
                }
                else
                {
                    m = Moves.NO_ACTION;
                }
                double v = double.Parse(split[4]);
                if (s.distance <= GameInfo.MAX_DISTANCE)
                {
                    if (!Q_table.ContainsKey(s.ToString()))
                    {
                        Q_table.Add(s.ToString(), new Dictionary<Moves, double>());
                    }
                    Q_table[s.ToString()].Add(m, v);
                }
            }
            archivo.Close();
        }


        public void SaveFile()
        {
            StreamWriter sw = new StreamWriter(GameInfo.Q_PATH);
            sw.WriteLine("Distance;Current_Velocity;Target_Velocity;Move;Value");
            foreach(var pair in Q_table)
            {
                foreach (var pair2 in pair.Value)
                {
                    string[] st = pair.Key.ToString().Split(';');
                    sw.WriteLine("{0};{1};{2};{3};{4}", st[0], st[1], st[2], pair2.Key.ToString(), pair2.Value);
                }
            }
            sw.Close();
        }

        public Moves ChooseMove(State current, int d) //d=circleposition-targetposition
        {
            Moves action;

            if(current.distance > GameInfo.MAX_DISTANCE) //If circle is far away of the current point, get closer
            {
                if(d > 0)
                {
                    return Moves.ROLL_LEFT;
                }
                else
                {
                    return Moves.ROLL_RIGHT;
                }
            }

            if (!Q_table.ContainsKey(current.ToString()))//First time in this state, act randomly
            {
                Q_table.Add(current.ToString(), new Dictionary<Moves, double>());
                foreach (Moves m in possibleMoves)
                {
                    Q_table[current.ToString()].Add(m, 0);
                }
                action = possibleMoves[random.Next(possibleMoves.Count)];
            }
            else if (random.NextDouble() < GameInfo.EPSILON)//Exploration
            {
                action = possibleMoves[random.Next(possibleMoves.Count)];
            }
            else//Exploitation
            {
                double max = Q_table[current.ToString()][Moves.NO_ACTION];
                action = Moves.NO_ACTION;
                foreach (Moves m in possibleMoves)
                {
                    if (Q_table[current.ToString()][m] > max)
                    {
                        max = Q_table[current.ToString()][m];
                        action = m;
                    }
                }
            }
            moves.Add(action);
            states.Add(current);
            if  (d < 0)//Adjustment due to working with positive distance only
            {
                if (action == Moves.ROLL_RIGHT)
                {
                    action = Moves.ROLL_LEFT;
                }
                else if  (action  == Moves.ROLL_LEFT)
                {
                    action = Moves.ROLL_RIGHT;
                }
            }
            return action;
        }

        public void UpdateTable(State current)
        {
            Moves m;
            State s;
            states.Add(current);
            if (!Q_table.ContainsKey(current.ToString()))
            {
                Q_table.Add(current.ToString(), new Dictionary<Moves, double>());
                foreach (Moves mo in possibleMoves)
                {
                    Q_table[current.ToString()].Add(mo, current.Reward());
                }
            }
            for (int i = moves.Count() - 1; i >= 0; i--)
            {
                m = moves[i];
                s = states[i];
                double old_value = Q_table[s.ToString()][m];
                double next_max = Q_table[states[i+1].ToString()][m];

                foreach (Moves mov in possibleMoves)
                {
                    if (Q_table[states[i+1].ToString()][mov] > next_max)
                    {
                        next_max = Q_table[states[i+1].ToString()][mov];
                    }
                }

                Q_table[s.ToString()][m] = (1 - GameInfo.ALPHA) * old_value + GameInfo.ALPHA * (Reward(s,m) + GameInfo.GAMMA * next_max);
            }

            moves = new List<Moves>();
            states = new List<State>();

            /*next_state, reward, done, info = env.step(action)

            old_value = q_table[state, action]
            next_max = np.max(q_table[next_state])

            new_value = (1 - alpha) * old_value + alpha * (reward + gamma * next_max)
            q_table[state, action] = new_value

            if reward == -10:
                penalties += 1

            state = next_state*/
        }

        int Reward(State s, Moves m)
        {
            if (s.IsFinal())
            {
                return 500;
            }
            int[] r = new int[]  { -3, -3, -3 }; // [ROLL_LEFT, NO_ACTION, ROLL_RIGHT]
            int brake_distance = (int)(s.current_velocity * s.current_velocity / (2 * GameInfo.ACCELERATION * GameInfo.PIXEL_LENGTH));
            int acceleration_distance = (int) (s.target_velocity * s.target_velocity / (2 * GameInfo.ACCELERATION * GameInfo.PIXEL_LENGTH));
            if (s.current_velocity >= 0)
            {
                if (s.target_velocity >= 0)
                {
                    if(s.distance > GameInfo.TARGET_POINT_ERROR)
                    {
                        r = new int[] { 10, -100, -100 };
                    }
                }
                else
                {
                    if (Math.Abs(s.distance + brake_distance - acceleration_distance) <= GameInfo.ERROR)
                    {
                        //Learn
                    }
                    else
                    {
                        if (s.distance + brake_distance > acceleration_distance)
                        {
                            r = new int[] { 10, -100, -100 };
                        }
                        else
                        {
                            r = new int[] { -100, -100, 10 };
                        }
                    }
                }
            }
            else
            {
                if (s.target_velocity > 0)
                {
                    if (Math.Abs(s.distance - brake_distance + acceleration_distance) <= GameInfo.ERROR)
                    {
                        //Learn
                    }
                    else
                    {
                        if (s.distance - brake_distance >  - acceleration_distance)
                        {
                            r = new int[] { 10, -100, -100 };
                        }
                        else
                        {
                            r = new int[] { -100, -100, 10 };
                        }
                    }
                }
                else
                {
                    //Learn
                }
            }

            switch (m)
            {
                case Moves.ROLL_LEFT:
                    return r[0];
                case Moves.ROLL_RIGHT:
                    return r[2];
                case Moves.NO_ACTION:
                    return r[1];
                default:
                    return -3;
            }
        }

    }
}
