using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface ITruckService
    {
        Task<ServiceResponse<TruckInfoDto>> GetTruckInfoAsync(int unitId);
        Task<ServiceResponse<List<TruckResponse>>> GetTruckListAsync(int companyId);
    }
}