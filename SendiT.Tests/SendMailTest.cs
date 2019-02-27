using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SendiT.Function;
using SendiT.Model;
using System.Collections.Generic;
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
                    new object[] { DataTest.EmailAllFields },
                    new object[] { DataTest.EmailRequiredFieldsOnly }
                };
            }
        }
    }
}
