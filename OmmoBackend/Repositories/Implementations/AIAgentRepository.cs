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

        //public async Task<Agent> RegisterAIAgentAsync(Agent agent)
        //{
        //    _dbContext.agent.Add(agent);
        //    await _dbContext.SaveChangesAsync();
        //    return agent;
        //}
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

        public async Task<(Agent, AgentSettings)> GetAgentWithSettingsByCompanyIdAsync(int companyId)
        {
            var agent = await _dbContext.agent.FirstOrDefaultAsync(x => x.company_id == companyId);
                //.FindAsync(companyId);

            if (agent == null)
                return (null, null);

            var settings = await _dbContext.agent_settings.FindAsync(agent.agent_guid);

            return (agent, settings);
        }

        public async Task<Agent?> GetAgentByGuidAsync(Guid agentGuid)
        {
            return await _dbContext.agent.FindAsync(agentGuid);
                //.FirstOrDefaultAsync(a => a.agent_guid == agentGuid);
        }

        public async Task<AgentSettings?> GetByAgentGuidAsync(Guid agentGuid)
        {
            return await _dbContext.agent_settings.FindAsync(agentGuid);
        }

        public async Task UpdateAsync(AgentSettings settings)
        {
            _dbContext.agent_settings.Update(settings);
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddAgentAsync(Agent agent)
        {
            await _dbContext.agent.AddAsync(agent);
            await _dbContext.SaveChangesAsync();
        }
    }
}
