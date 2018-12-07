//Ref.: https://github.com/TsuyoshiUshio/FunctionsValidation

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SendiT.Util
{
    public class HttpResponseBody<T>
    {
        public bool IsValid { get; set; }
        public T Value { get; set; }

        public IEnumerable<ValidationResult> ValidationResults { get; set; }
    }

    public static class ModelValidationExtension
    {
        public static async Task<HttpResponseBody<T>> GetBodyAsync<T>(this HttpRequest request)
        {
            var body = new HttpResponseBody<T>();
            var bodyString = await request.ReadAsStringAsync();
            body.Value = Deserialize<T>(bodyString);

            var results = new List<ValidationResult>();
            body.IsValid = Validator.TryValidateObject(body.Value, new ValidationContext(body.Value, null, null), results, true);
            body.ValidationResults = results;
            return body;
        }

        private static T Deserialize<T>(string bodyString)
        {
            var token = JToken.Parse(bodyString);

            if (token is JArray)
            {
                var values = JsonConvert.DeserializeObject<List<T>>(bodyString);
                return values[0];
            }
            else //token is JObject
            {
                return JsonConvert.DeserializeObject<T>(bodyString);
            }
        }
    }
    
}