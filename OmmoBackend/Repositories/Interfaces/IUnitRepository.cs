using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IUnitRepository
    {
        Task<IEnumerable<UnitInfoDto>> GetUnitsWithFiltersAsync(int companyId, string? driverName = null, string? unitStatus = null, int? truckId = null, int? trailerId = null);
    }
}