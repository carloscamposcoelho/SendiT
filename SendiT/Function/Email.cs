using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SendiT.Logic;
using SendiT.Model;
using SendiT.Util;
using System;
using System.Linq;
using System.Threading.Tasks;
using static SendiT.Model.Enumerators;

namespace SendiT.Function
{
    public static class Email
    {
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
            catch (JsonReaderException jrEx)
            {
                log.LogError("Json request error: {0}", jrEx);
                return new BadRequestObjectResult("Json format error.");
            }
            catch (Exception ex)
            {
                log.LogError("An error has occurred: {0}", ex);
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
                var toEmail = emailQueue.ToAddress.Email;

                //Check whether the recipient of the message is blocked
                //TODO: Change this method to check if the email is blocked calling a SendGrid Api
                var recipientIsBlocked = await EmailBlocker.CheckIfBlocked(toEmail, tbEmailBlocked);
                if (recipientIsBlocked)
                {
                    log.LogInformation($"Can't send email to {toEmail} because it has been blocked");
                    await EmailTracker.Update(tbEmailTrack, toEmail, emailQueue.TrackerId, Event.Blocked, log);
                    return;
                }

                var response = await SendMail.SendSingleEmail(emailQueue.FromAddress, emailQueue.ToAddress, emailQueue.Subject, emailQueue.Body,
                    emailQueue.TrackerId, log);

                if (!SuccessStatusCodes.Contains(response.StatusCode))
                {
                    throw new Exception($"Error sending mail. SendGrid response {response.StatusCode}");
                }
                //Track that email request was sent
                await EmailTracker.Update(tbEmailTrack, toEmail, emailQueue.TrackerId, Event.SendRequested, log, response.MessageId);
            }
            catch (Exception ex)
            {
                log.LogError("An error has occurred: {0}", ex);
                throw;
            }
        }
        #endregion
    }
}