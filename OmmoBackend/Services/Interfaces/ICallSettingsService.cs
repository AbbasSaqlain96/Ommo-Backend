using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface ICallSettingsService
    {
        Task<ServiceResponse<object>> GetAvailableNumbersAsync();
        Task<ServiceResponse<object>> BuyNumberAsync(int companyId, BuyNumberRequest request);
    }
}
