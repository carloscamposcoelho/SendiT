using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace SendiT.Model
{
    public class EmailTrack : TableEntity
    {
        string _email;

        public string Email {
            get => _email;
            set {
                _email = PartitionKey = value;
            }
        }

        /// <summary>
        /// Event that originates the record
        /// </summary>
        public string Event { get; set; }

        /// <summary>
        /// Addcional information about the event
        /// </summary>
        public string Reason { get; set; }
    }
}
