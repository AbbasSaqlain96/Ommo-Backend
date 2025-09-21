using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IEventService
    {
        Task<List<object>> GetEventsListAsync(
            int companyId, int? driverId, int? truckId, DateTime? startDate, DateTime? endDate, string eventType);

        Task<ServiceResponse<IEnumerable<EventDto>>> GetEventsAsync(
            int? driverId, int companyId, int? truckId, int? trailerId, int? loadId, string? authority, DateTime? startDate, DateTime? endDate, string? eventType);
    }
}
