using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Pwe.MapAgents.Fa
{
    public class UpdateAgentEndpoint
    {
        private readonly IMapAgentLogic _agentLogic;

        public UpdateAgentEndpoint(IMapAgentLogic agentLogic)
        {
            _agentLogic = agentLogic;
        }

        [FunctionName("UpdateAgent")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            string id = req.Query["id"];
            await _agentLogic.UpdateAgent(id, AgentCommand.Continue);

            return new OkObjectResult("success");
        }
    }
}
