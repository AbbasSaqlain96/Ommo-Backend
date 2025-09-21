using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface ITicketService
    {
        Task<ServiceResponse<TicketDetailResponse>> GetTicketDetailsAsync(int eventId, int companyId);
        Task<ServiceResponse<TicketCreationResult>> CreateTicketAsync(int companyId, CreateTicketRequest ticketDto);
        Task<ServiceResponse<TicketUpdateResult>> UpdateTicketAsync(int companyId, UpdateTicketRequest request);
    }
}