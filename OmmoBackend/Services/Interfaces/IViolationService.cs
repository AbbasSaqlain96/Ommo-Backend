using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IViolationService
    {
        Task<ServiceResponse<List<ViolationDto>>> GetViolationsAsync();
    }
}
