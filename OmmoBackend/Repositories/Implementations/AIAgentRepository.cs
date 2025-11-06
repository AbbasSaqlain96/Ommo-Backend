using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class AIAgentRepository : IAIAgentRepository
    {
        private readonly AppDbContext _dbContext;

        public AIAgentRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Agent> RegisterAIAgentAsync(Agent agent)
        {
            _dbContext.agent.Add(agent);
            await _dbContext.SaveChangesAsync();
            return agent;
        }
        public async Task<AgentSettings?> GetAgentSettingsAsync(Guid agentGuid)
        {
            // Note: property name matches what you added: agent_settings
            return await _dbContext.agent_settings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.AgentGuid == agentGuid);
        }

        public async Task<Guid?> GetAgentGuidByCompanyIdAsync(int companyId)
        {
            return await _dbContext.agent
                .AsNoTracking()
                .Where(a => a.company_id == companyId)
                .Select(a => (Guid?)a.agent_guid)
                .FirstOrDefaultAsync();
        }
    }
}
