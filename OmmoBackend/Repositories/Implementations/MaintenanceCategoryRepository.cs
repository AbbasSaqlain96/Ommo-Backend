using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class MaintenanceCategoryRepository : GenericRepository<MaintenanceCategory>, IMaintenanceCategoryRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<MaintenanceCategoryRepository> _logger;
        public MaintenanceCategoryRepository(AppDbContext dbContext, IConfiguration configuration, ILogger<MaintenanceCategoryRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<MaintenanceCategory>> GetCategoriesAsync(string? catType, int? carrierId)
        {
            try
            {
                _logger.LogInformation("Fetching maintenance categories. catType: {CatType}, carrierId: {CarrierId}", catType, carrierId);

                var query = _dbContext.maintenance_category
                    .Where(mc => mc.carrier_id == carrierId || mc.carrier_id == null);

                var categories = await query.ToListAsync();

                _logger.LogInformation("Fetched {Count} maintenance categories successfully.", categories.Count);

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching maintenance categories. catType: {CatType}, carrierId: {CarrierId}", catType, carrierId);
                throw;
            }
        }
    }
}
