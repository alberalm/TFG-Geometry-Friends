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
        Dictionary<State, Dictionary<Moves, double>> Q_table;
        List<Moves> moves;
        List<State> states;
        List<Moves> possibleMoves = new List<Moves>();
        public Learning()
        {
            Q_table = new Dictionary<State, Dictionary<Moves, double>>();
            possibleMoves.Add(Moves.ROLL_LEFT);
            possibleMoves.Add(Moves.ROLL_RIGHT);
            possibleMoves.Add(Moves.NO_ACTION);
        }
        
        public void LoadFile()
        {
            System.IO.StreamReader archivo = new System.IO.StreamReader(GameInfo.Q_PATH);
            string line;
            while ((line = archivo.ReadLine()) != null)
            {
                string[] split = line.Split(';');
                State s = new State(split[0]);
                Moves m;
                if(split[1] == "ROLL_RIGHT")
                {
                    m = Moves.ROLL_RIGHT;
                }
                else if(split[1] == "ROLL_LEFT")
                {
                    m = Moves.ROLL_LEFT;
                }
                else
                {
                    m = Moves.NO_ACTION;
                }
                double v = double.Parse(split[2]);
                if (!Q_table.ContainsKey(s)){
                    Q_table.Add(s, new Dictionary<Moves, double>());
                }
                Q_table[s].Add(m, v);
            }
        }

        public void SaveFile()
        {
            StreamWriter sw = new StreamWriter(GameInfo.Q_PATH);
            foreach(var pair in Q_table)
            {
                foreach (var pair2 in pair.Value)
                {
                    sw.WriteLine("{0};{1};{2}", pair.Key.toString(), pair2.Key.ToString(), pair2.Value);
                    //Beware if ToString of Moves return number
                }
            }
            
            sw.WriteLine();
            sw.Close();
        }

        public Moves ChooseMove(State current)
        {
            Moves action;
            Random random = new Random();
            List<Moves> possibleMoves = new List<Moves>();
            
            bool aleatorio = false;

            if (!Q_table.ContainsKey(current))
            {
                Q_table.Add(current, new Dictionary<Moves, double>());
                aleatorio = true;
                action = possibleMoves[random.Next(possibleMoves.Count)];
            }
            if (aleatorio || random.NextDouble() < GameInfo.EPSILON) {
                action = possibleMoves[random.Next(possibleMoves.Count)];
            }
            else {
                double max = Q_table[current][Moves.NO_ACTION];
                action = Moves.NO_ACTION;
                foreach (Moves m in possibleMoves)
                {
                    if (Q_table[current][m] > max)
                    {
                        max = Q_table[current][m];
                        action = m;
                    }
                }
            }

            moves.Add(action);
            states.Add(current);
            return action;
        }

        public void UpdateTable(State current)
        {
            Moves m;
            State s;
            states.Add(current);
            for(int i = moves.Count() - 1; i >= 0; i--)
            {
                m = moves[i];
                s = states[i];
                double old_value = Q_table[s][m];
                double next_max = Q_table[states[i+1]][m];

                foreach (Moves mov in possibleMoves)
                {
                    if (Q_table[states[i+1]][mov] > next_max)
                    {
                        next_max = Q_table[states[i+1]][mov];
                    }
                }
                double new_value = (1 - GameInfo.ALPHA) * old_value + GameInfo.ALPHA * (states[i+1].Reward() + GameInfo.GAMMA * next_max);
            }

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
