using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IAIAgentSettingService
    {
        Task<ServiceResponse<string>> AddAgentSettingAsync(Guid agentGuid);
    }
}
