using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class AIAgentSettingRepository : IAIAgentSettingRepository
    {
        private readonly AppDbContext _dbContext;
        public AIAgentSettingRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAgentSettingAsync(AgentSettings agentSettings) 
        {
            await _dbContext.agent_settings.AddAsync(agentSettings);
            await _dbContext.SaveChangesAsync();
        }
    }
}
