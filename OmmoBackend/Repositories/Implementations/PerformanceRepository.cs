using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class PerformanceRepository : GenericRepository<PerformanceEvents>, IPerformanceRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<PerformanceRepository> _logger;
        public PerformanceRepository(AppDbContext dbContext, ILogger<PerformanceRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> CheckPerformanceCompany(int companyId)
        {
            try
            {
                _logger.LogInformation("Checking performance data for company ID: {CompanyId}", companyId);
                var exists = await _dbContext.performance_event.AnyAsync(u => u.company_id == companyId);

                if (exists)
                {
                    _logger.LogInformation("Performance data found for company ID: {CompanyId}", companyId);
                }
                else
                {
                    _logger.LogWarning("No performance data found for company ID: {CompanyId}", companyId);
                }

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking performance data for company ID: {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<PerformanceDto> GetPerformanceAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Retrieving performance data for company ID: {CompanyId}", companyId);

                var performance = await (from e in _dbContext.performance_event
                                         where e.company_id == companyId
                                         group e by e.event_type into g
                                         select new
                                         {
                                             EventType = g.Key.ToString(),
                                             Count = g.Count()
                                         })
                             .ToListAsync();

                // Return default if no events found
                if (!performance.Any())
                {
                    _logger.LogInformation("No performance events found for company ID: {CompanyId}", companyId);
                    return new PerformanceDto();
                }

                var result = new PerformanceDto
                {
                    Accidents = performance.FirstOrDefault(e => e.EventType == "accident")?.Count ?? 0,
                    Incidents = performance.FirstOrDefault(e => e.EventType == "incident")?.Count ?? 0,
                    Citation = performance.FirstOrDefault(e => e.EventType == "citation")?.Count ?? 0,
                    DotInspection = performance.FirstOrDefault(e => e.EventType == "dot_inspection")?.Count ?? 0,
                    Warning = performance.FirstOrDefault(e => e.EventType == "warning")?.Count ?? 0,
                };

                _logger.LogInformation("Performance data retrieved successfully for company ID: {CompanyId}: {PerformanceData}", companyId, result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving performance data for company ID: {CompanyId}", companyId);
                throw;
            }
        }
    }
}
