using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IAIAgentSettingRepository
    {
        Task AddAgentSettingAsync(AgentSettings agentSettings);
    }
}
