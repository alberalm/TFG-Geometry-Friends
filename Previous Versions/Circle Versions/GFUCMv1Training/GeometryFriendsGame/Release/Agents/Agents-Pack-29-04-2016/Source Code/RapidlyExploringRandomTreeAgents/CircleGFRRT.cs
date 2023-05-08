
namespace GeometryFriendsAgents
{
	class CircleGFRRT
	{
		private State _xinit;
		private InfoDomain _domain;
		private int _maxiter;
		private Validator _validator;
		private Tactics _tactics;
        private CircleNode bestSoFar;

		public CircleGFRRT(State xinit, InfoDomain domain, int maxiter, Validator validator, Tactics tactics)
		{
			this._xinit = xinit;
			this._domain = domain;
			this._maxiter = maxiter;
			this._tactics = tactics;
			this._validator = validator;
		}

		public Path run()
		{
            Graph graph = new Graph(_xinit, new CircleNode());
			//Boolean busy = false;
			CircleNode node = null;
            CircleNode newnode = null;
            bestSoFar = graph.getRandomNode(node);

			for (int iter = 0; iter < _maxiter; iter++)
			{
				node = graph.getRandomNode(node);

                if(node == null)
                    return minimizePath(graph.traceBack(bestSoFar));

				newnode = _tactics.apply(node, _domain, graph); //or only node?

				if (newnode == null)
					continue;

				//busy = newnode.isBusy();
				if (_validator.validate(newnode, node, _domain, graph))
				{
					graph.addNode(newnode, node, newnode.getState().getAction());
                    if (newnode.getState().numberOfCollectibles() < bestSoFar.getState().numberOfCollectibles())
                        bestSoFar = newnode;
					// GOAL ACHIEVED
					if (newnode.getState().numberOfCollectibles() == 0)
						return minimizePath(graph.traceBack(newnode));
				}
				//else
				//{
				//    //busy = node.isBusy();
				//    //if (busy)
				//    //   graph.rollBack(node);
				//    //busy = false;
				//    if (node.isBusy())
				//        graph.rollBack(node);
				//}

			}
            return minimizePath(graph.traceBack(bestSoFar));
		}

		private Path minimizePath(Path p)
		{
			for (int n = 0; n < p.path.Count -1; n++)
			{
				if (((Point)p.path[n + 1]).Gap == true)
                {
                    ((Point)p.path[n]).Morph = true;
                    ((Point)p.path[n]).StartGap = true;
                    ((Point)p.path[n]).Size = ((Point)p.path[n + 1]).Size;
                    ((Point)p.path[n + 1]).Gap = true;
                }

				if (!((Point)p.path[n]).flagsTrue())
				{
					p.path.RemoveAt(n);
					n--;
				}

			}
			return p;
		}

	}

}
