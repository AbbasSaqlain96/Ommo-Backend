using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class TranscriptRepository : ITranscriptRepository
    {
        private readonly AppDbContext _dbContext;

        public TranscriptRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<CallTranscript>> GetByCallIdAsync(int callId)
        {
            return await _dbContext.call_transcript
                .Where(t => t.call_id == callId)
                .OrderBy(t => t.timestamp)
                .ToListAsync();
        }
    }
}
