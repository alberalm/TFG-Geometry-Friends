

using GeometryFriends;
using GeometryFriends.AI;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;
using System;
using System.Collections;
using System.Drawing;
using System.Linq;

namespace GeometryFriendsAgents
{
	class RectangleAgent : AbstractRectangleAgent
	{
		private bool implementedAgent;
		private Moves currentAction;
		private long lastMoveTime;
		private Random rnd;

		//Sensors Information
		private int[] numbersInfo;
		private float[] rectangleInfo;
		private float[] circleInfo;
		private float[] obstaclesInfo;
		private float[] rectanglePlatformsInfo;
		private float[] circlePlatformsInfo;
		private float[] collectiblesInfo;

		//Game solution
		private Path plays;

        RectangleWalker walker;
        RectangleWalkingPoint pointToTarget;

		private int nCollectiblesLeft;

		private string agentName = "RandRect";

		//Area of the game screen
		protected Rectangle area;

		public RectangleAgent() 
		{
			//Change flag if agent is not to be used
			SetImplementedAgent(true);

			lastMoveTime = DateTime.Now.Second;
			currentAction = 0;
			rnd = new Random();
		}

        public override void Setup(CountInformation nI, RectangleRepresentation rI, CircleRepresentation cI, ObstacleRepresentation[] oI, ObstacleRepresentation[] rPI, ObstacleRepresentation[] cPI, CollectibleRepresentation[] colI, Rectangle area, double timeLimit)
        {
            //convert to old way in order to maintain compatibility
            DeprecatedSetup(
                nI.ToArray(),
                rI.ToArray(),
                cI.ToArray(),
                ObstacleRepresentation.RepresentationArrayToFloatArray(oI),
                ObstacleRepresentation.RepresentationArrayToFloatArray(rPI),
                ObstacleRepresentation.RepresentationArrayToFloatArray(cPI),
                CollectibleRepresentation.RepresentationArrayToFloatArray(colI),
                area,
                timeLimit);
        }

        public void DeprecatedSetup(int[] nI, float[] sI, float[] cI, float[] oI, float[] sPI, float[] cPI, float[] colI, Rectangle area, double timeLimit)
        {
			// Time limit is the time limit of the level - if negative then there is no time constrains
			this.area = area;
			int temp;

			TableMaker tmaker = new TableMaker(nI, oI, sPI, cPI, colI);
			tmaker.makeTable();
			//tmaker.printTable();

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

			nCollectiblesLeft = nI[3];

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

			InfoDomain domain = new InfoDomain(nI, sI, cI, oI, sPI, cPI, colI, timeLimit, 1);
			TableMaker table = new TableMaker(nI, oI, sPI, cPI, colI);
			table.makeTable();
			domain.Table = table.Table;
			ArrayList collectibles = new ArrayList();

			temp = 1;
			while (temp <= nI[3])
			{

				collectibles.Add(colI[(temp * 2) - 2]);
				collectibles.Add(colI[(temp * 2) - 1]);

				temp++;
			}

            if (sI[0] > 0 && sI[1] > 0) {
                State root = new State(sI[2], sI[3], sI[0], sI[1], -1, collectibles, new ArrayList());
                root.setSizeOfAgent(96);

                RectangleValidator validator = new RectangleValidator();
                RectangleTactics tactics = new RectangleTactics();

                Obstacles.Obstacle o = domain.AllObstacles.getNextPlatform(root);
                root.CurrentPlatform = o;

                root.setPosY(root.CurrentPlatform.maxHeight() - root.getSizeOfAgent() / 2);

                RectangleGFRRT gfrrt = new RectangleGFRRT(root, domain, 10000000, validator, tactics);

                plays = gfrrt.run();

                // Begin walking test

                walker = new RectangleWalker(plays);
                pointToTarget = walker.getFirstPoint();

                // End walking test
                
                DebugSensorsInfo();
            }
		}

		private void SetImplementedAgent(bool b)
		{
			implementedAgent = b;
		}

        public override bool ImplementedAgent()
		{
			return implementedAgent;
		}

		private void SetAction(Moves a)
		{
			currentAction = a;
		}

		//Manager gets this action from agent
        public override Moves GetAction()
		{
			return currentAction;
		}

        public override void SensorsUpdated(int nC, RectangleRepresentation rI, CircleRepresentation cI, CollectibleRepresentation[] colI)
        {
            DeprecatedSensorsUpdated(nC,
                rI.ToArray(),
                cI.ToArray(),
                CollectibleRepresentation.RepresentationArrayToFloatArray(colI));
        }

        public void DeprecatedSensorsUpdated(int nC, float[] sI, float[] cI, float[] colI)
		{
			int temp;

			nCollectiblesLeft = nC;

			rectangleInfo[0] = sI[0];
			rectangleInfo[1] = sI[1];
			rectangleInfo[2] = sI[2];
			rectangleInfo[3] = sI[3];
			rectangleInfo[4] = sI[4];

			circleInfo[0] = cI[0];
			circleInfo[1] = cI[1];
			circleInfo[2] = cI[2];
			circleInfo[3] = cI[3];
			circleInfo[4] = cI[4];

			Array.Resize(ref collectiblesInfo, (nCollectiblesLeft * 2));

			temp = 1;
			while (temp <= nCollectiblesLeft)
			{
				collectiblesInfo[(temp * 2) - 2] = colI[(temp * 2) - 2];
				collectiblesInfo[(temp * 2) - 1] = colI[(temp * 2) - 1];

				temp++;
			}
		}

		// this method is deprecated, please use SensorsUpdated instead
        public override void UpdateSensors(int nC, float[] sI, float[] cI, float[] colI)
		{
			
		}

        public override void Update(TimeSpan elapsedGameTime)
		{
            if (rectangleInfo[0] > 0 && rectangleInfo[1] > 0)
            {
                SetAction(walker.WalkToPoint(pointToTarget, rectangleInfo[2], rectangleInfo[0], rectangleInfo[1], rectangleInfo[4]));
                pointToTarget = walker.hasReached(pointToTarget, rectangleInfo[0], rectangleInfo[1], collectiblesInfo.Length / 2, rectangleInfo[4]);
            }
		}

		public void toggleDebug()
		{
			//this.agentPane.AgentVisible = !this.agentPane.AgentVisible;
		}

		protected void DebugSensorsInfo()
		{
			int t = 0;
            /*
            foreach (int i in numbersInfo)
            {
                Log.LogInformation("RECTANGLE - Numbers info - " + t + " - " + i);
                t++;
            }
            */

            Log.LogInformation("RECTANGLE - collectibles left - " + nCollectiblesLeft);
            Log.LogInformation("RECTANGLE - collectibles info size - " + collectiblesInfo.Count());

            /*
            t = 0;
            foreach (long i in rectangleInfo)
            {
                Log.LogInformation("RECTANGLE - Rectangle info - " + t + " - " + i);
                t++;
            }

            t = 0;
            foreach (long i in circleInfo)
            {
                Log.LogInformation("RECTANGLE - Circle info - " + t + " - " + i);
                t++;
            }
			
            t = 0;
            foreach (long i in obstaclesInfo)
            {
                Log.LogInformation("RECTANGLE - Obstacles info - " + t + " - " + i);
                t++;
            }

            t = 0;
            foreach (long i in rectanglePlatformsInfo)
            {
                Log.LogInformation("RECTANGLE - Rectangle Platforms info - " + t + " - " + i);
                t++;
            }

            t = 0;
            foreach (long i in circlePlatformsInfo)
            {
                Log.LogInformation("RECTANGLE - Circle Platforms info - " + t + " - " + i);
                t++;
            }
            */
            t = 0;
			foreach (float i in collectiblesInfo)
			{
                Log.LogInformation("RECTANGLE - Collectibles info - " + t + " - " + i);
				t++;
			}
		}

        public override string AgentName()
		{
			return agentName;
		}

        public override void EndGame(int collectiblesCaught, int timeElapsed)
        {
            Log.LogInformation("RECTANGLE - Collectibles caught = " + collectiblesCaught + ", Time elapsed - " + timeElapsed);
		}
    }
}