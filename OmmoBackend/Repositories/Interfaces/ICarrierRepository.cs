using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ICarrierRepository : IGenericRepository<Carrier>
    {
        Task<int?> GetCarrierIdByCompanyIdAsync(int companyId);
        Task<Carrier?> GetCarrierByCompanyIdAsync(int companyId);
    }
}