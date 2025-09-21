using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IDotInspectionService
    {
        Task<ServiceResponse<DotInspectionCreationResult>> CreateDotInspectionAsync(int companyId, CreateDotInspectionRequest request);
        Task<ServiceResponse<DotInspectionDetailsDto>> GetDotInspectionDetailsAsync(int eventId, int companyId);
        Task<ServiceResponse<DotInspectionUpdateResult>> UpdateDotInspectionAsync(int companyId, UpdateDotInspectionRequest request);
    }
}
