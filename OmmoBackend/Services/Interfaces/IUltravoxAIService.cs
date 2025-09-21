using OmmoBackend.Dtos;

namespace OmmoBackend.Services.Interfaces
{
    public interface IUltravoxAIService
    {
        Task<AgentAIConfig> CreateLoadBoardAgentAsync(string companyName);
    }
}
