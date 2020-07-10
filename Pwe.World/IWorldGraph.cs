using Pwe.OverpassTiles;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pwe.World
{
    public interface IWorldGraph
    {
        Task<WayTileNode> GetNearbyNode(double lon, double lat);
        Task<List<WayTileNode>> GetNodeConnections(WayTileNode node);
    }
}
