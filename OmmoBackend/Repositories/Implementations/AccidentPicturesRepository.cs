using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class AccidentPicturesRepository : GenericRepository<AccidentPicture>, IAccidentPicturesRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<AccidentPicturesRepository> _logger;

        public AccidentPicturesRepository(AppDbContext dbContext, ILogger<AccidentPicturesRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<AccidentPicture>> GetAccidentImagesByAccidentIdAsync(int accidentId)
        {
            return await _dbContext.accident_pictures
                .Where(p => p.accident_id == accidentId)
                .ToListAsync();
        }
    }
}
