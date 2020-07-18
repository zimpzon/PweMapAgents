using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pwe.Shared;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GoogleApis
{
    public class LocationInformation : ILocationInformation
    {
        static readonly NumberFormatInfo nfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };
        static string NumberStr(double d) => d.ToString(nfi);

        private readonly ILogger _logger;
        private readonly string _apiKey;

        public LocationInformation(ILogger logger, IConfiguration config)
        {
            _logger = logger;
            _apiKey = config["GoogleApiKey"];
        }

        public async Task<string> GetInformation(GeoCoord point)
        {
            var client = new HttpClient();
            HttpResponseMessage httpRes;

            httpRes = await client.GetAsync($"https://maps.googleapis.com/maps/api/geocode/json?latlng={NumberStr(point.Lat)},{NumberStr(point.Lon)}&key={_apiKey}&language=da");
            string body = await httpRes.Content.ReadAsStringAsync();
            bool hasData = !body.Contains("ZERO_RESULTS");
            if (!hasData)
                return null;

            var result = JsonSerializer.Deserialize<LocationInfo>(body);
            string formatted = (result?.results[0].formatted_address ?? "").Trim();
            _logger.LogInformation($"Got location info, formatted: {formatted}");
            return formatted;
        }
    }
}
