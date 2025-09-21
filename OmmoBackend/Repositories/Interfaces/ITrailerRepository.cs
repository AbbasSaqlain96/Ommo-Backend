using OmmoBackend.Models;
using System.ComponentModel.Design;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ITrailerRepository : IGenericRepository<Trailer>
    {
        Task<Trailer> GetTrailerInfoByUnitIdAsync(int unitId);
        // Task<TruckTrailerLocation> GetTrailerLocationByTrailerId(int trailerId);

        Task<bool> IsValidTrailerIdAsync(int trailerId, int companyId);

        Task<bool> DoesTrailerBelongToCompanyAsync(int trailerId, int companyId);

        Task<bool> ExistsAsync(int trailerId);
    }
}