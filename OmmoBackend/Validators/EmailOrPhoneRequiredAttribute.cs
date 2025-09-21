using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Validators
{
    public class EmailOrPhoneRequiredAttribute : ValidationAttribute
    {
        private readonly string _otherProperty;

        public EmailOrPhoneRequiredAttribute(string otherProperty)
        {
            _otherProperty = otherProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var currentValue = value as string;
            var otherProperty = validationContext.ObjectType.GetProperty(_otherProperty);

            if (otherProperty == null)
                return new ValidationResult($"Unknown property: {_otherProperty}");

            var otherValue = otherProperty.GetValue(validationContext.ObjectInstance) as string;

            // Check if both values are null or whitespace
            if (string.IsNullOrWhiteSpace(currentValue) && string.IsNullOrWhiteSpace(otherValue))
            {
                return new ValidationResult(ErrorMessage ?? "Either Email or Phone must be provided.");
            }

            return ValidationResult.Success;
        }
    }
}