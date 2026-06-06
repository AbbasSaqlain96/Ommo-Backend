using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ICompanyOnboardingRepository
    {
        Task AddCompanyOnboardingAsync(CompanyOnboarding onboarding);
    }
}
