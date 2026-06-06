using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class CompanyOnboardingService : ICompanyOnboardingService
    {
        private readonly ICompanyOnboardingRepository _companyOnboardingRepository;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<CompanyOnboardingService> _logger;
        public CompanyOnboardingService(ICompanyOnboardingRepository companyOnboardingRepository, AppDbContext dbContext, ILogger<CompanyOnboardingService> logger)
        {
            _companyOnboardingRepository = companyOnboardingRepository;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<ServiceResponse<string>> AddCompanyOnboardingAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Adding onboarding for CompanyId {CompanyId}", companyId);

                CompanyOnboarding onboarding = new CompanyOnboarding
                {
                    company_id = companyId
                };

                await _companyOnboardingRepository.AddCompanyOnboardingAsync(onboarding);

                _logger.LogInformation("Onboarding added for CompanyId {CompanyId}", companyId);

                return ServiceResponse<string>.SuccessResponse("Onboarding added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add onboarding for CompanyId {CompanyId}", companyId);
                throw;
            }
        }

        public async Task UpdateOnboardingIfRequired(int companyId)
        {
            var onboarding =
                await _dbContext.company_onboarding
                    .FirstOrDefaultAsync(x =>
                        x.company_id == companyId);

            if (onboarding == null)
                return;

            if (onboarding.current_step != OnboardingStep.payment)
                return;

            onboarding.current_step = OnboardingStep.call_settings;
            onboarding.payment_completed_at = DateTimeOffset.UtcNow;
            onboarding.updated_at = DateTimeOffset.UtcNow;
            onboarding.is_completed = false;

            _dbContext.company_onboarding.Update(onboarding);
        }
    }
}
