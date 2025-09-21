using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface ICallService
    {
        Task<ServiceResponse<List<CalledLoadDto>>> GetCalledLoadsAsync(int companyId);
    }
}
