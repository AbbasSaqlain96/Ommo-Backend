using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class BillingService : IBillingService
    {
        private readonly IBillingRepository _billingRepository;
        private readonly ILogger<BillingService> _logger;
        private readonly AppDbContext _dbContext;
        private readonly IPackagePlanRepository _packagePlanRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly ICompanyPaymentProfileRepository _companyPaymentProfileRepository;
        private readonly IStripeBillingService _stripeBillingService;
        public BillingService(IBillingRepository billingRepository, ILogger<BillingService> logger, AppDbContext dbContext, IPackagePlanRepository packagePlanRepository, ICompanyRepository companyRepository, ICompanyPaymentProfileRepository companyPaymentProfileRepository, IStripeBillingService stripeBillingService)
        {
            _billingRepository = billingRepository;
            _logger = logger;
            _dbContext = dbContext;
            _packagePlanRepository = packagePlanRepository;
            _companyRepository = companyRepository;
            _companyPaymentProfileRepository = companyPaymentProfileRepository;
            _stripeBillingService = stripeBillingService;
        }

        public async Task<ServiceResponse<object>> GetCompanyProfileAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching company profile  for CompanyId {CompanyId}", companyId);

                var profile = await _billingRepository.GetCompanyProfileAsync(companyId);

                if (profile == null)
                {
                    _logger.LogWarning("Payment profile not found for CompanyId {CompanyId}", companyId);
                    return ServiceResponse<object>.ErrorResponse("Payment profile not found.", 404);
                }

                var minuteUnused = Math.Max(0, profile.TotalPackageMinutes - profile.MinutesUsed);

                var response = new
                {
                    subscription_plan = profile.SubscriptionPlan,
                    subscription_status = profile.SubscriptionStatus,
                    current_period_start = profile.CurrentPeriodStart,
                    current_period_end = profile.CurrentPeriodEnd,
                    cancel_at_period_end = profile.CancelAtPeriodEnd,
                    canceled_at = profile.CanceledAt,
                    minutes_used = profile.MinutesUsed,
                    total_package_minutes = profile.TotalPackageMinutes,
                    minute_unused = minuteUnused
                };

                _logger.LogInformation("Company profile fetched successfully for CompanyId {CompanyId}", companyId);
                return ServiceResponse<object>.SuccessResponse(response, "Company profile fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company profile for CompanyId {CompanyId}", companyId);
                return ServiceResponse<object>.ErrorResponse("Server error", 503);
            }
        }

        public async Task<ServiceResponse<BillingHistoryResponseDto>> GetBillingHistoryAsync(int companyId)
        {
            try
            {
                // Fetch last 10 records
                var records = await _billingRepository.GetLatestRecordsAsync(companyId);

                // Fetch aggregates
                var aggregates = await _billingRepository.GetLast4RecordsAggregatesAsync(companyId);

                var response = new BillingHistoryResponseDto
                {
                    Last4RecordMinuteConsumed = aggregates.MinuteConsumed,
                    Last4RecordTotalBilled = aggregates.TotalBilled,
                    Records = records ?? new List<BillingHistoryRecordDto>()
                };

                return ServiceResponse<BillingHistoryResponseDto>.SuccessResponse(response, "Billing history fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching billing history for CompanyId {CompanyId}", companyId);
                return ServiceResponse<BillingHistoryResponseDto>.ErrorResponse("Server error", 503);
            }
        }

        public async Task<ServiceResponse<object>> DummyCheckoutAsync(int companyId)
        {
            try
            {
                await _dbContext.company_onboarding
                    .Where(x => x.company_id == companyId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(
                            x => x.current_step,
                            OnboardingStep.call_settings)
                        .SetProperty(
                            x => x.payment_completed_at,
                            DateTime.UtcNow)
                        .SetProperty(
                            x => x.updated_at,
                            DateTime.UtcNow));

                return ServiceResponse<object>.SuccessResponse(null, "Checkout complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DummyCheckoutAsync");

                return ServiceResponse<object>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<object>> DummyBuyNumberAsync(int companyId)
        {
            try
            {
                await _dbContext.company_onboarding
                    .Where(x => x.company_id == companyId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(
                            x => x.current_step,
                            OnboardingStep.completed)
                        .SetProperty(
                            x => x.is_completed,
                            true)
                        .SetProperty(
                            x => x.call_settings_completed_at,
                            DateTime.UtcNow)
                        .SetProperty(
                            x => x.updated_at,
                            DateTime.UtcNow));

                return ServiceResponse<object>.SuccessResponse(null, "Number purchased successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DummyBuyNumberAsync");

                return ServiceResponse<object>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<CreateCheckoutSessionResponse>> CreateCheckoutSessionAsync(int companyId, CreateCheckoutSessionRequest request)
        {
            try
            {
                _logger.LogInformation("Checkout started. CompanyId={CompanyId}, PlanId={PlanId}", companyId, request.PlanId);

                var plan = await _packagePlanRepository.GetByIdAsync(request.PlanId);

                if (plan == null)
                {
                    return ServiceResponse<CreateCheckoutSessionResponse>.ErrorResponse("No such plan exists.", 404);
                }

                var planValidation = ValidatePlan(companyId, plan);

                if (!planValidation.Success)
                {
                    return ServiceResponse<CreateCheckoutSessionResponse>.ErrorResponse(planValidation.ErrorMessage, planValidation.StatusCode);
                }

                var company = await _companyRepository.GetByIdAsync(companyId);

                if (company == null)
                {
                    return ServiceResponse<CreateCheckoutSessionResponse>.ErrorResponse("Company not found.", 404);
                }

                var profile = await _companyPaymentProfileRepository.GetByCompanyIdAsync(companyId);

                string stripeCustomerId;

                if (profile != null)
                {
                    var validation = ValidateExistingSubscription(profile);

                    if (!validation.Success)
                    {
                        return ServiceResponse<CreateCheckoutSessionResponse>.ErrorResponse(validation.ErrorMessage, validation.StatusCode);
                    }

                    if (profile.subscription_plan != request.PlanId)
                    {
                        profile.subscription_plan = request.PlanId;
                        profile.updated_at = DateTimeOffset.UtcNow;

                        await _companyPaymentProfileRepository.UpdateAsync(profile);

                        await _dbContext.SaveChangesAsync();
                    }

                    stripeCustomerId = profile.stripe_customer_id!;
                }
                else
                {
                    stripeCustomerId = await _stripeBillingService.CreateCustomerAsync(company);

                    profile = new CompanyPaymentProfile
                    {
                        company_id = companyId,
                        stripe_customer_id = stripeCustomerId,
                        stripe_subscription_id = null,
                        subscription_plan = request.PlanId,
                        subscription_status = SubscriptionStatus.inactive,

                        trial_started_at = null,
                        trial_ends_at = null,

                        current_period_end = null,

                        cancel_at_period_end = false,
                        canceled_at = null,

                        minutes_used = 0,

                        payment_failed = false,
                        payment_failed_at = null,

                        payment_retry_count = 0,

                        updated_at = DateTimeOffset.UtcNow
                    };

                    await _companyPaymentProfileRepository.InsertAsync(profile);

                    await _dbContext.SaveChangesAsync();
                }

                var checkoutUrl = await _stripeBillingService.CreateCheckoutSessionAsync(stripeCustomerId, companyId, plan);

                _logger.LogInformation("Checkout session created. CompanyId={CompanyId}, StripeCustomerId={StripeCustomerId}, PlanId={PlanId}", companyId, stripeCustomerId, plan.plan_id);

                return ServiceResponse<CreateCheckoutSessionResponse>.SuccessResponse(
                    new CreateCheckoutSessionResponse
                    {
                        checkout_url = checkoutUrl
                    },
                    "Checkout session created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checkout session failed for CompanyId={CompanyId}", companyId);

                return ServiceResponse<CreateCheckoutSessionResponse>.ErrorResponse("Server error occurred while creating checkout session.", 500);
            }
        }

        private static ServiceResponse<bool> ValidatePlan(int companyId, PackagePlan plan)
        {
            if (plan.plan_type == PlanType.custom && plan.company_id != companyId)
            {
                return ServiceResponse<bool>.ErrorResponse("This plan does not belong to your account.", 403);
            }

            if (plan.plan_status == PlanStatus.inactive)
            {
                return ServiceResponse<bool>.ErrorResponse("This plan is no longer available.", 400);
            }

            return ServiceResponse<bool>.SuccessResponse(true);
        }

        private static ServiceResponse<bool> ValidateExistingSubscription(CompanyPaymentProfile profile)
        {
            if (profile.subscription_status == SubscriptionStatus.active || profile.subscription_status == SubscriptionStatus.trial)
            {
                return ServiceResponse<bool>.ErrorResponse("You already have an active package.", 400);
            }

            return ServiceResponse<bool>.SuccessResponse(true);
        }


        public async Task InsertHistoryAsync(
            CompanyPaymentProfile profile,
            PackagePlan plan,
            DateTimeOffset start,
            DateTimeOffset end,
            int minutesUsed)
        {
            var entity = new CompanyBillHistory
            {
                company_id = profile.company_id,
                start_date = start,
                end_date = end,
                minutes_used = minutesUsed,
                minute_unused = Math.Max(0, plan.est_minute - minutesUsed),
                package_name = plan.plan_name,
                amount = plan.price
            };

            await _dbContext.company_bill_history.AddAsync(entity);
        }

        public async Task InsertTrialHistoryAsync(CompanyPaymentProfile profile)
        {
            var entity = new CompanyBillHistory
            {
                company_id = profile.company_id,
                start_date = profile.trial_started_at!.Value,
                end_date = profile.trial_ends_at!.Value,
                minutes_used = profile.minutes_used,
                minute_unused = Math.Max(0, 50 - profile.minutes_used),
                package_name = "Trial",
                amount = 0
            };

            await _dbContext.company_bill_history.AddAsync(entity);
        }
    }
}
