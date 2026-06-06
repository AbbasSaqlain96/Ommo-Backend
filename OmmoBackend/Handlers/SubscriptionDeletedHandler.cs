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
    public class SubscriptionDeletedHandler : StripeEventHandlerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ICompanyPaymentProfileRepository _profileRepo;
        private readonly IBillingService _billingService;
        private readonly IPackagePlanRepository _planRepo;
        private readonly IStripeMetadataService _stripeMetadataService;

        public SubscriptionDeletedHandler(
            AppDbContext dbContext,
            IStripeProcessedEventRepository eventRepo,
            IAlertService alertService,
            ILogger<SubscriptionDeletedHandler> logger,
            ICompanyPaymentProfileRepository profileRepo,
            IBillingService billingService,
            IPackagePlanRepository planRepo,
            IStripeMetadataService stripeMetadataService)
            : base(eventRepo, alertService, logger)
        {
            _dbContext = dbContext;
            _profileRepo = profileRepo;
            _billingService = billingService;
            _planRepo = planRepo;
            _stripeMetadataService = stripeMetadataService;
        }

        public override string EventType => StripeEvents.CustomerSubscriptionDeleted;
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
                    {
                        await transaction.CommitAsync();
                        return;
                    }

                    var subscription = (Subscription)stripeEvent.Data.Object;

                    var companyId = await _stripeMetadataService.GetCompanyIdAsync(subscription.CustomerId);

                    var profile = await _profileRepo.GetForUpdateAsync(companyId);

                    if (profile == null)
                    {
                        await AlertAsync(
                            "missing_profile",
                            companyId,
                            stripeEvent.Type,
                            "Company payment profile not found.");

                        await transaction.CommitAsync();
                        return;
                    }

                    if (!profile.current_period_start.HasValue ||
                        !profile.current_period_end.HasValue)
                    {
                        await AlertAsync(
                            "missing_billing_period",
                            companyId,
                            stripeEvent.Type,
                            "Billing period is missing.");

                        await transaction.CommitAsync();
                        return;
                    }

                    var plan = await _planRepo.GetByIdAsync(profile.subscription_plan!.Value);

                    if (plan == null)
                    {
                        await AlertAsync(
                            "plan_not_found",
                            companyId,
                            stripeEvent.Type,
                            $"Plan {profile.subscription_plan} not found.");

                        await transaction.CommitAsync();
                        return;
                    }

                    await _billingService.InsertHistoryAsync(
                        profile,
                        plan,
                        profile.current_period_start.Value,
                        profile.current_period_end.Value,
                        profile.minutes_used);

                    profile.subscription_status = SubscriptionStatus.inactive;

                    profile.payment_failed = false;

                    profile.canceled_at = DateTimeOffset.UtcNow;

                    profile.updated_at = DateTimeOffset.UtcNow;

                    await _profileRepo.UpdateAsync(profile);

                    await _dbContext.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation("Subscription deleted processed. CompanyId={CompanyId}", companyId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogError(ex, "Error processing SubscriptionDeleted webhook.");

                    throw;
                }
            });
        }
    }
}
