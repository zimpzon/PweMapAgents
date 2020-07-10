using Microsoft.Extensions.Logging;
using Pwe.World;
using System;
using System.Threading.Tasks;

namespace Pwe.MapAgents
{
    public class MapAgentLogic : IMapAgentLogic
    {
        private readonly ILogger _logger;
        private readonly IWorldGraph _worldGraph;

        public MapAgentLogic(ILogger logger, IWorldGraph worldGraph)
        {
            _logger = logger;
            _worldGraph = worldGraph;
        }

        public async Task UpdateAgent(string id, AgentCommand command)
        {
            var rnd = new Random();
        }

        //        var startNode = await world.GetNearbyNode(12.342, 55.6075);
        //        List<WayTileNode> conn = await world.GetNodeConnections(startNode);
        //        int count = 0;

        //        // Compression: https://docs.microsoft.com/en-us/dotnet/api/system.io.compression.deflatestream.write?view=netcore-3.1

        //        // Better random walk:
        //        //  Count node visit count and go for lower counts?
        //        MapNode prevNode = null;
        //        MapNode node = startNode;
        //        MapNode nextNode = null;
        //            while(true)
        //            {
        //                conn = await world.GetNodeConnections(node);
        //                if (conn.Count == 1)
        //                {
        //                    // Dead end, you have to go back.
        //                    nextNode = conn[0];
        //                }
        //                else if (conn.Count == 2 && prevNode != null)
        //                {
        //                    // Continue following a road that doesn't end or branch
        //                    bool same = conn[0].Id == conn[1].Id;
        //                    if (same)
        //                    {
        //                        // Bug ? Both nodes had the same id. Proabably a link between two tiles. Better be resillient to stuff like that.
        //                        nextNode = conn[0];
        //                    }
        //                    else
        //                    {
        //                        // Bug ? Landed on a node that had two connections, but neither pointed back to previous node. Better be resillient to stuff like that. (use .First instead of .Single)
        //                        nextNode = conn.Where(c => c.Id != prevNode.Id).First();
        //                    }
        //                }
        //                else
        //                {
        //                    // Random branching (but do not go back where you came from)
        //                    int safety = 0;
        //                    while (true)
        //                    {
        //                        int idx = -1;
        //double minX = double.MaxValue;
        //                        for (int i = 0; i<conn.Count; ++i)
        //                        {
        //                            if (conn[i].Lat<minX)
        //                            {
        //                                idx = i;
        //                                minX = conn[i].Lat;
        //                            }
        //                        }

        //                        if (rnd.NextDouble() < 0.0 && idx != -1)
        //                        {
        //                            nextNode = conn[idx];
        //                        }
        //                        else
        //                        {
        //                            nextNode = conn[rnd.Next(conn.Count)];
        //                        }

        //                        if (prevNode == null || node.Id != prevNode.Id)
        //                            break;

        //                        if (safety++ > 100)
        //                            throw new InvalidOperationException("All branches leads back where we came from?");
        //                    }

        //                    if (++count >= 100000)
        //                        break;

        //                    if (count % 1000 == 0)
        //                        Console.WriteLine(count);
        //                }

        //                prevNode = node;
        //                node = nextNode;
        //        }
    }
}
