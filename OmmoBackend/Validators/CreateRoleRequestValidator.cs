using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using OmmoBackend.Dtos;

namespace OmmoBackend.Validators
{
    public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
    {
        public CreateRoleRequestValidator()
        {
            RuleFor(r => r.RoleName).NotEmpty().WithMessage("Role name is required.");
            RuleFor(r => r.CompanyId).NotEmpty().WithMessage("Company ID is required.");
            RuleFor(r => r.ModuleRoleRelationships).NotEmpty().WithMessage("Module role relationships are required.");
        }
    }
}