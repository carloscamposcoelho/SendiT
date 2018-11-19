using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SendiT.Model
{
    public class Enumerators
    {
        public enum DeliveryEvents
        {
            [Description("Message has been sent to SendGrid to be processed.")]
            SendRequested = 1,
            [Description("Message has been received and is ready to be delivered.")]
            Processed = 2,
            [Description("Message has been dropped, check reason for more information.")]
            Dropped = 3,
            [Description("Message has been successfully delivered to the receiving server.")]
            Delivered = 4,
            [Description("Receiving server temporarily rejected the message.")]
            Deferred = 5,
            [Description("Receiving server could not or would not accept the message.")]
            Bounce = 6,
        }
    }

    public enum EngagementEvents
    {

    }
}
