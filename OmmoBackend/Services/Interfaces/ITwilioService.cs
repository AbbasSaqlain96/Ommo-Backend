namespace OmmoBackend.Services.Interfaces
{
    public interface ITwilioService
    {
        Task<string> BuyNumberAsync();
    }
}
