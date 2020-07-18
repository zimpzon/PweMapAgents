using CoreTweet;
using GoogleApis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pwe.AzureBloBStore;
using Pwe.MapAgents;
using Pwe.OverpassTiles;
using Pwe.World;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Cmd
{
    class Program
    {
        // TODO:
        // When entering new tile, update coverage map (service bus or directly? Multiple layers? Use ImageSharp on PNG?)
        // Selfie address, maybe write on image?

        static async Task Main(string[] _)
        {
            var tokens = Tokens.Create("---", "---", "---", "---");

            var services = BuildServiceProvider();
            var blobs = services.GetRequiredService<IBlobStoreService>();

            var img = new Image<L8>(64, 64);
            for (int i = 0; i < 32; ++i)
                img[i, i] = new L8(255);

            img.Save("c:\\temp\\coverage.png");

            var agents = services.GetRequiredService<IMapAgentLogic>();
            var path = await agents.GetPath("1").ConfigureAwait(false);

            var selfies = services.GetRequiredService<ISelfie>();
            var locationInfo = services.GetRequiredService<ILocationInformation>();

            var (image, location) = await selfies.Take(path.Points);
            if (image != null)
            {
                string info = await locationInfo.GetInformation(location);
                image.Save($"c:\\temp\\selfie {info}.png");
                var memStream = new MemoryStream();
                image.SaveAsPng(memStream);
                memStream.Position = 0;

                //await tokens.Statuses.UpdateAsync(info, null, null, location.Lat, location.Lon, null, display_coordinates: true).ConfigureAwait(false);

                var uploadResult = await tokens.Media.UploadAsync(memStream).ConfigureAwait(false);
                var media = new List<long> { uploadResult.MediaId };
                await tokens.Statuses.UpdateAsync(info, null, null, location.Lat, location.Lon, null, true, null, media).ConfigureAwait(false);
            }

            //await agents.UpdateAgent("1", AgentCommand.Continue).ConfigureAwait(false);
            //string geoJson = GeoJsonBuilder.AgentPath(path);
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
            services.AddTransient<IWorldGraph, WorldGraph>();
            services.AddTransient<IMapAgentLogic, MapAgentLogic>();
            services.AddSingleton<IWayTileService, OverpassWayTileService>();
            services.AddSingleton<IBlobStoreService, AzureBlobStoreService>();
            services.AddSingleton<IMapTileCache, AzureBlobMapTileCache>();
            services.AddTransient<IStreetView, GoogleStreetView>();
            services.AddTransient<ISelfie, Selfie>();
            services.AddTransient<ILocationInformation, LocationInformation>();
        }
    }
}

