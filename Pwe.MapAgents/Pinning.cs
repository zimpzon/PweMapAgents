using Newtonsoft.Json;
using Pwe.AzureBloBStore;
using Pwe.Shared;
using System;
using System.Threading.Tasks;

namespace Pwe.MapAgents
{
    public class Pinning : IPinning
    {
        private readonly IBlobStoreService _blobStoreService;

        public Pinning(IBlobStoreService blobStoreService)
        {
            _blobStoreService = blobStoreService;
        }
        const string PathCurrentPinning = "pinning/current.json";

        public async Task<Pin> GetCurrentPinning()
        {
            bool hasPinning = await _blobStoreService.Exists(PathCurrentPinning).ConfigureAwait(false);
            if (!hasPinning)
                return null;

            var pinJson = await _blobStoreService.GetText(PathCurrentPinning, throwIfNotFound: true).ConfigureAwait(false);
            var pin = JsonConvert.DeserializeObject<Pin>(pinJson);
            if (DateTime.UtcNow > pin.TimeoutUtc || pin.SelfiesLeft <= 0)
                return null;

            return pin;
        }

        public async Task StorePinning(Pin pinning)
        {
            string pinJson = JsonConvert.SerializeObject(pinning);
            await _blobStoreService.StoreText(PathCurrentPinning, pinJson).ConfigureAwait(false);
        }
    }
}
