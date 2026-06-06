using OmmoBackend.Services.Interfaces.Stripe;
using Stripe;

namespace OmmoBackend.Services.Implementations.Stripe
{
    public class StripeMetadataService : IStripeMetadataService
    {
        public async Task<int> GetCompanyIdAsync(string customerId)
        {
            var customerService = new CustomerService();

            var customer =
                await customerService.GetAsync(customerId);

            if (customer == null)
                throw new Exception(
                    $"Stripe customer {customerId} not found.");

            if (!customer.Metadata.TryGetValue(
                    "company_id",
                    out var companyIdValue))
            {
                throw new Exception(
                    $"company_id metadata missing for customer {customerId}");
            }

            return int.Parse(companyIdValue);
        }
    }
}
