using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;

namespace Pwe.MapAgents.Fa
{
    public class UpdateAgentEndpoint
    {
        private readonly IMapAgentLogic _agentLogic;

        public UpdateAgentEndpoint(IMapAgentLogic agentLogic)
        {
            _agentLogic = agentLogic;
        }

        [FunctionName("GetAgentPath")]
        public async Task<HttpResponseMessage> GetAgentPath([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            string id = req.Query["id"];
            string json = await _agentLogic.GetAgentClientPath(id).ConfigureAwait(false);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }

        [FunctionName("UpdateAgent")]
        public async Task<IActionResult> UpdateAgent([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            string id = req.Query["id"];
            await _agentLogic.UpdateAgent(id, AgentCommand.Continue);

            return new OkObjectResult("success");
        }
    }
}
