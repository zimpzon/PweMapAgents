using Microsoft.Extensions.Logging;
using Pwe.AzureBloBStore;
using Pwe.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Pwe.World
{
    public class MapCoverage : IMapCoverage
    {
        private readonly IBlobStoreService _blobStoreService;
        private readonly ILogger _logger;

        public MapCoverage(IBlobStoreService blobStoreService, ILogger logger)
        {
            _blobStoreService = blobStoreService;
            _logger = logger;
        }

        public async Task UpdateCoverage(List<GeoCoord> points)
        {
            const int LayerMin = 2;
            const int LayerMax = 17;

            Dictionary<string, Image<Rgba32>> imageCache = new Dictionary<string, Image<Rgba32>>();
            Dictionary<string, long> pixelsChanged = new Dictionary<string, long>();

            async Task<Image<Rgba32>> getImage(string name)
            {
                if (imageCache.TryGetValue(name, out Image<Rgba32> cachedImage))
                    return cachedImage;

                var pngBytes = await _blobStoreService.GetBytes($"coveragetiles/{name}", throwIfNotFound: false);
                if (pngBytes != null)
                {
                    var imageFromBlob = Image.Load<Rgba32>(pngBytes);
                    imageCache[name] = imageFromBlob;
                    return imageFromBlob;
                }

                var newImage = new Image<Rgba32>(256, 256);
                imageCache[name] = newImage;
                return newImage;
            }

            static (int x, int y) GetLayerGlobalPixelPos(GeoCoord p, int zoom)
            {
                var (tileXAbs, tileYAbs) = TileMath.WorldToTilePos(p.Lon, p.Lat, zoom);
                int pixelX = (int)(tileXAbs * 256);
                int pixelY = (int)(tileYAbs * 256);
                return (pixelX, pixelY);
            }

            Rgba32 pixel = new Rgba32(0, 255, 0, 255);
            for (int zoom = LayerMin; zoom <= LayerMax; ++zoom)
            {
                int x0 = 0;
                int y0 = 0;
                int x1 = 0;
                int y1 = 0;
                if (zoom >= 16)
                {
                    x0 = -1; x1 = 1; y0 = -1; y1 = 1;
                }
                else if (zoom >= 14)
                {
                    x0 = 0; x1 = 1; y0 = 0; y1 = 1;
                }

                for (int i = 0; i < points.Count - 1; ++i)
                {
                    var p0 = points[i];
                    var p1 = points[i + 1];
                    var p0PixelPos = GetLayerGlobalPixelPos(p0, zoom);
                    var p1PixelPos = GetLayerGlobalPixelPos(p1, zoom);

                    async Task SetPixel(int globalX, int globalY)
                    {
                        int tileX = globalX >> 8;
                        int tileY = globalY >> 8;
                        int localX = globalX & 0xff;
                        int localY = globalY & 0xff;
                        string imageName = $"{tileX}-{tileY}-{zoom}.png";
                        pixelsChanged.TryGetValue(imageName, out long pixelChangeCount);

                        var image = await getImage(imageName);

                        for (int y = y0; y <= y1; ++y)
                        {
                            for (int x = x0; x <= x1; ++x)
                            {
                                int dstX = System.Math.Clamp(localX + x, 0, 255);
                                int dstY = System.Math.Clamp(localY + y, 0, 255);
                                if (image[dstX, dstY].G != pixel.G)
                                {
                                    image[dstX, dstY] = pixel;
                                    pixelChangeCount++;
                                }
                            }
                        }
                        pixelsChanged[imageName] = pixelChangeCount;
                    }

                    await GeoMath.Line(p0PixelPos.x, p0PixelPos.y, p1PixelPos.x, p1PixelPos.y, SetPixel);
                }
            }

            long imageWriteCount = 0;
            long imageSkipCount = 0;
            foreach (var pair in imageCache)
            {
                string imageName = pair.Key;
                var image = pair.Value;
                pixelsChanged.TryGetValue(imageName, out long pixelChangeCount);
                if (pixelChangeCount > 0)
                {
                    // Have to use temp file since image.SaveAsPng somehow didn't get all pixels in the final file.
                    string tempFile = Path.Combine(Path.GetTempPath(), imageName);
                    image.Save(tempFile);
                    var imageBytes = File.ReadAllBytes(tempFile);
                    File.Delete(tempFile);
                    await _blobStoreService.StoreBytes($"coveragetiles/{imageName}", imageBytes);
                    //File.WriteAllBytes($"c:\\temp\\{imageName}", imageBytes);
                    imageWriteCount++;
                }
                else
                {
                    imageSkipCount++;
                }
            }

            _logger.LogInformation($"Coverage images touched: {imageCache.Count}, written: {imageWriteCount}, skipped: {imageSkipCount}");
        }
    }
}
