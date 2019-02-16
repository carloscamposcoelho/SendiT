//Ref.: https://github.com/TsuyoshiUshio/FunctionsValidation

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Reflection;

namespace SendiT.Util
{
    public class HttpResponseBody<T>
    {
        public bool IsValid { get; set; }
        public T Value { get; set; }

        public List<ValidationResult> ValidationResults { get; set; }
    }

    public static class ModelValidationExtension
    {
        public static async Task<HttpResponseBody<T>> GetBodyAsync<T>(this HttpRequest request)
        {
            var body = new HttpResponseBody<T>();
            var bodyString = await request.ReadAsStringAsync();
            body.Value = Deserialize<T>(bodyString);

            var results = new List<ValidationResult>();
            Validate(body, results);

            return body;
        }

        private static void Validate<T>(HttpResponseBody<T> body, List<ValidationResult> results)
        {
            var isValid = false;
            body.IsValid = Validator.TryValidateObject(body.Value, new ValidationContext(body.Value, null, null), results, true);
            body.ValidationResults = results;

            foreach (PropertyInfo propertyInfo in body.Value.GetType().GetProperties())
            {
                if (propertyInfo.PropertyType.Namespace == "SendiT.Model")
                {
                    //It means that there's another model to validade
                    var prop = propertyInfo.GetValue(body.Value, null);
                    if (prop != null)
                    {
                        results = new List<ValidationResult>();
                        isValid = Validator.TryValidateObject(prop, new ValidationContext(prop, null, null), results, true);
                        body.ValidationResults.AddRange(results);

                        if (!isValid)
                        {
                            body.IsValid = isValid;
                        }
                    }
                }
            }
        }

        private static T Deserialize<T>(string bodyString)
        {
            var token = JToken.Parse(bodyString);
            return JsonConvert.DeserializeObject<T>(bodyString);
        }
    }

}