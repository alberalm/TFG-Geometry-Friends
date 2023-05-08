using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace GeometryFriendsAgents
{
    public class GameArea
    {
        public Rectangle Area;
        public PointF StartingPosition;
        public float CircleRadius = 40;
        public float GoalRadius = 20;
        public List<ObstacleRepresentation> Obstacles = new List<ObstacleRepresentation>();
        public List<CollectibleRepresentation> Collectibles = new List<CollectibleRepresentation>();
        public string CollectionName;
        public int WorldNumber, LevelNumber;
    }
}
