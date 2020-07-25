using Pwe.AzureBloBStore;
using Pwe.OverpassTiles;
using Pwe.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pwe.World
{
    public class GraphPeek : IGraphPeek
    {
        private readonly IWorldGraph _worldGraph;

        private readonly List<WayTileNode> _visitedList = new List<WayTileNode>();
        private readonly List<WayTileNode> _pendingList = new List<WayTileNode>();

        public GraphPeek(IWorldGraph worldGraph)
        {
            _worldGraph = worldGraph;
        }

        public async Task<(bool deadEndFound, bool unexploredNodeFound, List<GeoCoord> explored)> Peek(WayTileNode root, WayTileNode first)
        {
            _visitedList.Clear();
            _pendingList.Clear();

            _visitedList.Add(root);
            _pendingList.Add(first);

            var explored = new List<GeoCoord>();
            const int MaxSteps = 50;
            bool unexploredNodeFound = false;

            int stepCount = 0;
            while (true)
            {
                if (_pendingList.Count == 0)
                    break;

                var current = _pendingList.First();
                if (current.VisitCount == 0)
                    unexploredNodeFound = true;

                _pendingList.RemoveAt(0);

                bool alreadyTested = _visitedList.Any(x => x.Id == current.Id);
                if (alreadyTested)
                    continue;

                stepCount++;
                if (stepCount >= MaxSteps)
                    break;

                var connections = await _worldGraph.GetNodeConnections(current, updateVisitCount: false).ConfigureAwait(false);
                _pendingList.AddRange(connections);
                _visitedList.Add(current);

                foreach (var conn in connections)
                {
                    explored.Add(current.Point);
                    explored.Add(conn.Point);
                }
            }

            bool deadEndFound = stepCount < MaxSteps;
            return (deadEndFound, unexploredNodeFound, explored);
        }
    }
}
