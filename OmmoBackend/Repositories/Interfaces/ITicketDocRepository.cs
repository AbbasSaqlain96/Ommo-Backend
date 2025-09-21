using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ITicketDocRepository : IGenericRepository<TicketDoc>
    {
        Task<int> GetLastTicketDocIdAsync();
        //Task UpdateDocsAsync(int companyId, int ticketId, int eventId, List<UpdateDocumentRequest> newDocs);

        Task UpdateDocsAsync(int companyId, int ticketId, int eventId, string docNumber, IFormFile document);
    }
}
