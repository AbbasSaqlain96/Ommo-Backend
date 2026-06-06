using OmmoBackend.Data;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IGlobalIntegrationCredentialRepository
    {
        Task<GlobalIntegrationCredentials> GetByIntegrationIdAsync(int defaultIntegrationId);
        Task<GlobalIntegrationCredentials?> GetCredentialAsync(int integrationId, string key);
    }
}
