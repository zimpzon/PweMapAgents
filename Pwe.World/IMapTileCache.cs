using Pwe.OverpassTiles;
using System.Threading.Tasks;

namespace Pwe.World
{
    public interface IMapTileCache
    {
        Task<WayTile> GetTile(long tileId, int zoom);
    }
}
