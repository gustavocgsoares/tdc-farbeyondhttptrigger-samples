using System;
using System.Data.Common;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TDC.FarBeyondHttpTrigger.Helpers
{
    internal class CosmosDBHelper
    {
        public const string DatabaseName = "TdcFarBeyondHttpTrigger";
        public const string CollectionOrderMix = "OrderMix";

        public CosmosDBHelper()
        {
            string connectionString = Environment.GetEnvironmentVariable("AzureOrderCosmosTrigger");

            // Use this generic builder to parse the connection string
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            if (builder.TryGetValue("AccountKey", out object key))
            {
                AuthKey = key.ToString();
            }

            if (builder.TryGetValue("AccountEndpoint", out object uri))
            {
                ServiceEndpoint = new Uri(uri.ToString());
            }
        }

        public Uri ServiceEndpoint { get; set; }

        public string AuthKey { get; set; }

        public static async Task UpdateOrder(dynamic order)
        {
            var connectionString = new CosmosDBHelper();

            using (var client = new DocumentClient(connectionString.ServiceEndpoint, connectionString.AuthKey))
            {
                var request = new { CollectionName = CollectionOrderMix, Entity = order };
                var documentLink = UriFactory.CreateDocumentUri(DatabaseName, CollectionOrderMix, ((JObject)order)["id"].ToString());
                ResourceResponse<Microsoft.Azure.Documents.Document> response;

                try
                {
                    response = await client
                        .ReplaceDocumentAsync(documentLink, order)
                        .ConfigureAwait(false);
                }
                catch (DocumentClientException e)
                {
                    throw e;
                }
            }
        }
    }
}
