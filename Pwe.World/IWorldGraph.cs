using Pwe.OverpassTiles;
using Pwe.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pwe.World
{
    public interface IWorldGraph
    {
        List<WayTile> GetLoadedTiles();
        Task<WayTileNode> GetNearbyNode(GeoCoord point);
        Task<List<WayTileNode>> GetNodeConnections(WayTileNode node);
    }
}
