using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
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

            TraceWriter log)
        {
            emailQueue = null;
            try
            {
                string modelErros;

                log.Info("C# HTTP trigger function processed a request.");

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
                log.Error("An error has ocurred.", ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}