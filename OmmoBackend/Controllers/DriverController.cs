using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Middlewares;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/driver")]
    public class DriverController : ControllerBase
    {
        private readonly IDriverService _driverService;
        private readonly IDriverDocumentService _driverDocumentService;
        private readonly IDriverPerformanceService _driverPerformanceService;
        private readonly ILogger<DriverController> _logger;

        /// <summary>
        /// Initializes a new instance of the DriverController class with the specified driver service.
        /// </summary>
        public DriverController(IDriverService driverService, IDriverDocumentService driverDocumentService, IDriverPerformanceService driverPerformanceService, ILogger<DriverController> logger)
        {
            _driverService = driverService;
            _driverDocumentService = driverDocumentService;
            _driverPerformanceService = driverPerformanceService;
            _logger = logger;
        }

        [HttpGet("get-driver-info/{unitId}")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> GetDriverInfo(int unitId)
        {
            if (unitId <= 0)
            {
                _logger.LogWarning("Invalid Unit ID: {UnitId} provided by user {UserId}", unitId, User.Identity?.Name);
                return ApiResponse.Error("Invalid Unit ID provided", 400);
            }

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                // Call the service method to get a driver info
                var response = await _driverService.GetDriverInfoAsync(unitId, companyId);

                if (!response.Success)
                {
                    _logger.LogWarning("Failed to get driver info for Unit ID: {UnitId}, Company ID: {CompanyId} | Reason: {Reason}", unitId, companyId, response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Successfully fetched driver info for Unit ID: {UnitId}", unitId);
                return ApiResponse.Success(response.Data, "Driver info fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service temporarily unavailable for Unit ID: {UnitId}", unitId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpGet("get-driver-list")]
        [Authorize]
        public async Task<IActionResult> GetDriverList()
        {
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                // Fetch the driver list
                var response = await _driverService.GetDriverListAsync(companyId);

                if (!response.Success)
                {
                    _logger.LogWarning("Failed to fetch driver list for Company ID: {CompanyId}", companyId);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Successfully fetched driver list for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(response.Data, "Driver list fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the driver list.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpGet]
        [Route("detail")]
        [Authorize]
        public async Task<IActionResult> GetDriverDetail(int driverId)
        {
            if (driverId <= 0)
            {
                _logger.LogWarning("Invalid Driver ID provided: {DriverId} by user {UserId}", driverId, User.Identity?.Name);
                return ApiResponse.Error("No driver found for the provided Driver ID", 400);
            }

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                // Fetch the driver details
                var response = await _driverService.GetDriverDetailAsync(driverId, companyId);

                if (!response.Success)
                {
                    _logger.LogWarning("Failed to get driver details for Driver ID: {DriverId}, Company ID: {CompanyId}", driverId, companyId);
                    return ApiResponse.Error("No driver found for the provided Driver ID", 400);
                }

                _logger.LogInformation("Successfully fetched driver details for Driver ID: {DriverId}, Company ID: {CompanyId}", driverId, companyId);
                return ApiResponse.Success(response.Data, "Driver detail fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching driver details for Driver ID: {DriverId}", driverId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpGet]
        [Route("documents")]
        [Authorize]
        public async Task<IActionResult> GetDriverDocuments(int driverId)
        {
            if (driverId <= 0)
            {
                _logger.LogWarning("Invalid Driver ID provided: {DriverId} by user {UserId}", driverId, User.Identity?.Name);
                return ApiResponse.Error("No driver found for the provided Driver ID", 400);
            }

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                // Fetch driver documents
                var response = await _driverDocumentService.GetDriverDocumentsAsync(driverId, companyId);

                if (!response.Success)
                {
                    _logger.LogWarning("Failed to fetch driver documents for Driver ID: {DriverId}, Company ID: {CompanyId}", driverId, companyId);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Fetched {Count} documents for Driver ID: {DriverId}, Company ID: {CompanyId}", response.Data?.Count ?? 0, driverId, companyId);
                return ApiResponse.Success(new { driver = response.Data ?? new List<DriverDocumentDto>() },
                                  response.Data == null || !response.Data.Any()
                                  ? "No Documents found for the provided Driver"
                                  : "Driver documents fetched successfully.");
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Unauthorized access attempt for Driver ID: {DriverId} by user {UserId}", driverId, User.Identity?.Name);
                return ApiResponse.Error("You do not have permission to access this resource", 401);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error fetching driver documents for Driver ID: {DriverId}", driverId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpGet("performance")]
        [Authorize]
        public async Task<IActionResult> GetDriverPerformance(int driverId)
        {
            _logger.LogInformation("Fetching performance for Driver ID: {DriverId}", driverId);

            if (driverId <= 0)
            {
                _logger.LogWarning("Invalid Driver ID: {DriverId}", driverId);
                return ApiResponse.Error("No driver found for the provided Driver ID", 400);
            }

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                var response = await _driverPerformanceService.GetDriverPerformanceAsync(driverId, companyId);

                if (!response.Success)
                {
                    _logger.LogWarning("Failed to fetch driver performance: {ErrorMessage}", response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Successfully fetched driver performance for Driver ID: {DriverId}", driverId);
                return ApiResponse.Success(new { Driver_Performance = response.Data }, "Driver performance retrieved successfully.");
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Unauthorized access to driver performance by user {User}", User.Identity?.Name);
                return ApiResponse.Error("You do not have permission to access this resource.", 401);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while fetching driver performance for Driver ID: {DriverId}", driverId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("hire-driver")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [NotificationMetadata("Safety", "Driver", 2)]
        public async Task<IActionResult> HireDriver(
            [FromForm] HireDriverRequest driverDetails,
            IFormFile? DocsCDL,
            IFormFile? MedicalCard,
            IFormFile? SocialSecurity,
            IFormFile? Application,
            IFormFile? Contract,
            IFormFile? DrugTest,
            IFormFile? MVRAndPSP,
            IFormFile? W4,
            IFormFile? I9,
            IFormFile? TerminationFile,
            IFormFile? VOE,
            IFormFile? DriverLegalPlan,
            IFormFile? BankInfo,
            IFormFile? OCCACCApplication,
            IFormFile? FuelCard)
        {
            _logger.LogInformation("Attempting to hire driver: {DriverName} {DriverLastName}", driverDetails.DriverName, driverDetails.DriverLastName);

            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => ms.Value.Errors.First().ErrorMessage)
                    .FirstOrDefault();

                return ApiResponse.Error(firstError, 400);
            }

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                // Map driver details from the form to the DTO
                HireDriverRequestDto driverDetailsDto = new HireDriverRequestDto
                {
                    DriverName = driverDetails.DriverName,
                    DriverLastName = driverDetails.DriverLastName,
                    EmploymentType = driverDetails.EmploymentType,
                    CDLLicenseNumber = driverDetails.CDLLicenseNumber,
                    Address = driverDetails.Address,
                    Status = DriverStatus.active.ToString(),
                    HiringStatus = HiringStatus.approved.ToString(),
                    LicenseState = driverDetails.LicenseState,
                    Email = driverDetails.Email,
                    PhoneNumber = driverDetails.PhoneNumber,
                    Rating = driverDetails.Rating
                };

                // Create a list to hold documents with document names
                var documentList = new List<DocumentDto>();

                // Add each document to the list with its respective type and file
                if (DocsCDL != null)
                    documentList.Add(new DocumentDto { DocTypeId = 1, Document = DocsCDL, DocTypeName = "Docs - CDL" });
                if (MedicalCard != null)
                    documentList.Add(new DocumentDto { DocTypeId = 2, Document = MedicalCard, DocTypeName = "Medical Card" });
                if (SocialSecurity != null)
                    documentList.Add(new DocumentDto { DocTypeId = 3, Document = SocialSecurity, DocTypeName = "Social Security" });
                if (Application != null)
                    documentList.Add(new DocumentDto { DocTypeId = 4, Document = Application, DocTypeName = "Application" });
                if (Contract != null)
                    documentList.Add(new DocumentDto { DocTypeId = 5, Document = Contract, DocTypeName = "Contract" });
                if (DrugTest != null)
                    documentList.Add(new DocumentDto { DocTypeId = 6, Document = DrugTest, DocTypeName = "Drug Test" });
                if (MVRAndPSP != null)
                    documentList.Add(new DocumentDto { DocTypeId = 7, Document = MVRAndPSP, DocTypeName = "MVR & PSP" });
                if (W4 != null)
                    documentList.Add(new DocumentDto { DocTypeId = 8, Document = W4, DocTypeName = "W4" });
                if (I9 != null)
                    documentList.Add(new DocumentDto { DocTypeId = 9, Document = I9, DocTypeName = "I9" });
                if (TerminationFile != null)
                    documentList.Add(new DocumentDto { DocTypeId = 10, Document = TerminationFile, DocTypeName = "Termination File" });
                if (VOE != null)
                    documentList.Add(new DocumentDto { DocTypeId = 12, Document = VOE, DocTypeName = "VOE" });
                if (DriverLegalPlan != null)
                    documentList.Add(new DocumentDto { DocTypeId = 13, Document = DriverLegalPlan, DocTypeName = "Driver Legal Plan" });
                if (BankInfo != null)
                    documentList.Add(new DocumentDto { DocTypeId = 14, Document = BankInfo, DocTypeName = "Bank Info" });
                if (OCCACCApplication != null)
                    documentList.Add(new DocumentDto { DocTypeId = 15, Document = OCCACCApplication, DocTypeName = "OCC ACC Application" });
                if (FuelCard != null)
                    documentList.Add(new DocumentDto { DocTypeId = 16, Document = FuelCard, DocTypeName = "Fuel Card" });

                _logger.LogInformation("Uploaded document");

                // Attach documents to the driver request
                driverDetailsDto.Documents = documentList;

                // Call the service layer to save driver data and documents
                var hireDriverResponse = await _driverService.HireDriverAsync(companyId, driverDetailsDto);

                // Check if the service response was successful
                if (!hireDriverResponse.Success)
                {
                    _logger.LogWarning("Failed to hire driver: {ErrorMessage}", hireDriverResponse.ErrorMessage);
                    return ApiResponse.Error(hireDriverResponse.ErrorMessage, hireDriverResponse.StatusCode);
                }

                _logger.LogInformation("Driver hired successfully: {DriverId}", hireDriverResponse.Data.DriverId);
                return ApiResponse.Success(new { driverId = hireDriverResponse.Data.DriverId }, "Driver hired successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled server error while hiring driver.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}