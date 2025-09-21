using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using System.Security.Claims;

namespace OmmoBackend.Repositories.Implementations
{
    public class ClaimRepository : GenericRepository<Claims>, IClaimRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ClaimRepository> _logger;

        public ClaimRepository(AppDbContext dbContext, ILogger<ClaimRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task CreateClaimAsync(Claims claim)
        {
            try
            {
                _logger.LogInformation("Creating a new claim for ClaimId: {ClaimId}", claim.claim_id);

                _dbContext.claim.Add(claim);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully created claim with ClaimId: {ClaimId}", claim.claim_id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating claim for ClaimId: {ClaimId}", claim.claim_id);
                throw;
            }
        }

        public async Task UpdateClaimsAsync(int eventId, List<Claims> newClaims)
        {
            try
            {
                // Fetch existing claims for the event
                var existingClaims = await _dbContext.claim
                    .Where(c => c.event_id == eventId)
                    .ToListAsync();

                // Identify claims to delete (based on unique key: type + description + amount)
                var newClaimKeys = new HashSet<string>(
                    newClaims.Select(c => $"{c.claim_type}|{c.claim_description}|{c.claim_amount}"),
                    StringComparer.OrdinalIgnoreCase);

                var claimsToDelete = existingClaims
                    .Where(ec => !newClaimKeys.Contains($"{ec.claim_type}|{ec.claim_description}|{ec.claim_amount}"))
                    .ToList();

                _dbContext.claim.RemoveRange(claimsToDelete);

                // Add new claims not already existing
                foreach (var newClaim in newClaims)
                {
                    var isDuplicate = existingClaims.Any(ec =>
                        ec.claim_type == newClaim.claim_type &&
                        ec.claim_description.Equals(newClaim.claim_description, StringComparison.OrdinalIgnoreCase) &&
                        ec.claim_amount == newClaim.claim_amount);

                    if (!isDuplicate)
                    {
                        _dbContext.claim.Add(new Claims
                        {
                            event_id = eventId,
                            claim_type = newClaim.claim_type,
                            claim_amount = newClaim.claim_amount,
                            status = newClaim.status,
                            claim_description = newClaim.claim_description,
                            created_at = DateTime.UtcNow,
                            updated_at = DateTime.UtcNow
                        });
                    }
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Claims updated successfully for EventId: {EventId}", eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database update failed while updating claims for EventId: {EventId}", eventId);
                throw;
            }
        }
    }
}
