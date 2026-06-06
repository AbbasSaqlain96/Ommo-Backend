using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using OmmoBackend.Services.Interfaces.Stripe;
using Stripe;

namespace OmmoBackend.Handlers
{
    public class InvoicePaymentFailedHandler : StripeEventHandlerBase
    {
        private readonly ICompanyPaymentProfileRepository _profileRepo;
        private readonly IStripeMetadataService _stripeMetadataService;
        private readonly AppDbContext _dbContext;
        private readonly ICompanyRepository _companyRepository;
        private readonly IEmailService _emailService;
        public InvoicePaymentFailedHandler(
            IStripeProcessedEventRepository eventRepo,
            IAlertService alertService,
            ILogger<InvoicePaymentFailedHandler> logger,
            ICompanyPaymentProfileRepository profileRepo,
            IStripeMetadataService stripeMetadataService,
            AppDbContext dbContext,
            ICompanyRepository companyRepository,
            IEmailService emailService)
            : base(eventRepo, alertService, logger)
        {
            _profileRepo = profileRepo;
            _stripeMetadataService = stripeMetadataService;
            _dbContext = dbContext;
            _companyRepository = companyRepository;
            _emailService = emailService;
        }

        public override string EventType => StripeEvents.InvoicePaymentFailed;

        public override async Task HandleAsync(Event stripeEvent)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    var eventId = stripeEvent.Id;

                    if (await IsDuplicateAsync(eventId))
                        return;

                    var invoice = (Invoice)stripeEvent.Data.Object;

                    var companyId = await _stripeMetadataService.GetCompanyIdAsync(invoice.CustomerId);

                    var profile = await _profileRepo.GetForUpdateAsync(companyId);

                    if (profile == null)
                    {
                        await AlertAsync(
                            "missing_profile",
                            companyId,
                            stripeEvent.Type,
                            "Company payment profile not found.");

                        return;
                    }

                    if (profile.subscription_status != SubscriptionStatus.active &&
                        profile.subscription_status != SubscriptionStatus.trial)
                    {
                        return;
                    }

                    profile.payment_failed = true;
                    profile.payment_failed_at = DateTimeOffset.UtcNow;
                    profile.payment_retry_count++;

                    await _profileRepo.UpdateAsync(profile);

                    var company = await _companyRepository.GetByIdAsync(companyId);

                    if (company != null)
                    {
                        await SendFailureEmailAsync(
                            company.email,
                            profile.payment_retry_count);
                    }

                    if (profile.payment_retry_count > 3 &&
                        !string.IsNullOrWhiteSpace(profile.stripe_subscription_id))
                    {
                        var subscriptionService = new SubscriptionService();

                        await subscriptionService.CancelAsync(profile.stripe_subscription_id);
                    }

                    await _dbContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private async Task SendFailureEmailAsync(
            string email,
            int retryCount)
        {
            string subject;
            string body;

            switch (retryCount)
            {
                case 1:
                    subject = "Payment failed";
                    body =
                        "Payment failed on the first attempt. Please update your payment method.";
                    break;

                case 2:
                    subject = "Payment failed again";
                    body =
                        "Payment failed on the second attempt. Action is required.";
                    break;

                case 3:
                    subject = "Final payment warning";
                    body =
                        "Payment failed 3 times. Your service is at risk of suspension.";
                    break;

                default:
                    subject = "Subscription cancelled";
                    body =
                        "Your subscription has been cancelled due to repeated payment failures.";
                    break;
            }

            await _emailService.SendAsync(
                email,
                subject,
                body);
        }
    }
}
