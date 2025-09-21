using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class IncidentTypeRepository : GenericRepository<IncidentType>, IIncidentTypeRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<IncidentTypeRepository> _logger;

        public IncidentTypeRepository(AppDbContext dbContext, ILogger<IncidentTypeRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }


    }
}
