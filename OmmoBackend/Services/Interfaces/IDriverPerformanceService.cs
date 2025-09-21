using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IDriverPerformanceService
    {
        Task<ServiceResponse<DriverPerformanceDto>> GetDriverPerformanceAsync(int driverId, int companyId);
    }
}
