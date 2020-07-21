using Pwe.OverpassTiles;
using Pwe.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pwe.World
{
    public class WorldGraph : IWorldGraph
    {
        public const int Zoom = 14;

        private readonly List<WayTile> _tiles = new List<WayTile>();
        private readonly Random _rnd = new Random();
        private readonly Dictionary<long, WayTileNode> _nodeLut = new Dictionary<long, WayTileNode>();
        private readonly IMapTileCache _mapTileCache;

        public WorldGraph(IMapTileCache mapTileCache)
        {
            _mapTileCache = mapTileCache;
        }

        public List<WayTile> GetLoadedTiles()
            => _tiles;

        // Finds close node within 2 x 2 tiles, may find none if there are no nodes nearby.
        public async Task<WayTileNode> GetNearbyNode(GeoCoord point)
        {
            await LoadNearbyTiles(point);
            var result = FindClosestLoadedNode(point);
            return result;
        }

        // Will auto-load connected tiles
        public async Task<List<WayTileNode>> GetNodeConnections(WayTileNode node, bool updateVisitCount = true)
        {
            // If node is outside bounds load the linked tile
            if (!node.Inside.HasValue)
            {
                await LoadTileAtPoint(node.Point);

                // If node was a link to a new tile the id will now be replaced in the LUT, but the "real" node.
                node = _nodeLut[node.Id];
            }

            node.VisitCount++;
            return node.Conn.Select(id => _nodeLut[id]).ToList();
        }

        public async Task StoreUpdatedVisitCounts()
        {
            var nodeVisits = _tiles.Select(t => new TileVisits { TileId = t.Id }).ToList();
            foreach(var node in _nodeLut.Values.Where(node => node.VisitCount > 0))
            {
                var visitTile = nodeVisits.Where(vt => vt.TileId == node.TileId).FirstOrDefault();
                visitTile.NodeCounts.Add(new NodeCount { NodeId = node.Id, Count = node.VisitCount.Value });
            }
            await _mapTileCache.StoreTileVisits(nodeVisits);
        }

        WayTileNode FindClosestLoadedNode(GeoCoord point)
        {
            WayTileNode result = null;
            double closest = double.MaxValue;
            foreach(var node in _nodeLut.Values)
            {
                double distanceX = node.Point.Lon - point.Lon;
                double distanceY = node.Point.Lat - point.Lat;
                double distanceSqr = (distanceX * distanceX) + (distanceY * distanceY);
                if (distanceSqr < closest)
                {
                    closest = distanceSqr;
                    result = node;
                }
            }
            return result;
        }

        async Task LoadNearbyTiles(GeoCoord point)
        {
            await LoadTileAtPoint(point);
        }

        async Task LoadTileAtPoint(GeoCoord point)
        {
            long tileId = TileMath.GetTileId(point.Lon, point.Lat, Zoom);
            await LoadTile(tileId);
        }

        async Task LoadTile(long tileId)
        {
            if (!_tiles.Any(t => t.Id == tileId))
            {
                var tile = await _mapTileCache.GetTile(tileId, Zoom).ConfigureAwait(false);
                AddTile(tile);

                var tileVisits = await _mapTileCache.GetTileVisits(tileId).ConfigureAwait(false);
                foreach(var nodeCount in tileVisits.NodeCounts)
                {
                    var node = _nodeLut[nodeCount.NodeId];
                    node.VisitCount = nodeCount.Count;
                }
            }
        }

        public WayTileNode GetRandomNode()
        {
            int tileIdx = _rnd.Next(_tiles.Count);
            var tile = _tiles[tileIdx];
            int nodeIdx = _rnd.Next(tile.Nodes.Count);
            var node = tile.Nodes[nodeIdx];
            return node;
        }

        void AddTile(WayTile tile)
        {
            if (_tiles.Any(t => t.Id == tile.Id))
                return;

            _tiles.Add(tile);

            var (lon0, lat0, lon1, lat1) = TileMath.GetTileBounds(tile.Id, Zoom);
            foreach(var node in tile.Nodes)
            {
                node.TileId = tile.Id;
                node.VisitCount = 0;

                if (_nodeLut.TryGetValue(node.Id, out WayTileNode existingNode))
                {
                    // It is possible that both nodes are inside their own tile if they are exactly on the border. Which one wins doesn't matter.
                    var insideNode = existingNode.Inside.HasValue ? existingNode : node;
                    var outsideNode = existingNode.Inside.HasValue ? node : existingNode;

                    // Node id already exists. This is a link between two tiles. The existing node should be outside its tile
                    // and the new one should be inside. Otherwise something is wrong.
                    // 1) Let the node inside its own tile inherit the connections of the other
                    var tileLinkConnections = outsideNode.Conn.Where(id => insideNode.Conn.Any(idOther => idOther == id)).ToList();
                    insideNode.Conn.AddRange(tileLinkConnections);
                    // 2) Overwrite the old one with the new one in the LUT.
                    _nodeLut[node.Id] = insideNode;
                }
                else
                {
                    _nodeLut[node.Id] = node;
                }
            }

            var outside = _nodeLut.Values.Where(v => !v.Inside.HasValue).ToList();
            int loaded = 0;
            int notLoaded = 0;
            foreach(var n in outside)
            {
                if (!n.Inside.HasValue)
                {
                    long tileId = TileMath.GetTileId(n.Point.Lon, n.Point.Lat, Zoom);
                    if (_tiles.Any(t => t.Id == tileId))
                    {
                        loaded++;
                    }
                    else
                    {
                        notLoaded++;
                    }
                }
                // all outside nodes should be in tiles NOT loaded. 
            }
        }
    }
}
