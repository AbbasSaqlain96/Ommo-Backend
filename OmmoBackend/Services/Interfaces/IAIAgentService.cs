using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IAIAgentService
    {
        //Task<ServiceResponse<RegisterAIAgentResult>> RegisterAIAgentAsync(RegisterAIAgentRequest request);
        Task<ServiceResponse<AgentSettingsResponse>> GetAgentSettingsAsync(int companyId);
        Task<ServiceResponse<string>> UpdateAgentSettingsAsync(UpdateAgentSettingsRequest request, int companyId);
        Task<ServiceResponse<string>> AddAgentAsync(Guid agentGuid, int companyId);
    }
}
