using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SendiT.Model;
using System.Collections.Generic;
using System.Net;
using Xunit;
using static SendiT.Tests.TestFactory;

namespace SendiT.Tests
{
    public class SendMailTest
    {
        private readonly ILogger logger = CreateLogger();

        [Theory]
        [MemberData(nameof(GetEmails))]
        public async void SendEmail(OutgoingEmail email)
        {
            var queue = new AsyncCollector<OutgoingEmail>();
            var tbTrack = new AsyncCollector<SendEmailTrack>();

            var response = (ObjectResult)await Email.SendEmail(CreateMockRequest(email).Object, queue, tbTrack, logger);

            Assert.Equal(200, response.StatusCode);
            Assert.NotNull(((SendMailResponse)response.Value).TrackerId);
        }

        public static IEnumerable<object[]> GetEmails
        {
            get
            {
                return new[]
                {
                    new object[] { EmailAllFields },
                    new object[] { EmailRequiredFieldsOnly }
                };
            }
        }

        private static OutgoingEmail EmailAllFields => Sample;
        private static OutgoingEmail EmailRequiredFieldsOnly =>
            new OutgoingEmail
            {
                FromAddress = new EmailAddress { Email = Sample.FromAddress.Email },
                ToAddress = new EmailAddress { Email = Sample.ToAddress.Email }
            };
        private static OutgoingEmail Sample =>
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
