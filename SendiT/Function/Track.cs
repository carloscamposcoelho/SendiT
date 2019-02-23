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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static SendiT.Model.Enumerators;

namespace SendiT.Function
{
    public class Track
    {
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
                log.LogError("An error has occurred: {0}", ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
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
                log.LogError("An error has occurred: {0}", ex);
                throw;
            }
        }

    }
}
