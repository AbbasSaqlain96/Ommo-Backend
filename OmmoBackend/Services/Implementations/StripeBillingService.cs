using Microsoft.Extensions.Options;
using OmmoBackend.Models;
using OmmoBackend.Services.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace OmmoBackend.Services.Implementations
{
    public class StripeBillingService : IStripeBillingService
    {
        private readonly StripeSettings _stripeSettings;
        public StripeBillingService(IOptions<StripeSettings> stripeSettings)
        {
            _stripeSettings = stripeSettings.Value;
        }

        public async Task<string> CreateCustomerAsync(Company company)
        {
            var customerService = new CustomerService();

            var customer = await customerService.CreateAsync(
                new CustomerCreateOptions
                {
                    Email = company.email,
                    Name = company.name,

                    Metadata = new Dictionary<string, string>
                    {
                        { "company_id", company.company_id.ToString() }
                    }
                });

            return customer.Id;
        }

        public async Task<string> CreateCheckoutSessionAsync(string stripeCustomerId, int companyId, PackagePlan plan)
        {
            var sessionService = new SessionService();

            var session = await sessionService.CreateAsync(
                new SessionCreateOptions
                {
                    Customer = stripeCustomerId,

                    Mode = "subscription",

                    LineItems = new List<SessionLineItemOptions>
                    {
                        new()
                        {
                            Price = plan.stripe_price_id,
                            Quantity = 1
                        }
                    },

                    SuccessUrl = _stripeSettings.SuccessUrl,
                    CancelUrl = _stripeSettings.CancelUrl,

                    Metadata = new Dictionary<string, string>
                    {
                        { "company_id", companyId.ToString() },
                        { "plan_id", plan.plan_id.ToString() }
                    }
                });

            return session.Url!;
        }
    }

    public class StripeSettings
    {
        public string SecretKey { get; set; } = default!;
        public string PublishableKey { get; set; } = default!;
        public string WebhookSecret { get; set; } = default!;
        public string SuccessUrl { get; set; } = default!;
        public string CancelUrl { get; set; } = default!;
    }
}
