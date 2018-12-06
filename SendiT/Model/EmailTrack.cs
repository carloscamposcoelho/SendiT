using System;
using System.Data.Common;
using Microsoft.WindowsAzure.Storage.Table;

namespace SendiT.Model
{
    public class SendEmailTrack : TableEntity
    {
        /// <summary>
        /// Event that originates the record
        /// </summary>
        public string Event { get; set; }

        /// <summary>
        /// Date time of the latest event
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Email message content
        /// </summary>
        public OutgoingEmail Message { get; set; }
    }
}
