using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid.Helpers.Mail;
using SendiT.Model;
using SendiT.Util;
using System.IO;

namespace SendiT
{
    public static class Email
    {
        [FunctionName("EmailQueue")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Queue("email-queue", Connection = "AzureWebJobsStorage")] out OutgoingEmail emailQueue,
            ILogger log)
        {
            emailQueue = null;
			log.LogInformation("New email request.");

			try
            {
                string modelErros;

                string requestBody = new StreamReader(req.Body).ReadToEnd();
                var email = JsonConvert.DeserializeObject<OutgoingEmail>(requestBody);

                if (!ValidatorUtil.IsValid(email, out modelErros))
                {
					log.LogError($"The request model is invalid. Validation message {modelErros}");
					return new BadRequestObjectResult(modelErros);
				}

                //Queue email request
                emailQueue = email;

				log.LogInformation("Request queued successfully.");
				return new OkObjectResult("Request queued successfully.");
            }
            catch (System.Exception ex)
            {
                log.LogError("An error has ocurred.", ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [FunctionName("SendEmailFromQueue")]
        public static void QueueTrigger(
            [QueueTrigger("email-queue")] OutgoingEmail emailQueue,
			[SendGrid(ApiKey = "AzureWebJobsSendGridApiKey")] out SendGridMessage message,
			int dequeueCount,
			ILogger log)
        {
            log.LogInformation($"New email to send. Dequeue count for this message: {dequeueCount}.");

			try {
				message = new SendGridMessage();
				message.AddTo(emailQueue.To);
				message.AddContent("text/html", emailQueue.Body);
				message.SetFrom(new EmailAddress(emailQueue.From));
				message.SetSubject(emailQueue.Subject);

				log.LogInformation("Email sent successfully.");

			} catch (System.Exception ex) {
				log.LogError("An error has ocurred.", ex);
				throw;
			}

            
        }
    }
}