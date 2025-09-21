using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IDocInspectionRepository : IGenericRepository<DocInspection>
    {
        Task<DocInspection> GetDocInspectionByEventIdAsync(int eventId);
        Task UpdateDotInspectionDocsAsync(int companyId, int eventId, int docInspectionId, IFormFile docInspectionDoc, string docNumber);
        Task<bool> DocInspectionDocumentExist(int docInspectionId);
    }
}
