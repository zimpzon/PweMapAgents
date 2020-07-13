using Pwe.AzureBloBStore;
using Pwe.OverpassTiles;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pwe.World
{
    public class AzureBlobMapTileCache : IMapTileCache
    {
        private readonly IWayTileService _wayTileService;
        private readonly IBlobStoreService _blobStoreService;

        public AzureBlobMapTileCache(IWayTileService wayTileService, IBlobStoreService blobStoreService)
        {
            _wayTileService = wayTileService;
            _blobStoreService = blobStoreService;
        }

        public async Task<WayTile> GetTile(long tileId, int zoom)
        {
            string path = $"waytiles/{tileId}.json";
            string cachedJson = await _blobStoreService.GetText(path, throwIfNotFound: false);
            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                var result = JsonSerializer.Deserialize<WayTile>(cachedJson);
                return result;
            }

            var newTile = await _wayTileService.GetTile(tileId, zoom);
            string newJson = JsonSerializer.Serialize(newTile);
            await _blobStoreService.StoreText(path, newJson);
            return newTile;
        }
    }
}
