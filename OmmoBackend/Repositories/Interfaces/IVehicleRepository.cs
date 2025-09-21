using Microsoft.EntityFrameworkCore;
using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IVehicleRepository : IGenericRepository<Vehicle>
    {
        Task<IEnumerable<VehicleDto>> GetVehiclesAsync(int companyId);
        Task<Vehicle> GetVehicleByIdAsync(int vehicleId);
    }
}
