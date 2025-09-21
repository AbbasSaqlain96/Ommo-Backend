using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class IntegrationRepository : IIntegrationRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<IntegrationRepository> _logger;
        public IntegrationRepository(AppDbContext dbContext, ILogger<IntegrationRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<IntegrationDto>> GetIntegrationsByCompanyAsync(int companyId)
        {
            _logger.LogInformation("Fetching Integration");

            return await (from i in _dbContext.integrations
                          join d in _dbContext.default_integrations
                              on i.default_integration_id equals d.default_integration_id
                          where i.company_id == companyId
                          select new IntegrationDto
                          {
                              IntegrationId = i.integration_id,
                              DefaultIntegrationId = i.default_integration_id,
                              IntegrationStatus = i.integration_status,
                              LastUpdated = i.last_updated,
                              IntegrationName = d.integration_name,
                              IntegrationDescription = d.integration_description,
                              LogoPath = d.logo_path ?? ""
                          }).ToListAsync();
        }

        public async Task<List<DefaultIntegrationDto>> GetDefaultIntegrationsAsync(int companyId)
        {
            _logger.LogInformation("Fetching Default Integration");

            var query = from d in _dbContext.default_integrations
                        join i in _dbContext.integrations
                            .Where(x => x.company_id == companyId)
                            on d.default_integration_id equals i.default_integration_id into di
                        from i in di.DefaultIfEmpty()
                        select new DefaultIntegrationDto
                        {
                            DefaultIntegrationId = d.default_integration_id,
                            IntegrationName = d.integration_name,
                            IntegrationCat = d.integration_cat,
                            IntegrationDescription = d.integration_description,
                            LogoPath = d.logo_path ?? null,
                            IntegrationStatus = i.integration_status
                        };

            return await query.ToListAsync();
        }

        public async Task<List<Integrations>> GetActiveIntegrationsAsync(int companyId)
        {
            return await _dbContext.integrations
                .AsQueryable()
                .Where(i => i.company_id == companyId && i.integration_status.ToLower() == "active")
                .ToListAsync();
        }

        public async Task<(DefaultIntegrations Global, Integrations Company)> GetIntegrationWithCredentialsAsync(int companyId, string provider)
        {
            var query = from di in _dbContext.default_integrations
                        join i in _dbContext.integrations
                            on di.default_integration_id equals i.default_integration_id
                        where i.company_id == companyId && di.integration_name == provider && i.integration_status == "active"
                        select new 
                        {
                            Global = new DefaultIntegrations
                            {
                                default_integration_id = di.default_integration_id,
                                integration_name = di.integration_name,
                                logo_path = di.logo_path ?? string.Empty
                            },
                            Company = i
                        };

            var result = await query.FirstOrDefaultAsync();

            if (result == null)
                return (null, null);

            return (result.Global, result.Company);
        }

        public async Task<string> GetGlobalCredentialAsync(int defaultIntegrationId, string credentialName)
        {
            var cred = await _dbContext.global_integration_credentials
                .Where(c => c.default_integration_id == defaultIntegrationId && c.credential_name == credentialName)
                .FirstOrDefaultAsync();

            return cred?.credential_value;
        }

        public async Task AddIntegrationAsync(Integrations integration)
        {
            _dbContext.integrations.Add(integration);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Integrations?> GetByCompanyAndLoadboardAsync(int companyId, int loadboardId)
        {
            return await _dbContext.integrations
                .FirstOrDefaultAsync(i => i.company_id == companyId && i.default_integration_id == loadboardId);
        }

        public async Task MarkEmailProcessedAsync(string messageId)
        {
            _dbContext.integration_email_process.Add(new IntegrationEmailProcess { message_id = messageId, processed_at = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync();
        }

        public Task<bool> IsMessageProcessedAsync(string messageId)
        {
            return _dbContext.integration_email_process.AnyAsync(p => p.message_id == messageId);
        }

        public async Task EnqueueSendEmailAsync(SendEmail email)
        {
            _dbContext.send_email.Add(email);
            await _dbContext.SaveChangesAsync();
        }
    }
}
