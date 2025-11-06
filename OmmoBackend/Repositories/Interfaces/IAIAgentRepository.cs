using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IAIAgentRepository
    {
        Task<Agent> RegisterAIAgentAsync(Agent agent);
        Task<AgentSettings?> GetAgentSettingsAsync(Guid agentGuid);

        Task<Guid?> GetAgentGuidByCompanyIdAsync(int companyId);
    }
}
