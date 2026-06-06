using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class OnboardingRepository : IOnboardingRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<OnboardingRepository> _logger;
        public OnboardingRepository(AppDbContext dbContext, ILogger<OnboardingRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<OnboardingAuthDto?> GetOnboardingDataAsync(int companyId)
        {
            try
            {
                var result = await
                    (from c in _dbContext.company

                     join co in _dbContext.company_onboarding
                         on c.company_id equals co.company_id into coGroup
                     from co in coGroup.DefaultIfEmpty()

                     join cpp in _dbContext.companies_payment_profile
                         on c.company_id equals cpp.company_id into cppGroup
                     from cpp in cppGroup.DefaultIfEmpty()

                     where c.company_id == companyId

                     select new OnboardingAuthDto
                     {
                         IsCompleted = co != null ? co.is_completed : null,
                         CurrentStep = co != null ? co.current_step.ToString() : null,
                         SubscriptionStatus = cpp != null ? cpp.subscription_status.ToString() : null
                     })
                    .FirstOrDefaultAsync();

                return result;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Database failure in GetOnboardingDataAsync for CompanyId {CompanyId}",
                    companyId);

                throw;
            }
        }

        public async Task UpdateToIntegrationStepAsync(int companyId)
        {
            try
            {
                var rows = await _dbContext.company_onboarding
                    .Where(x => x.company_id == companyId && x.current_step == OnboardingStep.questionnaire)
                    .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.current_step, OnboardingStep.integration)
                    .SetProperty(x => x.questionnaire_completed_at, DateTime.UtcNow)
                    .SetProperty(x => x.updated_at, DateTime.UtcNow)
                    );

                if (rows == 0)
                {
                    _logger.LogWarning(
                        "No onboarding record updated for CompanyId {CompanyId}. Possible invalid state.",
                        companyId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to update onboarding step for CompanyId {CompanyId}",
                    companyId);

                throw;
            }
        }

        public async Task UpdateToPaymentStepAsync(int companyId)
        {
            await _dbContext.company_onboarding
                .Where(x => x.company_id == companyId &&
                            x.current_step == OnboardingStep.integration)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.current_step, OnboardingStep.payment)
                    .SetProperty(x => x.integration_completed_at, DateTime.UtcNow)
                    .SetProperty(x => x.updated_at, DateTime.UtcNow)
                );
        }

        public async Task MarkCallSettingsCompletedAsync(int companyId)
        {
            await _dbContext.company_onboarding
                .Where(x => x.company_id == companyId &&
                            x.current_step == OnboardingStep.call_settings)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.current_step, OnboardingStep.completed)
                    .SetProperty(x => x.call_settings_completed_at, DateTime.UtcNow)
                    .SetProperty(x => x.updated_at, DateTime.UtcNow)
                    .SetProperty(x => x.is_completed, true)
                );
        }
    }
}
