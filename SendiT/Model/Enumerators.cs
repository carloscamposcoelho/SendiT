using System.ComponentModel;
using System.Runtime.Serialization;

namespace SendiT.Model
{
    public class Enumerators
    {
        public enum DeliveryEvent
        {
            [EnumMember(Value = "SendRequested")]
            [Description("Message has been sent to SendGrid to be processed.")]
            SendRequested = 1,

            [EnumMember(Value = "processed")]
            [Description("Message has been received and is ready to be delivered.")]
            Processed = 2,

            [EnumMember(Value = "dropped")]
            [Description("Message has been dropped, check reason for more information.")]
            Dropped = 3,

            [EnumMember(Value = "delivered")]
            [Description("Message has been successfully delivered to the receiving server.")]
            Delivered = 4,

            [EnumMember(Value = "deferred")]
            [Description("Receiving server temporarily rejected the message.")]
            Deferred = 5,

            [EnumMember(Value = "bounce")]
            [Description("Receiving server could not or would not accept the message.")]
            Bounce = 6,
            
            [EnumMember(Value = "open")]
            Open = 7,

            [EnumMember(Value = "click")]
            Click = 8,

            [EnumMember(Value = "spamreport")]
            SpamReport = 9,

            [EnumMember(Value = "unsubscribe")]
            Unsubscribe = 10,

            [EnumMember(Value = "group_unsubscribe")]
            GroupUnsubscribe = 11,

            [EnumMember(Value = "group_resubscribe")]
            GroupResubscribe = 12,
        }
    }
}
