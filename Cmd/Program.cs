using CoreTweet;
using GoogleApis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pwe.AzureBloBStore;
using Pwe.GeoJson;
using Pwe.MapAgents;
using Pwe.OverpassTiles;
using Pwe.World;
using SixLabors.ImageSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Cmd
{
    class Program
    {
        static async Task Main(string[] _)
        {
            var services = BuildServiceProvider();
            var config = services.GetRequiredService<IConfiguration>();
            var tokens = Tokens.Create(config["TwitterConsumerKey"], config["TwitterConsumerSecret"], config["TwitterAccessToken"], config["TwitterAccessSecret"]);

            var blobs = services.GetRequiredService<IBlobStoreService>();
            var world = services.GetRequiredService<IWorldGraph>();

            var agents = services.GetRequiredService<IMapAgentLogic>();
            await agents.UpdateAgent("1", AgentCommand.Continue).ConfigureAwait(false);
            return;

            var path = await agents.GetPath("1").ConfigureAwait(false);

            var selfies = services.GetRequiredService<ISelfie>();
            var locationInfo = services.GetRequiredService<ILocationInformation>();

            for (int i = 0; i < 10; ++i)
            {
                var (image, location) = await selfies.Take(path.Points);
                if (image != null)
                {
                    string info = (await locationInfo.GetInformation(location)).Replace("/", "-");
                    image.Save($"c:\\temp\\selfie {info}.png");
                    var memStream = new MemoryStream();
                    image.SaveAsPng(memStream);
                    memStream.Position = 0;

                    //var uploadResult = await tokens.Media.UploadAsync(memStream).ConfigureAwait(false);
                    //var media = new List<long> { uploadResult.MediaId };
                    //await tokens.Statuses.UpdateAsync(info, null, null, location.Lat, location.Lon, null, true, null, media).ConfigureAwait(false);
                }
            }

            string geoJson = GeoJsonBuilder.AgentPath(path);
            Console.WriteLine("All done.");
        }

        private static IServiceProvider BuildServiceProvider()
        {
            var serviceCollection = new ServiceCollection();

            var configBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddJsonFile("local.settings.json");
            var config = configBuilder.Build();
            serviceCollection.AddSingleton<IConfiguration>(config);

            using (var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole()))
            {
                var logger = loggerFactory.CreateLogger("DefaultLogger");
                serviceCollection.AddSingleton<ILogger>(logger);
            }

            //Pwe.MapAgents.Fa.Startup.RegisterServices(serviceCollection);
            RegisterServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            return serviceProvider;
        }

        public static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<IWorldGraph, WorldGraph>();
            services.AddTransient<IMapAgentLogic, MapAgentLogic>();
            services.AddSingleton<IWayTileService, OverpassWayTileService>();
            services.AddSingleton<IBlobStoreService, AzureBlobStoreService>();
            services.AddSingleton<IMapTileCache, AzureBlobMapTileCache>();
            services.AddTransient<IStreetView, GoogleStreetView>();
            services.AddTransient<ISelfie, Selfie>();
            services.AddTransient<ILocationInformation, LocationInformation>();
            services.AddTransient<IMapCoverage, MapCoverage>();
            services.AddTransient<IGraphPeek, GraphPeek>();
            services.AddTransient<IPlaces, GooglePlaces>();
            services.AddTransient<IPinning, Pinning>();
        }
    }
}

