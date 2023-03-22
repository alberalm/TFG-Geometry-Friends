using GeometryFriends.AI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;

namespace GeometryFriendsAgents
{
    public class LearningCircle:Learning
    {
        public LearningCircle() : base()
        {
            possibleMoves.Add(Moves.ROLL_LEFT);
            possibleMoves.Add(Moves.ROLL_RIGHT);
            possibleMoves.Add(Moves.NO_ACTION);
            LoadFile();
        }
        
        public override void LoadFile()
        {
            for (int i = 0; i <= GameInfo.NUM_VELOCITIES_QLEARNING; i++)
            {
                StreamReader archivo = new StreamReader(GameInfo.Q_PATH1 + (GameInfo.VELOCITY_STEP_QLEARNING * i).ToString() + GameInfo.Q_PATH_EXTENSION);
                string line = archivo.ReadLine();
                int count = 0;
                while ((line = archivo.ReadLine()) != null && line != "")
                {
                    string[] split = line.Split(';');
                    StateCircle s = new StateCircle(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]));
                    Moves m;
                    if (split[3] == "ROLL_RIGHT")
                    {
                        m = Moves.ROLL_RIGHT;
                    }
                    else if (split[3] == "ROLL_LEFT")
                    {
                        m = Moves.ROLL_LEFT;
                    }
                    else
                    {
                        m = Moves.NO_ACTION;
                    }
                    double v = double.Parse(split[4]);
                    if (s.distance <= GameInfo.MAX_DISTANCE && Math.Abs(s.target_velocity) == GameInfo.VELOCITY_STEP_QLEARNING * i)
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
        }

        public override void SaveFile()
        {
            /*StreamWriter sw = new StreamWriter(GameInfo.Q_PATH1 + GameInfo.LEARNING_VELOCITY.ToString() + GameInfo.Q_PATH_EXTENSION);
            sw.WriteLine("Distance;Current_Velocity;Target_Velocity;Move;Value");
            foreach(var pair in Q_table)
            {
                foreach (var pair2 in pair.Value)
                {
                    string[] st = pair.Key.ToString().Split(';');
                    sw.WriteLine("{0};{1};{2};{3};{4}", st[0], st[1], st[2], pair2.Key.ToString(), pair2.Value);
                }
            }
            sw.Close();*/
        }

        public override Moves ChooseMove(State _current, int d)
        {
            StateCircle current = (StateCircle)_current;
            Moves action;

            if(current.distance > GameInfo.MAX_DISTANCE)
            {
                if (d > 0)
                {
                    return Moves.ROLL_LEFT;
                }
                else
                {
                    return Moves.ROLL_RIGHT;
                }
            }

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
            if  (d < 0)
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

        public override void UpdateTable(State current)
        {
            Moves m;
            StateCircle s;
            states.Add((StateCircle)current);
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
                s = (StateCircle) states[i];
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
        }
    }
}
