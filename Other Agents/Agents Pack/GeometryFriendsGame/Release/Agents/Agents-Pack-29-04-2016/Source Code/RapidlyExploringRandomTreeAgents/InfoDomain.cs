
namespace GeometryFriendsAgents
{
    public class InfoDomain
    {
        private int[] numbersInfo;        
        private float[] circleInfo;
        private float[] rectangleInfo;
        private float[] obstaclesInfo;
        private float[] circlePlatformsInfo;
        private float[] rectanglePlatformsInfo;
        private float[] collectiblesInfo;
        private int nCollectibles;
        private int[,] table;
        private Obstacles allObstacles;

        public Obstacles AllObstacles
        {
            get { return allObstacles; }
            set { allObstacles = value; }
        }


        public InfoDomain(int[] nI, float[] sI, float[] cI, float[] oI, float[] sPI, float[] cPI, float[] colI, double timeLimit, int typeOfAgent)
        {
			// typeOfAgent identifies the if the agent is the circle or the rectangle, 0 - circle | 1 - rectangle

            // Time limit is the time limit of the level - if negative then there is no time constrains
            int temp;

            // numbersInfo[] Description
            //
            // Index - Information
            //
            //   0   - Number of Obstacles
            //   1   - Number of Rectangle Platforms
            //   2   - Number of Circle Platforms
            //   3   - Number of Collectibles

            numbersInfo = new int[4];
            int i;
            for (i = 0; i < nI.Length; i++)
            {
                numbersInfo[i] = nI[i];
            }

            nCollectibles = nI[3];

            // rectangleInfo[] Description
            //
            // Index - Information
            //
            //   0   - Rectangle X Position
            //   1   - Rectangle Y Position
            //   2   - Rectangle X Velocity
            //   3   - Rectangle Y Velocity
            //   4   - Rectangle Height

            rectangleInfo = new float[5];

            rectangleInfo[0] = sI[0];
            rectangleInfo[1] = sI[1];
            rectangleInfo[2] = sI[2];
            rectangleInfo[3] = sI[3];
            rectangleInfo[4] = sI[4];

            // circleInfo[] Description
            //
            // Index - Information
            //
            //   0  - Circle X Position
            //   1  - Circle Y Position
            //   2  - Circle X Velocity
            //   3  - Circle Y Velocity
            //   4  - Circle Radius

            circleInfo = new float[5];

            circleInfo[0] = cI[0];
            circleInfo[1] = cI[1];
            circleInfo[2] = cI[2];
            circleInfo[3] = cI[3];
            circleInfo[4] = cI[4];


            // Obstacles and Platforms Info Description
            //
            //  X = Center X Coordinate
            //  Y = Center Y Coordinate
            //
            //  H = Platform Height
            //  W = Platform Width
            //
            //  Position (X=0,Y=0) = Left Superior Corner

            // obstaclesInfo[] Description
            //
            // Index - Information
            //
            // If (Number of Obstacles > 0)
            //  [0 ; (NumObstacles * 4) - 1]      - Obstacles' info [X,Y,H,W]
            // Else
            //   0                                - 0
            //   1                                - 0
            //   2                                - 0
            //   3                                - 0

            if (numbersInfo[0] > 0)
                obstaclesInfo = new float[numbersInfo[0] * 4];
            else obstaclesInfo = new float[4];

            temp = 1;
            if (nI[0] > 0)
            {
                while (temp <= nI[0])
                {
                    obstaclesInfo[(temp * 4) - 4] = oI[(temp * 4) - 4];
                    obstaclesInfo[(temp * 4) - 3] = oI[(temp * 4) - 3];
                    obstaclesInfo[(temp * 4) - 2] = oI[(temp * 4) - 2];
                    obstaclesInfo[(temp * 4) - 1] = oI[(temp * 4) - 1];
                    temp++;
                }
            }
            else
            {
                obstaclesInfo[0] = oI[0];
                obstaclesInfo[1] = oI[1];
                obstaclesInfo[2] = oI[2];
                obstaclesInfo[3] = oI[3];
            }

			if( typeOfAgent == 0)	// circle
            	AllObstacles = new Obstacles(nI[0], oI, nI[1], sPI);
			else 					// rectangle
				AllObstacles = new Obstacles(nI[0], oI, nI[2] , cPI);

            // rectanglePlatformsInfo[] Description
            //
            // Index - Information
            //
            // If (Number of Rectangle Platforms > 0)
            //  [0; (numRectanglePlatforms * 4) - 1]   - Rectangle Platforms' info [X,Y,H,W]
            // Else
            //   0                                  - 0
            //   1                                  - 0
            //   2                                  - 0
            //   3                                  - 0

            if (numbersInfo[1] > 0)
                rectanglePlatformsInfo = new float[numbersInfo[1] * 4];
            else
                rectanglePlatformsInfo = new float[4];

            temp = 1;
            if (nI[1] > 0)
            {
                while (temp <= nI[1])
                {
                    rectanglePlatformsInfo[(temp * 4) - 4] = sPI[(temp * 4) - 4];
                    rectanglePlatformsInfo[(temp * 4) - 3] = sPI[(temp * 4) - 3];
                    rectanglePlatformsInfo[(temp * 4) - 2] = sPI[(temp * 4) - 2];
                    rectanglePlatformsInfo[(temp * 4) - 1] = sPI[(temp * 4) - 1];
                    temp++;
                }
            }
            else
            {
                rectanglePlatformsInfo[0] = sPI[0];
                rectanglePlatformsInfo[1] = sPI[1];
                rectanglePlatformsInfo[2] = sPI[2];
                rectanglePlatformsInfo[3] = sPI[3];
            }

            // circlePlatformsInfo[] Description
            //
            // Index - Information
            //
            // If (Number of Circle Platforms > 0)
            //  [0; (numCirclePlatforms * 4) - 1]   - Circle Platforms' info [X,Y,H,W]
            // Else
            //   0                                  - 0
            //   1                                  - 0
            //   2                                  - 0
            //   3                                  - 0

            if (numbersInfo[2] > 0)
                circlePlatformsInfo = new float[numbersInfo[2] * 4];
            else
                circlePlatformsInfo = new float[4];

            temp = 1;
            if (nI[2] > 0)
            {
                while (temp <= nI[2])
                {
                    circlePlatformsInfo[(temp * 4) - 4] = cPI[(temp * 4) - 4];
                    circlePlatformsInfo[(temp * 4) - 3] = cPI[(temp * 4) - 3];
                    circlePlatformsInfo[(temp * 4) - 2] = cPI[(temp * 4) - 2];
                    circlePlatformsInfo[(temp * 4) - 1] = cPI[(temp * 4) - 1];
                    temp++;
                }
            }
            else
            {
                circlePlatformsInfo[0] = cPI[0];
                circlePlatformsInfo[1] = cPI[1];
                circlePlatformsInfo[2] = cPI[2];
                circlePlatformsInfo[3] = cPI[3];
            }

            //Collectibles' To Catch Coordinates (X,Y)
            //
            //  [0; (numCollectibles * 2) - 1]   - Collectibles' Coordinates (X,Y)

            collectiblesInfo = new float[numbersInfo[3] * 2];

            temp = 1;
            while (temp <= nI[3])
            {

                collectiblesInfo[(temp * 2) - 2] = colI[(temp * 2) - 2];
                collectiblesInfo[(temp * 2) - 1] = colI[(temp * 2) - 1];

                temp++;
            }

        }

        public int NCollectibles
        {
            get { return nCollectibles; }
            set { nCollectibles = value; }
        }

        public float[] CollectiblesInfo
        {
            get { return collectiblesInfo; }
            set { collectiblesInfo = value; }
        }

        public float[] RectangleInfo
        {
            get { return rectangleInfo; }
            set { rectangleInfo = value; }
        }

        public float[] ObstaclesInfo
        {
            get { return obstaclesInfo; }
            set { obstaclesInfo = value; }
        }

        public float[] RectanglePlatformsInfo
        {
            get { return rectanglePlatformsInfo; }
            set { rectanglePlatformsInfo = value; }
        }

        public float[] CirclePlatformsInfo
        {
            get { return circlePlatformsInfo; }
            set { circlePlatformsInfo = value; }
        }

        public float[] CircleInfo
        {
            get { return circleInfo; }
            set { circleInfo = value; }
        }

        public int[] NumbersInfo
        {
            get { return numbersInfo; }
            set { numbersInfo = value; }
        }

        public int[,] Table
        {
            get { return table; }
            set { table = value; }
        }
    }
}
