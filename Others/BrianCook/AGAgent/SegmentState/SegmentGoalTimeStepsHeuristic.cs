using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    public class SegmentGoalTimeStepsHeuristic : Heuristic<SegmentState>
    {
        GameArea gameArea;

        public SegmentGoalTimeStepsHeuristic(GameArea gameArea)
        {
            this.gameArea = gameArea;
        }

        public override EvaluationResult Compute(EvaluationContext<SegmentState> context)
        {
            float total = 0;

            var result = new EvaluationResult(total);
            return result;
        }
    }
}
