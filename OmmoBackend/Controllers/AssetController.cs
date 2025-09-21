using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Services.Implementations;
using OmmoBackend.Services.Interfaces;
using System.ComponentModel.Design;
using System.Reflection.Metadata;
using Twilio.Http;
using Twilio.TwiML.Voice;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/asset")]
    public class AssetController : Controller
    {
        private readonly IAssetService _assetService;
        private readonly ILogger<AssetController> _logger;

        /// <summary>
        /// Initializes a new instance of the AssetController class with the specified asset service.
        /// </summary>
        public AssetController(IAssetService assetService, ILogger<AssetController> logger)
        {
            _assetService = assetService;
            _logger = logger;
        }

        [HttpGet]
        [Route("get-assets")]
        [Authorize]
        public async Task<IActionResult> GetAssets()
        {
            _logger.LogInformation("Fetching assets for the authorized company.");

            // Extract CompanyId from token
            int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");
            if (companyId <= 0)
            {
                _logger.LogWarning("Invalid Company ID in token.");
                return ApiResponse.Error("Invalid Company ID.", 400);
            }

            try
            {
                // Call the service layer to fetch units with optional filtering
                var result = await _assetService.GetAssetsAsync(companyId);

                if (!result.Success)
                {
                    _logger.LogWarning("Error retrieving assets for CompanyId: {CompanyId}. Message: {ErrorMessage}", companyId, result.ErrorMessage);
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                _logger.LogInformation("Successfully retrieved assets for CompanyId: {CompanyId}.", companyId);
                return ApiResponse.Success(result.Data, string.IsNullOrEmpty(result.Message) ? "Assets retrieved successfully." : result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching asset details.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpGet]
        [Route("get-asset-details")]
        [Authorize]
        public async Task<IActionResult> GetAssetDetails([FromQuery] int vehicleId)
        {
            _logger.LogInformation("Fetching asset details for VehicleId: {VehicleId}", vehicleId);

            try
            {
                // Validate Vehicle ID
                if (vehicleId <= 0)
                {
                    _logger.LogWarning("Invalid VehicleId: {VehicleId} provided.", vehicleId);
                    return ApiResponse.Error("Invalid Vehicle ID.", 400);
                }

                // Extract CompanyId from token
                int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");
                if (companyId <= 0)
                {
                    _logger.LogWarning("Invalid Company ID in token.");
                    return ApiResponse.Error("Invalid Company ID.", 400);
                }

                var result = await _assetService.GetAssetDetailsAsync(vehicleId, companyId);

                if (!result.Success)
                {
                    _logger.LogWarning("Asset retrieval failed for VehicleId: {VehicleId}, CompanyId: {CompanyId}. Reason: {Reason}",
                        vehicleId, companyId, result.ErrorMessage);

                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                _logger.LogInformation("Successfully retrieved asset details for VehicleId: {VehicleId}.", vehicleId);
                return ApiResponse.Success(result.Data, "Asset details fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while retrieving asset details for VehicleId: {VehicleId}.", vehicleId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpGet]
        [Route("shop-history")]
        [Authorize]
        public async Task<IActionResult> GetShopHistory(int vehicleId)
        {
            _logger.LogInformation("Fetching shop history for VehicleId: {VehicleId}", vehicleId);

            try
            {
                // Validate Vehicle ID
                if (vehicleId <= 0)
                {
                    _logger.LogWarning("Invalid VehicleId: {VehicleId} provided.", vehicleId);
                    return ApiResponse.Error("Invalid Vehicle ID.", 400);
                }

                // Extract CompanyId from token
                int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");
                if (companyId <= 0)
                {
                    _logger.LogWarning("Invalid Company ID in token.");
                    return ApiResponse.Error("Invalid Company ID.", 400);
                }

                var result = await _assetService.GetShopHistoryAsync(vehicleId, companyId);
                if (!result.Success)
                {
                    _logger.LogWarning("Error while fetching shop history: {ErrorMessage}", result.ErrorMessage);
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                _logger.LogInformation("Successfully retrieved shop history for VehicleId: {VehicleId}.", vehicleId);
                return ApiResponse.Success(result.Data, "Shop history fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching shop history for VehicleId: {VehicleId}.", vehicleId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("add-asset")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddAsset(
            [FromForm] AddAssetRequest request,
            IFormFile? title,
            string? titleState,
            IFormFile? Registration,
            string? RegistrationState,
            IFormFile? Insurance,
            string? InsuranceState,
            IFormFile? IFTA,
            string? IFTAState,
            IFormFile? Permit,
            string? PermitState,
            IFormFile? UCR,
            string? UCRState,
            IFormFile? leaseAgreement,
            string? leaseAgreementState)
        {
            _logger.LogInformation("Processing AddAsset request for PlateNumber: {PlateNumber}", request.PlateNumber);

            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => ms.Value.Errors.First().ErrorMessage)
                    .FirstOrDefault();

                return ApiResponse.Error(firstError, 400);
            }

            // Extract CompanyId from token
            int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");
            if (companyId <= 0)
            {
                _logger.LogWarning("Invalid Company ID found in token.");
                return ApiResponse.Error("Invalid Company ID.", 400);
            }

            try
            {
                // Map asset details from the form to the DTO
                var addAssetRequestDto = new AddAssetRequestDto
                {
                    PlateNumber = request.PlateNumber,
                    PlateState = request.PlateState,
                    VinNumber = request.VinNumber,
                    VehicleType = request.VehicleType.ToString(),
                    Year = request.Year,
                    Trademark = request.Trademark,
                    IsHeadrake = request.IsHeadrake,
                    HaveFlatbed = request.HaveFlatbed,
                    HaveLoadbar = request.HaveLoadbar,
                    HaveVanStraps = request.HaveVanStraps,
                    Weight = request.Weight,
                    AxleSpacing = request.AxleSpacing,
                    NumOfAxles = request.NumOfAxles,
                    Brand = request.Brand,
                    Model = request.Model,
                    Color = request.Color,
                    FuelType = request.FuelType,
                    TrailerType = request.TrailerType
                };

                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpeg", ".jpg" };
                var documents = new List<DocumentUploadDto>();

                var documentInputs = new[]
                {
                    (title, titleState, 17),
                    (Registration, RegistrationState, 18),
                    (Insurance, InsuranceState, 19),
                    (IFTA, IFTAState, 20),
                    (Permit, PermitState, 21),
                    (UCR, UCRState, 22),
                    (leaseAgreement, leaseAgreementState, 23),
                };

                foreach (var (file, state, docTypeId) in documentInputs)
                {
                    if (file != null)
                    {
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            return ApiResponse.Error("Invalid document format for Asset Documents. Only PDF, DOC, DOCX, and JPEG formats are allowed.", 400);
                        }

                        documents.Add(new DocumentUploadDto
                        {
                            File = file,
                            State = state,
                            DocTypeId = docTypeId
                        });
                    }
                }

                addAssetRequestDto.Documents = documents;

                //// Create a list to hold documents with document names
                //var documentList = new List<DocumentUploadDto>();

                //// Add each document to the list with its respective state
                //if (title != null)
                //    documentList.Add(new DocumentUploadDto { File = title, State = titleState, DocTypeId = 17 });
                //if (Registration != null)
                //    documentList.Add(new DocumentUploadDto { File = Registration, State = RegistrationState, DocTypeId = 18 });
                //if (Insurance != null)
                //    documentList.Add(new DocumentUploadDto { File = Insurance, State = InsuranceState, DocTypeId = 19 });
                //if (IFTA != null)
                //    documentList.Add(new DocumentUploadDto { File = IFTA, State = IFTAState, DocTypeId = 20 });
                //if (Permit != null)
                //    documentList.Add(new DocumentUploadDto { File = Permit, State = PermitState, DocTypeId = 21 });
                //if (UCR != null)
                //    documentList.Add(new DocumentUploadDto { File = UCR, State = UCRState, DocTypeId = 22 });
                //if (leaseAgreement != null)
                //    documentList.Add(new DocumentUploadDto { File = leaseAgreement, State = leaseAgreementState, DocTypeId = 23 });

                //// Attach documents to the asset request
                //addAssetRequestDto.Documents = documentList;

                var result = await _assetService.AddAssetAsync(addAssetRequestDto, companyId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to add asset for CompanyId: {CompanyId}. Error: {ErrorMessage}", companyId, result.ErrorMessage);
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                _logger.LogInformation("Successfully added asset for PlateNumber: {PlateNumber}, CompanyId: {CompanyId}.", request.PlateNumber, companyId);
                return ApiResponse.Success(result.Data, "Asset created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AddAsset for CompanyId: {CompanyId}", companyId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}