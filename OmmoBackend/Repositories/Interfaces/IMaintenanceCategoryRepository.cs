using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IMaintenanceCategoryRepository : IGenericRepository<MaintenanceCategory>
    {
        Task<List<MaintenanceCategory>> GetCategoriesAsync(string? catType, int? carrierId);
    }
}
