using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class PackagePlanRepository : IPackagePlanRepository
    {
        private readonly AppDbContext _dbContext;
        public PackagePlanRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PackagePlan?> GetByIdAsync(int subscriptionPlan)
        {
            return await _dbContext.package_plan.FindAsync(subscriptionPlan);
        }
    }
}
