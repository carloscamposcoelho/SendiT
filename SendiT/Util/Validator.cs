using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace SendiT.Util
{
    public static class ValidatorUtil
    {
        public static bool IsValid(object instance, out string errors)
        {
            errors = "";
            var context = new ValidationContext(instance, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(instance, context, validationResults, true);

            if (!isValid)
            {
                errors = JsonConvert.SerializeObject(validationResults);
            }

            return isValid;
        }
    }
}
