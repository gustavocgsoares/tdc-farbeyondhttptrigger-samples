using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TDC.FarBeyondHttpTrigger.Helpers;
using Microsoft.Azure.Documents;

namespace TDC.FarBeyondHttpTrigger
{
    public static class EcommerceOrderMixHttpTrigger
    {
        [FunctionName("EcommerceOrderMixHttpTrigger")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/orders")] HttpRequest req,
            [CosmosDB(CosmosDBHelper.DatabaseName, CosmosDBHelper.CollectionOrderMix, ConnectionStringSetting = "AzureOrderCosmosTrigger")] out dynamic document,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = new StreamReader(req.Body).ReadToEndAsync().GetAwaiter().GetResult();
            document = JsonConvert.DeserializeObject(requestBody);

            return new AcceptedResult();
        }
    }
}
