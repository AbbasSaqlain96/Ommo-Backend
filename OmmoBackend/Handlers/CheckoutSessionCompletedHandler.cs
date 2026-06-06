using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;

namespace OmmoBackend.Handlers
{
    public class CheckoutSessionCompletedHandler : StripeEventHandlerBase
    {
        private readonly ICompanyPaymentProfileRepository _profileRepo;
        private readonly IPeriodCalculationService _periodService;
        private readonly ICompanyOnboardingService _onboarding;
        private readonly IPackagePlanRepository _packagePlanRepository;
        private readonly AppDbContext _dbContext;

        public CheckoutSessionCompletedHandler(
            IStripeProcessedEventRepository eventRepo,
            IAlertService alertService,
            ILogger<CheckoutSessionCompletedHandler> logger,
            ICompanyPaymentProfileRepository profileRepo,
            IPeriodCalculationService periodService,
            ICompanyOnboardingService onboarding,
            IPackagePlanRepository packagePlanRepository,
            AppDbContext dbContext)
            : base(eventRepo, alertService, logger)
        {
            _profileRepo = profileRepo;
            _periodService = periodService;
            _onboarding = onboarding;
            _packagePlanRepository = packagePlanRepository;
            _dbContext = dbContext;
        }

        public override string EventType => StripeEvents.CheckoutSessionCompleted;

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

                    var session = (Session)stripeEvent.Data.Object;

                    if (!session.Metadata.TryGetValue("company_id", out var companyIdRaw) ||
                        !int.TryParse(companyIdRaw, out var companyId))
                    {
                        await AlertAsync(
                            "missing_company_id",
                            0,
                            stripeEvent.Type,
                            "Missing company_id in Stripe session metadata");

                        return;
                    }

                    var subscriptionId = session.SubscriptionId;

                    if (string.IsNullOrEmpty(subscriptionId))
                    {
                        await AlertAsync(
                            "missing_subscription_id",
                            companyId,
                            stripeEvent.Type,
                            "SubscriptionId missing in checkout.session.completed");

                        return;
                    }

                    var profile = await _profileRepo.GetForUpdateAsync(companyId);

                    if (profile == null)
                    {
                        await AlertAsync(
                            "missing_profile",
                            companyId,
                            stripeEvent.Type,
                            "Company payment profile not found");

                        return;
                    }

                    var plan = await _packagePlanRepository.GetByIdAsync(profile.subscription_plan!.Value);

                    if (plan == null)
                    {
                        await AlertAsync(
                            "missing_plan",
                            companyId,
                            stripeEvent.Type,
                            "Plan not found for subscription");

                        return;
                    }

                    // CORE UPDATE
                    profile.stripe_subscription_id = subscriptionId;
                    profile.current_period_start = DateTimeOffset.UtcNow;
                    profile.minutes_used = 0;
                    profile.cancel_at_period_end = false;
                    profile.canceled_at = null;

                    if (profile.subscription_status == SubscriptionStatus.trial)
                    {
                        profile.trial_started_at = DateTimeOffset.UtcNow;
                        profile.trial_ends_at = DateTimeOffset.UtcNow.AddDays(7);
                        profile.current_period_end = profile.trial_ends_at;
                    }
                    else
                    {
                        profile.subscription_status = SubscriptionStatus.active;

                        profile.current_period_end = _periodService.GetPeriodEnd(plan, DateTimeOffset.UtcNow);
                    }

                    profile.updated_at = DateTimeOffset.UtcNow;

                    await _profileRepo.UpdateAsync(profile);

                    await _onboarding.UpdateOnboardingIfRequired(companyId);

                    await _dbContext.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation("Checkout completed successfully for CompanyId={CompanyId}", companyId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogError(ex, "CheckoutSessionCompleted failed");

                    throw;
                }
            });
        }
    }
}
