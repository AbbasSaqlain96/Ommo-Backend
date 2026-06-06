using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Services.Interfaces
{
    public interface IPackagePlanValidationService
    {
        Task<ValidationResult> ValidateAsync(int companyId, CompanyPaymentProfile profile);
    }

    public class PackagePlanValidationService : IPackagePlanValidationService
    {
        private readonly IPackagePlanRepository _planRepository;

        public PackagePlanValidationService(IPackagePlanRepository planRepository)
        {
            _planRepository = planRepository;
        }

        public async Task<ValidationResult> ValidateAsync(
            int companyId,
            CompanyPaymentProfile profile)
        {
            if (profile == null)
            {
                return new ValidationResult
                {
                    Success = false,
                    ErrorMessage = "Company payment profile not found."
                };
            }

            if (profile.subscription_status == SubscriptionStatus.active ||
                profile.subscription_status == SubscriptionStatus.trial)
            {
                return new ValidationResult
                {
                    Success = false,
                    ErrorMessage = "You already have an active package."
                };
            }

            if (profile.subscription_plan == null)
            {
                return new ValidationResult
                {
                    Success = false,
                    ErrorMessage = "No subscription plan selected."
                };
            }

            var plan = await _planRepository.GetByIdAsync(profile.subscription_plan.Value);

            if (plan == null)
            {
                return new ValidationResult
                {
                    Success = false,
                    ErrorMessage = "Selected plan does not exist."
                };
            }

            if (plan.plan_status == PlanStatus.inactive)
            {
                return new ValidationResult
                {
                    Success = false,
                    ErrorMessage = "This plan is no longer available."
                };
            }

            if (plan.plan_type == PlanType.custom && plan.company_id != companyId)
            {
                return new ValidationResult
                {
                    Success = false,
                    ErrorMessage = "This plan does not belong to your account."
                };
            }

            return new ValidationResult
            {
                Success = true,
                Plan = plan
            };
        }
    }
}
