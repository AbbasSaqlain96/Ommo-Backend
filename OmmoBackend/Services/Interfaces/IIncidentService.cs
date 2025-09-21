using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IIncidentService
    {
        Task<ServiceResponse<IncidentDetailsDto>> GetIncidentDetailsAsync(int eventId, int companyId);
        //Task<int> CreateIncidentAsync(CreateIncidentRequest request, int companyId);
        Task<ServiceResponse<IncidentCreationResult>> CreateIncidentAsync(int companyId, CreateIncidentRequest request);
        Task<ServiceResponse<IncidentUpdateResult>> UpdateIncidentAsync(int companyId, UpdateIncidentRequest request);
    }
}
