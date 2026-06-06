using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IBillingRepository
    {
        Task<CompanyPaymentProfileDto?> GetCompanyProfileAsync(int companyId);
        Task<List<BillingHistoryRecordDto>> GetLatestRecordsAsync(int companyId);
        Task<BillingAggregatesDto> GetLast12MonthsAggregatesAsync(int companyId);
        Task<BillingAggregatesDto> GetLast4RecordsAggregatesAsync(int companyId);
        Task<UserPlanLimitDto?> GetUserPlanLimitAsync(int companyId);
        Task<CompanyPlanFeaturesDto?> GetCompanyPlanFeaturesAsync(int companyId);
        Task<CompanyConcurrencyDto?> GetConcurrencyDataAsync(int companyId);
    }
}
