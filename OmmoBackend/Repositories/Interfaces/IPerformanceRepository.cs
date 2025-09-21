using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IPerformanceRepository : IGenericRepository<PerformanceEvents>
    {
        Task<bool> CheckPerformanceCompany(int companyId);
        Task<PerformanceDto> GetPerformanceAsync(int companyId);
    }
}
