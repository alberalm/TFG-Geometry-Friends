using System;
using System.Collections.Generic;

using System.Threading.Tasks;

namespace GeometryFriendsAgents
{
    public class Platform
    {
        public enum collideType
        {
            CEILING, FLOOR, OTHER
        };

        public enum movementType
        {
            NO_ACTION, STAIR_GAP, FALL, JUMP
        };
         
        public const int VELOCITYX_STEP = 20;

        private const float TIME_STEP = 0.01f;

        private int[] LENGTH_TO_ACCELERATE = new int[10] { 1, 5, 13, 20, 31, 49, 70, 95, 128, 166 };

        private const int STAIR_MAXWIDTH = 48;
        private const int STAIR_MAXHEIGHT = 16;

        public struct PlatformInfo
        {
            public int id;
            public int height;
            public int leftEdge;
            public int rightEdge;
            public List<MoveInfo> moveInfoList;

            public PlatformInfo(int id, int height, int leftEdge, int rightEdge, List<MoveInfo> moveInfoList)
            {
                this.id = id;
                this.height = height;
                this.leftEdge = leftEdge;
                this.rightEdge = rightEdge;
                this.moveInfoList = moveInfoList;
            }
        }

        public struct MoveInfo
        {
            public PlatformInfo reachablePlatform;
            public LevelArray.Point movePoint;
            public LevelArray.Point landPoint;
            public int velocityX;
            public bool rightMove;
            public movementType movementType;
            public bool[] collectibles_onPath;
            public int pathLength;
            public bool collideCeiling;

            public MoveInfo(PlatformInfo reachablePlatform, LevelArray.Point movePoint, LevelArray.Point landPoint, int velocityX, bool rightMove, movementType movementType, bool[] collectibles_onPath, int pathLength, bool collideCeiling)
            {
                this.reachablePlatform = reachablePlatform;
                this.movePoint = movePoint;
                this.landPoint = landPoint;
                this.velocityX = velocityX;
                this.rightMove = rightMove;
                this.movementType = movementType;
                this.collectibles_onPath = collectibles_onPath;
                this.pathLength = pathLength;
                this.collideCeiling = collideCeiling;
            }
        }

        private List<PlatformInfo> platformInfoList;

        public Platform()
        {
            platformInfoList = new List<PlatformInfo>();
        }

        public void SetUp(int[,] levelArray, int numCollectibles)
        {
            SetPlatformInfoList(levelArray);
            SetMoveInfoList(levelArray, numCollectibles);
        }

        public void SetPlatformInfoList(int[,] levelArray)
        {
            int[,] platformArray = new int[levelArray.GetLength(0), levelArray.GetLength(1)];

            for (int i = 0; i < levelArray.GetLength(0); i++)
            {
                Parallel.For(0, levelArray.GetLength(1), j =>
                {
                    LevelArray.Point circleCenter = LevelArray.ConvertArrayPointIntoPoint(new LevelArray.ArrayPoint(j, i));
                    circleCenter.y -= GameInfo.CIRCLE_RADIUS;

                    List<LevelArray.ArrayPoint> circlePixels = GetCirclePixels(circleCenter);

                    if (!IsObstacle_onPixels(levelArray, circlePixels))
                    {
                        if (levelArray[i, j - 1] == LevelArray.OBSTACLE || levelArray[i, j] == LevelArray.OBSTACLE)
                        {
                            platformArray[i, j] = LevelArray.OBSTACLE;
                        }
                    }
                });
            }

            Parallel.For(0, levelArray.GetLength(0), i =>
            {
                bool platformFlag = false;
                int height = 0, leftEdge = 0, rightEdge = 0;

                for (int j = 0; j < platformArray.GetLength(1); j++)
                {
                    if (platformArray[i, j] == LevelArray.OBSTACLE && !platformFlag)
                    {
                        height = LevelArray.ConvertValue_ArrayPointIntoPoint(i);
                        leftEdge = LevelArray.ConvertValue_ArrayPointIntoPoint(j);
                        platformFlag = true;
                    }

                    if (platformArray[i, j] == LevelArray.OPEN && platformFlag)
                    {
                        rightEdge = LevelArray.ConvertValue_ArrayPointIntoPoint(j - 1);

                        if (rightEdge >= leftEdge)
                        {
                            lock (platformInfoList)
                            {
                                platformInfoList.Add(new PlatformInfo(0, height, leftEdge, rightEdge, new List<MoveInfo>()));
                            }
                        }

                        platformFlag = false;
                    }
                }
            });

            SetPlatformID();  
        }

        public void SetPlatformID()
        {
            platformInfoList.Sort((a, b) => {
                int result = a.height - b.height;
                return result != 0 ? result : a.leftEdge - b.leftEdge;
            });

            Parallel.For(0, platformInfoList.Count, i =>
            {
               PlatformInfo tempPlatfom = platformInfoList[i];
               tempPlatfom.id = i + 1;
               platformInfoList[i] = tempPlatfom;
            });
        }

        public void SetMoveInfoList(int[,] levelArray, int numCollectibles)
        {
            SetMoveInfoList_StairOrGap(levelArray, numCollectibles);

            foreach (PlatformInfo i in platformInfoList)
            {
                movementType movementType;

                movementType = movementType.JUMP;

                int from = i.leftEdge + (i.leftEdge - GameInfo.LEVEL_ORIGINAL) % (LevelArray.PIXEL_LENGTH * 2);
                int to = i.rightEdge - (i.rightEdge - GameInfo.LEVEL_ORIGINAL) % (LevelArray.PIXEL_LENGTH * 2);

                Parallel.For(0, (to - from) / (LevelArray.PIXEL_LENGTH * 2) + 1, j =>
                {   
                    LevelArray.Point movePoint;
                                        
                    movePoint = new LevelArray.Point(from + j * LevelArray.PIXEL_LENGTH * 2, i.height - GameInfo.CIRCLE_RADIUS);
                    
                    SetMoveInfoList_NoAction(levelArray, i, movePoint, numCollectibles);

                    Parallel.For(0, (GameInfo.MAX_VELOCITYX / VELOCITYX_STEP), k =>
                    {
                        bool rightMove;
                        int velocityX;

                        velocityX = VELOCITYX_STEP * k;

                        rightMove = true;

                        SetMoveInfoList_JumpOrFall(levelArray, i, movePoint, velocityX, rightMove, movementType, numCollectibles);

                        rightMove = false;

                        SetMoveInfoList_JumpOrFall(levelArray, i, movePoint, velocityX, rightMove, movementType, numCollectibles);
                    });
                     
                });

                movementType = movementType.FALL;
                
                Parallel.For(0, 10, k =>
                {
                    LevelArray.Point movePoint;
                    bool rightMove;
                    int velocityX;

                    velocityX = VELOCITYX_STEP * k;

                    movePoint = new LevelArray.Point(i.leftEdge - LevelArray.PIXEL_LENGTH, i.height - GameInfo.CIRCLE_RADIUS);
                    
                    rightMove = false;

                    SetMoveInfoList_JumpOrFall(levelArray, i, movePoint, velocityX, rightMove, movementType, numCollectibles);

                    movePoint = new LevelArray.Point(i.rightEdge + LevelArray.PIXEL_LENGTH, i.height - GameInfo.CIRCLE_RADIUS);

                    rightMove = true;

                    SetMoveInfoList_JumpOrFall(levelArray, i, movePoint, velocityX, rightMove, movementType, numCollectibles);

                });
            }
        }

        
        private void SetMoveInfoList_StairOrGap(int[,] levelArray, int numCollectibles)
        {
            foreach (PlatformInfo i in platformInfoList)
            {
                foreach (PlatformInfo j in platformInfoList)
                {
                    if (i.Equals(j))
                    {
                        continue;
                    }

                    bool rightMove = false;

                    if (!IsStairOrGap(i, j, ref rightMove))
                    {
                        continue;
                    }

                    bool obstacleFlag = false;
                    bool[] collectible_onPath = new bool[numCollectibles];

                    int from = rightMove ? i.rightEdge : j.rightEdge;
                    int to = rightMove ? j.leftEdge : i.leftEdge;

                    for (int k = from; k <= to; k += LevelArray.PIXEL_LENGTH)
                    {
                        List<LevelArray.ArrayPoint> circlePixels = GetCirclePixels(new LevelArray.Point(k, j.height - GameInfo.CIRCLE_RADIUS));

                        if (IsObstacle_onPixels(levelArray, circlePixels))
                        {
                            obstacleFlag = true;
                            break;
                        }

                        collectible_onPath = GetCollectibles_onPixels(levelArray, circlePixels, numCollectibles);
                    }

                    if (!obstacleFlag)
                    {
                        LevelArray.Point movePoint = rightMove ? new LevelArray.Point(i.rightEdge, i.height) : new LevelArray.Point(i.leftEdge, i.height);
                        LevelArray.Point landPoint = rightMove ? new LevelArray.Point(j.leftEdge, j.height) : new LevelArray.Point(j.rightEdge, j.height);

                        AddMoveInfoToList(i, new MoveInfo(j, movePoint, landPoint, 0, rightMove, movementType.STAIR_GAP, collectible_onPath, (i.height - j.height) + Math.Abs(movePoint.x - landPoint.x), false));
                    }
                }
            }
        }

        private bool IsStairOrGap(PlatformInfo fromPlatform, PlatformInfo toPlatform, ref bool rightMove)
        {
            if (0 <= toPlatform.leftEdge - fromPlatform.rightEdge && toPlatform.leftEdge - fromPlatform.rightEdge <= STAIR_MAXWIDTH)
            {
                if (0 <= (fromPlatform.height - toPlatform.height) && (fromPlatform.height - toPlatform.height) <= STAIR_MAXHEIGHT)
                {
                    rightMove = true;
                    return true;
                }
            }

            if (0 <= fromPlatform.leftEdge - toPlatform.rightEdge && fromPlatform.leftEdge - toPlatform.rightEdge <= STAIR_MAXWIDTH)
            {
                if (0 <= (fromPlatform.height - toPlatform.height) && (fromPlatform.height - toPlatform.height) <= STAIR_MAXHEIGHT)
                {
                    rightMove = false;
                    return true;
                }
            }

            return false;
        }

        private void SetMoveInfoList_NoAction(int[,] levelArray, PlatformInfo fromPlatform, LevelArray.Point movePoint, int numCollectibles)
        {
            List<LevelArray.ArrayPoint> circlePixels = GetCirclePixels(movePoint);

            bool[] collectible_onPath = new bool[numCollectibles];

            collectible_onPath = GetCollectibles_onPixels(levelArray, circlePixels, collectible_onPath.Length);

            AddMoveInfoToList(fromPlatform, new MoveInfo(fromPlatform, movePoint, movePoint, 0, true, movementType.NO_ACTION, collectible_onPath, 0, false));
        }

        private void SetMoveInfoList_JumpOrFall(int[,] levelArray, PlatformInfo fromPlatform, LevelArray.Point movePoint, int velocityX, bool rightMove, movementType movementType, int numCollectibles)
        {
            if (!IsEnoughLengthToAccelerate(fromPlatform, movePoint, velocityX, rightMove))
            {
                return;
            }

            bool[] collectible_onPath = new bool[numCollectibles];
            float pathLength = 0;

            LevelArray.Point collidePoint = movePoint;
            LevelArray.Point prevCollidePoint;

            collideType collideType = collideType.OTHER;
            float collideVelocityX = rightMove ? velocityX : -velocityX;
            float collideVelocityY = (movementType == movementType.JUMP) ? GameInfo.JUMP_VELOCITYY : GameInfo.FALL_VELOCITYY;
            bool collideCeiling = false;

            do
            {
                prevCollidePoint = collidePoint;

                GetPathInfo(levelArray, collidePoint, collideVelocityX, collideVelocityY,
                    ref collidePoint, ref collideType, ref collideVelocityX, ref collideVelocityY, ref collectible_onPath, ref pathLength);

                if (collideType == collideType.CEILING)
                {
                    collideCeiling = true;
                }

                if (prevCollidePoint.Equals(collidePoint))
                {
                    break;
                }
            } 
            while (!(collideType == collideType.FLOOR));

            if (collideType == collideType.FLOOR)
            {
                PlatformInfo? toPlatform = GetPlatform_onCircle(collidePoint);

                if (toPlatform.HasValue)
                {
                    if (movementType == movementType.FALL)
                    {
                        movePoint.x = rightMove ? movePoint.x - LevelArray.PIXEL_LENGTH : movePoint.x + LevelArray.PIXEL_LENGTH;
                    }

                    AddMoveInfoToList(fromPlatform, new MoveInfo(toPlatform.Value, movePoint, collidePoint, velocityX, rightMove, movementType, collectible_onPath, (int)pathLength, collideCeiling));
                }
            }
        }

        private bool IsEnoughLengthToAccelerate(PlatformInfo fromPlatform, LevelArray.Point movePoint, int velocityX, bool rightMove)
        {
            int neededLengthToAccelerate;

            neededLengthToAccelerate = LENGTH_TO_ACCELERATE[velocityX / VELOCITYX_STEP];

            if (rightMove)
            {
                if (movePoint.x - fromPlatform.leftEdge < neededLengthToAccelerate)
                {
                    return false;
                }
            }
            else
            {
                if (fromPlatform.rightEdge - movePoint.x < neededLengthToAccelerate)
                {
                    return false;
                }
            }

            return true;
        }

        private void GetPathInfo(int[,] levelArray, LevelArray.Point movePoint, float velocityX, float velocityY, 
            ref LevelArray.Point collidePoint, ref collideType collideType, ref float collideVelocityX, ref float collideVelocityY, ref bool[] collectible_onPath, ref float pathLength)
        {
            LevelArray.Point previousCircleCenter;
            LevelArray.Point currentCircleCenter = movePoint;
            
            for (int i = 1; true; i++)
            {
                float currentTime = i * TIME_STEP;

                previousCircleCenter = currentCircleCenter;
                currentCircleCenter = GetCurrentCircleCenter(movePoint, velocityX, velocityY, currentTime);
                List<LevelArray.ArrayPoint> circlePixels = GetCirclePixels(currentCircleCenter);

                if (IsObstacle_onPixels(levelArray, circlePixels))
                {
                    collidePoint = previousCircleCenter;
                    collideType = GetCollideType(levelArray, currentCircleCenter, velocityY - GameInfo.GRAVITY * (i - 1) * TIME_STEP >= 0, velocityX > 0);

                    if (collideType == collideType.CEILING)
                    {
                        collideVelocityX = velocityX / 3;
                        collideVelocityY = - (velocityY - GameInfo.GRAVITY * (i - 1) * TIME_STEP) / 3;
                    }
                    else
                    {
                        collideVelocityX = 0;
                        collideVelocityY = 0;
                    }

                    return;
                }

                collectible_onPath = Utilities.GetOrMatrix(collectible_onPath, GetCollectibles_onPixels(levelArray, circlePixels, collectible_onPath.Length));

                pathLength += (float)Math.Sqrt(Math.Pow(currentCircleCenter.x - previousCircleCenter.x, 2) + Math.Pow(currentCircleCenter.y - previousCircleCenter.y, 2));
            }
        }

        private LevelArray.Point GetCurrentCircleCenter(LevelArray.Point movePoint, float velocityX, float velocityY, float currentTime)
        {
            float distanceX = velocityX * currentTime;
            float distanceY = - velocityY * currentTime + GameInfo.GRAVITY * (float)Math.Pow(currentTime, 2) / 2;

            return new LevelArray.Point((int)(movePoint.x + distanceX), (int)(movePoint.y + distanceY));
        }

        private List<LevelArray.ArrayPoint> GetCirclePixels(LevelArray.Point circleCenter)
        {
            List<LevelArray.ArrayPoint> circlePixels = new List<LevelArray.ArrayPoint>();

            LevelArray.ArrayPoint circleCenterArray = LevelArray.ConvertPointIntoArrayPoint(circleCenter, false, false);
            int circleHighestY = LevelArray.ConvertValue_PointIntoArrayPoint(circleCenter.y - GameInfo.CIRCLE_RADIUS, false);
            int circleLowestY = LevelArray.ConvertValue_PointIntoArrayPoint(circleCenter.y + GameInfo.CIRCLE_RADIUS, true);


            for (int i = circleHighestY; i <= circleLowestY; i++)
            {
                float circleWidth;

                if (i < circleCenterArray.yArray)
                {
                    circleWidth = (float)Math.Sqrt(Math.Pow(GameInfo.CIRCLE_RADIUS, 2) - Math.Pow(LevelArray.ConvertValue_ArrayPointIntoPoint(i + 1) - circleCenter.y, 2));
                }
                else if (i > circleCenterArray.yArray)
                {
                    circleWidth = (float)Math.Sqrt(Math.Pow(GameInfo.CIRCLE_RADIUS, 2) - Math.Pow(LevelArray.ConvertValue_ArrayPointIntoPoint(i) - circleCenter.y, 2));
                }
                else
                {
                    circleWidth = GameInfo.CIRCLE_RADIUS;
                }

                int circleLeftX = LevelArray.ConvertValue_PointIntoArrayPoint((int)(circleCenter.x - circleWidth), false);
                int circleRightX = LevelArray.ConvertValue_PointIntoArrayPoint((int)(circleCenter.x + circleWidth), true);

                for (int j = circleLeftX; j <= circleRightX; j++)
                {
                    circlePixels.Add(new LevelArray.ArrayPoint(j, i));
                }
            }

            return circlePixels;
        }

        private collideType GetCollideType(int[,] levelArray, LevelArray.Point circleCenter, bool ascent, bool rightMove)
        {
            LevelArray.ArrayPoint circleCenterArray = LevelArray.ConvertPointIntoArrayPoint(circleCenter, false, false);
            int circleHighestY = LevelArray.ConvertValue_PointIntoArrayPoint(circleCenter.y - GameInfo.CIRCLE_RADIUS, false);
            int circleLowestY = LevelArray.ConvertValue_PointIntoArrayPoint(circleCenter.y + GameInfo.CIRCLE_RADIUS, true);

            if (!ascent)
            {
                if (levelArray[circleLowestY, circleCenterArray.xArray] == LevelArray.OBSTACLE)
                {
                    return collideType.FLOOR;
                }
            }
            else
            {
                if (levelArray[circleHighestY, circleCenterArray.xArray] == LevelArray.OBSTACLE)
                {
                    return collideType.CEILING;
                }
            }         

            return collideType.OTHER;
        }

        private bool IsObstacle_onPixels(int[,] levelArray, List<LevelArray.ArrayPoint> checkPixels)
        {
            if (checkPixels.Count == 0)
            {
                return true;
            }

            foreach (LevelArray.ArrayPoint i in checkPixels)
            {
                if (levelArray[i.yArray, i.xArray] == LevelArray.OBSTACLE)
                {
                    return true;
                }
            }

            return false;
        }

        private bool[] GetCollectibles_onPixels(int[,] levelArray, List<LevelArray.ArrayPoint> checkPixels, int numCollectibles)
        {
            bool[] collectible_onPath = new bool[numCollectibles];

            foreach (LevelArray.ArrayPoint i in checkPixels)
            {
                if (!(levelArray[i.yArray, i.xArray] == LevelArray.OBSTACLE || levelArray[i.yArray, i.xArray] == LevelArray.OPEN))
                {
                    collectible_onPath[levelArray[i.yArray, i.xArray] - 1] = true;
                }
            }

            return collectible_onPath;
        }

        public PlatformInfo? GetPlatform_onCircle(LevelArray.Point circleCenter)
        {
            foreach (PlatformInfo i in platformInfoList)
            {
                if (i.leftEdge <= circleCenter.x && circleCenter.x <= i.rightEdge && 0 <= (i.height - circleCenter.y) && (i.height - circleCenter.y) <= 2 * GameInfo.CIRCLE_RADIUS)
                {
                    return i;
                }
            }

            return null;
        }

        private void AddMoveInfoToList(PlatformInfo fromPlatform, MoveInfo mI)
        {
            lock (platformInfoList)
            {
                List<MoveInfo> moveInfoToRemove = new List<MoveInfo>();

                if (IsPriorityHighest(fromPlatform, mI, ref moveInfoToRemove))
                {
                    fromPlatform.moveInfoList.Add(mI);
                }

                foreach (MoveInfo i in moveInfoToRemove)
                {
                    fromPlatform.moveInfoList.Remove(i);
                }
            }
        }

        private bool IsPriorityHighest(PlatformInfo fromPlatform, MoveInfo mI, ref List<MoveInfo> moveInfoToRemove)
        {
            if (fromPlatform.id == mI.reachablePlatform.id && !Utilities.IsTrueValue_inMatrix(mI.collectibles_onPath))
            {
                return false;
            }

            bool priorityHighestFlag = true;

            foreach (MoveInfo i in fromPlatform.moveInfoList)
            {
                if (!(mI.reachablePlatform.id == i.reachablePlatform.id))
                {
                    continue;
                }
                
                Utilities.numTrue trueNum = Utilities.CompTrueNum(mI.collectibles_onPath, i.collectibles_onPath);

                if (trueNum == Utilities.numTrue.MORETRUE)
                {
                    if (mI.movementType != movementType.NO_ACTION && i.movementType == movementType.NO_ACTION)
                    {
                        continue;
                    }
                    else if (mI.movementType != movementType.NO_ACTION && i.movementType != movementType.NO_ACTION)
                    {
                        if (mI.movementType > i.movementType)
                        {
                            continue;
                        }

                        if (mI.velocityX > i.velocityX)
                        {
                            continue;
                        }
                    }

                    moveInfoToRemove.Add(i);
                    continue;
                }

                if (trueNum == Utilities.numTrue.LESSTRUE)
                {
                    if (mI.movementType == movementType.NO_ACTION && i.movementType != movementType.NO_ACTION)
                    {
                        continue;
                    }
                    else if (mI.movementType != movementType.NO_ACTION && i.movementType != movementType.NO_ACTION)
                    {
                        if (mI.movementType < i.movementType)
                        {
                            continue;
                        }

                        if (mI.velocityX < i.velocityX)
                        {
                            continue;
                        }
                    }

                    priorityHighestFlag = false;
                    continue;
                }

                if (trueNum == Utilities.numTrue.DIFFERENTTRUE)
                {
                    continue;
                }

                if (trueNum == Utilities.numTrue.SAMETRUE)
                {
                    if (mI.movementType == movementType.NO_ACTION && i.movementType == movementType.NO_ACTION)
                    {
                        int middlePos = (mI.reachablePlatform.rightEdge + mI.reachablePlatform.leftEdge) / 2;

                        if (Math.Abs(middlePos - mI.landPoint.x) > Math.Abs(middlePos - i.landPoint.x))
                        {
                            priorityHighestFlag = false;
                            continue;
                        }

                        moveInfoToRemove.Add(i);
                        continue;
                    }

                    if (mI.movementType == movementType.NO_ACTION && i.movementType != movementType.NO_ACTION)
                    {
                        moveInfoToRemove.Add(i);
                        continue;
                    }

                    if (mI.movementType != movementType.NO_ACTION && i.movementType == movementType.NO_ACTION)
                    {
                        priorityHighestFlag = false;
                        continue;
                    }

                    if (mI.movementType != movementType.NO_ACTION && i.movementType != movementType.NO_ACTION)
                    {
                        if (mI.rightMove == i.rightMove || ((mI.movementType == movementType.JUMP && i.movementType == movementType.JUMP) && (mI.velocityX == 0 || i.velocityX == 0)))
                        {
                            if (mI.movementType > i.movementType)
                            {
                                priorityHighestFlag = false;
                                continue;
                            }

                            if (mI.movementType < i.movementType)
                            {
                                moveInfoToRemove.Add(i);
                                continue;
                            }

                            if (mI.velocityX > i.velocityX)
                            {
                                priorityHighestFlag = false;
                                continue;
                            }

                            if (mI.velocityX < i.velocityX)
                            {
                                moveInfoToRemove.Add(i);
                                continue;
                            }

                            int middlePos = (mI.reachablePlatform.rightEdge + mI.reachablePlatform.leftEdge) / 2;

                            if (Math.Abs(middlePos - mI.landPoint.x) > Math.Abs(middlePos - i.landPoint.x))
                            {
                                priorityHighestFlag = false;
                                continue;
                            }

                            moveInfoToRemove.Add(i);
                            continue;
                        }
                    }
                }              
            }

            return priorityHighestFlag;
        } 
    }
}
