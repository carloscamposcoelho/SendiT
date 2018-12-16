using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SendiT.Logic;
using SendiT.Model;
using SendiT.Model.SendGrid;
using SendiT.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static SendiT.Model.Enumerators;

namespace SendiT
{
    public static class Email
    {
        private const string EMAIL_QUEUE = "EmailQueue";
        private const string EMAIL_TRACK = "EmailTrack";
        private const string DELIVERY_STATUS_QUEUE = "DeliveryStatusQueue";
        private const string EMAIL_BLOCKED = "EmailBlocked";
        private static readonly IEnumerable<HttpStatusCode> _successStatusCodes = new List<HttpStatusCode>
        {
            HttpStatusCode.Accepted,
            HttpStatusCode.OK
        };

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
                await EmailTracker.Create(tbEmailTrack, body.Value, Event.Queued);

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
                    await EmailTracker.Update(tbEmailTrack, emailQueue.To, emailQueue.TrackerId, Event.Blocked, log);
                    return;
                }

                var response = await SendMail.SendSingleEmail(emailQueue.From, emailQueue.To, emailQueue.Subject, emailQueue.Body,
                    emailQueue.TrackerId, log);

                if (!_successStatusCodes.Contains(response.StatusCode))
                {
                    throw new Exception($"Error sending mail. SendGrid response {response.StatusCode}");
                }
                //Track that email request was sent
                await EmailTracker.Update(tbEmailTrack, emailQueue.To, emailQueue.TrackerId, Event.SendRequested, log,
                    response.MessageId);
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
                //TODO: Need to work the TimeStemp property
                //https://stackoverflow.com/questions/249760/how-can-i-convert-a-unix-timestamp-to-datetime-and-vice-versa/250400#250400

                log.LogInformation($"New status to update. Dequeue count for this item: {dequeueCount}.");
                var deliveryStatusList = JsonConvert.DeserializeObject<List<DeliveryWebHook>>(deliveryStatusQueue);

                foreach (var status in deliveryStatusList)
                {
                    if (DeliveryEvents.Contains(status.Event))
                    {
                        if (BlockerEvents.Contains(status.Event))
                        {
                            log.LogInformation($"Blocking email {status.Email}...");
                            await EmailBlocker.Create(tbEmailBlocked, status.Email, JsonConvert.SerializeObject(status), status.Event);
                        }
                        //TODO: Add to a list of Delivery Events
                        await EmailTracker.Update(tbEmailTrack, status.Email, status.TrackerId, status.Event, log, status.SgMessageId);
                    }
                    else if (EngagementEvents.Contains(status.Event))
                    {
                        //TODO: Add to a list of Engagement Events
                        await EmailTracker.Update(tbEmailTrack, status.Email, status.TrackerId, status.Event, log, status.SgMessageId);
                    }
                    else
                    {
                        throw new Exception($"Event not mapped {status.Event}.");
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
    }
}