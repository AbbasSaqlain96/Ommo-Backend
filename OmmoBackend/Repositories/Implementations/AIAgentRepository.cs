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
    }
}
