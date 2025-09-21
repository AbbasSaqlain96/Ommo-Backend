using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ITruckRepository : IGenericRepository<Truck>
    {
        Task<Truck> GetTruckInfoByUnitIdAsync(int unitId);
        Task<TruckTracking> GetTruckTrackingByTruckIdAsync(int truckId);
        Task<TruckLocation> GetTruckLocationByTruckIdAsync(int truckId);
        Task<List<TruckResponse>> GetTrucksByCompanyAsync(int companyId);
        Task<bool> IsValidTruckIdAsync(int truckId, int companyId);

        Task<bool> DoesTruckBelongToCompanyAsync(int truckId, int companyId);

        Task<bool> ExistsAsync(int truckId);
    }
}