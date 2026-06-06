using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface ISupportService
    {
        Task<ServiceResponse<object>> CreateSupportRequestAsync(SupportRequestDto request);
    }
}
