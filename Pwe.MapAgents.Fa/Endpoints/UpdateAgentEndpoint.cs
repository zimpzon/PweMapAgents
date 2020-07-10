using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Pwe.MapAgents.Fa
{
    public static class UpdateAgentEndpoint
    {
        [FunctionName("UpdateAgent")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log, IMapAgentLogic agentLogic)
        {
            string id = req.Query["id"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            return new OkObjectResult("success");
        }
    }
}
