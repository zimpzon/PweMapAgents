using GoogleApis;
using Microsoft.Extensions.Logging;
using Pwe.AzureBloBStore;
using Pwe.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pwe.MapAgents
{
    public class Selfie : ISelfie
    {
        private readonly IStreetView _streetView;
        private readonly IBlobStoreService _blobStoreService;
        private readonly ILogger _logger;
        private readonly Random _rnd = new Random();

        public Selfie(IStreetView streetView, IBlobStoreService blobStoreService, ILogger logger)
        {
            _streetView = streetView;
            _blobStoreService = blobStoreService;
            _logger = logger;
        }

        class OutfitPaths
        {
            public List<string> Sunglasses { get; set; }
            public List<string> Facemask { get; set; }
            public List<string> Default { get; set; }
        }

        class SelfieDateDto
        {
            public int Count { get; set; }
            public DateTime? NotBefore{ get; set; }
        }

        private static string GetSelfiePath()
        {
            string dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return  $"selfies/dto/{dateStr}";
        }

        async Task<SelfieDateDto> GetSelfieDto()
        {
            string path = GetSelfiePath();
            string json = await _blobStoreService.GetText(path, throwIfNotFound: false).ConfigureAwait(false);
            var dto = string.IsNullOrWhiteSpace(json) ? new SelfieDateDto() : JsonSerializer.Deserialize<SelfieDateDto>(json);
            return dto;
        }

        async Task SaveSelfieDto(SelfieDateDto dto)
        {
            string path = GetSelfiePath();
            await _blobStoreService.StoreText(path, JsonSerializer.Serialize(dto)).ConfigureAwait(false);
        }

        public async Task<bool> IsSelfiePending(int maxPerDay = 3)
        {
            var dto = await GetSelfieDto().ConfigureAwait(false);
            if (!dto.NotBefore.HasValue)
            {
                dto.NotBefore = DateTime.UtcNow.Date.AddHours(_rnd.NextDouble() * 3 + 6); // First selfie between hour X and Y (UTC)
                await SaveSelfieDto(dto).ConfigureAwait(false);
            }

            bool result = dto.Count < maxPerDay && DateTime.UtcNow > dto.NotBefore;
            _logger.LogInformation($"IsSelfiePending? count = {dto.Count}/{maxPerDay}, ready in: {dto.NotBefore - DateTime.UtcNow}");
            return result;
        }

        public async Task MarkPendingSelfieTaken()
        {
            var dto = await GetSelfieDto().ConfigureAwait(false);
            dto.Count++;
            if (dto.NotBefore.HasValue)
            {
                dto.NotBefore = dto.NotBefore.Value.AddHours(_rnd.NextDouble() * 3 + 1); // Add minimum hours before next selfie
            }
            _logger.LogInformation($"Selfie marked taken, new count: {dto.Count}, next notBefore: {dto.NotBefore}");
            await SaveSelfieDto(dto).ConfigureAwait(false);
        }

        public async Task<(Image image, GeoCoord location)> Take(List<GeoCoord> path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var outfits = (await _blobStoreService.GetBlobsInFolder("agentoutfits", includeSubfolders: false, returnFullPath: true).ConfigureAwait(false)).ToList();
            var outfitPaths = new OutfitPaths
            {
                Sunglasses = outfits.Where(o => o.Contains("glasses", StringComparison.InvariantCultureIgnoreCase)).ToList(),
                Facemask = outfits.Where(o => o.Contains("facemask", StringComparison.InvariantCultureIgnoreCase)).ToList(),
                Default = outfits.Where(o => !o.Contains("_", StringComparison.InvariantCultureIgnoreCase)).ToList(),
            };

            const int MaxAttempts = 5;
            for (int i = 0; i < MaxAttempts; ++i)
            {
                var randomPoint = path[_rnd.Next(path.Count)];
                var pictureBytes = await _streetView.GetRandomImage(randomPoint).ConfigureAwait(false);
                if (pictureBytes != null)
                {
                    var selfieImage = await Apply(pictureBytes, outfitPaths).ConfigureAwait(false);
                    return (selfieImage, randomPoint);
                }
            }

            _logger.LogInformation("No street view images found, aborting");
            return (null, default);
        }

        private static int Pct(int src, double pct) => (int)(src * pct);

        async Task<byte[]> GetOutfitBytes(double agentLuminance, OutfitPaths outfitPaths)
        {
            // 0.38 = in shadow, 0.46 = cloudy, 0.66 = sunny
            string path;
            if (agentLuminance > 0.7f)
            {
                int glassesIdx = DateTime.UtcNow.Day % outfitPaths.Sunglasses.Count;
                path = outfitPaths.Sunglasses[glassesIdx];
            }
            else
            {
                if (_rnd.NextDouble() < 0.1)
                    path = outfitPaths.Facemask[0];
                else
                    path = outfitPaths.Default[0];
            }
            _logger.LogInformation($"Selfie outfit chosen: {path}, agentLuminance: {agentLuminance}");

            var agentBytes = await _blobStoreService.GetBytes(path).ConfigureAwait(false);
            return agentBytes;
        }

        async Task<Image> Apply(byte[] pictureBytes, OutfitPaths outfitPaths)
        {
            Image<Rgba32> dstImg = Image.Load(pictureBytes);
            var dstSize = dstImg.Size();

            // Get average luminance for the area around agent
            var agentArea = new Rectangle(Pct(dstSize.Width, 0.35), Pct(dstSize.Height, 0.8), Pct(dstSize.Width, 0.3), Pct(dstSize.Height, 0.2));
            var size1 = dstImg.Clone(x => x.Crop(agentArea).Resize(1, 1));
            var pixel1 = size1[0, 0];
            double lum = (pixel1.R * 0.299 + pixel1.G * 0.587 + pixel1.B * 0.114) / 255.0;

            float agentLum = (float)(lum + 0.4);
            var agentBytes = await GetOutfitBytes(lum, outfitPaths).ConfigureAwait(false);

            var cactusImg = Image.Load(agentBytes);
            var srcSize = cactusImg.Size();

            int agentRotation = _rnd.Next(20) - 10;
            cactusImg.Mutate(x => x.Rotate(agentRotation));
            cactusImg.Mutate(x => x.Saturate(0.8f));

            cactusImg.Mutate(x => x.Brightness(agentLum));

            int selfieX = Pct(dstSize.Width, 0.45) + _rnd.Next(Pct(dstSize.Width, 0.1)) - Pct(srcSize.Width, 0.5);
            int offsetBottom = _rnd.Next(Pct(srcSize.Height, 0.15));
            int selfieY = dstSize.Height - Pct(srcSize.Height, 0.9) + offsetBottom;
            var selfiePoint = new Point(selfieX, selfieY);

            dstImg.Mutate(x => x.DrawImage(cactusImg, selfiePoint, 1));
            return dstImg;
        }
    }
}
