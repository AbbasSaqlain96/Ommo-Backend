using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class DriverDocumentService : IDriverDocumentService
    {
        private readonly IDriverDocumentRepository _driverDocumentRepository;
        private readonly ILogger<DriverDocumentService> _logger;
        public DriverDocumentService(IDriverDocumentRepository driverDocumentRepository, ILogger<DriverDocumentService> logger)
        {
            _driverDocumentRepository = driverDocumentRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<List<DriverDocumentDto>>> GetDriverDocumentsAsync(int driverId, int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching driver documents for DriverId: {DriverId}, CompanyId: {CompanyId}", driverId, companyId);

                // Ensure the driver belongs to the same company as the authenticated user
                var driverBelongsToCompany = await _driverDocumentRepository.CheckDriverCompany(driverId, companyId);
                if (!driverBelongsToCompany)
                {
                    _logger.LogWarning("DriverId: {DriverId} does not belong to CompanyId: {CompanyId}", driverId, companyId);
                    return ServiceResponse<List<DriverDocumentDto>>.ErrorResponse("No driver found for the provided Driver ID", 400);
                }

                // Fetch driver documents
                var documents = await _driverDocumentRepository.GetDriverDocumentsAsync(driverId);

                _logger.LogInformation("Successfully retrieved {Count} documents for DriverId: {DriverId}", documents?.Count ?? 0, driverId);
                return ServiceResponse<List<DriverDocumentDto>>.SuccessResponse(documents ?? new List<DriverDocumentDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for DriverId: {DriverId}, CompanyId: {CompanyId}", driverId, companyId);
                return ServiceResponse<List<DriverDocumentDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}
