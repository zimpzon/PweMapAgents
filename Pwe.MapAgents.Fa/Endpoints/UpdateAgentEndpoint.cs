using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;

namespace Pwe.MapAgents.Fa
{
    public static class UpdateAgentEndpoint
    {
        [FunctionName("UpdateAgent")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var p = req.HttpContext.RequestServices;
            var test = p.GetService(typeof(ILogger));
            string id = req.Query["id"];
            //await agentLogic.UpdateAgent(id, AgentCommand.Continue);

            return new OkObjectResult("success");
        }
    }
}
