using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IIncidentDocRepository : IGenericRepository<IncidentDoc>
    {
        Task UpdateDocsAsync(int eventId, int incidentId, int driverId, List<UpdateDocumentRequest> newDocs);
    }
}
