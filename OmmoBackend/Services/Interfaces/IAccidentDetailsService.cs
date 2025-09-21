using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IAccidentDetailsService
    {
        Task<ServiceResponse<AccidentDetailsResponse>> GetAccidentDetailsAsync(int eventId, int companyId);
    }
}
