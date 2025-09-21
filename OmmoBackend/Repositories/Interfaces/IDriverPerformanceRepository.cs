using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IDriverPerformanceRepository : IGenericRepository<PerformanceEvents>
    {
        Task<bool> CheckDriverCompany(int driverId, int companyId);
        Task<DriverPerformanceDto> GetDriverPerformanceAsync(int driverId, int companyId);
    }
}
