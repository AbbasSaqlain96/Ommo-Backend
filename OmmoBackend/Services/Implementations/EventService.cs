using Microsoft.Extensions.Logging;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IDriverRepository _driverRepository;
        private readonly ITruckRepository _truckRepository;
        private readonly ITrailerRepository _trailerRepository;
        private readonly ILogger<EventService> _logger;
        public EventService(
            IEventRepository eventRepository, 
            IDriverRepository driverRepository, 
            ITruckRepository truckRepository, 
            ITrailerRepository trailerRepository,
            ILogger<EventService> logger)
        {                                                     
            _eventRepository = eventRepository;
            _driverRepository = driverRepository;
            _truckRepository = truckRepository;
            _trailerRepository = trailerRepository;
            _logger = logger;
        }

        public async Task<List<object>> GetEventsListAsync(int companyId, int? driverId, int? truckId, DateTime? startDate, DateTime? endDate, string eventType)
        {
            _logger.LogInformation("Fetching events list for CompanyId: {CompanyId}, DriverId: {DriverId}, TruckId: {TruckId}, StartDate: {StartDate}, EndDate: {EndDate}, EventType: {EventType}", companyId, driverId, truckId, startDate, endDate, eventType);

            try
            {
                var events = await _eventRepository.FetchEventsListAsync(companyId, driverId, truckId, startDate, endDate, eventType);
                _logger.LogInformation("Successfully fetched {EventCount} events for CompanyId: {CompanyId}", events.Count, companyId);
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the events list for CompanyId: {CompanyId}", companyId);
                throw new Exception("An error occured while fetching the events list");
            }
        }

        //public async Task<IEnumerable<EventDto>> GetEventsAsync(int? driverId, int companyId, int? truckId, DateTime? startDate, DateTime? endDate, string? eventType)
        //{
        //    try
        //    {
        //        return await _eventRepository.GetEventsAsync(driverId, companyId, truckId, startDate, endDate, eventType);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("An error occured while fetching the events list");
        //    }
        //}

        public async Task<ServiceResponse<IEnumerable<EventDto>>> GetEventsAsync(int? driverId, int companyId, int? truckId, int? trailerId, int? loadId, string? authority, DateTime? startDate, DateTime? endDate, string? eventType)
        {
            _logger.LogInformation("Fetching events for CompanyId: {CompanyId}, DriverId: {DriverId}, TruckId: {TruckId}, StartDate: {StartDate}, EndDate: {EndDate}, EventType: {EventType}", companyId, driverId, truckId, startDate, endDate, eventType);

            try
            {
                // Validate eventType
                if (!string.IsNullOrEmpty(eventType) && !Enum.TryParse<EventType>(eventType, true, out _))
                {
                    _logger.LogWarning("Invalid event type provided: {EventType}", eventType);
                    return ServiceResponse<IEnumerable<EventDto>>.ErrorResponse("Invalid event type provided. Please provide a valid event type.", 400);
                }

                if (driverId.HasValue && !await _driverRepository.IsValidDriverIdAsync(driverId.Value, companyId))
                    return ServiceResponse<IEnumerable<EventDto>>.ErrorResponse("No driver found for the provided Driver ID", 400);

                if (truckId.HasValue && !await _truckRepository.IsValidTruckIdAsync(truckId.Value, companyId))
                    return ServiceResponse<IEnumerable<EventDto>>.ErrorResponse("No truck found for the provided Truck ID", 400);

                if (trailerId.HasValue && !await _trailerRepository.IsValidTrailerIdAsync(trailerId.Value, companyId))
                    return ServiceResponse<IEnumerable<EventDto>>.ErrorResponse("Trailer ID does not exist or belong to another company", 400);

                var response = await _eventRepository.GetEventsAsync(driverId, companyId, truckId, trailerId, loadId, authority, startDate, endDate, eventType);

                _logger.LogInformation("Successfully fetched events for CompanyId: {CompanyId} with {EventCount} results", companyId, response.Data?.Count() ?? 0);
                return ServiceResponse<IEnumerable<EventDto>>.SuccessResponse(response.Data, "Events retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while fetching events for CompanyId: {CompanyId}", companyId);
                return ServiceResponse<IEnumerable<EventDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}
