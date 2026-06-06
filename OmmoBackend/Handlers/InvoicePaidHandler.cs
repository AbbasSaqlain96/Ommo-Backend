using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using OmmoBackend.Services.Interfaces.Stripe;
using Stripe;

namespace OmmoBackend.Handlers
{
    public class InvoicePaidHandler : StripeEventHandlerBase
    {
        private readonly ICompanyPaymentProfileRepository _profileRepo;
        private readonly ICompanyPlanChangeRequestRepository _planChangeRepo;
        private readonly IPackagePlanRepository _planRepo;
        private readonly IBillingService _billingService;
        private readonly IPeriodCalculationService _periodService;
        private readonly IStripeMetadataService _stripeMetadataService;

        private readonly AppDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly ICompanyRepository _companyRepository;
        private readonly IConfiguration _configuration;
        public InvoicePaidHandler(
            IStripeProcessedEventRepository eventRepo,
            IAlertService alertService,
            ILogger<InvoicePaidHandler> logger,
            ICompanyPaymentProfileRepository profileRepo,
            ICompanyPlanChangeRequestRepository planChangeRepo,
            IPackagePlanRepository planRepo,
            IBillingService billingService,
            IPeriodCalculationService periodService,
            IStripeMetadataService stripeMetadataService,
            AppDbContext dbContext,
            IEmailService emailService,
            ICompanyRepository companyRepository,
            IConfiguration configuration)
            : base(eventRepo, alertService, logger)
        {
            _profileRepo = profileRepo;
            _planChangeRepo = planChangeRepo;
            _planRepo = planRepo;
            _billingService = billingService;
            _periodService = periodService;
            _stripeMetadataService = stripeMetadataService;
            _dbContext = dbContext;
            _emailService = emailService;
            _companyRepository = companyRepository;
            _configuration = configuration;
        }

        public override string EventType => StripeEvents.InvoicePaid;

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
                        await AlertAsync("missing_profile", companyId, stripeEvent.Type, "Profile missing");
                        return;
                    }

                    var plan = await _planRepo.GetByIdAsync(profile.subscription_plan!.Value);

                    if (plan == null)
                    {
                        await AlertAsync("missing_plan", companyId, stripeEvent.Type, "Plan missing");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(profile.stripe_subscription_id))
                    {
                        await AlertAsync(
                            "missing_subscription_id",
                            companyId,
                            stripeEvent.Type,
                            "Profile does not contain a Stripe subscription id.");

                        return;
                    }

                    var subscription = await new SubscriptionService().GetAsync(profile.stripe_subscription_id);

                    var stripePriceId = subscription?.Items?.Data?.FirstOrDefault()?.Price?.Id;

                    if (string.IsNullOrEmpty(stripePriceId) ||
                        stripePriceId != plan.stripe_price_id)
                    {
                        await AlertAsync(
                            "stripe_price_mismatch",
                            companyId,
                            stripeEvent.Type,
                            $"Stripe={stripePriceId}, Plan={plan.stripe_price_id}");

                        return;
                    }

                    var changeRequest = await _planChangeRepo.GetPendingOrScheduledAsync(companyId);

                    if (changeRequest != null)
                    {
                        await ProcessPlanChange(profile, plan, changeRequest);
                    }
                    else
                    {
                        await ProcessRenewal(profile, plan, profile.stripe_subscription_id);
                    }

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "InvoicePaid failed");
                    throw;
                }
            });
        }

        private async Task ProcessPlanChange(
            CompanyPaymentProfile profile,
            PackagePlan currentPlan,
            CompanyPlanChangeRequest request)
        {
            var endDate =
                request.status == PlanChangeRequestStatus.pending
                    ? DateTimeOffset.UtcNow
                    : profile.current_period_end!.Value;

            await _billingService.InsertHistoryAsync(
                profile,
                currentPlan,
                profile.current_period_start!.Value,
                endDate,
                profile.minutes_used);

            var newPlan = await _planRepo.GetByIdAsync(request.request_plan);

            if (newPlan == null)
                throw new Exception("Invalid plan in change request");

            profile.subscription_plan = newPlan.plan_id;
            profile.subscription_status = SubscriptionStatus.active;

            profile.current_period_start = DateTimeOffset.UtcNow;
            profile.current_period_end = _periodService.GetPeriodEnd(newPlan, DateTimeOffset.UtcNow);

            profile.cancel_at_period_end = false;
            profile.canceled_at = null;

            profile.minutes_used = 0;
            profile.payment_failed = false;
            profile.payment_failed_at = null;
            profile.payment_retry_count = 0;

            profile.stripe_subscription_id = profile.stripe_subscription_id;
            profile.updated_at = DateTimeOffset.UtcNow;

            await _profileRepo.UpdateAsync(profile);

            request.status = PlanChangeRequestStatus.completed;
            await _planChangeRepo.UpdateAsync(request);
        }

        private async Task ProcessRenewal(
            CompanyPaymentProfile profile,
            PackagePlan plan,
            string stripeSubscriptionId)
        {
            if (profile.subscription_status == SubscriptionStatus.trial)
            {
                await _billingService.InsertTrialHistoryAsync(profile);
            }
            else
            {
                await _billingService.InsertHistoryAsync(
                    profile,
                    plan,
                    profile.current_period_start!.Value,
                    profile.current_period_end!.Value,
                    profile.minutes_used);
            }

            profile.subscription_status = SubscriptionStatus.active;
            profile.current_period_start = DateTimeOffset.UtcNow;

            profile.current_period_end = _periodService.GetPeriodEnd(plan, DateTimeOffset.UtcNow);

            profile.cancel_at_period_end = false;
            profile.canceled_at = null;

            profile.minutes_used = 0;
            profile.payment_failed = false;
            profile.payment_failed_at = null;
            profile.payment_retry_count = 0;

            profile.stripe_subscription_id = stripeSubscriptionId;
            profile.updated_at = DateTimeOffset.UtcNow;

            await _profileRepo.UpdateAsync(profile);
        }
    }
}
