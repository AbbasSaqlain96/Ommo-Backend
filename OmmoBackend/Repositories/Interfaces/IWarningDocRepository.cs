using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IWarningDocRepository : IGenericRepository<WarningDocument>
    {
        Task UpdateWarningDocsAsync(int companyId, int eventId, int warningId, IFormFile warningDoc, string docNumber);
    }
}
