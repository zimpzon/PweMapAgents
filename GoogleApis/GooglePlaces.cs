using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pwe.Shared;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace GoogleApis
{
    public class GooglePlaces : IPlaces
    {
        static readonly NumberFormatInfo nfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };
        static string NumberStr(double d) => d.ToString(nfi);

        private readonly ILogger _logger;
        private readonly string _apiKey;

        public GooglePlaces(ILogger logger, IConfiguration config)
        {
            _logger = logger;
            _apiKey = config["GoogleApiKey"];
        }

        /// <summary>
        /// Places is skipped for now since the many of the images returned are not suited for selfies
        /// </summary>
        public async Task GetNearbyPlaces(GeoCoord location, long radiusMeters)
        {
            var client = new HttpClient();
            HttpResponseMessage httpRes;

            httpRes = await client.GetAsync($"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={location.Lat},{location.Lon}&key={_apiKey}");
            string body = await httpRes.Content.ReadAsStringAsync();
            bool hasData = !body.Contains("ZERO_RESULTS");
            if (!hasData)
                return;

            _logger.LogInformation($"Got Places data");
        }
    }
}
