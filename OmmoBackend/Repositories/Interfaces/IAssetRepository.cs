using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IAssetRepository : IGenericRepository<Vehicle>
    {
        Task<AssetDetailTruckDto> GetTruckByVehicleIdAsync(int vehicleId);
        Task<AssetDetailTrailerDto> GetTrailerByVehicleIdAsync(int vehicleId);
        Task<List<VehicleAttributeDto>> GetVehicleAttributesAsync(int vehicleId);
        Task<List<VehicleDocumentDto>> GetVehicleDocumentsAsync(int vehicleId);
        Task<List<AssetDetailIssueTicketDto>> GetShopHistoryByVehicleIdAsync(int vehicleId);
        Task<AssetResponseDto> AddAssetAsync(AddAssetRequestDto dto, int companyId);

        Task<bool> VinExistsAsync(string vin);
        Task<bool> PlateNumberStateExistsAsync(string plateNumber, string plateState);

    }
}