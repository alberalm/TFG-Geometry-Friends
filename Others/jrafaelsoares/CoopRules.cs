using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using GeometryFriends;
using GeometryFriends.AI.Perceptions.Information;
using GeometryFriends.AI.Communication;

namespace GeometryFriendsAgents
{
    public class CoopRules
    {
        //private int pixelSize = 40;
        //private List<List<PixelType>> levelInfo;

        private double maxJump = 320; //322.57;
        private double maxRadius = 40;

        private List<ObstacleRepresentation> yPlatforms;
        //private List<ObstacleRepresentation> xPlatforms;
        private CollectibleRepresentation[] diamonds;

        List<CollectibleRepresentation> circleDiamonds;
        List<CollectibleRepresentation> rectangleDiamonds;
        List<SortedDiamond> coopDiamonds;

        private Rectangle levelArea;
        private List<AgentMessage> messages = new List<AgentMessage>();

        public CoopRules(Rectangle area, CollectibleRepresentation[] diamonds, ObstacleRepresentation[] platforms, ObstacleRepresentation[] rectanglePlatforms, ObstacleRepresentation[] circlePlatforms)
        {
            this.diamonds = diamonds;
            levelArea = area;

            //xPlatforms = new List<ObstacleRepresentation>(platforms);
            yPlatforms = new List<ObstacleRepresentation>(platforms);

            //xPlatforms = xPlatforms.OrderBy(o => o.X).ToList();
            yPlatforms = yPlatforms.OrderBy(o => o.Y).ToList();

            /*levelInfo = new List<List<PixelType>>((int)(area.Height / pixelSize + 1));

            for (int y = 0; y <= (int)(area.Height / pixelSize); y++)
            {
                levelInfo.Add(new List<PixelType>((int)(area.Width / pixelSize + 1)));

                for (int x = 0; x <= (int)(area.Width / pixelSize); x++)
                {
                    levelInfo[y].Add(PixelType.NONE);
                }
            }

            Debug.Print(levelInfo.Count.ToString());

            foreach (ObstacleRepresentation platform in platforms)
            {
                int startX = (int)((platform.X - area.X - (platform.Width / 2)) / pixelSize);
                int endX = Math.Min((int)Math.Ceiling((platform.X - area.X + (platform.Width / 2)) / pixelSize), area.Width / pixelSize);

                int startY = (int)((platform.Y - area.Y - (platform.Height / 2)) / pixelSize);
                int endY = Math.Min((int)Math.Ceiling((platform.Y - area.Y + (platform.Height / 2)) / pixelSize), area.Height / pixelSize);

                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        levelInfo[y][x] = PixelType.PLATFORM;
                    }
                }
            }

            foreach (ObstacleRepresentation platform in rectanglePlatforms)
            {
                int startX = (int)((platform.X - area.X - (platform.Width / 2)) / pixelSize);
                int endX = Math.Min((int)Math.Ceiling((platform.X - area.X + (platform.Width / 2)) / pixelSize), area.Width / pixelSize);

                int startY = (int)((platform.Y - area.Y - (platform.Height / 2)) / pixelSize);
                int endY = Math.Min((int)Math.Ceiling((platform.Y - area.Y + (platform.Height / 2)) / pixelSize), area.Height / pixelSize);

                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        levelInfo[y][x] = PixelType.RECTANGLE_PLATFORM;
                    }
                }
            }

            foreach (ObstacleRepresentation platform in circlePlatforms)
            {
                int startX = (int)((platform.X - area.X - (platform.Width / 2)) / pixelSize);
                int endX = Math.Min((int)Math.Ceiling((platform.X - area.X + (platform.Width / 2)) / pixelSize), area.Width / pixelSize);

                int startY = (int)((platform.Y - area.Y - (platform.Height / 2)) / pixelSize);
                int endY = Math.Min((int)Math.Ceiling((platform.Y - area.Y + (platform.Height / 2)) / pixelSize), area.Height / pixelSize);

                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        levelInfo[y][x] = PixelType.CIRCLE_PLATFORM;
                    }
                }
            }

            foreach (CollectibleRepresentation diamond in diamonds)
            {
                int x = Math.Min((int)((diamond.X - area.X) / pixelSize), area.Width / pixelSize);
                int y = Math.Min((int)((diamond.Y - area.Y) / pixelSize), area.Height / pixelSize);

                levelInfo[y][x] = PixelType.DIAMOND;
            }*/

        }

        public void ApplyRules(CircleRepresentation c, RectangleRepresentation r)
        {
            circleDiamonds = new List<CollectibleRepresentation>();
            rectangleDiamonds = new List<CollectibleRepresentation>();
            coopDiamonds = new List<SortedDiamond>();

            // Rules return 0 for circle diamond, 1 for rectangle diamond and 2 for coop
            foreach (CollectibleRepresentation diamond in diamonds)
            {
                int unreachableJump = unreachableByJump(diamond.X, diamond.Y, c, r);
                int unreachableBetween = unreachableBetweenPlatforms(diamond.X, diamond.Y, c, r);

                //if it must be done by coop
                if (unreachableBetween == 2)
                {
                    //int on sorted means it is unreachebleBetween
                    coopDiamonds.Add(new SortedDiamond(diamond, 2));
                    continue;
                }

                if(unreachableJump == 2)
                {
                    coopDiamonds.Add(new SortedDiamond(diamond, 1));
                    continue;
                }

                if(unreachableBetween == 1)
                {
                    rectangleDiamonds.Add(diamond);
                    continue;
                }

                //if none of the others apply, its a circle diamond
                circleDiamonds.Add(diamond);
            }
        }

        public int unreachableByJump(float dX, float dY, CircleRepresentation c, RectangleRepresentation r)
        {
            float varY = dY;
            ObstacleRepresentation previousPlatform = new ObstacleRepresentation(-10f, -10f, 0, 0);

            foreach (ObstacleRepresentation platform in yPlatforms)
            {
                if (c.Y - varY < maxJump)
                {
                    return 0;
                }

                if (varY >= platform.Y)
                {
                    continue;
                }
                else if (platform.Y - varY < maxJump)
                {
                    if(previousPlatform.X >= 0)
                    {
                        if(Math.Max(previousPlatform.X - previousPlatform.Width / 2, levelArea.X) - Math.Max(platform.X - platform.Width / 2, levelArea.X) > maxRadius / 2 || Math.Min(platform.X + platform.Width / 2, levelArea.Width + levelArea.X) - Math.Min(previousPlatform.X + previousPlatform.Width / 2, levelArea.Width + levelArea.X) > maxRadius / 2)
                        {
                            previousPlatform = platform;
                            varY = platform.Y;
                        }
                    }
                    else if(dX - platform.X + platform.Width / 2 >= 0 && platform.X + platform.Width / 2 - dX >= 0)
                    {
                        varY = platform.Y;
                        previousPlatform = platform;
                    }

                }
                else
                {
                    return 2;
                }
            }

            return 2;
        }

        public int unreachableBetweenPlatforms(float dX, float dY, CircleRepresentation c, RectangleRepresentation r)
        {
            ObstacleRepresentation closestAbove = new ObstacleRepresentation(dX, levelArea.Y, 0, 0), closestBelow = new ObstacleRepresentation(dX, levelArea.Height + levelArea.Y, 0, 0);

            // Might be able to be changed into logarithmic complexity
            // Might not work for some combinations of Circle and regular platforms
            foreach (ObstacleRepresentation platform in yPlatforms)
            {
                // Check that diamond is above or below platform (same X)
                if (platform.X - platform.Width / 2 < dX && dX < platform.X + platform.Width / 2)
                {
                    // Since its ordered the first below is the closest
                    if (platform.Y > dY && platform.Y < closestBelow.Y)
                    {
                        closestBelow = platform;
                        break;
                    }
                    else if (platform.Y < dY && platform.Y > closestAbove.Y)
                    {
                        closestAbove = platform;
                    }
                }
            }

            // Either rectangle or coop
            if (closestBelow.Y - closestBelow.Height / 2 - closestAbove.Y - closestAbove.Height / 2 <= maxRadius * 2)
            {
                // If rectangle is on top of the platform below, it can get there alone
                if (r.Y + (r.Height / 2) - closestBelow.Y + closestBelow.Height / 2  <= 10)
                {
                    return 1;
                }
                // Else, it needs the help from the circle
                else
                {
                    return 2;
                }
            }

            return 0;
        }

        public override String ToString()
        {
            String result = "";

            /*for (int y = 0; y < levelInfo[0].Count + 1; y++)
            {
                result += "++";
            }

            result += "\n";

            for (int y = 0; y < levelInfo.Count; y++)
            {
                result += "+";
                for (int x = 0; x < levelInfo[0].Count; x++)
                {
                    result += levelInfo[y][x] == PixelType.PLATFORM ? "PP" : (levelInfo[y][x] == PixelType.DIAMOND ? "DD" : "  ");
                }

                result += "+\n";
            }

            for (int y = 0; y < levelInfo[0].Count + 1; y++)
            {
                result += "++";
            }*/

            result += "\n Circle Diamonds \n";

            foreach (CollectibleRepresentation d in circleDiamonds)
            {
                result += d.ToString() + "\n";
            }

            result += " Rectangle Diamonds \n";

            foreach (CollectibleRepresentation d in rectangleDiamonds)
            {
                result += d.ToString() + "\n";
            }

            result += " Coop Diamonds \n";

            foreach (SortedDiamond d in coopDiamonds)
            {
                result += d.ToString() + "\n";
            }

            return result;
        }

        /********************************************/
        /***************** GETTERS ******************/
        /********************************************/

        public CollectibleRepresentation[] getCircleDiamonds()
        {
            return circleDiamonds.ToArray();
        }

        public CollectibleRepresentation[] getRectangleDiamonds()
        {
            return rectangleDiamonds.ToArray();
        }

        public List<SortedDiamond> getCoopDiamonds()
        {
            return coopDiamonds;
        }

        public CollectibleRepresentation[] getAllDiamonds()
        {
            return diamonds;
        }

        /********************************************/
        /***************** SETTERS ******************/
        /********************************************/

        public void setCircleDiamonds(CollectibleRepresentation[] diamondInfo)
        {
            circleDiamonds = diamondInfo.ToList();
        }

        public void setRectangleDiamonds(CollectibleRepresentation[] diamondInfo)
        {
            rectangleDiamonds = diamondInfo.ToList();
        }
        public void setCoopDiamonds(List<SortedDiamond> diamondInfo)
        {
            coopDiamonds = diamondInfo;
        }

        /**********************************/
        /********* DIAMOND UPDATES ********/
        /**********************************/

        //Since SensorUpdate gets all Diamonds, we need to keep filtering the diamonds to see which ones got caught
        public CollectibleRepresentation[] updateCircleDiamonds(CollectibleRepresentation[] diamondInfo)
        {
            List<CollectibleRepresentation> newDiamondCollectible = new List<CollectibleRepresentation>();

            foreach(CollectibleRepresentation diamond in circleDiamonds)
            {
                //if contains work
                if (diamondInfo.Contains(diamond))
                {
                    newDiamondCollectible.Add(diamond);
                }
                //if it doesnt we need to do it hardcoded by transversing the vectors
            }

            circleDiamonds = newDiamondCollectible;
            return circleDiamonds.ToArray();
        }

        public CollectibleRepresentation[] updateRectangleDiamonds(CollectibleRepresentation[] diamondInfo)
        {
            List<CollectibleRepresentation> newDiamondCollectible = new List<CollectibleRepresentation>();

            foreach (CollectibleRepresentation diamond in rectangleDiamonds)
            {
                //if contains work
                if (diamondInfo.Contains(diamond))
                {
                    newDiamondCollectible.Add(diamond);
                }
                //if it doesnt we need to do it hardcoded by transversing the vectors
            }

            rectangleDiamonds = newDiamondCollectible;
            return rectangleDiamonds.ToArray();
        }

        public List<SortedDiamond> updateCoopDiamonds(CollectibleRepresentation[] diamondInfo)
        {
            List<SortedDiamond> newDiamondCollectible = new List<SortedDiamond>();

            foreach (SortedDiamond diamond in coopDiamonds)
            {
                if (diamondInfo.Contains(diamond.getDiamond()))
                {
                    newDiamondCollectible.Add(diamond);
                }
            }

            coopDiamonds = newDiamondCollectible;
            return coopDiamonds;
        }

        
        public void sendRectangleDiamonds()
        {
            messages.Add(new AgentMessage("Sending RectangleDiamonds", rectangleDiamonds));
        }

        public void receiveRectangleDiamonds()
        {
            rectangleDiamonds = (List<CollectibleRepresentation>) messages[0].Attachment;
        }
    }

    
}
