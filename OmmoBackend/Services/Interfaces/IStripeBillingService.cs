using OmmoBackend.Models;

namespace OmmoBackend.Services.Interfaces
{
    public interface IStripeBillingService
    {
        Task<string> CreateCustomerAsync(Company company);
        Task<string> CreateCheckoutSessionAsync(string stripeCustomerId, int companyId, PackagePlan plan);
    }
}
