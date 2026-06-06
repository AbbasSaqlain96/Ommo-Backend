using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class CompanyOnboardingRepository : ICompanyOnboardingRepository
    {
        private readonly AppDbContext _dbContext;
        public CompanyOnboardingRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddCompanyOnboardingAsync(CompanyOnboarding onboarding)
        {
            await _dbContext.company_onboarding.AddAsync(onboarding);
            await _dbContext.SaveChangesAsync();
        }
    }
}
