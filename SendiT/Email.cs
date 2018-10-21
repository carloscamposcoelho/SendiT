
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using SendiT.Model;
using SendiT.Util;

namespace SendiT
{
    public static class Email
    {
        [FunctionName("EmailQueue")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req,
            TraceWriter log)
        {
            string modelErros;
            log.Info("C# HTTP trigger function processed a request.");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            var email = JsonConvert.DeserializeObject<OutgoingEmail>(requestBody);
            
            if (!ValidatorUtil.IsValid(email, out modelErros))
            {
                return new BadRequestObjectResult(modelErros);
            }

            return email != null
                ? (ActionResult)new OkObjectResult($"Received: {JsonConvert.SerializeObject(email)}")
                : new BadRequestObjectResult("Please pass some json as a post request.");
        }
    }
}
