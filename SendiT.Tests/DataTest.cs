using SendiT.Model;
using System.Collections.Generic;

namespace SendiT.Tests
{
    public static class DataTest
    {
        public static OutgoingEmail EmailAllFields => Sample;
        public static OutgoingEmail EmailRequiredFieldsOnly =>
            new OutgoingEmail
            {
                FromAddress = new EmailAddress { Email = Sample.FromAddress.Email },
                ToAddress = new EmailAddress { Email = Sample.ToAddress.Email }
            };
        public static OutgoingEmail Sample =>
            new OutgoingEmail
            {
                FromAddress = new EmailAddress { Email = "sendit@email.com", Name = "SendiT Program" },
                ToAddress = new EmailAddress { Email = "no-one@email.com", Name = "Arya Stark" },
                Origin = "SenditTest",
                Body = "This is a test body.",
                Subject = "Test of email send",
                Type = "Test"
            };
    }
}
