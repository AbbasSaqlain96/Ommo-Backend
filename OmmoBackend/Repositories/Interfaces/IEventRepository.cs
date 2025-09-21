using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using System.Reflection;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IEventRepository : IGenericRepository<PerformanceEvents>
    {
        Task<List<object>> FetchEventsListAsync(int companyId, int? driverId, int? truckId, DateTime? startDate, DateTime? endDate, string eventType);
        //Task<ServiceResponse<IEnumerable<EventDto>>> GetEventsAsync(int? driverId, int companyId, int? truckId, DateTime? startDate, DateTime? endDate, string? eventType);
        Task<ServiceResponse<IEnumerable<EventDto>>> GetEventsAsync(int? driverId, int companyId, int? truckId, int? trailerId, int? loadId, string? authority, DateTime? startDate, DateTime? endDate, string? eventType);
        Task<int> CreateEventAsync(PerformanceEvents performanceEvents);
        Task<int> CreateEventAsync(int companyId, PerformanceEvents performanceEvents);
    }
}
