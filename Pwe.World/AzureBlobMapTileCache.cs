using Pwe.OverpassTiles;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pwe.World
{
    public class AzureBlobMapTileCache : IMapTileCache
    {
        const string Folder = "c:\\temp\\maptiles";
        private readonly IWayTileService _wayTileService;

        public AzureBlobMapTileCache(IWayTileService wayTileService)
        {
            _wayTileService = wayTileService;
        }

        public async Task<WayTile> GetTile(long tileId, int zoom)
        {
            string path = Path.Combine(Folder, tileId.ToString() + ".json");
            if (File.Exists(path))
            {
                string cachedJson = await File.ReadAllTextAsync(path);
                var result = JsonSerializer.Deserialize<WayTile>(cachedJson);
                return result;
            }

            var newTile = await _wayTileService.GetTile(tileId, zoom);
            string newJson = JsonSerializer.Serialize(newTile);
            await File.WriteAllTextAsync(path, newJson);
            return newTile;
        }
    }
}
