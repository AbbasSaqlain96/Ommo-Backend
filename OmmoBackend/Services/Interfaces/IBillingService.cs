using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;

namespace OmmoBackend.Services.Interfaces
{
    public interface IBillingService
    {
        Task<ServiceResponse<object>> GetCompanyProfileAsync(int companyId);
        Task<ServiceResponse<BillingHistoryResponseDto>> GetBillingHistoryAsync(int companyId);
        Task<ServiceResponse<object>> DummyCheckoutAsync(int companyId);
        Task<ServiceResponse<object>> DummyBuyNumberAsync(int companyId);


        Task<ServiceResponse<CreateCheckoutSessionResponse>> CreateCheckoutSessionAsync(int companyId, CreateCheckoutSessionRequest request);


        Task InsertHistoryAsync(
            CompanyPaymentProfile profile,
            PackagePlan plan,
            DateTimeOffset start,
            DateTimeOffset end,
            int minutesUsed);

        Task InsertTrialHistoryAsync(CompanyPaymentProfile profile);
    }
}
