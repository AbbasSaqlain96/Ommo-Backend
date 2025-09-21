using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class GlobalIntegrationCredentialRepository : IGlobalIntegrationCredentialRepository
    {
        private readonly AppDbContext _dbContext;
        public GlobalIntegrationCredentialRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<GlobalIntegrationCredentials> GetByIntegrationIdAsync(int defaultIntegrationId)
        {
            return await _dbContext.global_integration_credentials
                            .FirstOrDefaultAsync(g => g.default_integration_id == defaultIntegrationId);
        }
    }
}
