using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class SupportRequestRepository : ISupportRequestRepository
    {
        private readonly AppDbContext _dbContext;

        public SupportRequestRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(SupportRequest supportRequest)
        {
            await _dbContext.support_request.AddAsync(supportRequest);
            await _dbContext.SaveChangesAsync();
        }
    }
}
