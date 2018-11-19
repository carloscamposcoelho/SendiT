using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using SendiT.Model;
using SendiT.Util;
using System.Threading.Tasks;
using static SendiT.Model.Enumerators;

namespace SendiT
{
    public static class Email
    {
        [FunctionName("SendEmail")]
        public static async Task<IActionResult> SendEmail(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Queue("email-queue", Connection = "AzureWebJobsStorage")]  IAsyncCollector<OutgoingEmail> emailQueue,
            ILogger log)
        {
            try
            {
                var body = await req.GetBodyAsync<OutgoingEmail>();

                if (!body.IsValid)
                {
                    return new BadRequestObjectResult(body.ValidationResults);
                }

                //Queue email request
                await emailQueue.AddAsync(body.Value);

                return new OkObjectResult($"TrackerId: {body.Value.Tracker}");
            }
            catch (System.Exception ex)
            {
                log.LogError("An error has ocurred.", ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [FunctionName("ProcessEmailQueue")]
        public static async Task ProcessEmailQueue(
            [QueueTrigger("email-queue")] OutgoingEmail emailQueue,
            [SendGrid(ApiKey = "AzureWebJobsSendGridApiKey")] IAsyncCollector<SendGridMessage> emails,
            [Table("EmailTrack", Connection = "AzureWebJobsStorage")] ICollector<EmailTrack> tbEmailTrack,
            int dequeueCount,
            ILogger log)
        {
            log.LogInformation($"New email to send. Dequeue count for this message: {dequeueCount}.");

            try
            {
                await SendMessage(emailQueue, emails);

                tbEmailTrack.Add(new EmailTrack
                {
                    Email = emailQueue.To,
                    RowKey = emailQueue.Tracker,
                    Event = DeliveryEvents.SendRequested.ToString()
                });

            }
            catch (System.Exception ex)
            {
                log.LogError("An error has ocurred.", ex);
                throw;
            }
        }

        private static async Task SendMessage(OutgoingEmail emailQueue, IAsyncCollector<SendGridMessage> emails)
        {
            SendGridMessage message = new SendGridMessage();
            message.AddTo(emailQueue.To);
            message.AddContent("text/html", emailQueue.Body);
            message.SetFrom(new EmailAddress(emailQueue.From));
            message.SetSubject(emailQueue.Subject);

            await emails.AddAsync(message);
        }
    }
}