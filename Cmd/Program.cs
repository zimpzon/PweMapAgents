using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pwe.MapAgents;
using Pwe.World;
using System;
using System.Threading.Tasks;

namespace Cmd
{
    class Program
    {
        static async Task Main(string[] _)
        {
            var services = BuildServiceProvider();
            var agents = services.GetRequiredService<IMapAgentLogic>();
            await agents.UpdateAgent("1", AgentCommand.Continue);

            //string geo = world.ToGeoJsonMultiLine(addBoundingBox: true, addWays: false);
            //string geo = world.ToGeoJsonMultiPoint(addBoundingBox: true);
            Console.WriteLine("All done.");
        }

        private static IServiceProvider BuildServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            ILogger logger = loggerFactory.CreateLogger<Program>();
            serviceCollection.AddSingleton<ILogger>(logger);
            serviceCollection.AddSingleton<IWorldGraph, WorldGraph>();
            serviceCollection.AddSingleton<IMapAgentLogic, MapAgentLogic>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            return serviceProvider;
        }
    }
}

