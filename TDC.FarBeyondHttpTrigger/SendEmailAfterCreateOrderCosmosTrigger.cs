using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using TDC.FarBeyondHttpTrigger.Helpers;

namespace TDC.FarBeyondHttpTrigger
{
    public static class SendEmailAfterCreateOrderCosmosTrigger
    {
        [FunctionName("SendEmailAfterCreateOrderCosmosTrigger")]
        public static void Run([CosmosDBTrigger(
            databaseName: CosmosDBHelper.DatabaseName,
            collectionName: "Order",
            ConnectionStringSetting = "AzureOrderCosmosTrigger",
            LeaseCollectionName = "leases")]IReadOnlyList<Document> input, ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                var order = (dynamic)input[0];

                var from = new MailAddress("gu.cgsoares@gmail.com");
                var to = new MailAddress(order.customer.email.ToString());
                var subject = $"Pedido {order.number}"; // assunto;
                var body = $"Olá {order.customer.name}. Você acaba de adquirir o produto {order.product.name} por R$ {order.product.amount.ToString("N2", CultureInfo.InvariantCulture).Replace(".", ",")}. Parabéns!"; // mensagem
                var smtp = CreateSmtpClient(from);

                using (var message = GetMessage(from, to, subject, body))

                    smtp.Send(message);

                log.LogInformation("Documents modified " + input.Count);
                log.LogInformation("First document Id " + input[0].Id);
            }
        }

        private static MailMessage GetMessage(MailAddress from, MailAddress to, string subject, string body)
        {
            return new MailMessage(from, to)
            {
                Subject = subject,
                Body = body
            };
        }

        private static SmtpClient CreateSmtpClient(MailAddress from)
        {
            var password = Environment.GetEnvironmentVariable("NC_PASSWORD");

            return new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(from.Address, password)
            };
        }
    }
}
