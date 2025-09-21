using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IPerformanceService
    {
        Task<ServiceResponse<PerformanceDto>> GetPerformanceAsync(int companyId);
    }
}
