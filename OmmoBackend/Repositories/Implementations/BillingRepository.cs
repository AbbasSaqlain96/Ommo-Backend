using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using Org.BouncyCastle.Asn1.Cmp;

namespace OmmoBackend.Repositories.Implementations
{
    public class BillingRepository : IBillingRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<BillingRepository> _logger;
        public BillingRepository(AppDbContext dbContext, ILogger<BillingRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<CompanyPaymentProfileDto?> GetCompanyProfileAsync(int companyId)
        {
            try
            {
                var result = await
                    (from cpp in _dbContext.companies_payment_profile.AsNoTracking()
                     join pp in _dbContext.package_plan.AsNoTracking()
                     on cpp.subscription_plan equals pp.plan_id into planGroup
                     from pp in planGroup.DefaultIfEmpty()
                     
                     where cpp.company_id == companyId
                     
                     let totalMinutes = pp != null ? pp.est_minute : 0
                     
                     select new CompanyPaymentProfileDto
                     {
                         SubscriptionPlan = cpp.subscription_plan,
                         SubscriptionStatus = cpp.subscription_status.ToString(),
                         CurrentPeriodStart = cpp.current_period_start,
                         CurrentPeriodEnd = cpp.current_period_end,
                         CancelAtPeriodEnd = cpp.cancel_at_period_end,
                         CanceledAt = cpp.canceled_at,
                         MinutesUsed = cpp.minutes_used,
                         TotalPackageMinutes = totalMinutes,
                         MinutesUnused = Math.Max(0, totalMinutes - cpp.minutes_used)
                     })
                     .FirstOrDefaultAsync();
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Database failure in GetCompanyProfileAsync for CompanyId {CompanyId}",
                    companyId);

                throw;
            }
        }

        //public async Task<List<BillingHistoryRecordDto>> GetLatestRecordsAsync(int companyId)
        //{
        //    return await
        //        (from bh in _dbContext.company_bill_history

        //         join pp in _dbContext.package_plan
        //             on bh.package_id equals pp.plan_id into planGroup
        //         from pp in planGroup.DefaultIfEmpty()

        //         where bh.company_id == companyId

        //         orderby bh.created_at descending

        //         select new BillingHistoryRecordDto
        //         {
        //             PackageName = pp != null ? pp.plan_name : string.Empty,

        //             Amount = bh.amount,
        //             MinutesUsed = bh.minutes_used,

        //             // Safe calculation
        //             MinuteUnused = Math.Max(0,
        //                 (pp != null ? pp.est_minute : 0) - bh.minutes_used
        //             ),

        //             StartDate = bh.start_date,
        //             EndDate = bh.end_date,

        //             Interval = pp != null ? pp.interval.ToString() : string.Empty
        //         })
        //        .Take(10)
        //        .ToListAsync();
        //}

        public async Task<List<BillingHistoryRecordDto>> GetLatestRecordsAsync(int companyId)
        {
            return await
                (from bh in _dbContext.company_bill_history

                 join pp in _dbContext.package_plan
                     on bh.package_id equals pp.plan_id into planGroup
                 from pp in planGroup.DefaultIfEmpty()

                 where bh.company_id == companyId

                 orderby bh.end_date descending

                 select new BillingHistoryRecordDto
                 {
                     PackageName = pp != null ? pp.plan_name : bh.package_name,

                     Amount = bh.amount,
                     MinutesUsed = bh.minutes_used,

                     MinuteUnused = Math.Max(
                         0,
                         bh.minute_unused
                     ),

                     StartDate = bh.start_date,
                     EndDate = bh.end_date,

                     Interval = pp != null
                         ? pp.interval.ToString()
                         : string.Empty
                 })
                .Take(10)
                .ToListAsync();
        }

        public async Task<BillingAggregatesDto> GetLast12MonthsAggregatesAsync(int companyId)
        {
            var now = DateTime.UtcNow;
            var fromDate = now.AddMonths(-12);

            var query =
                from bh in _dbContext.company_bill_history

                join pp in _dbContext.package_plan
                    on bh.package_id equals pp.plan_id

                where bh.company_id == companyId
                      && pp.interval == PlanInterval.monthly
                      && bh.created_at >= fromDate
                      && bh.created_at <= now   // IMPORTANT guard

                select new
                {
                    MinutesUsed = (int?)bh.minutes_used,
                    Amount = (decimal?)bh.amount
                };

            var result = await query
                .GroupBy(_ => 1)
                .Select(g => new BillingAggregatesDto
                {
                    MinuteConsumed = g.Sum(x => x.MinutesUsed) ?? 0,
                    TotalBilled = g.Sum(x => x.Amount) ?? 0
                })
                .FirstOrDefaultAsync();

            return result ?? new BillingAggregatesDto
            {
                MinuteConsumed = 0,
                TotalBilled = 0
            };
        }

        //public async Task<BillingAggregatesDto> GetLast4RecordsAggregatesAsync(int companyId)
        //{
        //    var latestFourRecords = await
        //        (from bh in _dbContext.company_bill_history

        //         where bh.company_id == companyId

        //         orderby bh.created_at descending

        //         select new
        //         {
        //             MinutesUsed = (int?)bh.minutes_used,
        //             Amount = (decimal?)bh.amount
        //         })
        //        .Take(4)
        //        .ToListAsync();

        //    return new BillingAggregatesDto
        //    {
        //        MinuteConsumed = latestFourRecords.Sum(x => x.MinutesUsed ?? 0),
        //        TotalBilled = latestFourRecords.Sum(x => x.Amount ?? 0)
        //    };
        //}

        public async Task<BillingAggregatesDto> GetLast4RecordsAggregatesAsync(int companyId)
        {
            var latestFourRecords = await
                _dbContext.company_bill_history
                    .Where(x => x.company_id == companyId)
                    .OrderByDescending(x => x.end_date)
                    .Take(4)
                    .Select(x => new
                    {
                        MinutesUsed = x.minutes_used,
                        Amount = x.amount
                    })
                    .ToListAsync();

            return new BillingAggregatesDto
            {
                MinuteConsumed = latestFourRecords.Sum(x => x.MinutesUsed),
                TotalBilled = latestFourRecords.Sum(x => x.Amount)
            };
        }

        public async Task<UserPlanLimitDto?> GetUserPlanLimitAsync(int companyId)
        {
            var result = await
                (
                    from cpp in _dbContext.companies_payment_profile

                    join pp in _dbContext.package_plan
                        on cpp.subscription_plan equals pp.plan_id

                    where cpp.company_id == companyId

                    select new UserPlanLimitDto
                    {
                        AllowedUsers = pp.allowed_users,

                        ActiveUsers = _dbContext.users.Count(u =>
                            u.company_id == companyId &&
                            u.status == UserStatus.active)
                    }
                )
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<CompanyPlanFeaturesDto?> GetCompanyPlanFeaturesAsync(int companyId)
        {
            return await
                (
                    from cpp in _dbContext.companies_payment_profile

                    join pp in _dbContext.package_plan
                        on cpp.subscription_plan equals pp.plan_id

                    where cpp.company_id == companyId

                    select new CompanyPlanFeaturesDto
                    {
                        SubscriptionStatus = cpp.subscription_status,
                        MinutesUsed = cpp.minutes_used,
                        EstMinute = pp.est_minute,
                        IsTakeoverAllowed = pp.is_takeover_allowed,
                        PlanType = pp.plan_type
                    }
                )
                .FirstOrDefaultAsync();
        }

        public async Task<CompanyConcurrencyDto?> GetConcurrencyDataAsync(int companyId)
        {
            var result = await
                (
                    from cpp in _dbContext.companies_payment_profile

                    join pp in _dbContext.package_plan
                        on cpp.subscription_plan equals pp.plan_id

                    where cpp.company_id == companyId

                    select new CompanyConcurrencyDto
                    {
                        AllowedConcurrency = pp.concurrency,

                        ActiveCalls = _dbContext.call.Count(c =>
                            c.company_id == companyId &&
                            (
                                c.status_of_call == "initiated" ||
                                c.status_of_call == "ringing" ||
                                c.status_of_call == "in_progress"
                            ))
                    }
                )
                .FirstOrDefaultAsync();

            return result;
        }

    }
}
