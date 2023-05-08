
using GeometryFriends.AI;
using GeometryFriends.AI.Interfaces;
using GeometryFriends.AI.Perceptions.Information;
using GeometryFriendsAgents.GameStates;
using GeometryFriendsAgents.Learning;
using GeometryFriendsAgents.WorldObjects;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GeometryFriendsAgents {
   public class CircleAgent : AbstractCircleAgent {
        private int knownStatesNumber = 0;
        private int numberOfMovements = 0;
        private bool implementedAgent;
        private Moves currentAction;
        private DateTime lastMoveTime;
        private DateTime lastRefreshTime;
        private Random rnd;
        private DateTime startTime;
        private bool save;

        private string agentName = "LearningBall";

        private CircleWorldModel model;

        private Learning.LearningCenter learningCenter;

        internal LearningCenter LearningCenter {
            get { return learningCenter; }
        }

        //Sensors Information
        private int[] numbersInfo;
        private float[] squareInfo;
        private string gameStateId = "";


        public CircleAgent() {
            //Change flag if agent is not to be used
            SetImplementedAgent(true);

            lastMoveTime = DateTime.Now;
            lastRefreshTime = DateTime.Now;
            currentAction = 0;
            rnd = new Random(DateTime.Now.Millisecond);

            model = new CircleWorldModel(this);

            learningCenter = new CircleLearningCenter(model);
            learningCenter.InitializeLearning();

            startTime = DateTime.Now;
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

        public void DeprecatedSetup(int[] nI, float[] sI, float[] cI, float[] oI, float[] sPI, float[] cPI, float[] colI, Rectangle a, double timeLimit)
        {
            model.Area = a;
            int temp;

            // numbersInfo[] Description
            //
            // Index - Information
            //
            //   0   - Number of Obstacles
            //   1   - Number of Square Platforms
            //   2   - Number of Circle Platforms
            //   3   - Number of Collectibles

            numbersInfo = new int[4];
            int i;
            for (i = 0; i < nI.Length; i++) {
                numbersInfo[i] = nI[i];
            }

            model.NumberCollectiblesGlobal = nI[3];



            // squareInfo[] Description
            //
            // Index - Information
            //
            //   0   - Square X Position
            //   1   - Square Y Position
            //   2   - Square X Velocity
            //   3   - Square Y Velocity
            //   4   - Square Height

            squareInfo = new float[5];

            squareInfo[0] = sI[0];
            squareInfo[1] = sI[1];
            squareInfo[2] = sI[2];
            squareInfo[3] = sI[3];
            squareInfo[4] = sI[4];

            model.AgentXPosition = cI[0];
            model.AgentYPosition = cI[1];
            model.AgentXSpeed = cI[2];
            model.AgentYSpeed = cI[3];
            model.AgentRadius = cI[4];

            if (model.AgentXPosition < 0 || model.AgentYPosition < 0 || !LearningCenter.checkIfLearning())
            {
                save = false;
                learningCenter.setSave(save);
            } else {
                save = true;
                learningCenter.setSave(save);
            }

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
            model.InitPlatforms(oI, cI, nI[0], nI[2], sI);


            // squarePlatformsInfo[] Description
            //
            // Index - Information
            //
            // If (Number of Square Platforms > 0)
            //  [0; (numSquarePlatforms * 4) - 1]   - Square Platforms' info [X,Y,H,W]
            // Else
            //   0                                  - 0
            //   1                                  - 0
            //   2                                  - 0
            //   3                                  - 0

            //Collectibles' To Catch Coordinates (X,Y)
            //
            //  [0; (numCollectibles * 2) - 1]   - Collectibles' Coordinates (X,Y)
            List<Collectible> collectibles = new List<Collectible>();


            temp = 1;
            while (temp <= nI[3]) {
                collectibles.Add(new Collectible(colI[(temp * 2) - 2], colI[(temp * 2) - 1]));
                temp++;
            }
            model.RemainingCollectibles = collectibles;
            model.DistributeCollectbles();

            model.TimeLimit = timeLimit;
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

            model.NumberCollectiblesRemaining = nC;

            squareInfo[0] = sI[0];
            squareInfo[1] = sI[1];
            squareInfo[2] = sI[2];
            squareInfo[3] = sI[3];
            squareInfo[4] = sI[4];

            model.AgentXPosition = cI[0];
            model.AgentYPosition = cI[1];
            model.AgentXSpeed = cI[2];
            model.AgentYSpeed = cI[3];

            temp = 1;
            List<Collectible> collectibles = new List<Collectible>();
            while (temp <= nC)
            {
                collectibles.Add(new Collectible(colI[(temp * 2) - 2], colI[(temp * 2) - 1]));
                temp++;
            }

            if ((DateTime.Now - lastRefreshTime).TotalMilliseconds > 500)
            {
                model.RefreshPlatforms(squareInfo);
                lastRefreshTime = DateTime.Now;
            }



            model.RemainingCollectibles = collectibles;
            model.DistributeCollectbles();
        }

        private void SetImplementedAgent(bool b) {
            implementedAgent = b;
        }

        public override bool ImplementedAgent() {
            return implementedAgent;
        }

        private void DiscoverAction() {
            model.VerifyPlatform();
            if (model.AgentPlatform != null) {
                GameState state = model.GetGameState();
                if (model.WillHaveMoves() && learningCenter.ContainsState(state.GetStateId())) {
                    if (!gameStateId.Equals(state.GetStateId())) {
                        gameStateId = state.GetStateId();
                        numberOfMovements++;
                        knownStatesNumber++;
                        if (rnd.NextDouble() < 0.2) {
                            bool hasNewAction = false;
                            //currentAction = -1;
                            double maxValue = 0;
                            foreach (Moves move in learningCenter.GetMovesForState(state.GetStateId())) {
                                if (learningCenter.GetMoveValueInState(move, state.GetStateId()) > maxValue) {
                                    maxValue = learningCenter.GetMoveValueInState(move, state.GetStateId());
                                    currentAction = move;
                                    hasNewAction = true;
                                }
                            }
                            if (hasNewAction) {
                                selectAction();
                                return;
                            }
                        }
                    }
                    if (rnd.Next(1, 3) > 1 && model.WillHaveMoves()) {
                        currentAction = model.FindMoveTowardsClosestCollectible();
                    } else {
                        RandomAction();
                    }

                    selectAction();
                    return;
                }

                numberOfMovements++;
                if (rnd.Next(1, 3) > 1 && model.WillHaveMoves()) {
                    currentAction = model.FindMoveTowardsClosestCollectible();
                } else {
                    RandomAction();
                }



                selectAction();
            } else {
                currentAction = Moves.NO_ACTION;
            }
        }



        private void RandomAction() {
            double d = rnd.NextDouble();
            if (d < 0.05) {
                currentAction = (Moves) 3;
            } else {
                currentAction = (Moves) rnd.Next(1, 3);
            }

        }


        private void selectAction() {
            switch (currentAction) {
                case Moves.ROLL_LEFT:
                    SetAction(Moves.ROLL_LEFT);
                    break;
                case Moves.ROLL_RIGHT:
                    SetAction(Moves.ROLL_RIGHT);
                    break;
                case Moves.JUMP:
                    SetAction(Moves.JUMP);
                    break;
                default:
                    break;
            }
        }

        private void SetAction(Moves a) {
            currentAction = a;
        }

        //Manager gets this action from agent
        public override Moves GetAction() {
            if (model.WillHaveMoves()) {
                learningCenter.AddState(currentAction);
            }
            return currentAction;
        }

        public override void Update(TimeSpan elapsedGameTime) {
            if ((DateTime.Now - lastMoveTime).TotalMilliseconds > 250) {
                DiscoverAction();
                lastMoveTime = DateTime.Now;
            }
        }

        public override string AgentName()
        {
            return agentName;
        }

        public override void EndGame(int collectiblesCaught, int timeElapsed)
        {
            model.EndGame(collectiblesCaught, timeElapsed, learningCenter.GetIntraPlatformsPlayedStates(), learningCenter.GetInterPlatformsPlayedStates());
            learningCenter.EndGame((float)knownStatesNumber / numberOfMovements);
        }
   }
}


