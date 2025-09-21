using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IDriverService
    {
        Task<ServiceResponse<DriverDto>> GetDriverInfoAsync(int unitId, int companyId);
        Task<ServiceResponse<IEnumerable<DriverListDto>>> GetDriverListAsync(int companyId);
        Task<ServiceResponse<DriverDetailDto>> GetDriverDetailAsync(int driverId, int companyId);
        Task<ServiceResponse<HireDriverResponse>> HireDriverAsync(int companyId, HireDriverRequestDto request);
    }
}