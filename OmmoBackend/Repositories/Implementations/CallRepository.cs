using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class CallRepository : GenericRepository<Call>, ICallRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<CallRepository> _logger;

        public CallRepository(AppDbContext dbContext, ILogger<CallRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<CalledLoadDto>> GetCalledLoadsAsync(int companyId)
        {
            var since = DateTime.UtcNow.AddHours(-24);

            var query = from c in _dbContext.call
                        where c.company_id == companyId && c.call_timestamp >= since
                        select new CalledLoadDto
                        {
                            Source = c.loadboard_type,
                            MatchId = c.match_id.ToString(),
                            TruckStopId = c.truckstop_id.ToString(),
                            CalledAtUtc = c.call_timestamp
                        };

            return await query.ToListAsync();
        }
    }
}
