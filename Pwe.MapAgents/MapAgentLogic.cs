using GoogleApis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pwe.AzureBloBStore;
using Pwe.GeoJson;
using Pwe.OverpassTiles;
using Pwe.Shared;
using Pwe.World;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        private readonly ISelfie _selfie;
        private readonly IGraphPeek _graphPeek;
        private readonly ILocationInformation _locationInformation;
        private readonly IConfiguration _configuration;
        private readonly Random _rnd = new Random();

        static readonly NumberFormatInfo nfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };
        static string NumberStr(double d) => d.ToString(nfi);

        public MapAgentLogic(
            ILogger logger,
            IWorldGraph worldGraph,
            IBlobStoreService blobStoreService,
            IMapCoverage mapCoverage,
            ISelfie selfie,
            ILocationInformation locationInformation,
            IConfiguration configuration,
            IGraphPeek graphPeek)
        {
            _logger = logger;
            _worldGraph = worldGraph;
            _blobStoreService = blobStoreService;
            _mapCoverage = mapCoverage;
            _selfie = selfie;
            _locationInformation = locationInformation;
            _configuration = configuration;
            _graphPeek = graphPeek;
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

        private class Option
        {
            public WayTileNode Node { get; set; }
            public double BearingDiff { get; set; }
            public bool IsDeadEnd { get; set; }
            public long VisitedCount { get; set; }
            public long UnvisitedCount { get; set; }
            public long TotalVisitCount { get; set; }
            public double UnvisitedPct { get; set; }
            public double Score { get; set; }
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

            List<GeoCoord> deadEndDebugSegments = new List<GeoCoord>();
            List<GeoCoord> possibleWayoutDebugSegments = new List<GeoCoord>();

            double prevBearing = double.MaxValue;
            List<WayTileNode> conn = null;
            WayTileNode prevNode = null;
            WayTileNode node = startNode;
            WayTileNode nextNode = null;

            while (true)
            {
                conn = await _worldGraph.GetNodeConnections(node).ConfigureAwait(false);
                var options = conn.Select(x => new Option { Node = x }).ToList();

                // Don't go back if we can go forward
                if (options.Count > 1)
                    options = options.Where(x => x.Node != prevNode).ToList();

                foreach(var option in options)
                {
                    var (deadEndFound, unexploredNodeFound, visitedNodesFound, unvisitedNodesFound, totalVisitCount, exploredSegments) = await _graphPeek.Peek(node, option.Node).ConfigureAwait(false);
                    double bearing = GeoMath.CalculateBearing(node.Point, option.Node.Point);
                    option.BearingDiff = Math.Abs(bearing - prevBearing);
                    option.IsDeadEnd = deadEndFound;
                    option.UnvisitedCount = unvisitedNodesFound;
                    option.VisitedCount = visitedNodesFound;
                    option.TotalVisitCount = totalVisitCount;
                    option.UnvisitedPct = visitedNodesFound == 0 ? 100 : (double)unvisitedNodesFound / visitedNodesFound;
                    option.Score = ((unvisitedNodesFound + visitedNodesFound) / ((double)totalVisitCount + 1)) + _rnd.NextDouble() * 0.1; // Add a little randomness
                }

                // Scores should reflect the direction with the most unexplored nodes. Higher = better.
                //      If nothing was explored score = unvisitedNodesFound + visitedNodesFound
                //      If everything was explored exactly once, score = 1. Will go below 1 when visitcount gets bigger than 1.
                // If there are no detected unexplored areas nearby, prefer to go straight ahead to cover some ground quickly.
                bool preferStraight = false;
                bool onlyAlreadyExploredOptions = !options.Any(x => x.Score > 1.0);
                if (onlyAlreadyExploredOptions)
                {
                    long minVisitCount = options.Min(x => x.Node.VisitCount ?? 0);
                    bool allEqual = options.All(x => x.Node.VisitCount == minVisitCount);
                    if (allEqual)
                    {
                        preferStraight = onlyAlreadyExploredOptions && _rnd.NextDouble() < 0.9;
                    }
                    else
                    {
                        // All are already explored, but some have higher count than others. Pick from nodes with lowest count.
                        options = options.Where(x => x.Node.VisitCount == minVisitCount).ToList();
                    }
                }
                else
                {
                    // Pick best score
                    options = options.OrderByDescending(x => x.Score).ToList();
                }

                nextNode = options[0].Node;
                prevNode = node;
                node = nextNode;
                prevBearing = GeoMath.CalculateBearing(prevNode.Point, node.Point);

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

            //if (deadEndDebugSegments.Count > 0)
            //{
            //    string geo = GeoJsonBuilder.Segments(deadEndDebugSegments);
            //    await _blobStoreService.StoreText($"debug/skipped-deadends/deadends-{deadEndDebugSegments.Count}-{DateTime.UtcNow.Ticks}.json", geo).ConfigureAwait(false);
            //}

            //if (possibleWayoutDebugSegments.Count > 0)
            //{
            //    string geo = GeoJsonBuilder.Segments(possibleWayoutDebugSegments);
            //    await _blobStoreService.StoreText($"debug/possible-way-out/wayout-{possibleWayoutDebugSegments.Count}-{DateTime.UtcNow.Ticks}.json", geo).ConfigureAwait(false);
            //}

            await _worldGraph.StoreUpdatedVisitCounts().ConfigureAwait(false);

            newPath.PathMs = pathMs;
            newPath.TileIds = _worldGraph.GetLoadedTiles().Select(tile => tile.Id).ToList();
            newPath.EncodedPolyline = GooglePolylineConverter.Encode(newPath.Points);

            string newPathJson = JsonSerializer.Serialize(newPath);
            await _blobStoreService.StoreText(BuildPathPath(agentId), newPathJson, overwriteExisting: true).ConfigureAwait(false);

            //string geoJson = GeoJsonBuilder.AgentPath(newPath);
            //await _blobStoreService.StoreText(BuildGeoJsonPathPath(agentId), geoJson, overwriteExisting: true).ConfigureAwait(false);

            var clientPath = new AgentClientPath
            {
                MsStart = newPath.PointAbsTimestampMs.First(),
                MsEnd = newPath.PointAbsTimestampMs.Last(),
                EncodedPolyline = newPath.EncodedPolyline
            };
            string clientPathJson = JsonSerializer.Serialize(clientPath);
            await _blobStoreService.StoreText(BuildClientPathPath(agentId), clientPathJson, overwriteExisting: true).ConfigureAwait(false);

            await TryTakeSelfie(oldPath.Points).ConfigureAwait(false);
        }

        async Task TryTakeSelfie(List<GeoCoord> path)
        {
            if (await _selfie.IsSelfiePending().ConfigureAwait(false))
            {
                var (image, location) = await _selfie.Take(path).ConfigureAwait(false);
                if (image == null || location == null)
                {
                    _logger.LogInformation("No selfie returned from selfie service, aborting");
                    return;
                }

                string imageInfo = await _locationInformation.GetInformation(location).ConfigureAwait(false);
                string mapUrl = $"https://www.google.com/maps/search/?api=1&query={NumberStr(location.Lat)},{NumberStr(location.Lon)}";
                string message = $"{imageInfo}\n{mapUrl}";
                await PostToTwitter(image, message, location).ConfigureAwait(false);

                await _selfie.MarkPendingSelfieTaken().ConfigureAwait(false);
            }
        }

        async Task PostToTwitter(Image image, string message, GeoCoord location)
        {
            using var memStream = new MemoryStream();
            image.SaveAsPng(memStream);
            memStream.Position = 0;

            var tokens = CoreTweet.Tokens.Create(_configuration["TwitterConsumerKey"], _configuration["TwitterConsumerSecret"], _configuration["TwitterAccessToken"], _configuration["TwitterAccessSecret"]);
            var uploadResult = await tokens.Media.UploadAsync(memStream).ConfigureAwait(false);
            var media = new List<long> { uploadResult.MediaId };
            await tokens.Statuses.UpdateAsync(message, null, null, location.Lat, location.Lon, null, true, null, media).ConfigureAwait(false);
        }
    }
}
