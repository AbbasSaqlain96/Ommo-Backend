using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Exceptions;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace OmmoBackend.Repositories.Implementations
{
    public class VehicleRepository : GenericRepository<Vehicle>, IVehicleRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<VehicleRepository> _logger;

        public VehicleRepository(AppDbContext dbContext, ILogger<VehicleRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IEnumerable<VehicleDto>> GetVehiclesAsync(int companyId)
        {
            _logger.LogInformation("Fetching vehicles for CompanyId: {CompanyId}", companyId);

            try
            {
                var vehicles = from v in _dbContext.vehicle
                               join c in _dbContext.carrier on v.carrier_id equals c.carrier_id
                               where c.company_id == companyId
                               select new VehicleDto
                               {
                                   VehicleId = v.vehicle_id,
                                   PlateNumber = v.plate_number,
                                   PlateState = v.license_plate_state.ToString(),
                                   VinNumber = v.vin_number,
                                   VehicleType = v.vehicle_type.ToString(),
                                   Rating = (int)v.rating,
                                   IsAssigned = v.is_assigned,
                                   Year = v.year,
                                   Trademark = v.vehicle_trademark,
                                   Status = v.status.ToString()
                               };

                var result = await vehicles.ToListAsync();
                if (!result.Any())
                {
                    _logger.LogWarning("No vehicles found for CompanyId: {CompanyId}", companyId);
                    return new List<VehicleDto>();
                }
                _logger.LogInformation("Fetched {Count} vehicles for CompanyId: {CompanyId}", result.Count, companyId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching vehicles for CompanyId: {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<Vehicle> GetVehicleByIdAsync(int vehicleId)
        {
            _logger.LogInformation("Fetching vehicle details for VehicleId: {VehicleId}", vehicleId);

            try
            {
                var vehicle = await _dbContext.vehicle.FindAsync(vehicleId);

                if (vehicle == null)
                {
                    _logger.LogWarning("Vehicle not found for VehicleId: {VehicleId}", vehicleId);
                }
                else
                {
                    _logger.LogInformation("Vehicle found: {VehicleId}, PlateNumber: {PlateNumber}", vehicle.vehicle_id, vehicle.plate_number);
                }

                return vehicle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching vehicle details for VehicleId: {VehicleId}", vehicleId);
                throw;
            }
        }
    }
}
