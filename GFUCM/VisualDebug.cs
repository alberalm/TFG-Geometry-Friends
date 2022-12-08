using System;
using System.Collections.Generic;

using GeometryFriends.AI.Debug;
using GeometryFriends.XNAStub;
using System.Drawing;
using System.Linq;
using System.Text;

namespace GeometryFriendsAgents
{
    class VisualDebug
    {
        private const float TIME_STEP = 0.01f;
        private const int LIMIT = 200;
        public static void DrawParabola(ref List<DebugInformation> debugInformation, float x_0, float y_0, float vx_0, float vy_0, GeometryFriends.XNAStub.Color color)
        {
            for (int i = 0; i < LIMIT; i++)
            {
                float x_t = x_0 + vx_0 * i * TIME_STEP;
                float y_t = y_0 - vy_0 * i * TIME_STEP + GameInfo.GRAVITY * (float)Math.Pow(i * TIME_STEP, 2) / 2;

                debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(x_t, y_t), 2, color));
            }
        }
        public static void DrawArrow(ref List<DebugInformation> debugInformation, float x_0, float y_0, int numX, int numY, GeometryFriends.XNAStub.Color color)
        {
            //In case it's positive
            for (int i = 0; i < numX*4; i++)
            {
                debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(x_0+i*2, y_0), 2, color));
            }
            //In case it's negative
            for (int i = numX * 4; i < 0; i++)
            {
                debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(x_0 + i * 2, y_0), 2, color));
            }
            //In case it's positive
            for (int i = 0; i < numY * 4; i++)
            {
                debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(x_0 , y_0 + i * 2), 2, color));
            }
            //In case it's negative
            for (int i = numY * 4; i < 0; i++)
            {
                debugInformation.Add(DebugInformationFactory.CreateCircleDebugInfo(new PointF(x_0 , y_0 + i * 2), 2, color));
            }
        }
        public static void change(ref GeometryFriends.XNAStub.Color color)
        {

            if (color == GeometryFriends.XNAStub.Color.Yellow)
            {
                color = GeometryFriends.XNAStub.Color.Turquoise;
            }
            else
            {
                color = GeometryFriends.XNAStub.Color.Yellow;
            }
        }

    }
}
