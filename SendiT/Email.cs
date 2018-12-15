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
using System.Collections.Generic;
using System.IO;

namespace SendiT
{
    public static class Email
    {
        private const string EMAIL_QUEUE = "EmailQueue";
        private const string EMAIL_TRACK = "EmailTrack";
        private const string DELIVERY_STATUS_QUEUE = "DeliveryStatusQueue";
        private const string EMAIL_BLOCKED = "EmailBlocked";

        #region Http Triggers

        [FunctionName("SendEmail")]
        public static async Task<IActionResult> SendEmail(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Queue(EMAIL_QUEUE, Connection = "AzureWebJobsStorage")]  IAsyncCollector<OutgoingEmail> emailQueue,
            [Table(EMAIL_TRACK)] IAsyncCollector<SendEmailTrack> tbEmailTrack,
            ILogger log)
        {
            try
            {
                var body = await req.GetBodyAsync<OutgoingEmail>();

                if (!body.IsValid)
                {
                    log.LogInformation($"Invalid model: {JsonConvert.SerializeObject(body.ValidationResults)}");
                    return new BadRequestObjectResult(body.ValidationResults);
                }

                log.LogInformation($"Request received from {body.Value.Origin}, message type {body.Value.Type}.");

                //Setting the tracker id for this message.
                body.Value.TrackerId = Guid.NewGuid().ToString();
                //Queue email request
                await emailQueue.AddAsync(body.Value);

                //Track that email request was queued
               await EmailTracker.Create(tbEmailTrack, body.Value, DeliveryEvent.Queued);

                return new OkObjectResult(new SendMailResponse(body.Value.TrackerId));
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
         [Queue(DELIVERY_STATUS_QUEUE)]  IAsyncCollector<string> deliveryStatusQueue,
         ILogger log)
        {
            try
            {
                log.LogInformation("New web hook received from SendGrid");
                string requestBody = new StreamReader(req.Body).ReadToEnd();

                await deliveryStatusQueue.AddAsync(requestBody);

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
            [QueueTrigger(EMAIL_QUEUE)] OutgoingEmail emailQueue,
            [SendGrid(ApiKey = "AzureWebJobsSendGridApiKey")] IAsyncCollector<SendGridMessage> emails,
            [Table(EMAIL_TRACK)] CloudTable tbEmailTrack,
            [Table(EMAIL_BLOCKED)] CloudTable tbEmailBlocked,
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
                    await EmailTracker.Update(tbEmailTrack, emailQueue.To, emailQueue.TrackerId, DeliveryEvent.Blocked, log);
                    return;
                }

                await SendMessage(emailQueue, emails);

                //Track that email request was queued
                await EmailTracker.Update(tbEmailTrack, emailQueue.To, emailQueue.TrackerId, DeliveryEvent.SendRequested, log);
            }
            catch (Exception ex)
            {
                log.LogError("An error has occurred.", ex);
                throw;
            }
        }

        [FunctionName("ProcessDeliveryStatusQueue")]
        public static async Task ProcessDeliveryStatusQueue(
            [QueueTrigger(DELIVERY_STATUS_QUEUE)] string deliveryStatusQueue,
            [Table(EMAIL_BLOCKED)] IAsyncCollector<EmailBlocked> tbEmailBlocked,
            [Table(EMAIL_TRACK)] CloudTable tbEmailTrack,
            int dequeueCount,
            ILogger log)
        {
            try
            {
                log.LogInformation($"New status to update. Dequeue count for this item: {dequeueCount}.");
                var deliveryStatusList = JsonConvert.DeserializeObject<List<DeliveryWebHook>>(deliveryStatusQueue);

                foreach (var deliveryStatus in deliveryStatusList)
                {
                    switch (deliveryStatus.Event)
                    {
                        case DeliveryEvent.Dropped:
                        case DeliveryEvent.Deferred:
                        case DeliveryEvent.Bounce:
                            //TODO: put DeliveryWebHook as property of EmailBlocked instead of serialize the Json
                            await EmailBlocker.Create(tbEmailBlocked, deliveryStatus.Email, JsonConvert.SerializeObject(deliveryStatus), deliveryStatus.Event);

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
                }
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