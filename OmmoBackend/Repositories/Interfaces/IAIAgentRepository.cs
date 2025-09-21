using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IAIAgentRepository
    {
        Task<Agent> RegisterAIAgentAsync(Agent agent);
    }
}
