using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IIntegrationService
    {
        Task<ServiceResponse<List<IntegrationDto>>> GetIntegrationsAsync(int companyId);
        Task<ServiceResponse<List<DefaultIntegrationDto>>> GetDefaultIntegrationsAsync(int companyId);
        Task<ServiceResponse<object>> SendIntegrationRequestAsync(int userId, int companyId, IntegrationRequestDto request);
    }
}
