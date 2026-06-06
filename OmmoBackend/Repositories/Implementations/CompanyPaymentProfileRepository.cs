using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class CompanyPaymentProfileRepository : ICompanyPaymentProfileRepository
    {
        private readonly AppDbContext _dbContext;
        public CompanyPaymentProfileRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CompanyPaymentProfile?> GetByCompanyIdAsync(int companyId)
        {
            return await _dbContext.companies_payment_profile.FindAsync(companyId);
        }

        public async Task InsertAsync(CompanyPaymentProfile profile)
        {
            await _dbContext.companies_payment_profile.AddAsync(profile);
        }

        public Task UpdateAsync(CompanyPaymentProfile profile)
        {
            _dbContext.companies_payment_profile.Update(profile);

            return Task.CompletedTask;
        }

        public async Task<CompanyPaymentProfile?> GetForUpdateAsync(int companyId)
        {
            return await _dbContext
                .companies_payment_profile
                .FromSqlInterpolated($@"
                    SELECT *
                    FROM companies_payment_profile
                    WHERE company_id = {companyId}
                    FOR UPDATE")
                .FirstOrDefaultAsync();
        }
    }
}
