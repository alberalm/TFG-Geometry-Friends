using System.Collections.Generic;

namespace GeometryFriendsAgents.WorldObjects {
    class CirclePlatform : Platform {

        public CirclePlatform(int xPos, int yPos, int height, int width, Model.WorldModel model) : base(xPos, yPos, height, width, model) { }

        internal override Platform Clone() {
            CirclePlatform p = new CirclePlatform(xPos, yPos, Height, Width, _model);
            p._totalNumberCollectibles = _totalNumberCollectibles;
            p._collectibles = new List<Collectible>(_collectibles);
            p.ID = _id;

            if (this.Left < _model.Area.Left + CircleWorldModel.DEFAULT_RADIUS) {
                p.LeftWall = true;
            }
            if (this.Right > _model.Area.Right - CircleWorldModel.DEFAULT_RADIUS) {
                p.RightWall = true;
            }

            return p;
        }



        protected override bool IsThereColision(Platform topPlatform, Platform bottomPlatform) {
            if (topPlatform.Bottom > bottomPlatform.Top - 2.1 * CircleWorldModel.DEFAULT_RADIUS) {
                if (topPlatform.Right > bottomPlatform.Left && topPlatform.Left < bottomPlatform.Right) {
                    return true;
                }
            }
            return false;
        }

        public override List<Platform> SplitPlatform(Platform p) {
            List<Platform> platforms = new List<Platform>();
            int newXPos, newWidth;


            if (this.Left < p.Left) {
                newXPos = (this.Left + p.Left) / 2;
                newWidth = p.Left - this.Left;
                Platform newPlatform = new CirclePlatform(newXPos, yPos, Height, newWidth, _model);
                if (this.LeftWall) {
                    newPlatform.LeftWall = true;
                }
                newPlatform.RightWall = true;
                platforms.Add(newPlatform);
            }

            if (this.Right > p.Right) {
                newXPos = (p.Right + this.Right) / 2;
                newWidth = this.Right - p.Right;
                Platform newPlatform = new CirclePlatform(newXPos, yPos, Height, newWidth, _model);
                if (this.RightWall) {
                    newPlatform.RightWall = true;
                }
                newPlatform.LeftWall = true;
                platforms.Add(newPlatform);
            }

            return platforms;
        }
    }
}
