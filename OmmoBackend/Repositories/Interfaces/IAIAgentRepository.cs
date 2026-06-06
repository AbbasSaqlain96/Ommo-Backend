using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IAIAgentRepository
    {
        //Task<Agent> RegisterAIAgentAsync(Agent agent);
        Task<AgentSettings?> GetAgentSettingsAsync(Guid agentGuid);

        Task<Guid?> GetAgentGuidByCompanyIdAsync(int companyId);

        Task<(Agent, AgentSettings)> GetAgentWithSettingsByCompanyIdAsync(int companyId);

        Task<Agent?> GetAgentByGuidAsync(Guid agentGuid);

        Task<AgentSettings?> GetByAgentGuidAsync(Guid agentGuid);
        Task UpdateAsync(AgentSettings settings);
        Task AddAgentAsync(Agent agent);
    }
}
