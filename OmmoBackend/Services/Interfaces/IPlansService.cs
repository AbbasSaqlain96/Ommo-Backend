using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IPlansService
    {
        Task<ServiceResponse<string>> RequestCustomPackageAsync(
            int companyId,
            CustomPackageRequestDto request);

        Task<ServiceResponse<List<PlanDto>>> GetPlansAsync(int companyId);
    }
}
