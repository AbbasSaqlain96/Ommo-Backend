using Microsoft.EntityFrameworkCore;
using OmmoBackend.Models;
using Twilio.Http;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IViolationRepository : IGenericRepository<Violation>
    {
        Task CreateViolationsAsync(int ticketId, List<int> violations);
        Task<List<Violation>> GetByIdsAsync(List<int> violationIds);
        //Task UpdateViolationsAsync(int ticketId, List<int> violations);
        Task UpdateTicketViolationsAsync(int ticketId, List<int?>? newViolationIds);
        Task UpdateDotInspectionViolationsAsync(int eventId, List<int?>? newViolationIds);
        Task UpdateWarningViolationsAsync(int eventId, List<int?>? newViolationIds);
    }
}
