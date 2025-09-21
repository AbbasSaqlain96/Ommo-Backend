using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using OmmoBackend.Dtos;

namespace OmmoBackend.Validators
{
    public class CreateMaintenanceIssueRequestValidator : AbstractValidator<CreateMaintenanceIssueRequest>
    {
        public CreateMaintenanceIssueRequestValidator()
        {
            // Ensure Description is required and not empty
            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required.");

            // Ensure IssueType is required and matches either "recurring" or "one-time"
            RuleFor(x => x.IssueType)
                .NotEmpty()
                .WithMessage("Issue Type is required.")
                .Matches("recurring|one-time")
                .WithMessage("Issue Type must be either 'recurring' or 'one-time'.");

            // Optional CompanyId must be greater than zero if provided
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .When(x => x.CompanyId.HasValue)
                .WithMessage("Company ID must be a positive integer.");

            // Ensure ScheduleInterval is only specified if IssueType is "recurring"
            RuleFor(x => x.ScheduleInterval)
                .NotNull()
                .When(x => x.IssueType?.ToLower() == "recurring")
                .WithMessage("Schedule Interval is required for recurring issues.")
                .GreaterThan(DateTime.MinValue)
                .WithMessage("Schedule Interval must be a valid date.");
        }
    }
}