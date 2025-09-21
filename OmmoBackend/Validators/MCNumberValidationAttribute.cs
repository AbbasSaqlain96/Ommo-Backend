using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Validators
{
    public class MCNumberValidationAttribute : ValidationAttribute
    {
        private readonly string _companyTypeProperty;

        public MCNumberValidationAttribute(string companyTypeProperty)
        {
            _companyTypeProperty = companyTypeProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var companyTypeProperty = validationContext.ObjectType.GetProperty(_companyTypeProperty);
            if (companyTypeProperty == null)
                return new ValidationResult($"Unknown property: {_companyTypeProperty}");

            var companyTypeValue = companyTypeProperty.GetValue(validationContext.ObjectInstance);

            // Check if MCNumber is required based on the CompanyType
            if (companyTypeValue is int companyType && companyType == 1 && string.IsNullOrWhiteSpace(value as string))
            {
                return new ValidationResult(ErrorMessage);
            }
            return ValidationResult.Success;
        }
    }
}