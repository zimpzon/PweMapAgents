using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Pwe.OverpassTiles;
using Pwe.World;

[assembly: FunctionsStartup(typeof(Pwe.MapAgents.Fa.Startup))]

namespace Pwe.MapAgents.Fa
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            //builder.Services.AddHttpClient();

            builder.Services.AddSingleton<IWorldGraph, WorldGraph>();
            builder.Services.AddSingleton<IMapAgentLogic, MapAgentLogic>();
            builder.Services.AddSingleton<IWayTileService, OverpassWayTileService>();
        }
    }
}
