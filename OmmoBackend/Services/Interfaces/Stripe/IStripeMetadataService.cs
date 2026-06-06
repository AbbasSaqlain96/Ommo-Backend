namespace OmmoBackend.Services.Interfaces.Stripe
{
    public interface IStripeMetadataService
    {
        Task<int> GetCompanyIdAsync(string customerId);
    }
}
