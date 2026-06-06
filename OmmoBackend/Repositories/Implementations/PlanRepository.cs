using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class PlanRepository : IPlanRepository
    {
        private readonly AppDbContext _dbContext;
        public PlanRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<PlanDto>> GetPlansAsync(int companyId)
        {
            return await _dbContext.package_plan
                .Where(p =>
                    p.plan_status == PlanStatus.active &&
                    (
                        p.plan_type == PlanType.standard ||
                        (p.plan_type == PlanType.custom && p.company_id == companyId)
                    ))
                .OrderBy(p => p.price)
                .Select(p => new PlanDto
                {
                    PlanId = p.plan_id,
                    PlanType = p.plan_type.ToString(),
                    PlanName = p.plan_name,
                    Concurrency = p.concurrency,
                    Interval = p.interval.ToString(),
                    Price = p.price,
                    AllowedUsers = p.allowed_users,
                    EstMinute = p.est_minute, 
                    PlanStatus = p.plan_status.ToString(),
                    IsTranscriptAllowed = p.is_transcript_allowed,
                    IsTakeoverAllowed = p.is_takeover_allowed
                })
                .ToListAsync();
        }
    }
}
