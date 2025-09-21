using OmmoBackend.Data;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IGlobalIntegrationCredentialRepository
    {
        Task<GlobalIntegrationCredentials> GetByIntegrationIdAsync(int defaultIntegrationId);
    }
}
