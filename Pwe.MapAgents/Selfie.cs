using GoogleApis;
using Microsoft.Extensions.Logging;
using Pwe.AzureBloBStore;
using Pwe.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
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

        public async Task<(Image image, GeoCoord location)> Take(List<GeoCoord> path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var cactusBytes = await _blobStoreService.GetBytes("agentoutfits/cactus.png").ConfigureAwait(false);
            const int MaxAttempts = 20;
            for (int i = 0; i < MaxAttempts; ++i)
            {
                var randomPoint = path[_rnd.Next(path.Count)];
                var pictureBytes = await _streetView.GetRandomImage(randomPoint).ConfigureAwait(false);
                if (pictureBytes != null)
                {
                    var selfieImage = Apply(cactusBytes, pictureBytes);
                    return (selfieImage, randomPoint);
                }
            }
            return (null, default);
        }

        private static int Pct(int src, double pct) => (int)(src * pct);

        Image Apply(byte[] agentBytes, byte[] pictureBytes)
        {
            var cactusImg = Image.Load(agentBytes);
            var srcSize = cactusImg.Size();

            int agentRotation = _rnd.Next(20) - 10;
            cactusImg.Mutate(x => x.Rotate(agentRotation));
            cactusImg.Mutate(x => x.Saturate(0.8f));

            Image<Rgba32> dstImg;
            try
            {
                dstImg = Image.Load(pictureBytes);
            }
            catch(Exception e)
            {
                _logger.LogInformation($"Error loading streetview data from Google: {e}");
                return null;
            }
            var dstSize = dstImg.Size();

            // Get average luminance for the area around agent
            var agentArea = new Rectangle(Pct(dstSize.Width, 0.35), Pct(dstSize.Height, 0.8), Pct(dstSize.Width, 0.3), Pct(dstSize.Height, 0.2));
            var size1 = dstImg.Clone(x => x.Crop(agentArea).Resize(1, 1));
            var pixel1 = size1[0, 0];
            double lum = (pixel1.R * 0.299 + pixel1.G * 0.587 + pixel1.B * 0.114) / 255.0;

            // 0.38 = in shadow, 0.46 = cloudy, 0.66 = sunny
            float agentLum = (float)(lum + 0.4);
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
