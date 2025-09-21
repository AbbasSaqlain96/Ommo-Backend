using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using System.ComponentModel.Design;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ITicketRepository : IGenericRepository<UnitTicket>
    {
        Task<bool> EventBelongsToCompany(int eventId, int companyId);
        Task<TicketDetails> GetTicketDetailsAsync(int eventId, int companyId);
        string SaveTicketDocument(int companyId, int driverId, string base64EncodedDocument);
        //Task<ServiceResponse<int>> CreateTicketAsync(CreateTicketRequest request);
        Task<int> CreateTicketAsync(int eventId, TicketInfoDto ticketInfo, string documentPath);

        Task<bool> CreateTicketWithTransactionAsync(
            PerformanceEvents performanceEvents,
            TicketInfoDto ticketInfo,
            TicketInfoDocumentDto documentInfo,
            List<int> violations,
            TicketImageDto imageDto,
            int companyId);

        Task<(PerformanceEvents Event, UnitTicket Ticket)> GetTicketByEventIdAsync(int eventId);

        Task<bool> DoesTicketBelongToCompanyAsync(int ticketId, int companyId);

        Task<bool> TicketDocumentExist(int ticketId);
    }
}
