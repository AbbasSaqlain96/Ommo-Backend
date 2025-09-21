using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class AccidentDocRepository : GenericRepository<AccidentDoc>, IAccidentDocRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<AccidentDocRepository> _logger;

        public AccidentDocRepository(AppDbContext dbContext, ILogger<AccidentDocRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<int> GetLastAccidentDocIdAsync()
        {
            try
            {
                _logger.LogInformation("Fetching last accident document ID.");

                var lastAccidentDoc = await _dbContext.accident_doc
                .OrderByDescending(t => t.accident_doc_id)
                .FirstOrDefaultAsync();

                int lastId = lastAccidentDoc?.accident_doc_id ?? 0;
                _logger.LogInformation("Last accident document ID retrieved: {LastId}.", lastId);
                return lastId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the last accident document ID.");
                throw;
            }
        }

        public async Task AddMultipleAccidentDocsAsync(List<AccidentDoc> accidentDocs)
        {
            try
            {
                _logger.LogInformation("Adding {Count} accident documents.", accidentDocs.Count);

                await _dbContext.accident_doc.AddRangeAsync(accidentDocs);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully added {Count} accident documents.", accidentDocs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding multiple accident documents.");
                throw;
            }
        }

        public async Task<List<AccidentDoc>> GetAccidentDocumentsAsync(int accidentId)
        {
            return await _dbContext.accident_doc
                 .Where(doc => doc.accident_id == accidentId)
                 .ToListAsync();
        }

    }
}
