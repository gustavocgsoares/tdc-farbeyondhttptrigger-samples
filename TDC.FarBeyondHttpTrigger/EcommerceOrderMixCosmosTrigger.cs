using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TDC.FarBeyondHttpTrigger.Helpers;

namespace TDC.FarBeyondHttpTrigger
{
    public static class EcommerceOrderMixCosmosTrigger
    {
        [FunctionName("EcommerceOrderMixCosmosTrigger")]
        public static async Task Run(
            [CosmosDBTrigger(databaseName: CosmosDBHelper.DatabaseName, collectionName: CosmosDBHelper.CollectionOrderMix, ConnectionStringSetting = "AzureOrderCosmosTrigger", LeaseCollectionName = "leases")]IReadOnlyList<Document> documents,
            [EventHub("mix-credit-card-authorization", Connection = "EventHubConnectionAppSetting")]IAsyncCollector<EventData> outputMixCardAuthorization,
            [EventHub("mix-send-wait-email", Connection = "EventHubConnectionAppSetting")]IAsyncCollector<EventData> outputMixSendWaitEmail,
            [EventHub("mix-prepare-product", Connection = "EventHubConnectionAppSetting")]IAsyncCollector<EventData> outputMixPrepareProduct,
            [EventHub("mix-create-nfe", Connection = "EventHubConnectionAppSetting")]IAsyncCollector<EventData> outputMixCreateNfe,
            [EventHub("mix-send-finish-email", Connection = "EventHubConnectionAppSetting")]IAsyncCollector<EventData> outputMixSendFinishEmail,
            ILogger log)
        {
            if (documents != null && documents.Count > 0)
            {
                foreach (Document document in documents)
                {
                    var order = (dynamic)document;

                    if (!((IDictionary<string, object>)order).ContainsKey("steps"))
                    {
                        order.steps = new
                        {
                            cardAuthorizationDone = false,
                            sendWaitEmailDone = false,
                            prepareProductDone = false,
                            createNfeDone = false,
                            sendFinishEmailDone = false
                        };
                    }

                    if (order.steps.cardAuthorizationDone == false)
                    {
                        log.LogInformation("Executing CardAuthorization");
                        await outputMixCardAuthorization.AddAsync(GetEventData(order));
                        return;
                    }

                    if (order.steps.sendWaitEmailDone == false)
                    {
                        log.LogInformation("Executing SendWaitEmail");
                        await outputMixSendWaitEmail.AddAsync(GetEventData(order));
                        return;
                    }

                    if (order.steps.prepareProductDone == false)
                    {
                        log.LogInformation("Executing PrepareProduct");
                        await outputMixPrepareProduct.AddAsync(GetEventData(order));
                        return;
                    }

                    if (order.steps.createNfeDone == false)
                    {
                        log.LogInformation("Executing CreateNfe");
                        await outputMixCreateNfe.AddAsync(GetEventData(order));
                        return;
                    }

                    if (order.steps.sendFinishEmailDone == false)
                    {
                        log.LogInformation("Executing SendFinishEmail");
                        await outputMixSendFinishEmail.AddAsync(GetEventData(order));
                        return;
                    }

                    log.LogInformation("Finish order process");
                }
            }
        }

        private static EventData GetEventData(dynamic order)
        {
            var jsonText = JsonConvert.SerializeObject(order);
            return new EventData(Encoding.UTF8.GetBytes(jsonText));
        }
    }
}
