using Pwe.AzureBloBStore;
using Pwe.OverpassTiles;
using Pwe.Shared;
using System.Collections.Generic;
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

        public async Task<TileVisits> GetTileVisits(long tileId)
        {
            string path = $"tilevisits/{tileId}.json";
            string cachedJson = await _blobStoreService.GetText(path, throwIfNotFound: false);
            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                var result = JsonSerializer.Deserialize<TileVisits>(cachedJson);
                return result;
            }

            OnTileFirstVisit(tileId);
            return new TileVisits { TileId = tileId };
        }

        void OnTileFirstVisit(long tileId)
        {
            // Update coverage image(s)
        }

        public async Task StoreTileVisits(List<TileVisits> tileVisits)
        {
            foreach(var visits in tileVisits)
            {
                string json = JsonSerializer.Serialize(visits);
                string path = $"tilevisits/{visits.TileId}.json";
                await _blobStoreService.StoreText(path, json);
            }
        }
    }
}
