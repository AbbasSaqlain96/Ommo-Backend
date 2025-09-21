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
    public class UnitRepository : IUnitRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<UnitRepository> _logger;
        public UnitRepository(AppDbContext dbContext, ILogger<UnitRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }


        public async Task<IEnumerable<UnitInfoDto>> GetUnitsWithFiltersAsync(int companyId,
                                                                            string? driverName = null,
                                                                            string? unitStatus = null,
                                                                            int? truckId = null,
                                                                            int? trailerId = null)
        {
            _logger.LogInformation("Fetching units for CompanyId: {CompanyId}, DriverName: {DriverName}, UnitStatus: {UnitStatus}, TruckId: {TruckId}, TrailerId: {TrailerId}", companyId, driverName, unitStatus, truckId, trailerId);

            try
            {
                var query = from u in _dbContext.unit
                            join c in _dbContext.carrier on companyId equals c.company_id
                            join d in _dbContext.driver on u.driver_id equals d.driver_id
                            join t in _dbContext.truck on u.truck_id equals t.truck_id
                            from tt in _dbContext.truck_tracking.Where(tt => tt.truck_id == t.truck_id).DefaultIfEmpty()
                            from tl in _dbContext.truck_location.Where(tl => tl.truck_id == t.truck_id).DefaultIfEmpty()
                            join comp in _dbContext.company on c.company_id equals comp.company_id
                            where c.company_id == companyId && c.carrier_id == u.carrier_id
                            select new UnitInfoDto
                            {
                                UnitId = u.unit_id,
                                TruckStatus = t.truck_status.ToString(),
                                TruckId = u.truck_id,
                                TrailerId = u.trailer_id,
                                Latitude = tl.latitude,
                                Longitude = tl.longitude,
                                State = tl.location_state.ToString(),
                                City = tl.location_city,
                                DriverName = d.driver_name,
                                Speed = tt.speed,
                                CompanyId = u.carrier_id
                            };

                // Apply filtering
                if (!string.IsNullOrWhiteSpace(driverName))
                    query = query.Where(u => u.DriverName.Contains(driverName));
                if (!string.IsNullOrWhiteSpace(unitStatus))
                    query = query.Where(u => u.UnitStatus == unitStatus);
                if (truckId.HasValue)
                    query = query.Where(u => u.TruckId == truckId.Value);
                if (trailerId.HasValue)
                    query = query.Where(u => u.TrailerId == trailerId.Value);

                var result = await query.ToListAsync();

                // If no units found for the company, return an appropriate message
                if (!result.Any())
                {
                    _logger.LogInformation("No units found for CompanyId: {CompanyId} with the given filters.", companyId);
                }

                _logger.LogInformation("Successfully retrieved {Count} units for CompanyId: {CompanyId}.", result.Count, companyId);
                return result;

            }
            catch (DataAccessException ex)
            {
                _logger.LogError("DataAccessException occurred while retrieving units for CompanyId: {CompanyId}. Error: {Message}", companyId, ex.Message);
                // Rethrow DataAccessException as-is without wrapping
                throw;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError("Database operation failed while retrieving units for CompanyId: {CompanyId}. Error: {Message}", companyId, dbEx.Message);
                throw new DataAccessException(ErrorMessages.DatabaseOperationFailed, dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError("An unexpected error occurred while retrieving units for CompanyId: {CompanyId}. Error: {Message}", companyId, ex.Message);
                throw new DataAccessException(ErrorMessages.GenericOperationFailed, ex);
            }
        }
    }
}