using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TDC.FarBeyondHttpTrigger.Helpers;

namespace TDC.FarBeyondHttpTrigger
{
    public static class EcommerceOrderMixSendWaitEmailEventHubTrigger
    {
        [FunctionName("EcommerceOrderMixSendWaitEmailEventHubTrigger")]
        public static async Task Run(
            [EventHubTrigger("mix-send-wait-email", Connection = "EventHubConnectionAppSetting")] EventData[] events,
            ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    dynamic order = JsonConvert.DeserializeObject(messageBody);
                    SendEmail(order);

                    order.steps.sendWaitEmailDone = true;
                    CosmosDBHelper.UpdateOrder(order);

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");

                    await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }

        private static void SendEmail(dynamic order)
        {
            var from = new MailAddress("gu.cgsoares@gmail.com");
            var to = new MailAddress(order.customer.email.ToString());
            var subject = $"Pedido Mix {order.number} - Processando pedido"; // assunto;
            var body = $"Olá {order.customer.name}. Você acaba de adquirir o produto {order.product.name} por R$ {order.product.amount.ToString("N2", CultureInfo.InvariantCulture).Replace(".", ",")}. Estamos processando seu pedido. Aguarde!"; // mensagem
            var smtp = CreateSmtpClient(from);

            using (var message = GetMessage(from, to, subject, body))
            {
                smtp.Send(message);
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
