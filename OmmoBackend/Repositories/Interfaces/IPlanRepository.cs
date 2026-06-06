using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IPlanRepository
    {
        Task<List<PlanDto>> GetPlansAsync(int companyId);
    }
}
