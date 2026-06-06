namespace OmmoBackend.Services.Interfaces
{
    public interface IUltravoxService
    {
        //Task<AgentAIConfig> CreateLoadBoardAgentAsync(string companyName);

        Task<Guid> CreateAgentAsync(int companyId);
    }
}
