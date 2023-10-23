using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Line;
using System.Text;
using System.Threading.Tasks;

namespace Supermarker_mvp.Presenters.Common
{
    internal class ModelDataValidation
    {
        public void Validate(object model)
        {
            string errorMessage = "";
            List<ValidationResult> validationResults = new List<ValidationResult>();
            ValidationContext validationContext = new ValidationContext(model);
            bool isValid = Validator.TryValidateObject(
                model, validationContext, validationResults, true);
            if (isValid == false)
            {
                foreach (var item in validationResults)
                {
                    errorMessage += item.ErrorMessage + "\n";
                }
                throw new Exception(errorMessage);
            }
        }
    }
}
