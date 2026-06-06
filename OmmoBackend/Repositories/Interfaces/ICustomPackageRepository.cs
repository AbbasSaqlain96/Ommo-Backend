using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ICustomPackageRepository
    {
        Task<bool> HasPendingRequestAsync(int companyId);
        Task InsertAsync(CustomPackageRequest entity);
    }
}
