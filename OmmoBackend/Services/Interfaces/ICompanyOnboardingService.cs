using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface ICompanyOnboardingService
    {
        Task<ServiceResponse<string>> AddCompanyOnboardingAsync(int companyId);
        Task UpdateOnboardingIfRequired(int companyId);
    }
}
