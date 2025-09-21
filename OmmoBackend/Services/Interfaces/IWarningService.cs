using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IWarningService
    {
        Task<ServiceResponse<WarningCreationResult>> CreateWarningAsync(int companyId, CreateWarningRequest request);
        Task<ServiceResponse<WarningDetailsDto>> GetWarningDetailsAsync(int eventId, int companyId);
        Task<ServiceResponse<WarningUpdateResult>> UpdateWarningAsync(int companyId, UpdateWarningRequest request);
    }
}
