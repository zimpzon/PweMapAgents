using System.Threading.Tasks;

namespace Pwe.OverpassTiles
{
    public interface IWayTileService
    {
        Task<WayTile> GetTile(long tileId, int zoom);
    }
}
