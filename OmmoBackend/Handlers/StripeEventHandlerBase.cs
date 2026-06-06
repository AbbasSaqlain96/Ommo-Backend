using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using Stripe;

namespace OmmoBackend.Handlers
{
    public abstract class StripeEventHandlerBase : IStripeEventHandler
    {
        protected readonly IStripeProcessedEventRepository _eventRepo;
        protected readonly IAlertService _alertService;
        protected readonly ILogger _logger;

        protected StripeEventHandlerBase(
            IStripeProcessedEventRepository eventRepo,
            IAlertService alertService,
            ILogger logger)
        {
            _eventRepo = eventRepo;
            _alertService = alertService;
            _logger = logger;
        }

        public abstract string EventType { get; }

        public abstract Task HandleAsync(Event stripeEvent);

        protected async Task<bool> IsDuplicateAsync(string eventId)
        {
            return !(await _eventRepo.TryInsertAsync(eventId));
        }

        protected async Task AlertAsync(string reason, int companyId, string eventType, string message)
        {
            await _alertService.SendAsync(reason, companyId, eventType, message);
        }
    }
}
