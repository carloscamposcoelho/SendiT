using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SendiT.Model;
using Xunit;
namespace SendiT.Tests
{
    public class SendMailTest
    {
        private readonly ILogger logger = TestFactory.CreateLogger();

        [Fact]
        public async void SendEmail()
        {
            var email = new Model.OutgoingEmail
            {
                FromAddress = new Model.EmailAddress { Email = "sendit@email.com", Name = "SendiT Program" },
                ToAddress = new Model.EmailAddress { Email = "no-one@email.com", Name = "Arya Stark" },
                Origin = "SenditTest",
                Body = "This is a test body.",
                Subject = "Test of email send",
                Type = "Test"
            };

            // Mock DurableOrchestrationClientBase
            //var queueMock = new IAsyncCollector<OutgoingEmail>();

            var response = (StatusCodeResult) await Email.SendEmail(TestFactory.CreateHttpRequest(email), null, null, logger);

            //Assert.Equal("Hello, Bill", response.Value);
        }


    }
}
