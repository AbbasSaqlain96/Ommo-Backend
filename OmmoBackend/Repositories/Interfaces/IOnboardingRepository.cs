using OmmoBackend.Dtos;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IOnboardingRepository
    {
        Task<OnboardingAuthDto?> GetOnboardingDataAsync(int companyId);
        Task UpdateToIntegrationStepAsync(int companyId);
        Task UpdateToPaymentStepAsync(int companyId);
        Task MarkCallSettingsCompletedAsync(int companyId);
    }
}
