using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using System.Reflection;

namespace OmmoBackend.Repositories.Implementations
{
    public class EventRepository : GenericRepository<PerformanceEvents>, IEventRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<EventRepository> _logger;
        public EventRepository(AppDbContext dbContext, ILogger<EventRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<object>> FetchEventsListAsync(int companyId, int? driverId, int? truckId, DateTime? startDate, DateTime? endDate, string eventType)
        {
            try
            {
                _logger.LogInformation("Fetching events for CompanyId: {CompanyId}, DriverId: {DriverId}, TruckId: {TruckId}, StartDate: {StartDate}, EndDate: {EndDate}, EventType: {EventType}", companyId, driverId, truckId, startDate, endDate, eventType);

                var query = from e in _dbContext.performance_event
                            join t in _dbContext.truck on e.truck_id equals t.truck_id
                            join u in _dbContext.unit on t.unit_id equals u.unit_id
                            where u.carrier_id == companyId
                            select e;

                if (driverId.HasValue)
                {
                    query = query.Where(e => e.driver_id == driverId.Value);
                    _logger.LogDebug("Filtering by DriverId: {DriverId}", driverId.Value);
                }

                if (truckId.HasValue)
                {
                    query = query.Where(e => e.truck_id == truckId.Value);
                    _logger.LogDebug("Filtering by TruckId: {TruckId}", truckId.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(e => e.date >= startDate.Value);
                    _logger.LogDebug("Filtering by StartDate: {StartDate}", startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(e => e.date <= endDate.Value);
                    _logger.LogDebug("Filtering by EndDate: {EndDate}", endDate.Value);
                }

                if (!string.IsNullOrEmpty(eventType))
                {
                    query = query.Where(e => e.event_type.ToString() == eventType);
                    _logger.LogDebug("Filtering by EventType: {EventType}", eventType);
                }
                var result = await query
                   .Select(e => new
                   {
                       e.event_id,
                       e.event_type,
                       e.driver_id,
                       e.truck_id,
                       e.date
                   })
                   .ToListAsync<object>();

                _logger.LogInformation("Fetched {EventCount} events for CompanyId: {CompanyId}", result.Count, companyId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching events for CompanyId: {CompanyId}", companyId);
                throw;
            }
        }

        //public async Task<ServiceResponse<IEnumerable<EventDto>>> GetEventsAsync(int? driverId, int companyId, int? truckId, int? trailerId, int? loadId, string? authority, DateTime? startDate, DateTime? endDate, string? eventType)

        //{
        //    try
        //    {
        //        _logger.LogInformation("Fetching events for CompanyId: {CompanyId}, DriverId: {DriverId}, TruckId: {TruckId}, StartDate: {StartDate}, EndDate: {EndDate}, EventType: {EventType}", companyId, driverId, truckId, startDate, endDate, eventType);

        //        // Validate driver
        //        if (driverId.HasValue)
        //        {
        //            bool isDriverValid = await _dbContext.driver
        //                .AnyAsync(d => d.driver_id == driverId.Value && d.company_id == companyId);

        //            if (!isDriverValid)
        //            {
        //                _logger.LogWarning("Validation failed: Driver {DriverId} does not belong to CompanyId {CompanyId}", driverId.Value, companyId);
        //                return ServiceResponse<IEnumerable<EventDto>>.ErrorResponse("Driver does not belong to the specified company.");
        //            }
        //        }

        //        // Validate truck
        //        if (truckId.HasValue)
        //        {
        //            bool isTruckValid = await (
        //                from t in _dbContext.truck
        //                join v in _dbContext.vehicle on t.vehicle_id equals v.vehicle_id
        //                join c in _dbContext.carrier on v.carrier_id equals c.carrier_id
        //                join comp in _dbContext.company on c.company_id equals comp.company_id
        //                where t.truck_id == truckId.Value && comp.company_id == companyId
        //                select t
        //            ).AnyAsync();

        //            if (!isTruckValid)
        //            {
        //                _logger.LogWarning("Validation failed: Truck {TruckId} does not belong to CompanyId {CompanyId}", truckId.Value, companyId);
        //                return ServiceResponse<IEnumerable<EventDto>>.ErrorResponse("Truck does not belong to the specified company.");
        //            }
        //        }

        //        var query = from e in _dbContext.performance_event
        //                    join t in _dbContext.truck on e.truck_id equals t.truck_id
        //                    join v in _dbContext.vehicle on t.vehicle_id equals v.vehicle_id
        //                    join c in _dbContext.carrier on v.carrier_id equals c.carrier_id
        //                    join comp in _dbContext.company on c.company_id equals comp.company_id
        //                    join d in _dbContext.driver on e.driver_id equals d.driver_id
        //                    where comp.company_id == companyId
        //                    select new
        //                    {
        //                        e.event_id,
        //                        e.event_type,
        //                        e.driver_id,
        //                        e.truck_id,
        //                        e.date,
        //                        DriverName = d.driver_name
        //                    };

        //        if (driverId.HasValue)
        //        {
        //            query = query.Where(e => e.driver_id == driverId.Value);
        //            _logger.LogDebug("Filtering by DriverId: {DriverId}", driverId.Value);
        //        }

        //        if (truckId.HasValue)
        //        {
        //            query = query.Where(e => e.truck_id == truckId.Value);
        //            _logger.LogDebug("Filtering by TruckId: {TruckId}", truckId.Value);
        //        }

        //        if (startDate.HasValue)
        //        {
        //            query = query.Where(e => e.date >= startDate.Value);
        //            _logger.LogDebug("Filtering by StartDate: {StartDate}", startDate.Value);
        //        }

        //        if (endDate.HasValue)
        //        {
        //            query = query.Where(e => e.date <= endDate.Value);
        //            _logger.LogDebug("Filtering by EndDate: {EndDate}", endDate.Value);
        //        }

        //        if (!string.IsNullOrEmpty(eventType))
        //        {
        //            query = query.Where(e => e.event_type.ToString() == eventType);
        //            _logger.LogDebug("Filtering by EventType: {EventType}", eventType);
        //        }

        //        var result = await query
        //            .Select(e => new EventDto
        //            {
        //                EventId = e.event_id,
        //                EventType = e.event_type.ToString(),
        //                DriverId = e.driver_id,
        //                DriverName = e.DriverName,
        //                TruckId = e.truck_id,
        //                Date = e.date
        //            })
        //            .ToListAsync();

        //        _logger.LogInformation("Fetched {EventCount} events for CompanyId: {CompanyId}", result.Count, companyId);
        //        return ServiceResponse<IEnumerable<EventDto>>.SuccessResponse(result, "Events retrieved successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error occurred while fetching events for CompanyId: {CompanyId}", companyId);
        //        return ServiceResponse<IEnumerable<EventDto>>.ErrorResponse("An error occurred while fetching the events list.");
        //    }
        //}

        public async Task<ServiceResponse<IEnumerable<EventDto>>> GetEventsAsync(
            int? driverId, int companyId, int? truckId, int? trailerId, int? loadId,
            string? authority, DateTime? startDate, DateTime? endDate, string? eventType)
        {
            try
            {
                var query = from e in _dbContext.performance_event
                            join d in _dbContext.driver on e.driver_id equals d.driver_id
                            join t in _dbContext.truck on e.truck_id equals t.truck_id
                            join v in _dbContext.vehicle on t.vehicle_id equals v.vehicle_id
                            join c in _dbContext.carrier on v.carrier_id equals c.carrier_id
                            join comp in _dbContext.company on c.company_id equals comp.company_id
                            where e.company_id == companyId
                            select new EventDto
                            {
                                EventId = e.event_id,
                                EventType = e.event_type.ToString(),
                                DriverId = e.driver_id,
                                DriverName = d.driver_name,
                                TruckId = e.truck_id,
                                TrailerId = e.trailer_id,
                                Location = e.location,
                                Authority = e.authority.ToString(),
                                EventDate = e.date,
                                Description = e.description,
                                LoadId = e.load_id,
                                EventFee = e.event_fees,
                                FeesPaidBy = e.fees_paid_by.ToString(),
                                CompanyFeeApplied = e.company_fee_applied.ToString(),
                                CompanyFeeAmount = e.company_fee_amount,
                                CompanyFeeStatementDate = e.company_fee_statement_date
                            };

                if (driverId.HasValue) query = query.Where(e => e.DriverId == driverId);
                if (truckId.HasValue) query = query.Where(e => e.TruckId == truckId);
                if (trailerId.HasValue) query = query.Where(e => e.TrailerId == trailerId);
                if (loadId.HasValue) query = query.Where(e => e.LoadId == loadId);
                if (!string.IsNullOrEmpty(authority)) query = query.Where(e => e.Authority == authority);
                if (!string.IsNullOrEmpty(eventType)) query = query.Where(e => e.EventType == eventType);
                if (startDate.HasValue) query = query.Where(e => e.EventDate >= startDate);
                if (endDate.HasValue) query = query.Where(e => e.EventDate <= endDate);

                var result = await query.ToListAsync();

                return ServiceResponse<IEnumerable<EventDto>>.SuccessResponse(result, "Events retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event data.");
                return ServiceResponse<IEnumerable<EventDto>>.ErrorResponse("Database query failed.");
            }
        }



        public async Task<int> CreateEventAsync(PerformanceEvents performanceEvent)
        {
            try
            {
                _logger.LogInformation("Creating event for TruckId: {TruckId}, DriverId: {DriverId} on Date: {Date}", performanceEvent.truck_id, performanceEvent.driver_id, performanceEvent.date);

                _dbContext.performance_event.Add(performanceEvent);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Event created successfully with EventId: {EventId}", performanceEvent.event_id);

                return performanceEvent.event_id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating an event for TruckId: {TruckId}, DriverId: {DriverId}",
                    performanceEvent.truck_id, performanceEvent.driver_id);
                throw;
            }
        }

        public async Task<int> CreateEventAsync(int companyId, PerformanceEvents performanceEvents)
        {
            try
            {
                _logger.LogInformation("Creating event for CompanyId: {CompanyId}, TruckId: {TruckId}, DriverId: {DriverId} on Date: {Date}",
            companyId, performanceEvents.truck_id, performanceEvents.driver_id, performanceEvents.date);

                var performanceEvent = new PerformanceEvents
                {
                    date = performanceEvents.date,
                    driver_id = performanceEvents.driver_id,
                    truck_id = performanceEvents.truck_id,
                    company_id = companyId
                };

                _dbContext.performance_event.Add(performanceEvent);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Event created successfully with EventId: {EventId} for CompanyId: {CompanyId}", performanceEvent.event_id, companyId);
                return performanceEvent.event_id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating an event for CompanyId: {CompanyId}, TruckId: {TruckId}, DriverId: {DriverId}",
                    companyId, performanceEvents.truck_id, performanceEvents.driver_id);
                throw;
            }
        }
    }
}
