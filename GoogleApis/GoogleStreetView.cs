using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pwe.Shared;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace GoogleApis
{
    public class GoogleStreetView : IStreetView
    {
        private readonly ILogger _logger;
        private readonly string _apiKey;

        public GoogleStreetView(ILogger logger, IConfiguration config)
        {
            _logger = logger;
            _apiKey = config["GoogleApiKey"];
        }

        public async Task<byte[]> Test(GeoCoord point)
        {
            var client = new HttpClient();
            HttpResponseMessage httpRes;

            httpRes = await client.GetAsync($"https://maps.googleapis.com/maps/api/streetview/metadata?&location={point.Lat},{point.Lon}&key={_apiKey}");
            string body = await httpRes.Content.ReadAsStringAsync();
            bool hasData = !body.Contains("ZERO_RESULTS");
            if (!hasData)
                return null;

            int heading = new Random().Next(360);
            int pitch = (new Random().Next(20)) - 10;
            int fov = new Random().Next(20) + 70;
            httpRes = await client.GetAsync($"https://maps.googleapis.com/maps/api/streetview?size=600x600&location={point.Lat},{point.Lon}&fov={fov}&heading={heading}&pitch={pitch}&key={_apiKey}");
            var bytes = await httpRes.Content.ReadAsByteArrayAsync();

            _logger.LogInformation($"Got StreetView image, bytes: {bytes.Length}");
            return bytes;
        }
    }
}
