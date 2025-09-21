using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using OmmoBackend.Dtos;

namespace OmmoBackend.Validators
{
    public class CreateSubscriptionRequestValidator : AbstractValidator<CreateSubscriptionRequest>
    {
        public CreateSubscriptionRequestValidator()
        {
            RuleFor(x => x.DispatchId).NotEmpty().WithMessage("Dispatch ID is required");
            RuleFor(x => x.CarrierId).NotEmpty().WithMessage("Carrier ID is required");
            RuleFor(x => x.RequestedModules).NotEmpty().WithMessage("Requested modules are required");
        }
    }
}