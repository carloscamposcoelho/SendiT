using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;


namespace SendiT.Model
{
    public class OutgoingEmail
    {
        [Required(ErrorMessage = "Required field")]
        public EmailAddress ToAddress { get; set; }

        [Required(ErrorMessage = "Required field")]
        public EmailAddress FromAddress { get; set; }

        [StringLength(100)]
        public string Subject { get; set; }

        public string Body { get; set; }

        /// <summary>
        /// Caller's application name
        /// </summary>
        public string Origin { get; set; }

        /// <summary>
        /// Type of the message
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Tracker id of the message queued
        /// </summary>
        public string TrackerId { get; set; }
    }

    public class SendMailResponse
    {
        public SendMailResponse(string trackerId)
        {
            TrackerId = trackerId;
        }

        /// <summary>
        /// Id for track the email sending status
        /// </summary>
        public string TrackerId { get; set; }
    }

    // <summary>
    // An email object containing the email address and name of the sender or recipient.
    // </summary>
    public class EmailAddress
    {
        [Required(ErrorMessage = "Required field")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
