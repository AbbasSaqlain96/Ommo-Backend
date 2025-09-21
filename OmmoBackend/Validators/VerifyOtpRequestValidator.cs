using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using OmmoBackend.Dtos;

namespace OmmoBackend.Validators
{
    public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
    {
        public VerifyOtpRequestValidator()
        {
            RuleFor(x => x.OtpId)
                .NotEmpty()
                .WithMessage("OtpId must be provided.");

            RuleFor(x => x.OtpNumber)
                .NotEmpty()
                .WithMessage("Otp number must be provided.");
        }
    }
}