using Pwe.OverpassTiles;
using Pwe.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pwe.World
{
    public interface IMapTileCache
    {
        Task<WayTile> GetTile(long tileId, int zoom);
        Task<TileVisits> GetTileVisits(long tileId);
        Task StoreTileVisits(List<TileVisits> tileVisits);
    }
}
