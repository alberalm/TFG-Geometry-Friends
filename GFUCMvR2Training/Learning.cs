using GeometryFriends.AI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;

namespace GeometryFriendsAgents
{
    public class Learning
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
            
            possibleMoves.Add(Moves.MOVE_LEFT);
            possibleMoves.Add(Moves.MOVE_RIGHT);
            possibleMoves.Add(Moves.MORPH_UP);
            possibleMoves.Add(Moves.MORPH_DOWN);
            possibleMoves.Add(Moves.NO_ACTION);
            moves = new List<Moves>();
            states = new List<State>();
            random = new Random();
            LoadFile();
        }
        
        public void LoadFile()
        {
            StreamReader archivo = new StreamReader(GameInfo.Q_PATH_RECT + "15" + GameInfo.Q_PATH_EXTENSION);
            string line = archivo.ReadLine();
            int count = 0;
            while ((line = archivo.ReadLine()) != null && line != "")
            {
                string[] split = line.Split(';');
                State s = new State(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]), int.Parse(split[3]), int.Parse(split[4]));
                Moves m;
                if (split[5] == "MOVE_RIGHT")
                {
                    m = Moves.MOVE_RIGHT;
                }
                else if (split[5] == "MOVE_LEFT")
                {
                    m = Moves.MOVE_LEFT;
                }
                else if (split[5] == "MORPH_UP")
                {
                    m = Moves.MORPH_UP;
                }
                else if (split[5] == "MORPH_DOWN")
                {
                    m = Moves.MORPH_DOWN;
                }
                else
                {
                    m = Moves.NO_ACTION;
                }

                double v = double.Parse(split[6]);

                if (s.distance_x <= GameInfo.MAX_DISTANCE)
                {
                    if (!Q_table.ContainsKey(s.ToString()))
                    {
                        Q_table.Add(s.ToString(), new Dictionary<Moves, double>());
                    }
                    Q_table[s.ToString()].Add(m, v);
                }
                count++;
            }
            archivo.Close();
            
        }

        public void SaveFile()
        {
            StreamWriter sw = new StreamWriter(GameInfo.Q_PATH_RECT + "15" + GameInfo.Q_PATH_EXTENSION);
            sw.WriteLine("Distance_x;Distance_y;Current_velocity_x;Height;Hole_width;Action;Value");
            foreach (var pair in Q_table)
            {
                foreach (var pair2 in pair.Value)
                {
                    string[] st = pair.Key.ToString().Split(';');
                    if (int.Parse(st[1]) >= 0)
                    {
                        sw.WriteLine("{0};{1};{2};{3};{4};{5};{6}", st[0], st[1], st[2], st[3], st[4], pair2.Key.ToString(), pair2.Value);
                    }
                }
            }
            sw.Close();
        }

        public Moves ChooseMove(State current, int hole_width)
        {
            Moves action = Moves.NO_ACTION;

            if(-Math.Sign(hole_width) * current.distance_x > GameInfo.MAX_DISTANCE)
            {
                if (hole_width > 0)
                {
                    if (current.current_velocity_x > 20)
                    {
                        return Moves.NO_ACTION;
                    }
                    else
                    {
                        return Moves.MOVE_RIGHT;
                    }
                }
                else
                {
                    if (current.current_velocity_x < -20)
                    {
                        return Moves.NO_ACTION;
                    }
                    else
                    {
                        return Moves.MOVE_LEFT;
                    }
                }
            }

            if (current.distance_y >= 0)
            {
                if (!Q_table.ContainsKey(current.ToString()))
                {
                    Q_table.Add(current.ToString(), new Dictionary<Moves, double>());
                    foreach (Moves m in possibleMoves)
                    {
                        Q_table[current.ToString()].Add(m, 0);
                    }
                    action = possibleMoves[random.Next(possibleMoves.Count)];
                }
                else if (random.NextDouble() < GameInfo.EPSILON)
                {
                    action = possibleMoves[random.Next(possibleMoves.Count)];
                }
                else
                {
                    action = possibleMoves[random.Next(possibleMoves.Count)];
                    double max = Q_table[current.ToString()][action];
                    
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
                if (hole_width < 0)
                {
                    if (action == Moves.MOVE_RIGHT)
                    {
                        action = Moves.MOVE_LEFT;
                    }
                    else if (action == Moves.MOVE_LEFT)
                    {
                        action = Moves.MOVE_RIGHT;
                    }
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

                Q_table[s.ToString()][m] = (1 - GameInfo.ALPHA) * old_value + GameInfo.ALPHA * (states[i + 1].Reward() + GameInfo.GAMMA * next_max);
                
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
    }
}
