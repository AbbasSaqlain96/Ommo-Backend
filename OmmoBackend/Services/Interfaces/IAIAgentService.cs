using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IAIAgentService
    {
        Task<ServiceResponse<RegisterAIAgentResult>> RegisterAIAgentAsync(RegisterAIAgentRequest request);
    }
}
