using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Table;
using SendiT.Model;
using System;
using System.Threading.Tasks;
using static SendiT.Model.Enumerators;

namespace SendiT.Logic
{
    public static class EmailTracker
    {
        public static async Task Create(IAsyncCollector<EmailTrack> tbTracker, OutgoingEmail emailMessage, DeliveryEvent dEvent)
        {
            await tbTracker.AddAsync(new EmailTrack
            {
                PartitionKey = emailMessage.To,
                RowKey = emailMessage.Tracker,
                Event = dEvent.ToString(),
                Date = DateTime.UtcNow,
                Message = emailMessage
            });
        }

        public static async Task Update(CloudTable tbTracker, string email, string trackerId, DeliveryEvent dEvent)
        {
            var emailTrack = new EmailTrack
            {
                PartitionKey = email,
                RowKey = trackerId,
                Event = dEvent.ToString(),
                Date = DateTime.UtcNow
            };

            var operation = TableOperation.Replace(emailTrack);
            await tbTracker.ExecuteAsync(operation);
        }
    }
}
