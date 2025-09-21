using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Exceptions;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class UnitService : IUnitService
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ILogger<UnitService> _logger;

        public UnitService(IUnitRepository unitRepository, ILogger<UnitService> logger)
        {
            _unitRepository = unitRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<IEnumerable<UnitInfoResult>>> GetUnitsAsync(int companyId, string? driverName, string? unitStatus, int? truckId, int? trailerId)
        {
            _logger.LogInformation("Fetching units for CompanyId: {CompanyId}, DriverName: {DriverName}, UnitStatus: {UnitStatus}, TruckId: {TruckId}, TrailerId: {TrailerId}",
                companyId, driverName, unitStatus, truckId, trailerId);

            try
            {
                // Fetch units with optional filters
                var units = await _unitRepository.GetUnitsWithFiltersAsync(companyId, driverName, unitStatus, truckId, trailerId);

                if (units == null || !units.Any())
                {
                    _logger.LogInformation("No units found matching the provided criteria for CompanyId: {CompanyId}", companyId);
                    return ServiceResponse<IEnumerable<UnitInfoResult>>.SuccessResponse(Enumerable.Empty<UnitInfoResult>(), "No units found.");
                }

                //// Ensure all results belong to the provided company
                //if (units.Any(unit => unit.CompanyId != companyId))
                //{
                //    return ServiceResponse<IEnumerable<UnitInfoResult>>.ErrorResponse("One or more trucks, drivers, or trailers do not belong to the same company.");
                //}

                var unitInfoList = units.Select(item => new UnitInfoResult
                {
                    UnitId = item.UnitId,
                    TruckStatus = item.TruckStatus.ToString(),
                    TruckId = item.TruckId,
                    TrailerId = item.TrailerId,
                    Latitude = item.Latitude,
                    Longitude = item.Longitude,
                    State = item.State.ToString(),
                    City = item.City,
                    DriverName = item.DriverName,
                    Speed = item.Speed
                }).ToList();

                _logger.LogInformation("Successfully fetched {Count} units for CompanyId: {CompanyId}", unitInfoList.Count, companyId);
                return ServiceResponse<IEnumerable<UnitInfoResult>>.SuccessResponse(unitInfoList);
            }
            catch (DataAccessException ex)
            {
                _logger.LogError(ex, "Data access error while fetching units for CompanyId: {CompanyId}", companyId);
                return ServiceResponse<IEnumerable<UnitInfoResult>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching units for CompanyId: {CompanyId}", companyId);
                return ServiceResponse<IEnumerable<UnitInfoResult>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}