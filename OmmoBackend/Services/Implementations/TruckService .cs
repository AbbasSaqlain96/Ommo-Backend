using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class TruckService : ITruckService
    {
        private readonly ITruckRepository _truckRepository;
        private readonly ILogger<TruckService> _logger;
        public TruckService(ITruckRepository truckRepository, ILogger<TruckService> logger)
        {
            _truckRepository = truckRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<TruckInfoDto?>> GetTruckInfoAsync(int unitId)
        {
            try
            {
                _logger.LogInformation("Fetching truck info for Unit ID: {UnitId}", unitId);

                // Call the repository to fetch truck, location, and tracking data
                var truck = await _truckRepository.GetTruckInfoByUnitIdAsync(unitId);

                if (truck == null)
                {
                    _logger.LogWarning("Truck information not found for Unit ID: {UnitId}", unitId);
                    return ServiceResponse<TruckInfoDto?>.ErrorResponse("Truck information not found for the given Unit ID.");
                }

                // Map Truck to TruckDto
                var truckDto = MapToTruckDto(truck);
                _logger.LogInformation("Truck info retrieved successfully for Unit ID: {UnitId}", unitId);

                // Fetch truck tracking information
                var truckTracking = await _truckRepository.GetTruckTrackingByTruckIdAsync(truck.truck_id);

                if (truckTracking == null)
                {
                    _logger.LogWarning("Truck tracking info not found for Truck ID: {TruckId}", truck.truck_id);
                    return ServiceResponse<TruckInfoDto?>.SuccessResponse(null);
                }

                // Map TruckTracking to TruckTrackingDto
                var truckTrackingDto = MapToTruckTrackingDto(truckTracking);

                // Fetch truck location information
                var truckLocation = await _truckRepository.GetTruckLocationByTruckIdAsync(truck.truck_id);

                if (truckLocation == null)
                {
                    _logger.LogWarning("Truck location info not found for Truck ID: {TruckId}", truck.truck_id);
                    return ServiceResponse<TruckInfoDto?>.SuccessResponse(null);
                }

                var truckLocationDto = MapToTruckLocationDto(truckLocation);

                _logger.LogInformation("Truck info, tracking, and location retrieved successfully for Unit ID: {UnitId}", unitId);
                return ServiceResponse<TruckInfoDto?>.SuccessResponse(new TruckInfoDto
                {
                    Truck = truckDto,
                    TruckTracking = truckTrackingDto,
                    TruckLocation = truckLocationDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the truck information for Unit ID: {UnitId}", unitId);
                throw new ApplicationException("An error occurred while retrieving the truck information.");
            }
        }

        public async Task<ServiceResponse<List<TruckResponse>>> GetTruckListAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching truck list for Company ID: {CompanyId}", companyId);

                // Ensure that trucks are fetched only for the authenticated user's company
                var truckList = await _truckRepository.GetTrucksByCompanyAsync(companyId);

                if (truckList == null || !truckList.Any())
                {
                    _logger.LogInformation("No trucks found for Company ID: {CompanyId}", companyId);
                    return ServiceResponse<List<TruckResponse>>.SuccessResponse(new List<TruckResponse>(), "No trucks found.");
                }

                _logger.LogInformation("Truck list retrieved successfully for Company ID: {CompanyId}", companyId);
                return ServiceResponse<List<TruckResponse>>.SuccessResponse(truckList, "Trucks retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the truck list for Company ID: {CompanyId}", companyId);
                return ServiceResponse<List<TruckResponse>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }


        private TruckDto MapToTruckDto(Truck truck) => new TruckDto
        {
            TruckId = truck.truck_id,
            Brand = truck.brand,
            VehicleId = truck.vehicle_id,
            Model = truck.model,
            FuelType = truck.fuel_type.ToString(),
            Color = truck.color
        };

        private TruckTrackingDto MapToTruckTrackingDto(TruckTracking truckTracking) => new TruckTrackingDto
        {
            Odometer = truckTracking.odometer,
            LastUpdateOdometer = truckTracking.last_update_odometer,
            Speed = truckTracking.speed,
            LastUpdateSpeed = truckTracking.last_update_speed,
            Mileage = truckTracking.mileage,
            LastUpdatedMileage = truckTracking.last_updated_mileage
        };

        private TruckLocationDto MapToTruckLocationDto(TruckLocation truckLocation) => new TruckLocationDto
        {
            Latitude = truckLocation.latitude,
            Longitude = truckLocation.longitude,
            LocationState = truckLocation.location_state.ToString(),
            LocationCity = truckLocation.location_city,
            LastUpdatedLocation = truckLocation.last_updated_location
        };
    }
}