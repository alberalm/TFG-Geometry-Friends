using GeometryFriendsAgents.Model;
using System;
using System.Collections.Generic;

namespace GeometryFriendsAgents.WorldObjects {
    abstract class Platform : WorldObject, IComparable {

        private const int TOBEINITIALIZED = -1;
        protected int _id;
        protected WorldModel _model;

        public WorldModel Model {
            get { return _model; }
        }

        public int ID {
            get { return _id; }
            set { _id = value; }
        }

        public bool LeftWall = false;

        public bool RightWall = false;

        protected int _width;

        public int Width {
            get { return _width; }
            set { _width = value; }
        }

        protected int _height;

        public int Height {
            get { return _height; }
            set { _height = value; }
        }

        public int Left {
            get { return xPos - Width / 2; }
        }

        public int Right {
            get { return xPos + Width / 2; }
        }

        public int Top { 
            get { return yPos - Height / 2; } 
        }

        public int Bottom {
            get { return yPos + Height / 2; }
        }

        protected List<Collectible> _collectibles;

        public List<Collectible> Collectibles {
            get { return _collectibles; }
        }

        protected int _totalNumberCollectibles;

        public int TotalNumberCollectibles {
            get { return _totalNumberCollectibles; }
        }

        protected DateTime _startTime;

        public DateTime StartTime {
            get { return _startTime; }
            set { _startTime = value; }
        }

        protected DateTime _endTime;

        public DateTime EndTime {
            get { return _endTime; }
            set { _endTime = value; }
        }

        protected Platform(int xPos, int yPos, int height, int width, WorldModel model) {
            _xPos = xPos;
            _yPos = yPos;
            _width = width;
            _height = height;
            _totalNumberCollectibles = TOBEINITIALIZED;
            _model = model;
            _collectibles = new List<Collectible>();
        }

        public void AddCollectibles(List<Collectible> c) {
            if (_totalNumberCollectibles == -1) {
                _totalNumberCollectibles = c.Count;
            }
            _collectibles = c;
        }

        public int CompareTo(object obj) {
            Platform p = (Platform)obj;

            int myTop = _yPos - _height / 2;
            int hisTop = p.yPos - p.Height / 2;

            int myLeft = _xPos - _width / 2;
            int hisLeft = p.xPos - p.Width / 2;

            if (myTop < hisTop)
                return -1;
            else if (myTop > hisTop)
                return 1;
            else if (myLeft < hisLeft)
                return -1;
            else if (hisLeft < myLeft)
                return 1;
            else
                return 0;
        }

        public int NumberCollectiblesCaught { get { return _totalNumberCollectibles - _collectibles.Count; } }

        public int PercentageCollectiblesCaught {
            get {
                if (TotalNumberCollectibles == 0) {
                    return 100;
                } else {
                    return (int)(((float)NumberCollectiblesCaught / TotalNumberCollectibles) * 100);
                }
            }
        }

        internal abstract Platform Clone();

        internal virtual bool Colides(Platform analysingPlatform) {
            Platform topPlatform = null, bottomPlatform = null;
            
            if (this.Top <= analysingPlatform.Top) {
                topPlatform = this;
                bottomPlatform = analysingPlatform;
            } else if (this.Top > analysingPlatform.Top) {
                topPlatform = analysingPlatform;
                bottomPlatform = this;
            } 

            return IsThereColision(topPlatform, bottomPlatform);
        }

        protected abstract bool IsThereColision(Platform topPlatform, Platform bottomPlatform);

        public abstract List<Platform> SplitPlatform(Platform p);

        public override bool Equals(object obj) {
            return ((Platform)obj).ID == ID;
        }
    }
}
