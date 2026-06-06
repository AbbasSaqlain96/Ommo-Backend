namespace OmmoBackend.Repositories.Interfaces
{
    public interface IDefaultIntegrationRepository
    {
        Task<string?> GetLogoPathByIntegrationIdAsync(int defaultIntegrationId);
    }
}
