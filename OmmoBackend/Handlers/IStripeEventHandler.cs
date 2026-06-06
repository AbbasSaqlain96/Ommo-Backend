using Stripe;

namespace OmmoBackend.Handlers
{
    public interface IStripeEventHandler
    {
        string EventType { get; }
        Task HandleAsync(Event stripeEvent);
    }
}