using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Exceptions;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class TruckRepository : GenericRepository<Truck>, ITruckRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<TruckRepository> _logger;
        public TruckRepository(AppDbContext dbContext, ILogger<TruckRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Truck?> GetTruckInfoByUnitIdAsync(int unitId)
        {
            _logger.LogInformation("Fetching truck information for UnitId: {UnitId}", unitId);

            try
            {
                // Fetch Truck object based on the Unit's Truck Id
                var truck = await GetTruckByUnitIdQuery(unitId).FirstOrDefaultAsync();
                if (truck != null)
                {
                    _logger.LogInformation("Truck found for UnitId: {UnitId} with TruckId: {TruckId}", unitId, truck.truck_id);
                }
                else
                {
                    _logger.LogWarning("No truck found for UnitId: {UnitId}", unitId);
                }

                return truck;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Database query failed while retrieving truck info for UnitId: {UnitId}", unitId);
                throw new InvalidOperationException("Database query failed while retrieving truck info.", ex);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database operation failed while fetching truck info for UnitId: {UnitId}", unitId);
                throw new DataAccessException(ErrorMessages.DatabaseOperationFailed, dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching truck info for UnitId: {UnitId}", unitId);
                throw new DataAccessException(ErrorMessages.GenericOperationFailed, ex);
            }
        }

        public async Task<TruckTracking?> GetTruckTrackingByTruckIdAsync(int truckId)
        {
            _logger.LogInformation("Fetching truck tracking information for TruckId: {TruckId}", truckId);

            try
            {
                // Fetch Truck Tracking object based on the Truck Id
                var tracking = await GetTruckTrackingByTruckIdQuery(truckId).FirstOrDefaultAsync();

                if (tracking != null)
                {
                    _logger.LogInformation("Truck tracking found for TruckId: {TruckId}", truckId);
                }
                else
                {
                    _logger.LogWarning("No truck tracking data found for TruckId: {TruckId}", truckId);
                }

                return tracking;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Database query failed while retrieving truck tracking info for TruckId: {TruckId}", truckId);
                throw new InvalidOperationException("Database query failed while retrieving truck tracking info.", ex);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database operation failed while fetching truck tracking info for TruckId: {TruckId}", truckId);
                throw new DataAccessException(ErrorMessages.DatabaseOperationFailed, dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching truck tracking info for TruckId: {TruckId}", truckId);
                throw new DataAccessException(ErrorMessages.GenericOperationFailed, ex);
            }
        }

        public async Task<TruckLocation?> GetTruckLocationByTruckIdAsync(int truckId)
        {
            _logger.LogInformation("Fetching truck location for TruckId: {TruckId}", truckId);

            try
            {
                // Fetch Truck Location object based on the Truck Id
                var location = await GetTruckLocationByTruckIdQuery(truckId).FirstOrDefaultAsync();

                if (location != null)
                {
                    _logger.LogInformation("Truck location found for TruckId: {TruckId}", truckId);
                }
                else
                {
                    _logger.LogWarning("No truck location found for TruckId: {TruckId}", truckId);
                }

                return location;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Database query failed while retrieving truck location info for TruckId: {TruckId}", truckId);
                throw new InvalidOperationException("Database query failed while retrieving truck location info.", ex);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database operation failed while fetching truck location info for TruckId: {TruckId}", truckId);
                throw new DataAccessException(ErrorMessages.DatabaseOperationFailed, dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching truck location info for TruckId: {TruckId}", truckId);
                throw new DataAccessException(ErrorMessages.GenericOperationFailed, ex);
            }
        }

        public async Task<List<TruckResponse>> GetTrucksByCompanyAsync(int companyId)
        {
            _logger.LogInformation("Fetching trucks for CompanyId: {CompanyId}", companyId);

            try
            {
                var trucks = await (from t in _dbContext.truck
                                    join v in _dbContext.vehicle on t.vehicle_id equals v.vehicle_id
                                    join c in _dbContext.carrier on v.carrier_id equals c.carrier_id
                                    join comp in _dbContext.company on c.company_id equals comp.company_id
                                    where comp.company_id == companyId
                                    select new TruckResponse
                                    {
                                        TruckId = t.truck_id,
                                        Model = t.model,
                                        Brand = t.brand
                                    })
                              .ToListAsync();

                if (trucks.Any())
                {
                    _logger.LogInformation("{Count} trucks found for CompanyId: {CompanyId}", trucks.Count, companyId);
                }
                else
                {
                    _logger.LogWarning("No trucks found for CompanyId: {CompanyId}", companyId);
                }

                return trucks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching trucks for CompanyId: {CompanyId}", companyId);
                throw new DataAccessException(ErrorMessages.GenericOperationFailed, ex);
            }
        }

        private IQueryable<Truck> GetTruckByUnitIdQuery(int unitId)
        {
            _logger.LogDebug("Creating query to fetch truck by UnitId: {UnitId}", unitId);

            return from u in _dbContext.unit
                   join t in _dbContext.truck on u.truck_id equals t.truck_id
                   where u.unit_id == unitId
                   select new Truck
                   {
                       truck_id = t.truck_id,
                       brand = t.brand,
                       model = t.model,
                       color = t.color
                   };
        }

        private IQueryable<TruckTracking> GetTruckTrackingByTruckIdQuery(int truckId)
        {
            _logger.LogDebug("Creating query to fetch truck tracking by TruckId: {TruckId}", truckId);

            return from tt in _dbContext.truck_tracking
                   where tt.truck_id == truckId
                   select new TruckTracking
                   {
                       odometer = tt.odometer,
                       last_update_odometer = tt.last_update_odometer,
                       speed = tt.speed,
                       last_update_speed = tt.last_update_speed,
                       mileage = tt.mileage,
                       last_updated_mileage = tt.last_updated_mileage
                   };
        }

        private IQueryable<TruckLocation> GetTruckLocationByTruckIdQuery(int truckId)
        {
            _logger.LogDebug("Creating query to fetch truck location by TruckId: {TruckId}", truckId);

            return from tl in _dbContext.truck_location
                   where tl.truck_id == truckId
                   select new TruckLocation
                   {
                       latitude = tl.latitude,
                       longitude = tl.longitude,
                       location_state = tl.location_state,
                       location_city = tl.location_city,
                       last_updated_location = tl.last_updated_location
                   };
        }

        public async Task<bool> IsValidTruckIdAsync(int truckId, int companyId)
        {
            return await _dbContext.truck.AnyAsync(t => t.truck_id == truckId);
        }



        public async Task<bool> DoesTruckBelongToCompanyAsync(int truckId, int companyId)
        {
            return await _dbContext.truck
                .Where(t => t.truck_id == truckId)
                .Join(_dbContext.vehicle,
                      t => t.vehicle_id,
                      v => v.vehicle_id,
                      (t, v) => new { t, v })
                .Join(_dbContext.carrier,
                      tv => tv.v.carrier_id,
                      c => c.carrier_id,
                      (tv, c) => new { tv.t, c })
                .AnyAsync(result => result.c.company_id == companyId);
        }

        public async Task<bool> ExistsAsync(int truckId)
        {
            return await _dbContext.truck
                .AnyAsync(t => t.truck_id == truckId);
        }
    }
}