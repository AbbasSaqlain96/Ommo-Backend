using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Services.Interfaces
{
    public interface ICarrierService
    {
        Task<int?> GetCarrierIdByCompanyIdAsync(int companyId);
    }
}