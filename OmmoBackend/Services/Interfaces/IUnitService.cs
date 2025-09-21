using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IUnitService
    {
        Task<ServiceResponse<IEnumerable<UnitInfoResult>>> GetUnitsAsync(int companyId, string? driverName, string? unitStatus, int? truckId, int? trailerId);
    }
}