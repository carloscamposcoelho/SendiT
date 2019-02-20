using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SendiT.Model;
using System.Net;
using Xunit;
using static SendiT.Tests.TestFactory;

namespace SendiT.Tests
{
    public class SendMailTest
    {
        private readonly ILogger logger = CreateLogger();

        [Fact]
        public async void SendEmail()
        {
            var email = new OutgoingEmail
            {
                FromAddress = new EmailAddress { Email = "sendit@email.com", Name = "SendiT Program" },
                ToAddress = new EmailAddress { Email = "no-one@email.com", Name = "Arya Stark" },
                Origin = "SenditTest",
                Body = "This is a test body.",
                Subject = "Test of email send",
                Type = "Test"
            };

            var queue = new AsyncCollector<OutgoingEmail>();
            var tbTrack = new AsyncCollector<SendEmailTrack>();

            var response = (ObjectResult) await Email.SendEmail(CreateMockRequest(email).Object, queue, tbTrack, logger);

            Assert.Equal(200, response.StatusCode);
            Assert.NotNull(((SendMailResponse)response.Value).TrackerId);
        }


    }
}
