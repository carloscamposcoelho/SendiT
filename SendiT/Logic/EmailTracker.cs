using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Table;
using SendiT.Model;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static SendiT.Model.Enumerators;
using Newtonsoft.Json;

namespace SendiT.Logic
{
    public static class EmailTracker
    {
        public static async Task Create(IAsyncCollector<SendEmailTrack> tbTracker, OutgoingEmail emailMessage, Event dEvent,
            string messageId = null)
        {
            var content = JsonConvert.SerializeObject(emailMessage);
            await tbTracker.AddAsync(new SendEmailTrack
            {
                PartitionKey = emailMessage.To,
                RowKey = emailMessage.TrackerId,
                Event = dEvent.ToString(),
                Content = content,
                Date = DateTime.UtcNow,
                MessageId = messageId
            });
        }

        public static async Task Update(CloudTable tbTracker, string email, string trackerId, Event dEvent, ILogger log,
            string messageId = null)
        {
            //retrieving the table
            TableResult retrievedResult = await tbTracker.ExecuteAsync(TableOperation.Retrieve<SendEmailTrack>(email, trackerId));

            if (retrievedResult.Result is SendEmailTrack emailTrack)
            {
                //update table
                emailTrack.Event = dEvent.ToString();
                emailTrack.Date = DateTime.UtcNow;
                emailTrack.MessageId = messageId ?? emailTrack.MessageId;

                var operation = TableOperation.Replace(emailTrack);
                await tbTracker.ExecuteAsync(operation);
            }
            else
            {
                log.LogError($"Email track could not be found. PartitionKey {email}; RowKey: {trackerId}");
            }
        }
    }
}
