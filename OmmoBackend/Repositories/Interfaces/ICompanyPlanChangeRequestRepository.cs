using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ICompanyPlanChangeRequestRepository
    {
        Task<CompanyPlanChangeRequest?> GetPendingOrScheduledAsync(int companyId);

        Task UpdateAsync(CompanyPlanChangeRequest request);
    }

    public class CompanyPlanChangeRequestRepository : ICompanyPlanChangeRequestRepository
    {
        private readonly AppDbContext _db;

        public CompanyPlanChangeRequestRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<CompanyPlanChangeRequest?> GetPendingOrScheduledAsync(int companyId)
        {
            return await _db.company_plan_change_request
                .Where(x =>
                    x.company_id == companyId &&
                    (x.status == PlanChangeRequestStatus.pending ||
                     x.status == PlanChangeRequestStatus.scheduled))
                .OrderByDescending(x => x.created_at)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(CompanyPlanChangeRequest request)
        {
            var existing = await _db.company_plan_change_request
                .FirstOrDefaultAsync(x =>
                    x.company_plan_change_request_id ==
                    request.company_plan_change_request_id);

            if (existing == null)
                throw new InvalidOperationException("Plan change request not found.");

            existing.status = request.status;
            existing.updated_at = DateTimeOffset.UtcNow;

            existing.request_plan = request.request_plan;
            existing.current_cycle_end_date = request.current_cycle_end_date;

            _db.company_plan_change_request.Update(existing);
        }
    }
}
