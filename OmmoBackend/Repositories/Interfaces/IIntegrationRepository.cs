using Microsoft.EntityFrameworkCore;
using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IIntegrationRepository
    {
        Task<List<IntegrationDto>> GetIntegrationsByCompanyAsync(int companyId);
        Task<List<DefaultIntegrationDto>> GetDefaultIntegrationsAsync(int companyId);
        Task<List<Integrations>> GetActiveIntegrationsAsync(int companyId);
        Task<(DefaultIntegrations Global, Integrations Company)> GetIntegrationWithCredentialsAsync(int companyId, string provider);
        Task<string> GetGlobalCredentialAsync(int defaultIntegrationId, string credentialName);
        Task AddIntegrationAsync(Integrations integration);
        Task<Integrations?> GetByCompanyAndLoadboardAsync(int companyId, int loadboardId);
        Task MarkEmailProcessedAsync(string messageId);
        Task<bool> IsMessageProcessedAsync(string messageId);
        Task EnqueueSendEmailAsync(SendEmail email);
        Task<Integrations> GetByIdAsync(int id);
        Task UpdateAsync(Integrations integration);
    }
}
