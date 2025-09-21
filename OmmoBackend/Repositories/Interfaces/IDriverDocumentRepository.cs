using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IDriverDocumentRepository : IGenericRepository<DriverDoc>
    {
        Task<bool> CheckDriverCompany(int driverId, int companyId);
        Task<List<DriverDocumentDto>> GetDriverDocumentsAsync(int driverId);

    }
}
