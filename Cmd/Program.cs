using GoogleApis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pwe.AzureBloBStore;
using Pwe.MapAgents;
using Pwe.OverpassTiles;
using Pwe.World;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Threading.Tasks;

namespace Cmd
{
    class Program
    {
        static async Task Main(string[] _)
        {
            var services = BuildServiceProvider();
            var blobs = services.GetRequiredService<IBlobStoreService>();
            var cactusBytes = await blobs.GetBytes("agentoutfits/cactus.png");

            var agents = services.GetRequiredService<IMapAgentLogic>();
            var path = await agents.GetPath("1").ConfigureAwait(false);

            int Pct(int src, double pct) => (int)(src * pct);

            var rnd = new Random();
            for (int i = 0; i < 20; ++i)
            {
                var randomPoint = path.Points[rnd.Next(path.Points.Count)];

                var view = services.GetRequiredService<IStreetView>();
                var bytes = await view.Test(randomPoint);
                if (bytes != null)
                {
                    var cactusImg = SixLabors.ImageSharp.Image.Load(cactusBytes);
                    var srcSize = cactusImg.Size();

                    int agentRotation = rnd.Next(44) - 22;
                    cactusImg.Mutate(x => x.Rotate(agentRotation));

                    var dstImg = SixLabors.ImageSharp.Image.Load(bytes);
                    var dstSize = dstImg.Size();
                    int offsetBottom = rnd.Next(Pct(srcSize.Height, 0.3));
                    int selfieX = Pct(dstSize.Width, 0.4) + rnd.Next(Pct(dstSize.Width, 0.2)) - Pct(srcSize.Width, 0.5);
                    int selfieY = dstSize.Height - Pct(srcSize.Height, 0.95) + offsetBottom;
                    var selfiePoint = new Point(selfieX, selfieY);

                    dstImg.Mutate(x => x.DrawImage (cactusImg, selfiePoint, 1));
                    dstImg.Save($"c:\\temp\\image{i}.png");
                }
            }

            //await agents.UpdateAgent("1", AgentCommand.Continue).ConfigureAwait(false);
            //string geoJson = GeoJsonBuilder.AgentPath(path);
            Console.WriteLine("All done.");
        }

        private static IServiceProvider BuildServiceProvider()
        {
            var serviceCollection = new ServiceCollection();

            var configBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
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
        }
    }
}

