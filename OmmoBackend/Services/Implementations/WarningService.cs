using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Implementations;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using System.Linq;

namespace OmmoBackend.Services.Implementations
{
    public class WarningService : IWarningService
    {
        private readonly ITruckRepository _truckRepository;
        private readonly IDriverRepository _driverRepository;
        private readonly ITrailerRepository _trailerRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ICarrierRepository _carrierRepository;
        private readonly IViolationRepository _violationRepository;
        private readonly IWarningRepository _warningRepository;
        private readonly IPerformanceRepository _performanceRepository;
        private readonly IWarningDocRepository _warningDocRepository;
        private readonly AppDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<WarningService> _logger;
        public WarningService(ITruckRepository truckRepository,
            IDriverRepository driverRepository,
            ICarrierRepository carrierRepository,
            IVehicleRepository vehicleRepository,
            ITrailerRepository trailerRepository,
            IViolationRepository violationRepository,
            IWarningRepository warningRepository,
            IPerformanceRepository performanceRepository,
            IWarningDocRepository warningDocRepository,
            AppDbContext dbContext,
            IUnitOfWork unitOfWork,
            ILogger<WarningService> logger)
        {
            _truckRepository = truckRepository;
            _driverRepository = driverRepository;
            _carrierRepository = carrierRepository;
            _vehicleRepository = vehicleRepository;
            _trailerRepository = trailerRepository;
            _violationRepository = violationRepository;
            _warningRepository = warningRepository;
            _performanceRepository = performanceRepository;
            _warningDocRepository = warningDocRepository;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ServiceResponse<WarningCreationResult>> CreateWarningAsync(int companyId, CreateWarningRequest request)
        {
            try
            {
                _logger.LogInformation("Creating warning for Company ID: {CompanyId}", companyId);

                // Event Date Validation
                if (request.WarningEventInfoDto.EventDate > DateTime.UtcNow.Date)
                    return ServiceResponse<WarningCreationResult>.ErrorResponse("Event date cannot be in the future.", 400);

                // Document Null Check
                if (request.WarningDocumentsDto == null)
                    return ServiceResponse<WarningCreationResult>.ErrorResponse("Document is required.", 400);

                // Document Format Validation
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

                // Null check for file
                if (request.WarningDocumentsDto.DocPath != null)
                {
                    var fileName = request.WarningDocumentsDto.DocPath.FileName?.Trim();

                    // Only validate format if fileName is not null or empty
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        var extension = Path.GetExtension(fileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(extension))
                        {
                            return ServiceResponse<WarningCreationResult>.ErrorResponse("Invalid document format. Only PDF, DOC, and DOCX formats are allowed", 400);
                        }
                    }
                }

                // Truck Validation
                var truck = await _truckRepository.GetByIdAsync(request.WarningEventInfoDto.TruckId);
                if (truck == null)
                    return ServiceResponse<WarningCreationResult>.ErrorResponse("No Truck found for the provided Truck ID", 400);

                var vehicle = await _vehicleRepository.GetByIdAsync(truck.vehicle_id);
                if (vehicle == null || !vehicle.carrier_id.HasValue)
                    return ServiceResponse<WarningCreationResult>.ErrorResponse("Truck belongs to another company.", 400);

                var carrier = await _carrierRepository.GetByIdAsync(vehicle.carrier_id.Value);
                if (carrier == null || carrier.company_id != companyId)
                    return ServiceResponse<WarningCreationResult>.ErrorResponse("Truck belongs to another company.", 400);

                // Driver Validation
                var driver = await _driverRepository.GetByIdAsync(request.WarningEventInfoDto.DriverId);
                if (driver == null || driver.company_id != companyId)
                    return ServiceResponse<WarningCreationResult>.ErrorResponse("No driver found for the provided Driver ID", 400);

                // Validate Trailer if provided
                if (request.WarningEventInfoDto.TrailerId.HasValue)
                {
                    var trailer = await _trailerRepository.GetByIdAsync(request.WarningEventInfoDto.TrailerId.Value);
                    if (trailer == null)
                    {
                        _logger.LogWarning("Trailer {TrailerId} not found", request.WarningEventInfoDto.TrailerId.Value);
                        return ServiceResponse<WarningCreationResult>.ErrorResponse("No trailer found for the provided Trailer ID", 400);
                    }

                    var trailerVehicle = await _vehicleRepository.GetByIdAsync(trailer.vehicle_id);
                    if (trailerVehicle == null || !trailerVehicle.carrier_id.HasValue)
                    {
                        _logger.LogWarning("Invalid vehicle or carrier not found for Trailer {TrailerId}", request.WarningEventInfoDto.TrailerId.Value);
                        return ServiceResponse<WarningCreationResult>.ErrorResponse("Trailer belongs to another company.", 400);
                    }

                    var trailerCarrier = await _carrierRepository.GetByIdAsync(trailerVehicle.carrier_id.Value);
                    if (trailerCarrier == null || trailerCarrier.company_id != companyId)
                    {
                        _logger.LogWarning("Carrier mismatch for trailer CompanyId: {CompanyId}", companyId);
                        return ServiceResponse<WarningCreationResult>.ErrorResponse("Trailer belongs to another company.", 400);
                    }
                }

                // Violations Validation
                var violationIds = request.Violations ?? new List<int>();
                var existingViolations = await _violationRepository.GetByIdsAsync(violationIds);
                if (existingViolations.Count != violationIds.Count)
                {
                    return ServiceResponse<WarningCreationResult>.ErrorResponse("Selected Violations does not exist", 400);
                }

                if (!Enum.TryParse(request.WarningEventInfoDto.Authority, true, out EventAuthority eventAuthority))
                    throw new ArgumentException($"Invalid event authority: {request.WarningEventInfoDto.Authority}");

                if (!Enum.TryParse(request.WarningEventInfoDto.FeesPaidBy, true, out FeesPaidBy feesPaidBy))
                    throw new ArgumentException($"Invalid fees paid by: {request.WarningEventInfoDto.FeesPaidBy}");

                var companyFeeApplied = request.WarningEventInfoDto.CompanyFeeApplied?.ToLower() == "yes";

                // Prepare Performance Event Data
                var performanceEvents = new PerformanceEvents
                {
                    truck_id = request.WarningEventInfoDto.TruckId,
                    driver_id = request.WarningEventInfoDto.DriverId,
                    trailer_id = request.WarningEventInfoDto.TrailerId ?? 0,
                    location = request.WarningEventInfoDto.Location,
                    event_type = EventType.warning,
                    authority = eventAuthority,
                    date = request.WarningEventInfoDto.EventDate,
                    description = request.WarningEventInfoDto.Description,
                    load_id = request.WarningEventInfoDto.LoadId ?? 0,
                    event_fees = request.WarningEventInfoDto.EventFee,
                    fees_paid_by = feesPaidBy,
                    company_fee_applied = companyFeeApplied,
                    company_fee_amount = request.WarningEventInfoDto.CompanyFeeAmount,
                    company_fee_statement_date = request.WarningEventInfoDto.CompanyFeeStatementDate,
                    company_id = companyId
                };

                // Create Warning & Related Data in a Transaction
                bool isCreated = await _warningRepository.CreateWarningWithTransactionAsync(performanceEvents, request.WarningDocumentsDto, request.Violations!, companyId);
                if (!isCreated)
                {
                    _logger.LogError("Failed to create warning for Company ID: {CompanyId}", companyId);
                    return ServiceResponse<WarningCreationResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
                }

                _logger.LogInformation("Dot inspection created successfully for Company ID: {CompanyId}", companyId);
                return ServiceResponse<WarningCreationResult>.SuccessResponse(null, "Warning created successfully.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating a warning.");
                return ServiceResponse<WarningCreationResult>.ErrorResponse(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CreateWarningAsync.");
                return ServiceResponse<WarningCreationResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    
        public async Task<ServiceResponse<WarningDetailsDto>> GetWarningDetailsAsync(int eventId, int companyId)
        {
            _logger.LogInformation("Fetching warning details for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);

            try
            {
                // Ensure the event belongs to the same company
                var eventBelongsToCompany = await _warningRepository.CheckEventCompany(eventId, companyId);
                if (!eventBelongsToCompany)
                {
                    _logger.LogWarning("Event {EventId} does not belong to Company {CompanyId}", eventId, companyId);
                    return ServiceResponse<WarningDetailsDto>.ErrorResponse("No warning found for the given event ID.", 400);
                }

                // Fetch warning details
                var warningDetails = await _warningRepository.FetchWarningDetailsAsync(eventId, companyId);
                if (warningDetails == null)
                {
                    _logger.LogWarning("No warning details found for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);
                    return ServiceResponse<WarningDetailsDto>.ErrorResponse("No warning info found for the given event ID.", 400);
                }

                _logger.LogInformation("Warning details retrieved successfully for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);
                return ServiceResponse<WarningDetailsDto>.SuccessResponse(warningDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching warning details for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);
                return ServiceResponse<WarningDetailsDto>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<WarningUpdateResult>> UpdateWarningAsync(int companyId, UpdateWarningRequest request)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    var eventEntity = await _performanceRepository.GetByIdAsync(request.EventId);
                    if (eventEntity == null || eventEntity.company_id != companyId || eventEntity.event_type != EventType.warning)
                    {
                        return ServiceResponse<WarningUpdateResult>.ErrorResponse("No warning found for the provided Event ID.", 400);
                    }

                    // Document Format Validation
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

                    // Null check for file
                    if (request.WarningDoc != null)
                    {
                        var fileName = request.WarningDoc.FileName?.Trim();

                        // Only validate format if fileName is not null or empty
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            var extension = Path.GetExtension(fileName).ToLowerInvariant();

                            if (!allowedExtensions.Contains(extension))
                            {
                                return ServiceResponse<WarningUpdateResult>.ErrorResponse("Invalid document format. Only PDF, DOC, and DOCX formats are allowed", 400);
                            }
                        }
                    }

                    // Truck
                    if (request.TruckId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateTruckOwnershipAsync(_truckRepository, request.TruckId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<WarningUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var truck = (Truck)entity!;
                        eventEntity.truck_id = truck.truck_id;
                    }

                    // Driver
                    if (request.DriverId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateDriverOwnershipAsync(_driverRepository, request.DriverId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<WarningUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var driver = (Driver)entity!;
                        eventEntity.driver_id = driver.driver_id;
                    }

                    // Trailer
                    if (request.TrailerId.HasValue)
                    {
                        var (isValid, errorMessage, entity) = await OwnershipValidator.ValidateTrailerOwnershipAsync(_trailerRepository, request.TrailerId.Value, companyId);
                        if (!isValid)
                            return ServiceResponse<WarningUpdateResult>.ErrorResponse(errorMessage, errorMessage!.Contains("does not belong") ? 403 : 400);

                        var trailer = (Trailer)entity!;
                        eventEntity.trailer_id = trailer.trailer_id;
                    }

                    if (!string.IsNullOrEmpty(request.Location))
                        eventEntity.location = request.Location;

                    if (!string.IsNullOrEmpty(request.Authority))
                    {
                        if (!Enum.TryParse(request.Authority, true, out EventAuthority eventAuthority))
                            throw new ArgumentException($"Invalid event authority: {request.Authority}");

                        eventEntity.authority = eventAuthority;
                    }

                    // Event Date Validation
                    if (request.EventDate.HasValue)
                    {
                        if (request.EventDate.Value > DateTime.UtcNow)
                        {
                            return ServiceResponse<WarningUpdateResult>.ErrorResponse("Event date cannot be in the future.", 400);
                        }

                        eventEntity.date = request.EventDate.Value;
                    }

                    if (!string.IsNullOrEmpty(request.Description))
                        eventEntity.description = request.Description;

                    if (request.LoadId.HasValue)
                        eventEntity.load_id = request.LoadId.Value;

                    if (request.EventFee.HasValue)
                        eventEntity.event_fees = request.EventFee.Value;

                    if (!string.IsNullOrEmpty(request.FeesPaidBy))
                    {
                        if (!Enum.TryParse(request.FeesPaidBy, true, out FeesPaidBy feesPaidBy))
                            throw new ArgumentException($"Invalid fees paid by: {request.FeesPaidBy}");

                        eventEntity.fees_paid_by = feesPaidBy;
                    }

                    var companyFeeApplied = request.CompanyFeeApplied?.ToLower() == "yes";

                    if (!string.IsNullOrEmpty(request.CompanyFeeApplied))
                        eventEntity.company_fee_applied = companyFeeApplied;

                    if (request.CompanyFeeAmount.HasValue)
                        eventEntity.company_fee_amount = request.CompanyFeeAmount.Value;

                    if (request.CompanyFeeStatementDate.HasValue)
                        eventEntity.company_fee_statement_date = request.CompanyFeeStatementDate;

                    await _performanceRepository.UpdateAsync(eventEntity);

                    var warning = await _warningRepository.GetWarningByEventId(request.EventId);

                    int warningId = 0;
                    if (warning != null)
                    {
                        warningId = warning.warning_id;
                    }

                    // Handle Warning Document
                    if (request.WarningDoc != null)
                    {
                        bool warningDocExist = await _warningRepository.WarningDocumentExist(warningId);
                        if (request.DocNumber == null)
                            return ServiceResponse<WarningUpdateResult>.ErrorResponse("Warning Doc Number required", 400);

                        await _warningDocRepository.UpdateWarningDocsAsync(companyId, eventEntity.event_id, warningId, request.WarningDoc, request.DocNumber);
                    }

                    // Handle Violations
                    if (request.Violations != null && request.Violations.Any())
                    {
                        await _violationRepository.UpdateWarningViolationsAsync(eventEntity.event_id, request.Violations);
                    }

                    await transaction.CommitAsync();

                    return ServiceResponse<WarningUpdateResult>.SuccessResponse(null, "Warning updated successfully.");
                }
                catch (ArgumentException ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogWarning(ex, "Validation error while creating a warning.");
                    return ServiceResponse<WarningUpdateResult>.ErrorResponse(ex.Message, 400);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogError(ex, "Error updating warning");
                    return ServiceResponse<WarningUpdateResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
                }
            });
        }
    }
}
