using Microsoft.Extensions.Logging;
using Pwe.AzureBloBStore;
using Pwe.GeoJson;
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
        private readonly IMapCoverage _mapCoverage;
        private readonly Random _rnd = new Random();

        public MapAgentLogic(ILogger logger, IWorldGraph worldGraph, IBlobStoreService blobStoreService, IMapCoverage mapCoverage)
        {
            _logger = logger;
            _worldGraph = worldGraph;
            _blobStoreService = blobStoreService;
            _mapCoverage = mapCoverage;
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
        private static string BuildGeoJsonPathPath(string agentId) => $"agents/{agentId}-geojson.json";

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
            long pathMs = 0;
            double pathMeters = 0;

            // Keep all points that are in the future
            List<GeoCoord> visitedPoints = new List<GeoCoord>();
            long msPrev = -1;
            for (int i = 0; i < oldPath.PointAbsTimestampMs.Count; ++i)
            {
                if (oldPath.PointAbsTimestampMs[i] >= pathUnixMs)
                {
                    pathUnixMs = oldPath.PointAbsTimestampMs[i];
                    newPath.PointAbsTimestampMs.Add(pathUnixMs);
                    newPath.Points.Add(oldPath.Points[i]);
                    long msCurrent = oldPath.PointAbsTimestampMs[i];
                    if (msPrev != -1)
                    {
                        long segmentMs = msCurrent - msPrev;
                        pathMs += segmentMs;
                    }
                    msPrev = msCurrent;
                }
                else
                {
                    visitedPoints.Add(oldPath.Points[i]);
                }
            }
            await _mapCoverage.UpdateCoverage(visitedPoints).ConfigureAwait(false);

            if (newPath.Points.Count == 0)
            {
                newPath.Points.Add(new GeoCoord(agent.StartLon, agent.StartLat));
                newPath.PointAbsTimestampMs.Add(pathUnixMs);
            }

            // Start path here
            var startPoint = newPath.Points.Last();
            var startNode = await _worldGraph.GetNearbyNode(startPoint).ConfigureAwait(false);

            List<WayTileNode> conn = null;
            WayTileNode prevNode = null;
            WayTileNode node = startNode;
            WayTileNode nextNode = null;
            while (true)
            {
                conn = await _worldGraph.GetNodeConnections(node).ConfigureAwait(false);

                long minCount = conn.Min(n => (long)n.VisitCount);
                var leastVisited = conn.Where(n => n.VisitCount == minCount).ToList();
                if (leastVisited.Count > 1)
                {
                    // Prefer not going back. Dead end simulation (moving from right to left): 0...0...1 -> 0...1...1 -> 1...1...1 -> 1...2...1 -> 50% chance of going back to dead end.
                    leastVisited.Remove(prevNode);
                }

                nextNode = leastVisited[_rnd.Next(leastVisited.Count)];
                prevNode = node;
                node = nextNode;

                double segmentMeters = GeoMath.MetersDistanceTo(prevNode.Point, node.Point);
                long segmentMs = (long)((segmentMeters / agent.MetersPerSecond) * 1000);
                newPath.Points.Add(node.Point);
                pathUnixMs += segmentMs;
                newPath.PointAbsTimestampMs.Add(pathUnixMs);
                pathMeters += segmentMeters;
                pathMs += segmentMs;

                if (pathMs >= 60 * 10 * 1000) // 10 minutes per path
                    break;
            }

            await _worldGraph.StoreUpdatedVisitCounts().ConfigureAwait(false);

            newPath.PathMs = pathMs;
            newPath.TileIds = _worldGraph.GetLoadedTiles().Select(tile => tile.Id).ToList();
            newPath.EncodedPolyline = GooglePolylineConverter.Encode(newPath.Points);

            string newPathJson = JsonSerializer.Serialize(newPath);
            await _blobStoreService.StoreText(BuildPathPath(agentId), newPathJson, overwriteExisting: true).ConfigureAwait(false);

            string geoJson = GeoJsonBuilder.AgentPath(newPath);
            await _blobStoreService.StoreText(BuildGeoJsonPathPath(agentId), geoJson, overwriteExisting: true).ConfigureAwait(false);

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
