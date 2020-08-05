using Microsoft.Extensions.Logging;
using Pwe.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pwe.OverpassTiles
{
    public class OverpassWayTileService : IWayTileService
    {
        const string UrlPrimary = "https://overpass.kumi.systems/api/interpreter";
        const string UrlSecondary = "https://overpass-api.de/api/interpreter";

        static readonly NumberFormatInfo nfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };
        static string NumberStr(double d) => d.ToString(nfi);
        static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true, };

        private readonly ILogger _logger;
        private string _currentUrl;

        public OverpassWayTileService(ILogger logger)
        {
            _logger = logger;
            _currentUrl = UrlPrimary;
        }

        public async Task<WayTile> GetTile(long tileId, int zoom)
        {
            var bbox = TileMath.GetTileBounds(tileId, zoom);
            string dbgMsg = $"TileId {tileId}, zoom: {zoom}, bbox: ({bbox.lon0}, {bbox.lat0}, {bbox.lon1}, {bbox.lat1})";
            _logger.LogInformation($"Loading: {dbgMsg}");

            // https://wiki.openstreetmap.org/wiki/Bounding_Box
            // The order of values in the bounding box used by Overpass API is (South, West, North, East):
            // minimum latitude, minimum longitude, maximum latitude, maximum longitude
            string cmd = $"[out: json];way(if:is_tag(\"highway\") || is_tag(\"route\"))({NumberStr(bbox.lat1)}, {NumberStr(bbox.lon0)}, {NumberStr(bbox.lat0)}, {NumberStr(bbox.lon1)});out qt;node(w);out skel qt;";
            var client = new HttpClient();
            var content = new StringContent(cmd);
            OsmResponse osmResponse = null;
            string body = "";
            HttpResponseMessage httpRes = null;

            int attempt = 0;
            while (true)
            {
                try
                {
                    var sw = Stopwatch.StartNew();
                    httpRes = await client.PostAsync(_currentUrl, content);
                    body = await httpRes.Content.ReadAsStringAsync();
                    long elapsed = sw.ElapsedMilliseconds;
                    osmResponse = JsonSerializer.Deserialize<OsmResponse>(body, SerializerOptions);
                    _logger.LogInformation($"Loaded (ms: {elapsed}, chars: {body.Length}, elements: {osmResponse.Elements.Count}): {dbgMsg}, url: {_currentUrl}");
                    break;
                }
                catch (Exception e)
                {
                    if (++attempt > 3)
                    {
                        _logger.LogWarning($"Giving up getting tile: {dbgMsg}, exception: {e}");
                        return null;
                    }

                    // Try switching URL on errors
                    _currentUrl = _currentUrl == UrlPrimary ? UrlSecondary : UrlPrimary;

                    _logger.LogInformation($"Retrying tile: {dbgMsg}, url: {_currentUrl}, exception: {e}");
                    await Task.Delay(200);
                }
            }

            var osmNodes = osmResponse.Elements.Where(e => e.Type == "node").ToList();
            var osmWays = osmResponse.Elements.Where(e => e.Type == "way").ToList();

            var result = new WayTile();
            result.Id = tileId;

            var nodeLut = new Dictionary<long, WayTileNode>();

            // First create all nodes...
            foreach (var osmNode in osmNodes)
            {
                var node = new WayTileNode();
                node.Id = osmNode.Id;
                node.Point = new GeoCoord(osmNode.Lon, osmNode.Lat);
                node.Inside = TileMath.IsInsideBounds(osmNode.Lon, osmNode.Lat, bbox) ? (byte?)1 : null;

                nodeLut[osmNode.Id] = node;
                result.Nodes.Add(node);
            }

            // ...then add connections between them
            foreach (var osmWay in osmWays)
            {
                int nodeCount = osmWay.Nodes.Count;
                for (int i = 0; i < nodeCount - 1; ++i)
                {
                    var wayPointA = osmWay.Nodes[i];
                    var wayPointB = osmWay.Nodes[i + 1];
                    nodeLut.TryGetValue(wayPointA, out WayTileNode nodeA);
                    nodeLut.TryGetValue(wayPointB, out WayTileNode nodeB);
                    if (nodeA != null && nodeB != null)
                    {
                        // Skip connection if both nodes are out of bounds
                        bool withinBoundsA = TileMath.IsInsideBounds(nodeA.Point.Lon, nodeA.Point.Lat, bbox);
                        bool withinBoundsB = TileMath.IsInsideBounds(nodeB.Point.Lon, nodeB.Point.Lat, bbox);
                        if (withinBoundsA || withinBoundsB)
                        {
                            nodeA.Conn.Add(wayPointB);
                            nodeB.Conn.Add(wayPointA);
                        }
                    }
                }
            }

            // Remove orphaned nodes outside of bounds
            result.Nodes = result.Nodes.Where(n => n.Conn.Count > 0).ToList();
            _logger.LogInformation($"Parsed (nodes: {result.Nodes.Count}): {dbgMsg}");

            return result;
        }

        private class OsmResponse
        {
            public List<OsmElement> Elements { get; set; } = new List<OsmElement>();
        }

        private class OsmElement
        {
            public string Type { get; set; }
            public long Id { get; set; }
            public List<long> Nodes { get; set; } = new List<long>();
            public double Lon { get; set; }
            public double Lat { get; set; }
        }
    }
}
