using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class IncidentEquipDamageRepository : GenericRepository<IncidentEquipDamage>, IIncidentEquipDamageRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<IncidentEquipDamageRepository> _logger;

        public IncidentEquipDamageRepository(AppDbContext dbContext, ILogger<IncidentEquipDamageRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }
    }
}
