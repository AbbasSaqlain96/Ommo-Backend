using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class DefaultIntegrationRepository : IDefaultIntegrationRepository
    {
        private readonly AppDbContext _context;

        public DefaultIntegrationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string?> GetLogoPathByIntegrationIdAsync(int defaultIntegrationId)
        {
            return await _context.default_integrations
                .Where(x => x.default_integration_id == defaultIntegrationId)
                .Select(x => x.logo_path)
                .FirstOrDefaultAsync();
        }
    }
}
