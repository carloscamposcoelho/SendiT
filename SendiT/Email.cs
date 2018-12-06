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
using System;
using System.Threading.Tasks;
using static SendiT.Model.Enumerators;
using SendiT.Logic;

namespace SendiT
{
    public static class Email
    {
        #region Http Triggers

        [FunctionName("SendEmail")]
        public static async Task<IActionResult> SendEmail(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req,
            [Queue("email-queue", Connection = "AzureWebJobsStorage")]  IAsyncCollector<OutgoingEmail> emailQueue,
            [Table("SendEmailTrack")] IAsyncCollector<SendEmailTrack> tbEmailTrack,
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

                //Track that email request was queued
                await EmailTracker.Create(tbEmailTrack, body.Value, DeliveryEvent.Queued);

                return new OkObjectResult(new SendMailResponse(body.Value.Tracker));
            }
            catch (Exception ex)
            {
                log.LogError("An error has occurred.", ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [FunctionName("SendGridHook")]
        public static async Task<IActionResult> SendGridHook(
         [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
         [Table("EmailBlocked", Connection = "AzureWebJobsStorage")] IAsyncCollector<EmailBlocked> tbEmailBlocked,
         [Table("SendEmailTrack")] CloudTable tbEmailTrack,
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

                //TODO: Create a queue based on the DeliveryEvent
                switch (body.Value.Event)
                {
                    case DeliveryEvent.Dropped:
                    case DeliveryEvent.Deferred:
                    case DeliveryEvent.Bounce:
                        //TODO: put DeliveryWebHook as property of EmailBlocked instead of serialize the Json
                        await EmailBlocker.Create(tbEmailBlocked, body.Value.Email, JsonConvert.SerializeObject(body.Value), body.Value.Event);
                        
                        //TODO: Fist I need to store the trackerId into some custom field of SendGrid
                        //track that email has been blocked
                        //await EmailTracker.Update(tbEmailTrack, body.Value.Email, emailQueue.Tracker, DeliveryEvent.SendRequested);
                        break;
                    case DeliveryEvent.Queued:
                        break;
                    case DeliveryEvent.SendRequested:
                        break;
                    case DeliveryEvent.Processed:
                        break;
                    case DeliveryEvent.Delivered:
                        break;
                    case DeliveryEvent.Open:
                        break;
                    case DeliveryEvent.Click:
                        break;
                    case DeliveryEvent.SpamReport:
                        break;
                    case DeliveryEvent.Unsubscribe:
                        break;
                    case DeliveryEvent.GroupUnsubscribe:
                        break;
                    case DeliveryEvent.GroupResubscribe:
                        break;
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError("An error has occurred.", ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        #endregion

        #region Queue Triggers

        [FunctionName("ProcessEmailQueue")]
        public static async Task ProcessEmailQueue(
            [QueueTrigger("email-queue")] OutgoingEmail emailQueue,
            [SendGrid(ApiKey = "AzureWebJobsSendGridApiKey")] IAsyncCollector<SendGridMessage> emails,
            [Table("SendEmailTrack", Connection = "AzureWebJobsStorage")] CloudTable tbEmailTrack,
            [Table("EmailBlocked", Connection = "AzureWebJobsStorage")] CloudTable tbEmailBlocked,
            int dequeueCount,
            ILogger log)
        {
            log.LogInformation($"New email to send. Dequeue count for this message: {dequeueCount}.");

            try
            {
                //Check whether the recipient of the message is blocked
                var recipientIsBlocked = await EmailBlocker.CheckIfBlocked(emailQueue.To, tbEmailBlocked);
                if (recipientIsBlocked)
                {
                    log.LogInformation($"Can't send email to {emailQueue.To} because it has been blocked");
                    return;
                }

                await SendMessage(emailQueue, emails);

                //Track that email request was queued
                await EmailTracker.Update(tbEmailTrack, emailQueue.To, emailQueue.Tracker, DeliveryEvent.SendRequested, log);

            }
            catch (Exception ex)
            {
                log.LogError("An error has occurred.", ex);
                throw;
            }
        }

        #endregion

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