using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using SendiT.Model.SendGrid;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SendiT.Logic
{
    public static class SendMail
    {
        private const string SEND_GRID_API_KEY = "AzureWebJobsSendGridApiKey";
        private const string TXT_TRACKER_ID = "TrackerId";

        #region SendSingleEmail
        public static async Task<SendResponse> SendSingleEmail(string from, string to, string subject, string htmlContent,
            string trackerId, ILogger log)
        {
            var fromAddress = new EmailAddress(from);
            var toAddress = new EmailAddress(to);
            return await SendSingleEmail(fromAddress, toAddress, subject, htmlContent, trackerId, log);
        }

        public static async Task<SendResponse> SendSingleEmail(EmailAddress fromAddress, EmailAddress toAddress, string subject, string htmlContent,
                string trackerId, ILogger log)
        {
            try
            {
                // Retrieve the API key from the environment variables.
                var apiKey = Environment.GetEnvironmentVariable(SEND_GRID_API_KEY);

                var client = new SendGridClient(apiKey);

                // Send a Single Email using the Mail Helper
                var msg = MailHelper.CreateSingleEmail(fromAddress, toAddress, subject, string.Empty, htmlContent);

                if (!string.IsNullOrEmpty(trackerId))
                {
                    msg.AddCustomArg(TXT_TRACKER_ID, trackerId);
                }
                var response = await client.SendEmailAsync(msg);

                return new SendResponse
                {
                    MessageId = response.Headers.GetValues("X-Message-Id").FirstOrDefault(),
                    StatusCode = response.StatusCode
                };
            }
            catch (Exception ex)
            {
                log.LogError("An error has occurred.", ex);
                throw;
            }
        }
        #endregion //SendSingleEmail
    }
}
