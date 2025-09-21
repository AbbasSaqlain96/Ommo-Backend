using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ICallRepository : IGenericRepository<Call>
    {
        Task<List<CalledLoadDto>> GetCalledLoadsAsync(int companyId);
    }
}
