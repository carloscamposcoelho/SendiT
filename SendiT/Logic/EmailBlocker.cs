using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Table;
using SendiT.Model;
using System.Linq;
using System.Threading.Tasks;
using static SendiT.Model.Enumerators;

namespace SendiT.Logic
{
    public static class EmailBlocker
    {
        public static async Task Create(IAsyncCollector<EmailBlocked> tbEmailBlocked, string email, string content, DeliveryEvent dEvent)
        {
            await tbEmailBlocked.AddAsync(new EmailBlocked
            {
                PartitionKey = email,
                RowKey = dEvent.ToString(),
                Content = content
            });
        }

        public static async Task<bool> CheckIfBlocked(string email, CloudTable tbEmailBlocked)
        {
            bool isBlocked;
            var rangeQuery = new TableQuery<EmailBlocked>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, email));

            // Execute the query 
            var emails = await tbEmailBlocked.ExecuteQuerySegmentedAsync(rangeQuery, null);

            isBlocked = emails != null && emails.Any();

            return isBlocked;
        }
    }
}
