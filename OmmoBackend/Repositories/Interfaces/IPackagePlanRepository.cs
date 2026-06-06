using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IPackagePlanRepository
    {
        Task<PackagePlan> GetByIdAsync(int subscriptionPlan);
    }
}
