using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SendGrid.Helpers.Mail;
using SendiT.Model;
using SendiT.Model.SendGrid;
using SendiT.Util;
using System.Linq;
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

                return new OkObjectResult(new SendMailResponse(body.Value.Tracker));
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
            [Table("EmailBlocked", Connection = "AzureWebJobsStorage")] CloudTable tbEmailBlocked,
            int dequeueCount,
            ILogger log)
        {
            log.LogInformation($"New email to send. Dequeue count for this message: {dequeueCount}.");

            try
            {
                //Check whether the recipient of the message is blocked
                var recipientIsBlocked = await CheckIfBlocked(emailQueue.To, tbEmailBlocked);
                if (recipientIsBlocked)
                {
                    //TODO: Update SendEmailTrack
                    log.LogInformation($"Can't send email to {emailQueue.To} because it has been blocked");
                    return;
                }

                await SendMessage(emailQueue, emails);

                tbEmailTrack.Add(new EmailTrack
                {
                    Email = emailQueue.To,
                    RowKey = emailQueue.Tracker,
                    Event = DeliveryEvent.SendRequested.ToString()
                });

            }
            catch (System.Exception ex)
            {
                log.LogError("An error has ocurred.", ex);
                throw;
            }
        }

        [FunctionName("SendGridHook")]
        public static async Task<IActionResult> SendGridHook(
         [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
         [Table("EmailBlocked", Connection = "AzureWebJobsStorage")] ICollector<EmailBlocked> tbEmailBlocked,
         ILogger log)
        {
            try
            {
                log.LogInformation($"Request received: {JsonConvert.SerializeObject(req.Body)}");
                var body = await req.GetBodyAsync<DeliveryWebHook>();

                if (!body.IsValid)
                {
                    log.LogInformation($"Invalid request: {body.ValidationResults}");
                    return new BadRequestObjectResult(body.ValidationResults);
                }

                switch (body.Value.Event)
                {
                    case DeliveryEvent.Dropped:
                    case DeliveryEvent.Deferred:
                    case DeliveryEvent.Bounce:
                        tbEmailBlocked.Add(new EmailBlocked
                        {
                            PartitionKey = body.Value.Email,
                            RowKey = body.Value.Event.ToString(),
                            Content = JsonConvert.SerializeObject(body.Value)
                        });
                        break;
                    default:
                        break;
                }

                return new OkResult();
            }
            catch (System.Exception ex)
            {
                log.LogError("An error has ocurred.", ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
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

        private static async Task<bool> CheckIfBlocked(string email, CloudTable tbEmailBlocked)
        {
            bool isBlocked;
            TableQuery<EmailBlocked> rangeQuery = new TableQuery<EmailBlocked>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, email));

            // Execute the query 
            var emails = await tbEmailBlocked.ExecuteQuerySegmentedAsync(rangeQuery, null);

            isBlocked = emails != null && emails.Count() > 0;

            return isBlocked;
        }
    }
}