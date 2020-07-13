using Microsoft.Extensions.Logging;
using Pwe.AzureBloBStore;
using Pwe.OverpassTiles;
using Pwe.Shared;
using Pwe.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pwe.MapAgents
{
    public class MapAgentLogic : IMapAgentLogic
    {
        private readonly ILogger _logger;
        private readonly IWorldGraph _worldGraph;
        private readonly IBlobStoreService _blobStoreService;
        private readonly Random _rnd = new Random();

        public MapAgentLogic(ILogger logger, IWorldGraph worldGraph, IBlobStoreService blobStoreService)
        {
            _logger = logger;
            _worldGraph = worldGraph;
            _blobStoreService = blobStoreService;
        }

        //var ag = new MapAgent
        //{
        //    Id = "1",
        //    Name = "Fætter Guf",
        //    StartLon = 12.568264,
        //    StartLat = 55.675745,
        //    StartTimeUtc = DateTime.UtcNow,
        //};
        //ag.Lon = ag.StartLon;
        //ag.Lat = ag.StartLat;
        //await _blobStoreService.StoreText($"agents/1", JsonSerializer.Serialize(ag));

        public async Task<MapAgentPath> GetPath(string agentId)
        {
            string json = await _blobStoreService.GetText(BuildPathPath(agentId)).ConfigureAwait(false);
            return JsonSerializer.Deserialize<MapAgentPath>(json);
        }

        private static string BuildPathPath(string agentId) => $"agents/{agentId}-path.json";

        private static string BuildClientPathPath(string agentId) => $"agents/{agentId}-clientpath.json";

        public async Task<string> GetAgentClientPath(string agentId)
        {
            var clientPath = await _blobStoreService.GetText(BuildClientPathPath(agentId)).ConfigureAwait(false);
            return clientPath;
        }

        public async Task UpdateAgent(string agentId, AgentCommand command)
        {
            _logger.LogInformation($"Updating agent {agentId}");
            string agentJson = await _blobStoreService.GetText($"agents/{agentId}.json").ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(agentJson))
                throw new ArgumentException($"Unknown agent: {agentId}");

            var agent = JsonSerializer.Deserialize<MapAgent>(agentJson);

            string pathJson = await _blobStoreService.GetText(BuildPathPath(agentId), throwIfNotFound: false).ConfigureAwait(false);
            var oldPath = string.IsNullOrWhiteSpace(pathJson) ? new MapAgentPath() : JsonSerializer.Deserialize<MapAgentPath>(pathJson);

            var newPath = new MapAgentPath();
            long pathUnixMs = GeoMath.UnixMs();
            if (newPath.Points.Count == 0)
            {
                newPath.Points.Add(new GeoCoord(agent.StartLon, agent.StartLat));
                newPath.PointAbsTimestampMs.Add(pathUnixMs);
            }
            else
            {
                // Keep all points that are in the future
            }

            // Start path here
            var startPoint = newPath.Points.Last();
            var startNode = await _worldGraph.GetNearbyNode(startPoint).ConfigureAwait(false);

            List<WayTileNode> conn = await _worldGraph.GetNodeConnections(startNode).ConfigureAwait(false);
            int count = 0;
            double pathMeters = 0;
            long pathMs = 0;
            WayTileNode prevNode = null;
            WayTileNode node = startNode;
            WayTileNode nextNode = null;
            while (true)
            {
                conn = await _worldGraph.GetNodeConnections(node).ConfigureAwait(false);
                if (conn.Count == 1)
                {
                    // Dead end, you have to go back.
                    nextNode = conn[0];
                }
                else if (conn.Count == 2 && prevNode != null)
                {
                    // Continue following a road that doesn't end or branch
                    bool same = conn[0].Id == conn[1].Id;
                    if (same)
                    {
                        // Bug ? Both nodes had the same id. Proabably a link between two tiles. Better be resillient to stuff like that.
                        nextNode = conn[0];
                    }
                    else
                    {
                        // Bug ? Landed on a node that had two connections, but neither pointed back to previous node. Better be resillient to stuff like that. (use .First instead of .Single)
                        nextNode = conn.Where(c => c.Id != prevNode.Id).First();
                    }
                }
                else
                {
                    // Random branching (but do not go back where you came from)
                    int safety = 0;
                    while (true)
                    {
                        nextNode = conn[_rnd.Next(conn.Count)];

                        if (prevNode == null || node.Id != prevNode.Id)
                            break;

                        if (safety++ > 100)
                            throw new InvalidOperationException("All branches leads back where we came from?");
                    }

                    if (++count >= 100)
                        break;
                }

                prevNode = node;
                node = nextNode;

                double segmentMeters = GeoMath.MetersDistanceTo(prevNode.Point, node.Point);
                long segmentMs = (long)((segmentMeters / agent.MetersPerSecond) * 1000);
                newPath.Points.Add(node.Point);
                pathUnixMs += segmentMs;
                newPath.PointAbsTimestampMs.Add(pathUnixMs);
                pathMeters += segmentMeters;
                pathMs += segmentMs;
            }

            newPath.PathMeters = pathMeters;
            newPath.PathMs = pathMs;
            newPath.TileIds = _worldGraph.GetLoadedTiles().Select(tile => tile.Id).ToList();
            newPath.EncodedPolyline = GooglePolylineConverter.Encode(newPath.Points);

            string newPathJson = JsonSerializer.Serialize(newPath);
            await _blobStoreService.StoreText(BuildPathPath(agentId), newPathJson, overwriteExisting: true).ConfigureAwait(false);

            var clientPath = new AgentClientPath
            {
                MsStart = newPath.PointAbsTimestampMs.First(),
                MsEnd = newPath.PointAbsTimestampMs.Last(),
                EncodedPolyline = newPath.EncodedPolyline
            };
            string clientPathJson = JsonSerializer.Serialize(clientPath);
            await _blobStoreService.StoreText(BuildClientPathPath(agentId), clientPathJson, overwriteExisting: true).ConfigureAwait(false);
        }
    }
}
