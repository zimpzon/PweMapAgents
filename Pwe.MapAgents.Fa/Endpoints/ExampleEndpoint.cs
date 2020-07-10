//using System.IO;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Extensions.Http;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;

//namespace Pwe.MapAgents.Fa
//{
//    public static class ExampleEndpoint
//    {
//        [FunctionName("PostExample")]
//        public static async Task<IActionResult> Run(
//            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
//            ILogger log, IMapAgentLogic agentLogic)
//        {
//            log.LogInformation("C# HTTP trigger function processed a request.");

//            string id = req.Query["id"];

//            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
//            dynamic data = JsonConvert.DeserializeObject(requestBody);

//            return new OkObjectResult("");
//        }
//    }
//}
