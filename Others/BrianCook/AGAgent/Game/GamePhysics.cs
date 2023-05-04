using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GeometryFriendsAgents
{
    static public class GamePhysics
    {
        public const float JUMP_VELOCITY_Y = -437f;
        public const float GRAVITY = 299.1f;
        public const int CIRCLE_RADIUS = 40;
        public const int GOAL_RADIUS = 20; 
        public const int MAX_X_VELOCITY = 200;
        public const float MAX_COLLISION_DEPTH = 0.8f;
        public const float GROUND_EPSILON = 1f;
        public const double GROUND_MAX_Y_VELOCITY = 20;
        public const float JUMP_PEAK_TIME = -JUMP_VELOCITY_Y / GRAVITY;
        public const float JUMP_PEAK_HEIGHT = 0.5f * GRAVITY * JUMP_PEAK_TIME * JUMP_PEAK_TIME;
        public const float MAX_ROLL_UP_X = CIRCLE_RADIUS * 3 / 2;
        public const float MAX_ROLL_UP_Y = CIRCLE_RADIUS * 3 / 4;
        static public bool GetPlatformJumpLocations(float xVel, float fromX1, float fromX2, float fromY, float toX1, float toX2, float toY, out float minX, out float maxX)
        {
            var deltaY = toY - fromY;
            if (deltaY <= -JUMP_PEAK_HEIGHT || xVel == 0)
            {
                minX = 0;
                maxX = 0;
                return false;
            }
            if (deltaY + CIRCLE_RADIUS < 0)
            {
                var t1 = (float)Math.Sqrt(-2 * (deltaY + CIRCLE_RADIUS) / GRAVITY);
                var t2 = JUMP_PEAK_TIME - t1;
                var t3 = (float)Math.Sqrt(2 * (deltaY + JUMP_PEAK_HEIGHT) / GRAVITY);

                var xDistanceToLanding = xVel * (t1 + t2 + t3);

                if (xVel > 0)
                {
                    maxX = Math.Min(toX1 - CIRCLE_RADIUS - xVel * t1, toX2 - xDistanceToLanding);
                    minX = toX1 - xDistanceToLanding;
                }
                else
                {
                    minX = Math.Max(toX2 + CIRCLE_RADIUS - xVel * t1, toX1 - xDistanceToLanding);
                    maxX = toX2 - xDistanceToLanding;
                }
            }
            else if (deltaY < 0)
            {
                if (xVel > 0)
                {
                    maxX = toX1 - CIRCLE_RADIUS;
                    minX = toX2 - 2 * xVel * JUMP_PEAK_TIME + CIRCLE_RADIUS;
                }
                else
                {
                    minX = toX2 + CIRCLE_RADIUS;
                    maxX = toX1 - 2 * xVel * JUMP_PEAK_TIME - CIRCLE_RADIUS;
                }
            }
            else
            {
                var t3 = (float)Math.Sqrt(2 * (deltaY + JUMP_PEAK_HEIGHT) / GRAVITY);
                var xDistanceToLanding = xVel * (JUMP_PEAK_TIME + t3);

                if (xVel > 0)
                {
                    maxX = toX2 - xDistanceToLanding;
                    minX = toX1 - xDistanceToLanding;
                }
                else
                {
                    minX = toX1 - xDistanceToLanding;
                    maxX = toX2 - xDistanceToLanding;
                }
            }
            if (maxX < fromX1 || minX > fromX2)
                return false;

            maxX = Math.Min(maxX, fromX2);
            minX = Math.Max(minX, fromX1);
            return true;
        }
        static public bool GetGoalJumpLocations(float xVel, float fromX1, float fromX2, float fromY, float goalX, float goalY, out float minX1, out float maxX1, out float minX2, out float maxX2)
        {
            var deltaY = goalY - fromY;
            minX1 = float.MaxValue;
            maxX1 = -float.MaxValue;
            minX2 = float.MaxValue;
            maxX2 = -float.MaxValue;
            if (deltaY <= -(JUMP_PEAK_HEIGHT + 2 * CIRCLE_RADIUS + GOAL_RADIUS))
                return false;
            if (deltaY + JUMP_PEAK_HEIGHT - GOAL_RADIUS < 0)
            {
                var t1 = (float)Math.Sqrt(-2 * (deltaY + 2 * CIRCLE_RADIUS + GOAL_RADIUS) / GRAVITY);
                var t2 = JUMP_PEAK_TIME - t1;
                var t3 = t2;

                if (xVel > 0)
                {
                    minX1 = goalX - xVel * t1;
                    maxX1 = goalX + xVel * (t2 + t3);
                }
                else
                {
                    maxX1 = goalX - xVel * t1;
                    minX1 = goalX + xVel * (t2 + t3);
                }
            }
            else if (deltaY + 2 * CIRCLE_RADIUS + GOAL_RADIUS < 0)
            {
                var t1 = (float)Math.Sqrt(-2 * (deltaY + 2 * CIRCLE_RADIUS + GOAL_RADIUS) / GRAVITY);
                var t2 = (float)Math.Sqrt(-2 * (deltaY - GOAL_RADIUS) / GRAVITY) - t1;
                var t3 = JUMP_PEAK_TIME - t1 - t2;

                if (xVel > 0)
                {
                    minX1 = goalX - xVel * (t1 + t2);
                    maxX1 = goalX - xVel * t1;
                    minX2 = goalX - xVel * (t1 + t2 + t3 + t3 + t2);
                    maxX2 = goalX - xVel * (t1 + t2 + t3 + t3);
                }
                else
                {
                    maxX1 = goalX - xVel * (t1 + t2);
                    minX1 = goalX - xVel * t1;
                    maxX2 = goalX - xVel * (t1 + t2 + t3 + t3 + t2);
                    minX2 = goalX - xVel * (t1 + t2 + t3 + t3);
                }
            }
            else
            {
                var t1 = (float)Math.Sqrt(2 * (deltaY + JUMP_PEAK_HEIGHT - GOAL_RADIUS) / GRAVITY);
                var t2 = (float)Math.Sqrt(2 * (deltaY + JUMP_PEAK_HEIGHT + 2 * CIRCLE_RADIUS + GOAL_RADIUS) / GRAVITY);

                if (xVel > 0)
                {
                    minX2 = goalX - xVel * (JUMP_PEAK_TIME + t1 + t2);
                    maxX2 = goalX - xVel * (JUMP_PEAK_TIME + t1);
                }
                else
                {
                    maxX2 = goalX - xVel * (JUMP_PEAK_TIME + t1 + t2);
                    minX2 = goalX - xVel * (JUMP_PEAK_TIME + t1);
                }
            }

            maxX1 = Math.Min(maxX1, fromX2);
            maxX2 = Math.Min(maxX2, fromX2);
            minX1 = Math.Max(minX1, fromX1);
            minX2 = Math.Max(minX2, fromX1);

            var valid1 = maxX1 >= minX1 && minX1 <= fromX2 && maxX1 >= fromX1;
            var valid2 = maxX2 >= minX2 && minX2 <= fromX2 && maxX2 >= fromX1;

            if (!valid1)
            {
                minX1 = 0;
                maxX1 = 0;
            }
            if (!valid2)
            {
                minX2 = 0;
                maxX2 = 0;
            }

            return valid1 || valid2;
        }

        static public bool GetPlatformRollVelocities(bool positiveVelocity, float fromX1, float fromX2, float fromY, float toX1, float toX2, float toY, out float fromXvel, out float toXvel)
        {
            var isDebug = false;

            var deltaY = toY - fromY;
            if (isDebug) Logger.Write($"ROLL?: DeltaY {deltaY}  X:{fromX1}..{fromX2} Y:{fromY}  to   X:{toX1}..{toX2} Y:{toY}");
            if (deltaY <= 0)
            {
                if (deltaY > -MAX_ROLL_UP_Y)
                {
                    if (positiveVelocity && toX1 >= fromX2 && toX1 <= fromX2 + MAX_ROLL_UP_X)
                    {
                        fromXvel = 1;
                        toXvel = MAX_X_VELOCITY;
                        return true;
                    }
                    if (!positiveVelocity && toX2 <= fromX1 && toX2 >= fromX1 - MAX_ROLL_UP_X)
                    {
                        fromXvel = -1;
                        toXvel = -MAX_X_VELOCITY;
                        return true;
                    }
                }

                if (isDebug) Logger.Write($"TOO HIGH");
                fromXvel = 0;
                toXvel = 0;
                return false;
            }
            var t1 = (float)Math.Sqrt(2 * deltaY / GRAVITY);

            if (positiveVelocity)
            {
                var fromDistance = Math.Max(CIRCLE_RADIUS, toX1 - fromX2);
                var toDistance = toX2 - fromX2;

                fromXvel = Math.Min(MAX_X_VELOCITY, fromDistance / t1);
                toXvel = Math.Min(MAX_X_VELOCITY, toDistance / t1);

                if (isDebug) Logger.Write($"t1 {t1}  DIST:{fromDistance}..{toDistance}  XVEL:{fromXvel}..{toXvel}");
                if (toXvel < fromXvel)
                {
                    if (isDebug) Logger.Write($"IMPOSSIBLE");
                    return false;
                }
            }
            else
            {
                var fromDistance = Math.Min(-CIRCLE_RADIUS, toX2 - fromX1);
                var toDistance = toX1 - fromX1;

                fromXvel = Math.Max(-MAX_X_VELOCITY, fromDistance / t1);
                toXvel = Math.Max(-MAX_X_VELOCITY, toDistance / t1);

                if (isDebug) Logger.Write($"NEGATIVE: t1 {t1}  DIST:{fromDistance}..{toDistance}  XVEL:{fromXvel}..{toXvel}");
                if (toXvel > fromXvel)
                {
                    if (isDebug) Logger.Write($"IMPOSSIBLE");
                    return false;
                }
            }

            if (isDebug) Logger.Write($"POSSIBLE");
            return true;
        }

        static public bool GetGoalRollVelocities(float fromX1, float fromX2, float fromY, float goalX, float goalY, out float fromXvel, out float toXvel)
        {
            var deltaY = goalY - fromY;
            if (deltaY <= -(2 * CIRCLE_RADIUS + GOAL_RADIUS))
            {
                fromXvel = 0;
                toXvel = 0;
                return false;
            }
            var d1 = deltaY - GOAL_RADIUS;
            var t1 = d1 > 0 ? (float)Math.Sqrt(2 * d1 / GRAVITY) : 0;
            var d2 = deltaY + GOAL_RADIUS + 2 * CIRCLE_RADIUS;
            var t2 = d2 > 0 ? (float)Math.Sqrt(2 * d2 / GRAVITY) : 0;

            if (goalX >= fromX2)
            {
                var deltaX = goalX - fromX2;
                fromXvel = Math.Max(0, (deltaX - GOAL_RADIUS) / t2);
                toXvel = t1 > 0 ? Math.Min(MAX_X_VELOCITY, (deltaX + GOAL_RADIUS) / t1) : MAX_X_VELOCITY;
                if (toXvel < fromXvel)
                    return false;
            }
            else if (goalX <= fromX1)
            {
                var deltaX = goalX - fromX1;
                fromXvel = Math.Min(0, (deltaX + GOAL_RADIUS) / t2);
                toXvel = t1 > 0 ? Math.Max(-MAX_X_VELOCITY, (deltaX - GOAL_RADIUS) / t1) : -MAX_X_VELOCITY;
                if (toXvel > fromXvel)
                    return false;
            }
            else
            {
                fromXvel = 0;
                toXvel = 0;
                return false;
            }

            return true;
        }

        static public PointF GetFirstCollisionPoint(float x, float y, float xVelocity, ObstacleGrid obstacleGrid)
        {
            float yVelocity = JUMP_VELOCITY_Y;
            float deltaTime = 0.01f;
            while (true)
            {
                var point = new PointF(x, y);
                if (obstacleGrid.IsCollision(point, CIRCLE_RADIUS))
                    return point;

                x += xVelocity * deltaTime;
                y += yVelocity * deltaTime;
                yVelocity += GRAVITY * deltaTime;
            }
        }

        #region X Velocity Data

        static bool xVelocityInitialized = false;
        static int maxXVelocityDistance, minXVelocityDistance;
        static Dictionary<int, float> xVelocityByDistance = new Dictionary<int, float>();

        static public float GetAchievableXVelocity(float xDistance)
        {
            if (!xVelocityInitialized)
                InitializeXVelocityData();

            int distance = (int)xDistance;
            distance = Math.Min(distance, maxXVelocityDistance);
            distance = Math.Max(distance, minXVelocityDistance);
            return xVelocityByDistance[distance];
        }

        static void InitializeXVelocityData()
        {
            xVelocityByDistance[-250] = -192.1123f;
            xVelocityByDistance[-249] = -188.4266f;
            xVelocityByDistance[-248] = -153.4199f;
            xVelocityByDistance[-247] = -188.4273f;
            xVelocityByDistance[-246] = -185.8396f;
            xVelocityByDistance[-245] = -184.4784f;
            xVelocityByDistance[-244] = -185.8403f;
            xVelocityByDistance[-243] = -185.4757f;
            xVelocityByDistance[-242] = -183.0136f;
            xVelocityByDistance[-241] = -185.4763f;
            xVelocityByDistance[-240] = -183.0142f;
            xVelocityByDistance[-239] = -185.477f;
            xVelocityByDistance[-238] = -185.4776f;
            xVelocityByDistance[-237] = -189.5234f;
            xVelocityByDistance[-236] = -177.8934f;
            xVelocityByDistance[-235] = -191.0013f;
            xVelocityByDistance[-234] = -177.894f;
            xVelocityByDistance[-233] = -185.3676f;
            xVelocityByDistance[-232] = -177.8946f;
            xVelocityByDistance[-231] = -185.3682f;
            xVelocityByDistance[-230] = -177.8952f;
            xVelocityByDistance[-229] = -177.8958f;
            xVelocityByDistance[-228] = -181.0729f;
            xVelocityByDistance[-227] = -177.8964f;
            xVelocityByDistance[-226] = -181.0735f;
            xVelocityByDistance[-225] = -181.0741f;
            xVelocityByDistance[-224] = -180.0798f;
            xVelocityByDistance[-223] = -181.0747f;
            xVelocityByDistance[-222] = -185.3713f;
            xVelocityByDistance[-221] = -176.034f;
            xVelocityByDistance[-220] = -185.372f;
            xVelocityByDistance[-219] = -176.0345f;
            xVelocityByDistance[-218] = -185.3726f;
            xVelocityByDistance[-217] = -176.0351f;
            xVelocityByDistance[-216] = -176.0357f;
            xVelocityByDistance[-215] = -180.0828f;
            xVelocityByDistance[-214] = -180.0834f;
            xVelocityByDistance[-213] = -182.9571f;
            xVelocityByDistance[-212] = -172.995f;
            xVelocityByDistance[-211] = -172.9953f;
            xVelocityByDistance[-210] = -172.9956f;
            xVelocityByDistance[-209] = -141.9681f;
            xVelocityByDistance[-208] = -172.9964f;
            xVelocityByDistance[-207] = -184.0117f;
            xVelocityByDistance[-206] = -172.997f;
            xVelocityByDistance[-205] = -172.9975f;
            xVelocityByDistance[-204] = -172.9977f;
            xVelocityByDistance[-203] = -172.9981f;
            xVelocityByDistance[-202] = -184.0131f;
            xVelocityByDistance[-201] = -172.9988f;
            xVelocityByDistance[-200] = -184.0137f;
            xVelocityByDistance[-199] = -172.9995f;
            xVelocityByDistance[-198] = -185.4583f;
            xVelocityByDistance[-197] = -173.8008f;
            xVelocityByDistance[-196] = -185.4589f;
            xVelocityByDistance[-195] = -173.8013f;
            xVelocityByDistance[-194] = -173.8019f;
            xVelocityByDistance[-193] = -122.8712f;
            xVelocityByDistance[-192] = -173.8025f;
            xVelocityByDistance[-191] = -175.5167f;
            xVelocityByDistance[-190] = -173.8031f;
            xVelocityByDistance[-189] = -175.5173f;
            xVelocityByDistance[-188] = -173.9886f;
            xVelocityByDistance[-187] = -173.9892f;
            xVelocityByDistance[-186] = -142.6998f;
            xVelocityByDistance[-185] = -170.5854f;
            xVelocityByDistance[-184] = -170.5856f;
            xVelocityByDistance[-183] = -170.5859f;
            xVelocityByDistance[-182] = -170.5865f;
            xVelocityByDistance[-181] = -170.5867f;
            xVelocityByDistance[-180] = -170.5871f;
            xVelocityByDistance[-179] = -170.5873f;
            xVelocityByDistance[-178] = -176.227f;
            xVelocityByDistance[-177] = -170.5878f;
            xVelocityByDistance[-176] = -176.2275f;
            xVelocityByDistance[-175] = -170.5888f;
            xVelocityByDistance[-174] = -170.5894f;
            xVelocityByDistance[-173] = -170.5895f;
            xVelocityByDistance[-172] = -170.5899f;
            xVelocityByDistance[-171] = -164.375f;
            xVelocityByDistance[-170] = -170.5905f;
            xVelocityByDistance[-169] = -170.591f;
            xVelocityByDistance[-168] = -170.5912f;
            xVelocityByDistance[-167] = -170.5918f;
            xVelocityByDistance[-166] = -177.6425f;
            xVelocityByDistance[-165] = -170.5923f;
            xVelocityByDistance[-164] = -177.9336f;
            xVelocityByDistance[-163] = -174.6309f;
            xVelocityByDistance[-162] = -177.8902f;
            xVelocityByDistance[-161] = -168.2083f;
            xVelocityByDistance[-160] = -168.2089f;
            xVelocityByDistance[-159] = -168.2091f;
            xVelocityByDistance[-158] = -168.2097f;
            xVelocityByDistance[-157] = -168.2098f;
            xVelocityByDistance[-156] = -168.2102f;
            xVelocityByDistance[-155] = -139.062f;
            xVelocityByDistance[-154] = -168.2108f;
            xVelocityByDistance[-153] = -168.2114f;
            xVelocityByDistance[-152] = -168.2115f;
            xVelocityByDistance[-151] = -168.2121f;
            xVelocityByDistance[-150] = -170.6694f;
            xVelocityByDistance[-149] = -168.2127f;
            xVelocityByDistance[-148] = -170.4156f;
            xVelocityByDistance[-147] = -168.2134f;
            xVelocityByDistance[-146] = -146.7432f;
            xVelocityByDistance[-145] = -168.214f;
            xVelocityByDistance[-144] = -147.8077f;
            xVelocityByDistance[-143] = -156.6792f;
            xVelocityByDistance[-142] = -156.6798f;
            xVelocityByDistance[-141] = -141.9893f;
            xVelocityByDistance[-140] = -156.6803f;
            xVelocityByDistance[-139] = -156.6808f;
            xVelocityByDistance[-138] = -156.6812f;
            xVelocityByDistance[-137] = -156.6813f;
            xVelocityByDistance[-136] = -138.8719f;
            xVelocityByDistance[-135] = -156.6822f;
            xVelocityByDistance[-134] = -156.6827f;
            xVelocityByDistance[-133] = -158.9324f;
            xVelocityByDistance[-132] = -159.5564f;
            xVelocityByDistance[-131] = -137.9929f;
            xVelocityByDistance[-130] = -150.5944f;
            xVelocityByDistance[-129] = -150.5949f;
            xVelocityByDistance[-128] = -132.4671f;
            xVelocityByDistance[-127] = -150.5954f;
            xVelocityByDistance[-126] = -148.5599f;
            xVelocityByDistance[-125] = -148.5341f;
            xVelocityByDistance[-124] = -148.5605f;
            xVelocityByDistance[-123] = -134.343f;
            xVelocityByDistance[-122] = -140.6001f;
            xVelocityByDistance[-121] = -140.6006f;
            xVelocityByDistance[-120] = -128.0374f;
            xVelocityByDistance[-119] = -145.8347f;
            xVelocityByDistance[-118] = -145.8351f;
            xVelocityByDistance[-117] = -128.0385f;
            xVelocityByDistance[-116] = -145.8356f;
            xVelocityByDistance[-115] = -145.8361f;
            xVelocityByDistance[-114] = -145.8366f;
            xVelocityByDistance[-113] = -144.3016f;
            xVelocityByDistance[-112] = -139.0482f;
            xVelocityByDistance[-111] = -139.0487f;
            xVelocityByDistance[-110] = -138.8276f;
            xVelocityByDistance[-109] = -139.0492f;
            xVelocityByDistance[-108] = -136.098f;
            xVelocityByDistance[-107] = -130.1106f;
            xVelocityByDistance[-106] = -136.0985f;
            xVelocityByDistance[-105] = -136.099f;
            xVelocityByDistance[-104] = -138.8299f;
            xVelocityByDistance[-103] = -141.5767f;
            xVelocityByDistance[-102] = -135.1632f;
            xVelocityByDistance[-101] = -135.1637f;
            xVelocityByDistance[-100] = -139.2912f;
            xVelocityByDistance[-99] = -135.164f;
            xVelocityByDistance[-98] = -139.9871f;
            xVelocityByDistance[-97] = -139.2921f;
            xVelocityByDistance[-96] = -133.21f;
            xVelocityByDistance[-95] = -133.2104f;
            xVelocityByDistance[-94] = -126.698f;
            xVelocityByDistance[-93] = -128.4691f;
            xVelocityByDistance[-92] = -128.4696f;
            xVelocityByDistance[-91] = -128.47f;
            xVelocityByDistance[-90] = -128.4704f;
            xVelocityByDistance[-89] = -128.1315f;
            xVelocityByDistance[-88] = -121.3374f;
            xVelocityByDistance[-87] = -121.3378f;
            xVelocityByDistance[-86] = -121.3384f;
            xVelocityByDistance[-85] = -105.0832f;
            xVelocityByDistance[-84] = -124.0848f;
            xVelocityByDistance[-83] = -120.1869f;
            xVelocityByDistance[-82] = -120.1873f;
            xVelocityByDistance[-81] = -120.1877f;
            xVelocityByDistance[-80] = -100.7649f;
            xVelocityByDistance[-79] = -124.2535f;
            xVelocityByDistance[-78] = -120.3462f;
            xVelocityByDistance[-77] = -120.3466f;
            xVelocityByDistance[-76] = -120.347f;
            xVelocityByDistance[-75] = -124.37f;
            xVelocityByDistance[-74] = -124.3704f;
            xVelocityByDistance[-73] = -124.3708f;
            xVelocityByDistance[-72] = -124.3712f;
            xVelocityByDistance[-71] = -124.3717f;
            xVelocityByDistance[-70] = -124.1998f;
            xVelocityByDistance[-69] = -124.3721f;
            xVelocityByDistance[-68] = -116.5977f;
            xVelocityByDistance[-67] = -116.5981f;
            xVelocityByDistance[-66] = -116.5985f;
            xVelocityByDistance[-65] = -118.5774f;
            xVelocityByDistance[-64] = -118.5778f;
            xVelocityByDistance[-63] = -110.5963f;
            xVelocityByDistance[-62] = -110.5967f;
            xVelocityByDistance[-61] = -110.5971f;
            xVelocityByDistance[-60] = -107.003f;
            xVelocityByDistance[-59] = -94.19148f;
            xVelocityByDistance[-58] = -107.0033f;
            xVelocityByDistance[-57] = -107.0037f;
            xVelocityByDistance[-56] = -107.004f;
            xVelocityByDistance[-55] = -107.0044f;
            xVelocityByDistance[-54] = -103.1514f;
            xVelocityByDistance[-53] = -103.1517f;
            xVelocityByDistance[-52] = -103.1521f;
            xVelocityByDistance[-51] = -103.1524f;
            xVelocityByDistance[-50] = -105.878f;
            xVelocityByDistance[-49] = -105.8784f;
            xVelocityByDistance[-48] = -105.8787f;
            xVelocityByDistance[-47] = -104.7983f;
            xVelocityByDistance[-46] = -105.8791f;
            xVelocityByDistance[-45] = -105.8794f;
            xVelocityByDistance[-44] = -97.45233f;
            xVelocityByDistance[-43] = -97.45266f;
            xVelocityByDistance[-42] = -97.45299f;
            xVelocityByDistance[-41] = -99.24011f;
            xVelocityByDistance[-40] = -90.1289f;
            xVelocityByDistance[-39] = -92.995f;
            xVelocityByDistance[-38] = -92.99532f;
            xVelocityByDistance[-37] = -92.99563f;
            xVelocityByDistance[-36] = -92.99594f;
            xVelocityByDistance[-35] = -82.20198f;
            xVelocityByDistance[-34] = -84.9912f;
            xVelocityByDistance[-33] = -84.99177f;
            xVelocityByDistance[-32] = -84.99205f;
            xVelocityByDistance[-31] = -84.99233f;
            xVelocityByDistance[-30] = -77.00327f;
            xVelocityByDistance[-29] = -77.00352f;
            xVelocityByDistance[-28] = -74.14907f;
            xVelocityByDistance[-27] = -74.14931f;
            xVelocityByDistance[-26] = -76.42004f;
            xVelocityByDistance[-25] = -71.64719f;
            xVelocityByDistance[-24] = -71.64767f;
            xVelocityByDistance[-23] = -71.6479f;
            xVelocityByDistance[-22] = -68.87601f;
            xVelocityByDistance[-21] = -72.22713f;
            xVelocityByDistance[-20] = -72.22737f;
            xVelocityByDistance[-19] = -69.29703f;
            xVelocityByDistance[-18] = -73.65787f;
            xVelocityByDistance[-17] = -73.65836f;
            xVelocityByDistance[-16] = -72.59757f;
            xVelocityByDistance[-15] = -69.68943f;
            xVelocityByDistance[-14] = -69.92046f;
            xVelocityByDistance[-13] = -69.92093f;
            xVelocityByDistance[-12] = -67.37728f;
            xVelocityByDistance[-11] = -63.59456f;
            xVelocityByDistance[-10] = -62.03841f;
            xVelocityByDistance[-9] = -62.03882f;
            xVelocityByDistance[-8] = -57.16184f;
            xVelocityByDistance[-7] = -57.16216f;
            xVelocityByDistance[-6] = -53.64109f;
            xVelocityByDistance[-5] = -51.8199f;
            xVelocityByDistance[-4] = -50.57573f;
            xVelocityByDistance[-3] = -45.80175f;
            xVelocityByDistance[-2] = -48.42749f;
            xVelocityByDistance[-1] = -40.32453f;
            xVelocityByDistance[0] = 0;
            xVelocityByDistance[1] = 25.86238f;
            xVelocityByDistance[2] = 29.43263f;
            xVelocityByDistance[3] = 34.34068f;
            xVelocityByDistance[4] = 38.39024f;
            xVelocityByDistance[5] = 40.47731f;
            xVelocityByDistance[6] = 46.83772f;
            xVelocityByDistance[7] = 46.83725f;
            xVelocityByDistance[8] = 51.8934f;
            xVelocityByDistance[9] = 49.7816f;
            xVelocityByDistance[10] = 52.93227f;
            xVelocityByDistance[11] = 56.47262f;
            xVelocityByDistance[12] = 56.47243f;
            xVelocityByDistance[13] = 60.89401f;
            xVelocityByDistance[14] = 60.89361f;
            xVelocityByDistance[15] = 69.78557f;
            xVelocityByDistance[16] = 69.78511f;
            xVelocityByDistance[17] = 66.84027f;
            xVelocityByDistance[18] = 70.86069f;
            xVelocityByDistance[19] = 70.86022f;
            xVelocityByDistance[20] = 78.89657f;
            xVelocityByDistance[21] = 78.89631f;
            xVelocityByDistance[22] = 78.89605f;
            xVelocityByDistance[23] = 75.54391f;
            xVelocityByDistance[24] = 87.22956f;
            xVelocityByDistance[25] = 83.19453f;
            xVelocityByDistance[26] = 83.19416f;
            xVelocityByDistance[27] = 83.19389f;
            xVelocityByDistance[28] = 83.19361f;
            xVelocityByDistance[29] = 83.19324f;
            xVelocityByDistance[30] = 92.43633f;
            xVelocityByDistance[31] = 92.43603f;
            xVelocityByDistance[32] = 92.43542f;
            xVelocityByDistance[33] = 89.60488f;
            xVelocityByDistance[34] = 93.34247f;
            xVelocityByDistance[35] = 93.34216f;
            xVelocityByDistance[36] = 93.34184f;
            xVelocityByDistance[37] = 93.34153f;
            xVelocityByDistance[38] = 89.87167f;
            xVelocityByDistance[39] = 99.43031f;
            xVelocityByDistance[40] = 97.31199f;
            xVelocityByDistance[41] = 97.31166f;
            xVelocityByDistance[42] = 97.31133f;
            xVelocityByDistance[43] = 97.311f;
            xVelocityByDistance[44] = 97.58598f;
            xVelocityByDistance[45] = 97.58565f;
            xVelocityByDistance[46] = 97.58532f;
            xVelocityByDistance[47] = 97.58501f;
            xVelocityByDistance[48] = 97.58469f;
            xVelocityByDistance[49] = 97.46148f;
            xVelocityByDistance[50] = 97.46115f;
            xVelocityByDistance[51] = 97.46082f;
            xVelocityByDistance[52] = 97.46049f;
            xVelocityByDistance[53] = 105.8038f;
            xVelocityByDistance[54] = 104.0839f;
            xVelocityByDistance[55] = 99.94526f;
            xVelocityByDistance[56] = 99.94494f;
            xVelocityByDistance[57] = 99.9446f;
            xVelocityByDistance[58] = 99.94428f;
            xVelocityByDistance[59] = 102.3958f;
            xVelocityByDistance[60] = 102.3954f;
            xVelocityByDistance[61] = 102.3951f;
            xVelocityByDistance[62] = 102.3947f;
            xVelocityByDistance[63] = 102.3944f;
            xVelocityByDistance[64] = 102.394f;
            xVelocityByDistance[65] = 107.9397f;
            xVelocityByDistance[66] = 107.9393f;
            xVelocityByDistance[67] = 113.1311f;
            xVelocityByDistance[68] = 106.9504f;
            xVelocityByDistance[69] = 106.95f;
            xVelocityByDistance[70] = 111.3528f;
            xVelocityByDistance[71] = 106.9497f;
            xVelocityByDistance[72] = 113.6137f;
            xVelocityByDistance[73] = 109.7861f;
            xVelocityByDistance[74] = 109.7857f;
            xVelocityByDistance[75] = 109.7854f;
            xVelocityByDistance[76] = 109.7851f;
            xVelocityByDistance[77] = 117.624f;
            xVelocityByDistance[78] = 117.1647f;
            xVelocityByDistance[79] = 117.1643f;
            xVelocityByDistance[80] = 123.8899f;
            xVelocityByDistance[81] = 123.8895f;
            xVelocityByDistance[82] = 131.5804f;
            xVelocityByDistance[83] = 127.591f;
            xVelocityByDistance[84] = 127.5906f;
            xVelocityByDistance[85] = 127.5902f;
            xVelocityByDistance[86] = 127.5897f;
            xVelocityByDistance[87] = 116.759f;
            xVelocityByDistance[88] = 134.4561f;
            xVelocityByDistance[89] = 134.4556f;
            xVelocityByDistance[90] = 134.4552f;
            xVelocityByDistance[91] = 116.7577f;
            xVelocityByDistance[92] = 134.4547f;
            xVelocityByDistance[93] = 127.2301f;
            xVelocityByDistance[94] = 127.2297f;
            xVelocityByDistance[95] = 116.9102f;
            xVelocityByDistance[96] = 127.2293f;
            xVelocityByDistance[97] = 132.4834f;
            xVelocityByDistance[98] = 132.4829f;
            xVelocityByDistance[99] = 113.369f;
            xVelocityByDistance[100] = 132.4825f;
            xVelocityByDistance[101] = 130.8957f;
            xVelocityByDistance[102] = 132.4816f;
            xVelocityByDistance[103] = 138.0323f;
            xVelocityByDistance[104] = 138.0318f;
            xVelocityByDistance[105] = 132.4807f;
            xVelocityByDistance[106] = 138.0314f;
            xVelocityByDistance[107] = 138.0309f;
            xVelocityByDistance[108] = 141.0162f;
            xVelocityByDistance[109] = 128.5356f;
            xVelocityByDistance[110] = 141.0157f;
            xVelocityByDistance[111] = 148.2729f;
            xVelocityByDistance[112] = 152.2352f;
            xVelocityByDistance[113] = 135.6896f;
            xVelocityByDistance[114] = 135.6892f;
            xVelocityByDistance[115] = 135.6887f;
            xVelocityByDistance[116] = 157.3977f;
            xVelocityByDistance[117] = 135.6882f;
            xVelocityByDistance[118] = 140.3318f;
            xVelocityByDistance[119] = 129.451f;
            xVelocityByDistance[120] = 135.82f;
            xVelocityByDistance[121] = 137.8613f;
            xVelocityByDistance[122] = 131.319f;
            xVelocityByDistance[123] = 137.8608f;
            xVelocityByDistance[124] = 137.8604f;
            xVelocityByDistance[125] = 137.8599f;
            xVelocityByDistance[126] = 131.3177f;
            xVelocityByDistance[127] = 137.8594f;
            xVelocityByDistance[128] = 137.859f;
            xVelocityByDistance[129] = 141.1293f;
            xVelocityByDistance[130] = 137.8585f;
            xVelocityByDistance[131] = 137.8581f;
            xVelocityByDistance[132] = 137.8576f;
            xVelocityByDistance[133] = 141.1279f;
            xVelocityByDistance[134] = 117.7262f;
            xVelocityByDistance[135] = 136.9584f;
            xVelocityByDistance[136] = 134.6477f;
            xVelocityByDistance[137] = 134.6472f;
            xVelocityByDistance[138] = 134.6468f;
            xVelocityByDistance[139] = 136.9571f;
            xVelocityByDistance[140] = 134.6464f;
            xVelocityByDistance[141] = 139.194f;
            xVelocityByDistance[142] = 139.1936f;
            xVelocityByDistance[143] = 144.4613f;
            xVelocityByDistance[144] = 139.1931f;
            xVelocityByDistance[145] = 133.7738f;
            xVelocityByDistance[146] = 147.0093f;
            xVelocityByDistance[147] = 133.7735f;
            xVelocityByDistance[148] = 133.773f;
            xVelocityByDistance[149] = 133.7726f;
            xVelocityByDistance[150] = 150.5375f;
            xVelocityByDistance[151] = 133.7724f;
            xVelocityByDistance[152] = 142.0988f;
            xVelocityByDistance[153] = 150.5365f;
            xVelocityByDistance[154] = 142.0983f;
            xVelocityByDistance[155] = 138.3378f;
            xVelocityByDistance[156] = 140.4733f;
            xVelocityByDistance[157] = 138.3374f;
            xVelocityByDistance[158] = 138.3369f;
            xVelocityByDistance[159] = 142.0964f;
            xVelocityByDistance[160] = 138.3364f;
            xVelocityByDistance[161] = 145.5833f;
            xVelocityByDistance[162] = 150.3035f;
            xVelocityByDistance[163] = 145.5828f;
            xVelocityByDistance[164] = 145.5823f;
            xVelocityByDistance[165] = 137.6712f;
            xVelocityByDistance[166] = 145.5813f;
            xVelocityByDistance[167] = 145.5805f;
            xVelocityByDistance[168] = 153.1278f;
            xVelocityByDistance[169] = 145.58f;
            xVelocityByDistance[170] = 145.5796f;
            xVelocityByDistance[171] = 145.5798f;
            xVelocityByDistance[172] = 145.5791f;
            xVelocityByDistance[173] = 145.5786f;
            xVelocityByDistance[174] = 158.4754f;
            xVelocityByDistance[175] = 149.8929f;
            xVelocityByDistance[176] = 158.4688f;
            xVelocityByDistance[177] = 149.8924f;
            xVelocityByDistance[178] = 149.8916f;
            xVelocityByDistance[179] = 156.8614f;
            xVelocityByDistance[180] = 149.8911f;
            xVelocityByDistance[181] = 149.8902f;
            xVelocityByDistance[182] = 153.8242f;
            xVelocityByDistance[183] = 149.8897f;
            xVelocityByDistance[184] = 149.8892f;
            xVelocityByDistance[185] = 156.3466f;
            xVelocityByDistance[186] = 149.8887f;
            xVelocityByDistance[187] = 149.8882f;
            xVelocityByDistance[188] = 149.8878f;
            xVelocityByDistance[189] = 149.8873f;
            xVelocityByDistance[190] = 162.1497f;
            xVelocityByDistance[191] = 154.2764f;
            xVelocityByDistance[192] = 154.2759f;
            xVelocityByDistance[193] = 154.2757f;
            xVelocityByDistance[194] = 154.2752f;
            xVelocityByDistance[195] = 154.2749f;
            xVelocityByDistance[196] = 154.2746f;
            xVelocityByDistance[197] = 154.2741f;
            xVelocityByDistance[198] = 147.2792f;
            xVelocityByDistance[199] = 154.2732f;
            xVelocityByDistance[200] = 162.8409f;
            xVelocityByDistance[201] = 160.0576f;
            xVelocityByDistance[202] = 160.057f;
            xVelocityByDistance[203] = 163.3034f;
            xVelocityByDistance[204] = 157.006f;
            xVelocityByDistance[205] = 160.056f;
            xVelocityByDistance[206] = 157.0055f;
            xVelocityByDistance[207] = 157.005f;
            xVelocityByDistance[208] = 134.8068f;
            xVelocityByDistance[209] = 157.0042f;
            xVelocityByDistance[210] = 162.4679f;
            xVelocityByDistance[211] = 163.9621f;
            xVelocityByDistance[212] = 158.3868f;
            xVelocityByDistance[213] = 146.6619f;
            xVelocityByDistance[214] = 158.3862f;
            xVelocityByDistance[215] = 158.3857f;
            xVelocityByDistance[216] = 146.6609f;
            xVelocityByDistance[217] = 158.3851f;
            xVelocityByDistance[218] = 162.4652f;
            xVelocityByDistance[219] = 158.3846f;
            xVelocityByDistance[220] = 158.3841f;
            xVelocityByDistance[221] = 158.3836f;
            xVelocityByDistance[222] = 163.9355f;
            xVelocityByDistance[223] = 149.2271f;
            xVelocityByDistance[224] = 160.5396f;
            xVelocityByDistance[225] = 164.9907f;
            xVelocityByDistance[226] = 160.5391f;
            xVelocityByDistance[227] = 160.5382f;
            xVelocityByDistance[228] = 151.9505f;
            xVelocityByDistance[229] = 160.5377f;
            xVelocityByDistance[230] = 160.5373f;
            xVelocityByDistance[231] = 160.5372f;
            xVelocityByDistance[232] = 160.5366f;
            xVelocityByDistance[233] = 143.4222f;
            xVelocityByDistance[234] = 160.5358f;
            xVelocityByDistance[235] = 160.5356f;
            xVelocityByDistance[236] = 160.5352f;
            xVelocityByDistance[237] = 160.7865f;
            xVelocityByDistance[238] = 184.7374f;
            xVelocityByDistance[239] = 163.0118f;
            xVelocityByDistance[240] = 170.1928f;
            xVelocityByDistance[241] = 163.0112f;
            xVelocityByDistance[242] = 159.9663f;
            xVelocityByDistance[243] = 159.2562f;
            xVelocityByDistance[244] = 159.9656f;
            xVelocityByDistance[245] = 170.5959f;
            xVelocityByDistance[246] = 159.965f;
            xVelocityByDistance[247] = 159.9645f;
            xVelocityByDistance[248] = 173.5615f;
            xVelocityByDistance[249] = 159.964f;
            xVelocityByDistance[250] = 159.9637f;

            for (int i = 0; i < 250; i++)
                xVelocityByDistance[i + 1] = Math.Max(xVelocityByDistance[i], xVelocityByDistance[i + 1]);
            for (int i = 0; i > -250; i--)
                xVelocityByDistance[i - 1] = Math.Min(xVelocityByDistance[i], xVelocityByDistance[i - 1]);

            minXVelocityDistance = xVelocityByDistance.Keys.Min();
            maxXVelocityDistance = xVelocityByDistance.Keys.Max();

            if (xVelocityByDistance.Count != 501)
                throw new ApplicationException(string.Format("Only got {0} xVel samples!", xVelocityByDistance.Count));

            xVelocityInitialized = true;
        }

        #endregion
    }
}
