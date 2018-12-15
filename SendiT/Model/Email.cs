using System;
using System.ComponentModel.DataAnnotations;

namespace SendiT.Model
{
    public class OutgoingEmail
    {
        [Required(ErrorMessage = "Required field")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string To { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Required(ErrorMessage = "Required field")]
        public string From { get; set; }

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
}
