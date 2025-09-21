using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IAssetService
    {
        Task<ServiceResponse<IEnumerable<VehicleDto>>> GetAssetsAsync(int companyId);
        Task<ServiceResponse<AssetDetailsDto>> GetAssetDetailsAsync(int vehicleId, int companyId);
        Task<ServiceResponse<List<AssetDetailIssueTicketDto>>> GetShopHistoryAsync(int vehicleId, int companyId);
        Task<ServiceResponse<AssetResponseDto>> AddAssetAsync(AddAssetRequestDto dto, int companyId);
    }
}