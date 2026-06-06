using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class CustomPackageRepository : ICustomPackageRepository
    {
        private readonly AppDbContext _dbContext;
        public CustomPackageRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> HasPendingRequestAsync(int companyId)
        {
            return await _dbContext.custom_package_request
                .AnyAsync(x => x.company_id == companyId);
        }

        public async Task InsertAsync(CustomPackageRequest entity)
        {
            await _dbContext.custom_package_request.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
        }
    }
}
