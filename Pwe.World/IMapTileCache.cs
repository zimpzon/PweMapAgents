using Pwe.OverpassTiles;
using System.Threading.Tasks;

namespace Pwe.World
{
    internal interface IMapTileCache
    {
        Task<WayTile> GetTile(long tileId, int zoom);
    }
}
