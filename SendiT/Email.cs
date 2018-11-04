using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            try
            {
                string modelErros;

                string requestBody = new StreamReader(req.Body).ReadToEnd();
                var email = JsonConvert.DeserializeObject<OutgoingEmail>(requestBody);

                if (!ValidatorUtil.IsValid(email, out modelErros))
                {
                    return new BadRequestObjectResult(modelErros);
                }

                //Queue email request
                emailQueue = email;

                return new OkObjectResult("Request was successfully queued.");

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
            ILogger log)
        {
            log.LogInformation($"Triggered an email for send. {JsonConvert.SerializeObject(emailQueue)}");

            
        }
    }
}