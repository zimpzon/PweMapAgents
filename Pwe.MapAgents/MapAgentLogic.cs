﻿using GoogleApis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pwe.AzureBloBStore;
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
        private readonly IPinning _pinning;
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
            IGraphPeek graphPeek,
            IPinning pinning)
        {
            _logger = logger;
            _worldGraph = worldGraph;
            _blobStoreService = blobStoreService;
            _mapCoverage = mapCoverage;
            _selfie = selfie;
            _locationInformation = locationInformation;
            _configuration = configuration;
            _graphPeek = graphPeek;
            _pinning = pinning;
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

            // Keep all points that are in the future - and one point in the past so we can interpolate the line segment
            List<GeoCoord> visitedPoints = new List<GeoCoord>();
            long msPrev = -1;

            for (int i = 0; i < oldPath.PointAbsTimestampMs.Count; ++i)
            {
                bool pointIsInTheFuture = oldPath.PointAbsTimestampMs[i] >= pathUnixMs;
                bool nextPointIsInTheFuture = i < oldPath.PointAbsTimestampMs.Count - 1 && oldPath.PointAbsTimestampMs[i + 1] >= pathUnixMs;
                bool keepPoint = pointIsInTheFuture || nextPointIsInTheFuture;
                if (keepPoint)
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

            // Make sure all visited lines are drawn by adding the points that are cut off.
            if (newPath.Points.Count > 1)
            {
                visitedPoints.Add(newPath.Points[0]);
                visitedPoints.Add(newPath.Points[1]);
            }

            await _mapCoverage.UpdateCoverage(visitedPoints).ConfigureAwait(false);

            // The following code block places the agent at a hardcoded point.
            {
                // If stuck, clear newPath and add a single point at or near a valid location. Then run update once from Cmd. A new valid path should now be written.
                // 55.6336876,37.5789257
                // Do not include this code block if publishing to Azure!
                var newStartPoint = new GeoCoord(5.92891, 52.94243);
                newPath.Points.Clear();
                newPath.Points.Add(newStartPoint);
                newPath.PointAbsTimestampMs.Clear();
                newPath.PointAbsTimestampMs.Add(GeoMath.UnixMs());
                var newPin = new Pin
                {
                    Center = newStartPoint,
                    TimeoutUtc = DateTime.UtcNow.AddHours(4),
                    SelfiesLeft = 10,
                    NextSelfieTimeUtc = DateTime.UtcNow.AddMinutes(3), // Make sure first selfie is in next update, not this one (selfie uses the previous path, not the one generated now).
                    MaxDistanceMeters = 1000,
                    MinTimeBetweenSelfies = TimeSpan.FromMinutes(20),
                    MaxTimeBetweenSelfies = TimeSpan.FromMinutes(30),
                };
                await _pinning.StorePinning(newPin).ConfigureAwait(false);
            }

            if (newPath.Points.Count == 0)
            {
                // Path is out of date, nothing to keep. Reuse latest known position.
                int oldCount = oldPath.Points.Count;
                newPath.Points.Clear();
                newPath.PointAbsTimestampMs.Clear();
                newPath.Points.Add(oldPath.Points[oldCount - 1]);
                newPath.Points.Add(oldPath.Points[oldCount - 1]);
                newPath.PointAbsTimestampMs.Add(GeoMath.UnixMs());
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

            var pin = await _pinning.GetCurrentPinning().ConfigureAwait(false);

            while (true)
            {
                conn = await _worldGraph.GetNodeConnections(node).ConfigureAwait(false);

                // I have seen GetNodeConnections return the same node twice, remove duplicates.
                var grouped = conn.GroupBy(c => c.Id).Select(g => g.First()).ToList();
                if (grouped.Count != conn.Count)
                {
                    _logger.LogWarning($"GetNodeConnections returned duplicates, removed (count before: {conn.Count}, count after: {grouped.Count})");
                    conn = grouped;
                }

                var options = conn.Select(x => new Option { Node = x }).ToList();
                if (pin != null)
                {
                    // Remove options that are too far away from pinning point.
                    int countBefore = options.Count;
                    options = options.Where(o => GeoMath.MetersDistanceTo(pin.Center, o.Node.Point) <= pin.MaxDistanceMeters).ToList();
                    int countAfter = options.Count;
                    if (countAfter != countBefore)
                        _logger.LogInformation($"Removed {countBefore - countAfter} node connection(s) too far away from pinning");
                }

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

                    // Absolute score - number of new nodes scores high.
                    option.Score = (unvisitedNodesFound - totalVisitCount) + _rnd.Next(0, 5); // Add a little randomness

                    // Percentage score - small dead ends scores high.
                    //option.Score = ((unvisitedNodesFound + visitedNodesFound) / ((double)totalVisitCount + 1)) + _rnd.NextDouble() * 0.1; // Add a little randomness
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

                // Most of the time select element 0, but sometimes randomly select another
                int maxIdx = options.Count > 1 ? 1 : 0;
                int selectedIdx = _rnd.NextDouble() < 0.95 ? 0 : _rnd.Next(0, maxIdx + 1);
                nextNode = options[selectedIdx].Node;
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

            if (pin != null)
            {
                await TryTakePinnedSelfie(oldPath.Points, pin).ConfigureAwait(false);
                await _pinning.StorePinning(pin).ConfigureAwait(false);
            }
            else
            {
                await TryTakeSelfie(oldPath.Points).ConfigureAwait(false);
            }
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

        async Task TryTakePinnedSelfie(List<GeoCoord> path, Pin pin)
        {
            if (DateTime.UtcNow > pin.NextSelfieTimeUtc)
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

                var delaySeconds = _rnd.Next((int)pin.MinTimeBetweenSelfies.TotalSeconds, (int)pin.MaxTimeBetweenSelfies.TotalSeconds);
                pin.NextSelfieTimeUtc = DateTime.UtcNow.AddSeconds(delaySeconds);
                pin.SelfiesLeft -= 1;
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
