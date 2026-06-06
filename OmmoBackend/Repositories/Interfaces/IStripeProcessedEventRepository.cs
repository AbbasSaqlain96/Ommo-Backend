using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IStripeProcessedEventRepository
    {
        Task<bool> TryInsertAsync(string eventId);
    }

    public class StripeProcessedEventRepository : IStripeProcessedEventRepository
    {
        private readonly AppDbContext _db;

        public StripeProcessedEventRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<bool> TryInsertAsync(string eventId)
        {
            var exists = await _db.stripe_processed_events
                .AnyAsync(x => x.event_id == eventId);

            if (exists)
                return false;

            await _db.stripe_processed_events.AddAsync(
                new StripeProcessedEvent
                {
                    event_id = eventId,
                    processed_at = DateTimeOffset.UtcNow
                });

            return true;
        }
    }
}
