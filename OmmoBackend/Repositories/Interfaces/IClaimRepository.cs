using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IClaimRepository : IGenericRepository<Claims>
    {
        Task CreateClaimAsync(Claims claim);
        Task UpdateClaimsAsync(int eventId, List<Claims> newClaims);
    }
}
