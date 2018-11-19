using Microsoft.WindowsAzure.Storage.Table;

namespace SendiT.Model
{
    public class EmailBlocked : TableEntity
    {
        public string Content { get; set; }
    }
}
