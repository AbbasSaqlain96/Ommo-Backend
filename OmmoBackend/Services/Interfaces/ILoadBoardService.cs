using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface ILoadBoardService
    {
        Task<ServiceResponse<List<NormalizedLoadDto>>> GetLoadsAsync(int companyId, LoadFiltersDto filters);
    }
}
