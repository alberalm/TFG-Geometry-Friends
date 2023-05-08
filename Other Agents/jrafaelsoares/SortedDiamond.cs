using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public class SortedDiamond
    {
        private CollectibleRepresentation diamond;
        private int rule;

        public SortedDiamond(CollectibleRepresentation diamond, int rule)
        {
            this.diamond = diamond;
            this.rule = rule;
        }

        override
        public string ToString()
        {
            string result = "";

            result += diamond.ToString();
            result += "Rule: " + rule.ToString();

            return result;
        }

        public CollectibleRepresentation getDiamond()
        {
            return diamond;
        }

        public int getRule()
        {
            return rule;
        }

        public void setRule(int rule)
        {
            this.rule = rule;
        }
    }
}
