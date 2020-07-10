using Pwe.OverpassTiles;
using Pwe.TileUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pwe.World
{
    public class WorldGraph : IWorldGraph
    {
        const int Zoom = 14;

        private readonly List<WayTile> _tiles = new List<WayTile>();
        private readonly Random _rnd = new Random();
        private readonly Dictionary<long, WayTileNode> _nodeLut = new Dictionary<long, WayTileNode>();
        private readonly IMapTileCache _mapTileCache;

        public WorldGraph(IWayTileService wayTileService)
        {
            _mapTileCache = new AzureBlobMapTileCache(wayTileService);
        }

        // Finds close node within 2 x 2 tiles, may find none if there are no nodes nearby.
        public async Task<WayTileNode> GetNearbyNode(double lon, double lat)
        {
            await LoadNearbyTiles(lon, lat);
            var result = FindClosestLoadedNode(lon, lat);
            return result;
        }

        // Will auto-load connected tiles
        public async Task<List<WayTileNode>> GetNodeConnections(WayTileNode node)
        {
            // If node is outside bounds load the linked tile
            if (!node.Inside.HasValue)
                await LoadTileAtPoint(node.Lon, node.Lat);

            return node.Conn.Select(id => _nodeLut[id]).ToList();
        }

        WayTileNode FindClosestLoadedNode(double lon, double lat)
        {
            WayTileNode result = null;
            double closest = double.MaxValue;
            foreach(var node in _nodeLut.Values)
            {
                double distanceX = node.Lon - lon;
                double distanceY = node.Lat - lat;
                double distanceSqr = (distanceX * distanceX) + (distanceY * distanceY);
                if (distanceSqr < closest)
                {
                    closest = distanceSqr;
                    result = node;
                }
            }
            return result;
        }

        async Task LoadNearbyTiles(double lon, double lat)
        {
            await LoadTileAtPoint(lon, lat);
        }

        async Task LoadTileAtPoint(double lon, double lat)
        {
            long tileId = TileMath.GetTileId(lon, lat, Zoom);
            await LoadTile(tileId);
        }

        async Task LoadTile(long tileId)
        {
            if (!_tiles.Any(t => t.Id == tileId))
            {
                var tile = await _mapTileCache.GetTile(tileId, Zoom);
                AddTile(tile);
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

            var bounds = TileMath.GetTileBounds(tile.Id, Zoom);
            foreach(var node in tile.Nodes)
            {
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
                    long tileId = TileMath.GetTileId(n.Lon, n.Lat, Zoom);
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
