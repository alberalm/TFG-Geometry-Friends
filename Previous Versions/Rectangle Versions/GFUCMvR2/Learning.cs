using GeometryFriends.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    public abstract class Learning
    {
        // 3 moves: roll right, roll left and do nothing
        protected Dictionary<string, Dictionary<Moves, double>> Q_table;
        protected List<Moves> moves;
        protected List<State> states;
        protected List<Moves> possibleMoves = new List<Moves>();
        protected Random random;

        public Learning()
        {
            Q_table = new Dictionary<string, Dictionary<Moves, double>>();
            moves = new List<Moves>();
            states = new List<State>();
            random = new Random();
        }

        public abstract void LoadFile();
        public abstract void SaveFile();
        public abstract Moves ChooseMove(State current, int d);
        public abstract void UpdateTable(State current);


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
