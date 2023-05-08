using GeometryFriendsAgents.Model;
using GeometryFriendsAgents.WorldObjects;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GeometryFriendsAgents.Managers {
    abstract class PlatformManager {


        protected ConcurrentDictionary<int, Platform> _platforms;
        protected WorldModel _model;
        protected Graph graph;
        protected float[] oI, aI;
        protected int numberGeneric, numberAgent;

        protected PlatformManager(WorldModel model) {
            _model = model;
        }

        public Graph GetGraph() {
            return graph;
        }

        public void DistributeCollectibles(List<Collectible> collectibles) {

            List<Collectible> listCollectibles = new List<Collectible>(collectibles);

            foreach (Platform p in _platforms.Values) {
                List<Collectible> toPlatform = new List<Collectible>();
                foreach (Collectible c in listCollectibles) {
                    if (_model.isOnPlatform(p,c)) {
                        if (c.yPos <= p.Top) {
                            toPlatform.Add(c);
                        }
                    }
                }
                p.AddCollectibles(toPlatform);
                foreach (Collectible c in toPlatform) {
                    listCollectibles.Remove(c);
                }
            }
        }

        internal virtual Platform FindPlatform(float xPos, float yPos) {
            foreach (Platform p in _platforms.Values) {
                if (xPos >= p.Left && xPos <= p.Right) {
                    if (yPos <= p.Top && yPos >= p.Top - 100) {
                        return p;
                    }
                }
            }
            return null;
        }

        internal abstract Platform FindAgentPlatform(float xPos, float yPos);

        protected List<Platform> SplitPlatforms(List<Platform> platforms) {
            List<Platform> finalPlatforms = new List<Platform>();

            while (platforms.Count > 0) {
                Platform analysingPlatform = platforms[platforms.Count - 1];
                platforms.RemoveAt(platforms.Count - 1);
                List<Platform> newPlatforms = new List<Platform>();
                bool splited = false;
                foreach (Platform p in platforms) {
                    if (p.Colides(analysingPlatform)) {
                        newPlatforms.AddRange(analysingPlatform.SplitPlatform(p));
                        splited = true;
                        break;
                    }
                }
                if (splited) {
                    platforms.AddRange(newPlatforms);
                    platforms.Sort();
                } else {
                    finalPlatforms.Add(analysingPlatform);
                }
            }

            finalPlatforms.Sort();
            return finalPlatforms;
        }


        internal abstract void CreatePlatforms(float[] oI, float[] aI, int numberGeneric, int numberAgent,float[] agentInfo);

        internal List<Platform> GetPlatforms() {
            return new List<Platform>(_platforms.Values);
        }

        internal Platform GetPlatform(int platformId) {
            return _platforms[platformId];
        }

        internal void AddPlatforms(float[] oI, float[] aI, int numberGeneric, int numberAgent, float[] agentInfo) {
            this.oI = oI;
            this.aI = aI;
            this.numberAgent = numberAgent;
            this.numberGeneric = numberGeneric;
            CreatePlatforms(oI, aI, numberGeneric, numberAgent, agentInfo);    
        }

        internal void RefreshPlatforms(float[] agentsInfo) {
            CreatePlatforms(oI, aI, numberGeneric, numberAgent, agentsInfo);
        }

        protected bool OtherAgentIsInPlay(float[] agentsInfo) {
            return agentsInfo[0] > 0 && agentsInfo[1] > 0;
        }

    }
}

