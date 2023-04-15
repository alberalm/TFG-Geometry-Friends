using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    public class LevelMapCooperative
    {
        public LevelMapRectangle levelMapRectangle;
        public LevelMapCircle levelMapCircle;

        public LevelMapCooperative(LevelMapCircle levelMapCircle, LevelMapRectangle levelMapRectangle)
        {
            this.levelMapCircle = levelMapCircle;
            this.levelMapRectangle = levelMapRectangle;
        }

        public void CreateLevelMap()
        {
            levelMapCircle.AddCooperative(levelMapRectangle);
            //levelMapRectangle.AddCooperative(levelMapCircle);
            levelMapCircle.GenerateMoveInformation();
            levelMapRectangle.GenerateMoveInformation();
        }
    }
}
