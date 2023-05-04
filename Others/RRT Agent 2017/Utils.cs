using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public class Utils
    {
        List<Platform> Platforms;
        Platform ground;
        float radius;

        public Utils(List<Platform> platforms, Platform g, float r)
        {
            Platforms = platforms;
            ground = g;
            radius = r;
        }

        public Platform getDiamondPlatform(float x, float y)
        {
            Platform possiblePlatform = ground;

            foreach (Platform platform in Platforms)
            {
                //if the platform is higher than the possible one and the diamond is within it x range
                if (y < platform.getY() &&
                    x >= platform.getX() - platform.getWidth() / 2 && x <= platform.getX() + platform.getWidth() / 2 &&
                    platform.getY() < possiblePlatform.getY())
                {
                    possiblePlatform = platform;                    
                }
            }
            return possiblePlatform;
        }

        //TODO - add OnPlatform and OnSamePlatform here
        public Platform onPlatform(float x, float y, Rectangle area)
        {
            float margin = 25;
            foreach (Platform platform in Platforms)
            {
                if (x >= (platform.getX() - platform.getWidth() / 2) &&
                   x <= (platform.getX() + platform.getWidth() / 2) &&
                   y + 10 >= (platform.getY() - platform.getHeight() / 2 - margin) &&
                   y + 10 <= (platform.getY() + platform.getHeight() / 2 + margin))
                {
                    return platform;
                }
            }
            if (y + 10 >= 720)
            {
                return ground;
            }
            return null;
        }

        //check if two platforms are the same
        public bool samePlatform(Platform p1, Platform p2)
        {
            if(p1 != null && p2 != null &&
               Math.Round(p1.getX()) == Math.Round(p2.getX()) &&
               Math.Round(p1.getY()) == Math.Round(p2.getY()))
            {
                return true;
            }

            return false;
        }
        //agent x, agent height, point pos and platform
        //y is not necessary for this function should only be called if the points are in the same platform
        public bool obstacleBetween(float x1, float x2, Platform platform)
        {
            foreach(Platform otherPlatform in Platforms)
            {
                //check if there is an obstacle in the platform
                if (otherPlatform.getX() - otherPlatform.getWidth()/2 >= platform.getX() - platform.getWidth()/2 &&
                    otherPlatform.getX() + otherPlatform.getWidth()/2 <= platform.getX() + platform.getWidth()/2 &&
                    otherPlatform.getY() + otherPlatform.getHeight()/2 >= platform.getY() - platform.getHeight()/2 - radius*2)
                {
                    //check if the agent and the other point are on different sides
                    if(x1 < otherPlatform.getX() && x2 > otherPlatform.getX() ||
                       x1 > otherPlatform.getX() && x2 < otherPlatform.getX())
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
