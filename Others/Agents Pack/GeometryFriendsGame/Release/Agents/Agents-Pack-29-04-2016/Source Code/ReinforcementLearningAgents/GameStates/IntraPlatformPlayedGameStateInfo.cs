using GeometryFriends.AI;
using GeometryFriendsAgents.GameStates;
using System;

namespace GeometryFriendsAgents {
    class IntraPlatformPlayedGameStateInfo {

        private GameState _state;

        public GameState State {
            get { return _state; }
        }

        private Moves _move;

        public Moves Move {
            get { return _move; }
        }

        private DateTime _playedTime;

        public DateTime PlayedTime {
            get {
                return _playedTime;
            }
        }

        private int _platformId;

        public int PlatformId {
            get { return _platformId; }
        }

        public IntraPlatformPlayedGameStateInfo(GameState state, Moves move, int platformId, DateTime playedTime) {
            _state = state;
            _move = move;
            _platformId = platformId;
            _playedTime = playedTime;
        }
    }

}

