using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Implementations;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class AssetService : IAssetService
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly ICarrierRepository _carrierRepository;
        private readonly ILogger<AssetService> _logger;
        public AssetService(IVehicleRepository vehicleRepository, IAssetRepository assetRepository, ICarrierRepository carrierRepository, ILogger<AssetService> logger)
        {
            _vehicleRepository = vehicleRepository;
            _assetRepository = assetRepository;
            _carrierRepository = carrierRepository;
            _logger = logger;
        }
        public async Task<ServiceResponse<IEnumerable<VehicleDto>>> GetAssetsAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching assets for companyId: {CompanyId}", companyId);

                var vehicles = await _vehicleRepository.GetVehiclesAsync(companyId);

                // Always return a success response with an empty list if nothing is found
                if (vehicles == null || !vehicles.Any())
                {
                    _logger.LogInformation("No vehicles found for companyId: {CompanyId}", companyId);
                    return ServiceResponse<IEnumerable<VehicleDto>>.SuccessResponse(new List<VehicleDto>());
                }

                _logger.LogInformation("Assets retrieved successfully for companyId: {CompanyId}");
                return ServiceResponse<IEnumerable<VehicleDto>>.SuccessResponse(vehicles, "Assets retrieved successfully.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving assets for companyId: {CompanyId}", companyId);
                return ServiceResponse<IEnumerable<VehicleDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<AssetDetailsDto>> GetAssetDetailsAsync(int vehicleId, int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching asset details for vehicleId: {VehicleId}, companyId: {CompanyId}", vehicleId, companyId);

                // Check if Vehicle belongs to the same company
                var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId);
                if (vehicle == null)
                {
                    _logger.LogWarning("Vehicle not found for vehicleId: {VehicleId}", vehicleId);
                    return ServiceResponse<AssetDetailsDto>.ErrorResponse("No vehicle found for the provided Vehicle ID.", 400);
                }

                if (vehicle.carrier_id == null)
                {
                    _logger.LogWarning("Vehicle {VehicleId} does not have an associated carrier.", vehicleId);
                    return ServiceResponse<AssetDetailsDto>.ErrorResponse("Vehicle does not have an associated carrier.", 400);
                }

                var carrier = await _carrierRepository.GetByIdAsync(vehicle.carrier_id.Value);
                if (carrier == null || carrier.company_id != companyId)
                {
                    _logger.LogWarning("Unauthorized access: VehicleId {VehicleId} does not belong to companyId {CompanyId}", vehicleId, companyId);
                    return ServiceResponse<AssetDetailsDto>.ErrorResponse("You do not have permission to access this resource", 401);
                }

                // Fetch asset details
                var truck = await _assetRepository.GetTruckByVehicleIdAsync(vehicleId);
                var trailer = await _assetRepository.GetTrailerByVehicleIdAsync(vehicleId);
                var attributes = await _assetRepository.GetVehicleAttributesAsync(vehicleId);
                var documents = await _assetRepository.GetVehicleDocumentsAsync(vehicleId);

                _logger.LogInformation("Successfully fetched asset details for vehicleId: {VehicleId}", vehicleId);

                var assetDetails = new AssetDetailsDto
                {
                    Truck = truck,
                    TrailerType = trailer?.Trailer_Type,
                    Attributes = attributes,
                    Documents = documents
                };

                return ServiceResponse<AssetDetailsDto>.SuccessResponse(assetDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while retrieving asset details for vehicleId: {VehicleId}, companyId: {CompanyId}", vehicleId, companyId);
                return ServiceResponse<AssetDetailsDto>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<List<AssetDetailIssueTicketDto>>> GetShopHistoryAsync(int vehicleId, int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching shop history for vehicleId: {VehicleId}, companyId: {CompanyId}", vehicleId, companyId);

                // Check if Vehicle belongs to the same company
                var vehicle = await _vehicleRepository.GetVehicleByIdAsync(vehicleId);
                if (vehicle == null)
                {
                    _logger.LogWarning("Vehicle not found for vehicleId: {VehicleId}", vehicleId);
                    return ServiceResponse<List<AssetDetailIssueTicketDto>>.ErrorResponse("No vehicle found for the provided Vehicle ID.", 400);
                }

                if (vehicle.carrier_id == null)
                {
                    _logger.LogWarning("Vehicle {VehicleId} does not have an associated carrier.", vehicleId);
                    return ServiceResponse<List<AssetDetailIssueTicketDto>>.ErrorResponse("No vehicle found for the provided Vehicle ID.", 400);
                }

                // Fetch Carrier to get CompanyId
                var carrier = await _carrierRepository.GetByIdAsync(vehicle.carrier_id.Value);
                if (carrier == null || carrier.company_id != companyId)
                {
                    _logger.LogWarning("Unauthorized access attempt. VehicleId: {VehicleId} does not belong to companyId: {CompanyId}", vehicleId, companyId);
                    return ServiceResponse<List<AssetDetailIssueTicketDto>>.ErrorResponse("No vehicle found for the provided Vehicle ID.", 400);
                }

                var shopHistory = await _assetRepository.GetShopHistoryByVehicleIdAsync(vehicleId);

                _logger.LogInformation("Successfully fetched shop history for vehicleId: {VehicleId}", vehicleId);

                // Return 200 even if no history is found
                return ServiceResponse<List<AssetDetailIssueTicketDto>>.SuccessResponse(shopHistory ?? new List<AssetDetailIssueTicketDto>(), "Shop history retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shop history for vehicleId: {VehicleId}, companyId: {CompanyId}", vehicleId, companyId);
                return ServiceResponse<List<AssetDetailIssueTicketDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<AssetResponseDto>> AddAssetAsync(AddAssetRequestDto dto, int companyId)
        {
            try
            {
                _logger.LogInformation("Validating uniqueness for VIN and Plate+State.");

                if (await _assetRepository.VinExistsAsync(dto.VinNumber))
                {
                    return ServiceResponse<AssetResponseDto>.ErrorResponse(
                        "The VIN number is already registered to another vehicle.", 400);
                }

                if (await _assetRepository.PlateNumberStateExistsAsync(dto.PlateNumber, dto.PlateState))
                {
                    return ServiceResponse<AssetResponseDto>.ErrorResponse(
                        "A vehicle with the same plate number and state already exists.", 400);
                }

                _logger.LogInformation("Adding new asset for companyId: {CompanyId}", companyId);

                var assetResult = await _assetRepository.AddAssetAsync(dto, companyId);

                if (!assetResult.Success)
                {
                    var errorMsg = assetResult.ErrorMessage?.ToLowerInvariant();

                    if (errorMsg != null)
                    {
                        if (errorMsg.Contains("plate number") && errorMsg.Contains("already exists"))
                            return ServiceResponse<AssetResponseDto>.ErrorResponse("A vehicle with the same plate number and state already exists.", 400);

                        if (errorMsg.Contains("vin") && errorMsg.Contains("already exists"))
                            return ServiceResponse<AssetResponseDto>.ErrorResponse("The VIN number is already registered to another vehicle.", 400);
                    }

                    _logger.LogWarning("Failed to add asset. Error: {Error}", assetResult.ErrorMessage);
                    return ServiceResponse<AssetResponseDto>.ErrorResponse(assetResult.ErrorMessage!, 400);
                }

                _logger.LogInformation("Successfully added asset for companyId: {CompanyId}", companyId);
                return ServiceResponse<AssetResponseDto>.SuccessResponse(assetResult, "Asset created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding asset for companyId: {CompanyId}", companyId);
                return ServiceResponse<AssetResponseDto>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}