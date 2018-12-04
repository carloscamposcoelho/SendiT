using System.ComponentModel;
using System.Runtime.Serialization;

namespace SendiT.Model
{
    public class Enumerators
    {
        public enum DeliveryEvent
        {
            [EnumMember(Value = "RequestQueued")]
            [Description("Message has been queued and will be send in a while.")]
            Queued = 1,

            [EnumMember(Value = "SendRequested")]
            [Description("Message has been sent to SendGrid to be processed.")]
            SendRequested = 2,

            [EnumMember(Value = "processed")]
            [Description("Message has been received and is ready to be delivered.")]
            Processed = 3,

            [EnumMember(Value = "dropped")]
            [Description("Message has been dropped, check reason for more information.")]
            Dropped = 4,

            [EnumMember(Value = "delivered")]
            [Description("Message has been successfully delivered to the receiving server.")]
            Delivered = 5,

            [EnumMember(Value = "deferred")]
            [Description("Receiving server temporarily rejected the message.")]
            Deferred = 6,

            [EnumMember(Value = "bounce")]
            [Description("Receiving server could not or would not accept the message.")]
            Bounce = 7,
            
            [EnumMember(Value = "open")]
            Open = 8,

            [EnumMember(Value = "click")]
            Click = 9,

            [EnumMember(Value = "spamreport")]
            SpamReport = 10,

            [EnumMember(Value = "unsubscribe")]
            Unsubscribe = 11,

            [EnumMember(Value = "group_unsubscribe")]
            GroupUnsubscribe = 12,

            [EnumMember(Value = "group_resubscribe")]
            GroupResubscribe = 13,
        }
    }
}
