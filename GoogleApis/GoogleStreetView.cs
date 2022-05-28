using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pwe.Shared;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GoogleApis
{
    public class GoogleStreetView : IStreetView
    {
        static readonly NumberFormatInfo nfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };
        static string NumberStr(double d) => d.ToString(nfi);

        private readonly ILogger _logger;
        private readonly string _apiKey;

        public GoogleStreetView(ILogger logger, IConfiguration config)
        {
            _logger = logger;
            _apiKey = config["GoogleApiKey"];
        }

        public async Task<byte[]> GetRandomImage(GeoCoord point)
        {
            var client = new HttpClient();
            HttpResponseMessage httpRes;

            httpRes = await client.GetAsync($"https://maps.googleapis.com/maps/api/streetview/metadata?location={point.Lat},{point.Lon}&key={_apiKey}");
            string body = await httpRes.Content.ReadAsStringAsync();
            bool hasData = !body.Contains("ZERO_RESULTS");
            if (!hasData)
                return null;

            int heading = new Random().Next(360);
            int pitch = (new Random().Next(20)) - 10;
            int fov = new Random().Next(20) + 70;
            httpRes = await client.GetAsync($"https://maps.googleapis.com/maps/api/streetview?size=640x640&location={NumberStr(point.Lat)},{NumberStr(point.Lon)}&fov={fov}&heading={heading}&pitch={pitch}&key={_apiKey}");
            var bytes = await httpRes.Content.ReadAsByteArrayAsync();
            if (bytes.Length < 1000)
            {
                string txt = Encoding.UTF8.GetString(bytes);
                _logger.LogInformation($"Error getting StreetView image, bytes: {bytes.Length}, message: {txt}");
                return null;
            }

            _logger.LogInformation($"Got StreetView image, bytes: {bytes.Length}");
            return bytes;
        }
    }
}
