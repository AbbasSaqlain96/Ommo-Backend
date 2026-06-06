using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ICompanyPaymentProfileRepository
    {
        Task<CompanyPaymentProfile?> GetByCompanyIdAsync(int companyId);
        Task InsertAsync(CompanyPaymentProfile profile);
        Task UpdateAsync(CompanyPaymentProfile profile);

        Task<CompanyPaymentProfile?> GetForUpdateAsync(int companyId);

    }
}
