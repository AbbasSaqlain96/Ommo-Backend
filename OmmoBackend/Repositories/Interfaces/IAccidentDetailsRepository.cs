using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IAccidentDetailsRepository : IGenericRepository<Accident>
    {
        Task<bool> ValidateEventAndDriver(int eventId, int driverId);
        Task<AccidentDetailsResponse> GetAccidentDetailsAsync(int eventId, int companyId);
        Task<List<ClaimDto>> GetClaimsAsync(int eventId);
        Task<bool> IsEventValidForCompany(int eventId, int companyId);

    }
}
