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
            builder.Services.AddTransient<IWorldGraph, WorldGraph>();
            builder.Services.AddTransient<IMapAgentLogic, MapAgentLogic>();
            builder.Services.AddTransient<IWayTileService, OverpassWayTileService>();
        }
    }
}
