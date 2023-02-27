using GeometryFriends.AI;
using GeometryFriendsAgents.GameStates;
using GeometryFriendsAgents.Model;
using System;

namespace GeometryFriendsAgents {
    class InterPlatformPlayedGameStateInfo {

        private GameState _state;

        public GameState State {
            get { return _state; }
        }

        private Moves _move;

        public Moves Move
        {
            get { return _move; }
        }

        private DateTime _playedTime;

        public DateTime PlayedTime {
            get {
                return _playedTime;
            }
        }

        private GraphLink _link;

        public GraphLink Link {
            get { return _link; }
        }

        public InterPlatformPlayedGameStateInfo(GameState state, Moves move, GraphLink link, DateTime playedTime)
        {
            _state = state;
            _move = move;
            _link = link;
            _playedTime = playedTime;
        }
    }

}

