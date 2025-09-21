using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class DriverPerformanceRepository : GenericRepository<PerformanceEvents>, IDriverPerformanceRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<DriverPerformanceRepository> _logger;
        public DriverPerformanceRepository(AppDbContext dbContext, ILogger<DriverPerformanceRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> CheckDriverCompany(int driverId, int companyId)
        {
            try
            {
                _logger.LogInformation("Checking if driver ID {DriverId} belongs to company ID {CompanyId}", driverId, companyId);

                var exists = await _dbContext.driver.AnyAsync(d => d.driver_id == driverId && d.company_id == companyId);

                _logger.LogInformation("Driver ID {DriverId} {Exists} in company ID {CompanyId}", driverId, exists ? "exists" : "does not exist", companyId);

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking driver-company association for driver ID {DriverId} and company ID {CompanyId}", driverId, companyId);
                throw;
            }
        }

        public async Task<DriverPerformanceDto> GetDriverPerformanceAsync(int driverId, int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching performance data for driver ID {DriverId} and company ID {CompanyId}", driverId, companyId);

                var performance = await _dbContext.performance_event
                    .Where(e => e.driver_id == driverId && e.company_id == companyId)
                    .GroupBy(e => e.event_type)
                    .Select(g => new
                    {
                        EventType = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                var result = new DriverPerformanceDto
                {
                    Accidents = performance.FirstOrDefault(e => e.EventType.ToString() == "accident")?.Count ?? 0,
                    Incidents = performance.FirstOrDefault(e => e.EventType.ToString() == "incident")?.Count ?? 0,
                    Citation = performance.FirstOrDefault(e => e.EventType.ToString() == "citation")?.Count ?? 0,
                    DotInspection = performance.FirstOrDefault(e => e.EventType.ToString() == "dot_inspection")?.Count ?? 0,
                    Warning = performance.FirstOrDefault(e => e.EventType.ToString() == "warning")?.Count ?? 0,
                };

                _logger.LogInformation("Performance data retrieved for driver ID {DriverId}: Accidents={Accidents}, Incidents={Incidents}, Tickets={Tickets}", driverId, result.Accidents, result.Incidents, result.Citation, result.DotInspection, result.Warning);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching performance data for driver ID {DriverId} and company ID {CompanyId}", driverId, companyId);
                throw new Exception("An error occurred while retrieving driver performance data.");
            }
        }
    }
}
