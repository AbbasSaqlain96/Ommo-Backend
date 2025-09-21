using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class DriverService : IDriverService
    {
        private readonly IDriverRepository _driverRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DriverService> _logger;

        public DriverService(IDriverRepository driverRepository, IWebHostEnvironment environment,
            IConfiguration configuration, ILogger<DriverService> logger)
        {
            _driverRepository = driverRepository;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ServiceResponse<DriverDto>> GetDriverInfoAsync(int unitId, int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching driver info for UnitId: {UnitId}, CompanyId: {CompanyId}", unitId, companyId);

                // Fetch driver info using unit Id
                var driver = await _driverRepository.GetDriverInfoByUnitIdAsync(unitId);

                if (driver == null)
                {
                    _logger.LogWarning("No unit found for the provided Unit ID: {UnitId}", unitId);
                    return ServiceResponse<DriverDto>.ErrorResponse("No unit found for the provided Unit ID", 404);
                }

                // Ensure driver belongs to the requested company
                if (driver.company_id != companyId)
                {
                    _logger.LogWarning("Access denied. Unit ID: {UnitId} does not belong to Company ID: {CompanyId}", unitId, companyId);
                    return ServiceResponse<DriverDto>.ErrorResponse("Access denied. This unit does not belong to your company.", 400);
                }

                _logger.LogInformation("Successfully retrieved driver info for DriverId: {DriverId}", driver.driver_id);

                // Map Driver to DriverDto
                var driverDto = new DriverDto
                {
                    DriverId = driver.driver_id,
                    DriverName = driver.driver_name,
                    LastName = driver.last_name,
                    EmploymentType = driver.employment_type.ToString(),
                    Status = driver.status.ToString(),
                    HiringStatus = driver.hiring_status.ToString(),
                    CDLLicenseNumber = driver.cdl_license_number,
                    Address = driver.address,
                    LicenseState = driver.license_state.ToString(),
                    Email = driver.email,
                    PhoneNumber = driver.phone_number,
                    Rating = driver.rating,
                    CompanyId = driver.company_id
                };

                return ServiceResponse<DriverDto>.SuccessResponse(driverDto, "Driver info retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching driver info for UnitId: {UnitId}, CompanyId: {CompanyId}", unitId, companyId);
                return ServiceResponse<DriverDto>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<IEnumerable<DriverListDto>>> GetDriverListAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching driver list for CompanyId: {CompanyId}", companyId);

                var drivers = await _driverRepository.GetDriverListByCompanyIdAsync(companyId);

                if (drivers == null || !drivers.Any())
                {
                    _logger.LogWarning("No drivers found for CompanyId: {CompanyId}", companyId);
                    return ServiceResponse<IEnumerable<DriverListDto>>.SuccessResponse(new List<DriverListDto>(), "No drivers found for the provided company.");
                }

                _logger.LogInformation("Successfully retrieved driver list for CompanyId: {CompanyId}", companyId);
                return ServiceResponse<IEnumerable<DriverListDto>>.SuccessResponse(drivers, "Driver list fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching driver list for CompanyId: {CompanyId}", companyId);
                return ServiceResponse<IEnumerable<DriverListDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<DriverDetailDto>> GetDriverDetailAsync(int driverId, int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching driver detail for DriverId: {DriverId}, CompanyId: {CompanyId}", driverId, companyId);

                var driver = await _driverRepository.GetDriverDetailAsync(driverId);

                if (driver == null || driver.CompanyId != companyId)
                {
                    _logger.LogWarning("Driver not found or belongs to another company. DriverId: {DriverId}, CompanyId: {CompanyId}", driverId, companyId);
                    return ServiceResponse<DriverDetailDto>.ErrorResponse("No driver found for the provided Driver ID", 400);
                }

                _logger.LogInformation("Successfully retrieved driver detail for DriverId: {DriverId}", driverId);
                return ServiceResponse<DriverDetailDto>.SuccessResponse(driver, "Driver detail fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving driver detail for DriverId: {DriverId}, CompanyId: {CompanyId}", driverId, companyId);
                return ServiceResponse<DriverDetailDto>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<HireDriverResponse>> HireDriverAsync(
        int companyId,
        HireDriverRequestDto request)
        {
            _logger.LogInformation("Initiating driver hiring process for CompanyId: {CompanyId}, DriverName: {DriverName}, Email: {Email}", companyId, request.DriverName, request.Email);

            // Rating validation
            if (request.Rating > 5)
                return ServiceResponse<HireDriverResponse>.ErrorResponse("Rating cannot be more than 5.", 400);

            // Email format
            if (!Regex.IsMatch(request.Email ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return ServiceResponse<HireDriverResponse>.ErrorResponse("Invalid email address format.", 400);

            // Phone format
            if (string.IsNullOrWhiteSpace(request.PhoneNumber) || 
                !Regex.IsMatch(request.PhoneNumber, @"^\+?[1-9]\d{0,2}[-.\s]?\(?\d{2,4}\)?[-.\s]?\d{3}[-.\s]?\d{3,4}$"))
            {
                return ServiceResponse<HireDriverResponse>.ErrorResponse("Invalid phone number format.", 400);
            }

            var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);

            // Check for duplicate CDL, Email, and Phone
            var existingDriver = await _driverRepository.GetDriverByCDLOrEmailAsync(
                request.CDLLicenseNumber, request.Email, normalizedPhone);

            if (existingDriver != null)
            {
                if (existingDriver.CDLLicenseNumber == request.CDLLicenseNumber)
                {
                    _logger.LogWarning("Duplicate CDL License detected: {CDLLicenseNumber}", request.CDLLicenseNumber);
                    return ServiceResponse<HireDriverResponse>.ErrorResponse("A driver with this CDL License Number already exists.", 400);
                }

                if (existingDriver.Email == request.Email)
                {
                    _logger.LogWarning("Duplicate Email detected: {Email}", request.Email);
                    return ServiceResponse<HireDriverResponse>.ErrorResponse("A driver with this Email already exists.", 400);
                }

                if (NormalizePhoneNumber(existingDriver.PhoneNumber) == normalizedPhone)
                {
                    _logger.LogWarning("Duplicate PhoneNumber detected: {PhoneNumber}", request.PhoneNumber);
                    return ServiceResponse<HireDriverResponse>.ErrorResponse("A driver with this Phone Number already exists.", 400);
                }
            }

            // Validate document formats
            var allowedExtensions = new[] { ".pdf", ".jpeg", ".jpg", ".png" };
            foreach (var doc in request.Documents)
            {
                if (doc.Document != null)
                {
                    var ext = Path.GetExtension(doc.Document.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext))
                    {
                        return ServiceResponse<HireDriverResponse>.ErrorResponse(
                            $"Invalid document format for '{doc.DocTypeName}'. Only PDF, JPEG, JPG, PNG formats are allowed.", 400);
                    }
                }

            }

            try
            {
                _logger.LogInformation("Saving driver information for CompanyId: {CompanyId}, DriverName: {DriverName}", companyId, request.DriverName);

                // Save driver information
                var driver = await _driverRepository.AddDriverAsync(companyId, new DriverInfoDto
                {
                    DriverName = request.DriverName,
                    DriverLastName = request.DriverLastName,
                    EmploymentType = request.EmploymentType,
                    CDLLicenseNumber = request.CDLLicenseNumber,
                    Address = request.Address,
                    Status = request.Status,
                    HiringStatus = request.HiringStatus,
                    LicenseState = request.LicenseState,
                    Email = request.Email,
                    PhoneNumber = NormalizePhoneNumber(request.PhoneNumber),
                    Rating = request.Rating
                });

                if (driver == null)
                {
                    _logger.LogError("Failed to add driver for CompanyId: {CompanyId}", companyId);
                    return ServiceResponse<HireDriverResponse>.ErrorResponse("Failed to add the driver.", 503);
                }

                _logger.LogInformation("Driver added successfully. DriverId: {DriverId}, CompanyId: {CompanyId}", driver.driver_id, companyId);

                // Save documents
                foreach (var document in request.Documents)
                {
                    if (document.Document != null && document.Document.Length > 0)
                    {
                        try
                        {
                            _logger.LogInformation("Saving document of type {DocTypeId} for DriverId: {DriverId}", document.DocTypeId, driver.driver_id);

                            string filePath = await SaveDocumentAsync(companyId, driver.driver_id, document, request.DriverName);
                            await _driverRepository.AddDriverDocumentAsync(driver.driver_id, document.DocTypeId, filePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing document for DriverId: {DriverId}, DocTypeId: {DocTypeId}", driver.driver_id, document.DocTypeId);
                            return ServiceResponse<HireDriverResponse>.ErrorResponse($"Error processing document: {ex.Message}");
                        }
                    }
                }

                _logger.LogInformation("Driver hiring process completed successfully. DriverId: {DriverId}", driver.driver_id);
                return ServiceResponse<HireDriverResponse>.SuccessResponse(new HireDriverResponse { DriverId = driver.driver_id }, "Driver hired successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while hiring driver.");
                return ServiceResponse<HireDriverResponse>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        private async Task<string> SaveDocumentAsync(int companyId, int driverId, DocumentDto document, string driverName)
        {
            _logger.LogInformation("Starting document save process for DriverId: {DriverId}, CompanyId: {CompanyId}, DocumentTypeId: {DocTypeId}", driverId, companyId, document.DocTypeId);

            // Get the base folder path from AppSettings
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
            {
                _logger.LogError("Server document directory is not configured.");
                throw new InvalidOperationException("Server document directory is not configured.");
            }

            // Construct the complete directory path
            string folderPath = Path.Combine(baseFolderPath, "DriverDoc", companyId.ToString(), driverName, driverId.ToString(), document.DocTypeId.ToString(), document.DocTypeName);

            try
            {
                // Create the directory if it doesn't exist
                if (!Directory.Exists(folderPath))
                {
                    _logger.LogInformation("Creating directory: {FolderPath}", folderPath);
                    Directory.CreateDirectory(folderPath);
                }

                // Generate the file name
                var fileName = $"{companyId}_{driverId}_{document.DocTypeId}.pdf";
                string filePath = Path.Combine(folderPath, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await document.Document.CopyToAsync(stream);
                }

                _logger.LogInformation("Successfully saved document for DriverId: {DriverId} at {FilePath}", driverId, filePath);

                // Get the server URL from configuration
                string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");
                if (string.IsNullOrWhiteSpace(serverUrl))
                {
                    _logger.LogError("Server URL is not configured.");
                    throw new InvalidOperationException("Server URL is not configured.");
                }

                // Construct the public URL dynamically
                string driverDocumentUrl = $"{serverUrl}/Documents/DriverDoc/{companyId}/{driverName}/{driverId}/{document.DocTypeId}/{document.DocTypeName}/{fileName}";

                _logger.LogInformation("Generated document URL: {DocumentUrl}", driverDocumentUrl);

                return driverDocumentUrl;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error writing document to the server for DriverId: {DriverId}, CompanyId: {CompanyId}", driverId, companyId);
                throw new InvalidOperationException("Error writing document to the server: " + ex.Message);
            }
        }

        private static string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "";

            // Keep leading "+" if it exists, and remove all other non-digit characters
            var normalized = Regex.Replace(phone, @"[^\d]", ""); // remove all non-digits
            if (phone.StartsWith("+"))
                return "+" + normalized;

            return normalized;
        }

    }
}