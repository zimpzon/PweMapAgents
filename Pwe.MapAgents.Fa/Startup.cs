using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pwe.AzureBloBStore;
using Pwe.OverpassTiles;
using Pwe.World;

[assembly: FunctionsStartup(typeof(Pwe.MapAgents.Fa.Startup))]

namespace Pwe.MapAgents.Fa
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {

            builder.Services.AddLogging();
            builder.Services.AddSingleton((provider) => provider.GetService<ILoggerFactory>().CreateLogger("DefaultLogger"));

            RegisterServices(builder.Services);
        }

        public static void RegisterServices(IServiceCollection services)
        {
            services.AddTransient<IWorldGraph, WorldGraph>();
            services.AddTransient<IMapAgentLogic, MapAgentLogic>();
            services.AddSingleton<IWayTileService, OverpassWayTileService>();
            services.AddSingleton<IBlobStoreService, AzureBlobStoreService>();
            services.AddSingleton<IMapTileCache, AzureBlobMapTileCache>();
        }
    }
}
