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
    }
}
