using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static SendiT.Model.Enumerators;

namespace SendiT.Model.SendGrid
{
    public class DeliveryWebHook
    {
        public string Email { get; set; }

        public long TimeStamp { get; set; }

        [JsonProperty("smtp-id")]
        public string SmtpId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public DeliveryEvent Event { get; set; }
        
        [JsonProperty("sg_event_id")]
        public string SgEventId { get; set; }

        [JsonProperty("sg_message_id")]
        public string SgMessageId { get; set; }

        public string Reason { get; set; }

        public string Status { get; set; }
    }
}
